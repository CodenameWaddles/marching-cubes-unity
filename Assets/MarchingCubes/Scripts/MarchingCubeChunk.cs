using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes.Scripts
{
    public class MarchingCubeChunk : MonoBehaviour
    {
        [Header("Marching Cubes values")]
        [SerializeField, Range(4, 48)] public int resolution = 16;
        [SerializeField] public float chunkSize = 32f;
        [SerializeField] public float surfaceLevel = 1f;
        [SerializeField] public Vector3 sampleSpacePosition = Vector3.zero;
        
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _faces = new List<int>();

        private struct Cube
        {
            public float[] Values;
            public Vector3[] Corners;
        }
        
        void Start()
        {
            RebuildMesh();
        }

        public void RebuildMesh()
        {
            _vertices.Clear();
            _faces.Clear();
            
            MarchCubes();
            Mesh mesh = new Mesh();
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _faces.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
        }

        void MarchCubes()
        {
            float step = chunkSize / resolution;
            
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    for (int k = 0; k < resolution; k++)
                    {
                        Cube cube = new Cube
                        {
                            Corners = new Vector3[8],
                            Values = new float[8]
                        };

                        cube.Corners[0] = new Vector3(i, j, k) * step;
                        cube.Corners[1] = new Vector3(i, j, k+1) * step;
                        cube.Corners[2] = new Vector3(i+1, j, k+1) * step;
                        cube.Corners[3] = new Vector3(i+1, j, k) * step;
                        cube.Corners[4] = new Vector3(i, j+1, k) * step;
                        cube.Corners[5] = new Vector3(i, j+1, k+1) * step;
                        cube.Corners[6] = new Vector3(i+1, j+1, k+1) * step;
                        cube.Corners[7] = new Vector3(i+1, j+1, k) * step;

                        for (int x = 0; x < 8; x++)
                        {
                            cube.Values[x] = DensityFunction(sampleSpacePosition + cube.Corners[x]);
                        }
                        
                        ProcessCube(cube);
                    }
                }
            }
        }

        void ProcessCube(Cube cube)
        {
            int cubeIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (cube.Values[i] < surfaceLevel)
                {
                    cubeIndex |= 1 << i;
                }
            }
            
            //Debug.Log("cube index : " + cubeIndex);
            
            //int[] triangulation = new int[16];
            for (int i = 0; MarchingCubesData.TriangleTable[cubeIndex, i] != -1; i+=3)
            {
                //int edgeIndex = MarchingCubesData.TriangleTable[cubeIndex, i];
                //Debug.Log("edgeIndex " + edgeIndex);
                int a0 = MarchingCubesData.cornerIndexAFromEdge[MarchingCubesData.TriangleTable[cubeIndex, i]];
                int a1= MarchingCubesData.cornerIndexBFromEdge[MarchingCubesData.TriangleTable[cubeIndex, i]];
                
                int b0 = MarchingCubesData.cornerIndexAFromEdge[MarchingCubesData.TriangleTable[cubeIndex, i + 1]];
                int b1= MarchingCubesData.cornerIndexBFromEdge[MarchingCubesData.TriangleTable[cubeIndex, i + 1]];
                
                int c0 = MarchingCubesData.cornerIndexAFromEdge[MarchingCubesData.TriangleTable[cubeIndex, i + 2]];
                int c1= MarchingCubesData.cornerIndexBFromEdge[MarchingCubesData.TriangleTable[cubeIndex, i + 2]];

                Vector3 v1 = InterpolateVertices(cube.Corners[a0], cube.Values[a0], cube.Corners[a1], cube.Values[a1]);
                Vector3 v2 = InterpolateVertices(cube.Corners[b0], cube.Values[b0], cube.Corners[b1], cube.Values[b1]);
                Vector3 v3 = InterpolateVertices(cube.Corners[c0], cube.Values[c0], cube.Corners[c1], cube.Values[c1]);
                
                // Vector3 v1 = (cube.Corners[a0] + cube.Corners[a1]) / 2;
                // Vector3 v2 = (cube.Corners[b0] + cube.Corners[b1]) / 2;
                // Vector3 v3 = (cube.Corners[c0] + cube.Corners[c1]) / 2;
                
                int currentIndex = _vertices.Count;
                
                _vertices.Add(v1);
                _vertices.Add(v3);
                _vertices.Add(v2);
                
                //Vector3Int face = new Vector3Int(currentIndex, currentIndex + 1, currentIndex + 2);
                
                _faces.Add(currentIndex);
                _faces.Add(currentIndex + 1);
                _faces.Add(currentIndex + 2);
                //Debug.Log("face added");
            }
        }

        Vector3 InterpolateVertices(Vector3 a, float va, Vector3 b, float vb)
        {
            float t = (surfaceLevel - va) / (vb - va);
            return a + t * (b - a);
        }

        float DensityFunction(Vector3 position) {
            //return ChunkManager.Instance.GetDensity(position);
            
            float density = position.y;
            density += position.x/2;
            // density += ThreeDNoise(position * 4.05f) * 0.25f;
            // density += ThreeDNoise(position * 2.03f) * 0.5f;
            // density += ThreeDNoise(position * 1.02f);
            // density += ThreeDNoise(position * 0.52f) * 2.05f;
            // density += ThreeDNoise(position * 0.245f) * 4.03f;
            // density += ThreeDNoise(position * 0.125f) * 8.05f;
            // density += ThreeDNoise(position * 0.0625f) * 16.03f;
            // density += ThreeDNoise(position * 0.03126f) * 32.06f;
            // density += ThreeDNoise(position * 0.015624f) * 64.04f;
            return density;
        }
        
        float ThreeDNoise(Vector3 position)
        {
            float ab = Mathf.PerlinNoise(position.x, position.y);
            float bc = Mathf.PerlinNoise(position.y, position.z);
            float ca = Mathf.PerlinNoise(position.z, position.x);
            
            float ba = Mathf.PerlinNoise(position.y, position.x);
            float cb = Mathf.PerlinNoise(position.z, position.y);
            float ac = Mathf.PerlinNoise(position.x, position.z);
            
            float abc = ab + bc + ca + ba + cb + ac;
            return abc/6f;
        }
    }
}
