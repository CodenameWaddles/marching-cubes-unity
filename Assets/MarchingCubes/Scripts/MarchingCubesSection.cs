using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCubes.Scripts
{
    public class MarchingCubesSection : MonoBehaviour
    {
        public Vector3Int sectionSize = new Vector3Int(8, 8, 8);
        public float step = 1f;
        public float surfaceLevel = 0f;
        public Vector3 sampleSpacePosition = Vector3.zero;
        public Density.DensityFunction densityFunction = Density.PlaneDensity;
        
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _faces = new List<int>();
        
        private Mesh _mesh;
        
        public List<Vector3> Vertices  => _vertices;
        public List<int> Faces => _faces;
        public Mesh Mesh => _mesh;
        
        public void BuildMesh()
        {
            if(_mesh != null)
                Clear();
            
            MarchCubes(densityFunction);
            _mesh = new Mesh();
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.vertices = _vertices.ToArray();
            _mesh.triangles = _faces.ToArray();

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            
        }
        
        public void Clear()
        {
            _vertices.Clear();
            _faces.Clear();
            _mesh.Clear();
        }

        void MarchCubes(Density.DensityFunction function)
        {
            for (int i = 0; i < sectionSize.x; i++)
            {
                for (int j = 0; j < sectionSize.y; j++)
                {
                    for (int k = 0; k < sectionSize.z; k++)
                    {
                        MarchingCubesUtils.Cube cube = new MarchingCubesUtils.Cube
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
                            cube.Values[x] = function(sampleSpacePosition + cube.Corners[x]);
                        }
                        
                        ProcessCube(cube);
                    }
                }
            }
        }

        void ProcessCube(MarchingCubesUtils.Cube cube)
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

                Vector3 v1 = MarchingCubesUtils.InterpolateVertices(cube.Corners[a0], cube.Values[a0], cube.Corners[a1], cube.Values[a1], surfaceLevel);
                Vector3 v2 = MarchingCubesUtils.InterpolateVertices(cube.Corners[b0], cube.Values[b0], cube.Corners[b1], cube.Values[b1], surfaceLevel);
                Vector3 v3 = MarchingCubesUtils.InterpolateVertices(cube.Corners[c0], cube.Values[c0], cube.Corners[c1], cube.Values[c1], surfaceLevel);
                
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
    }
}
