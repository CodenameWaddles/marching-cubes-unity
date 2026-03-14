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
        public bool inverseMesh = false;
        
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _faces = new List<int>();
        
        private Mesh _mesh;
        
        private int[,,] _xEdges;
        private int[,,] _yEdges;
        private int[,,] _zEdges;
        
        public List<Vector3> Vertices  => _vertices;
        public List<int> Faces => _faces;
        public Mesh Mesh => _mesh;
        public DensityField densityField = new DensityField();
        
        public void BuildMesh()
        {
            if(_mesh != null)
                Clear();
            
            densityField.GenerateField(sectionSize, sampleSpacePosition, densityFunction, step);
            
            MarchCubes();
            _mesh = new Mesh();
            _mesh.MarkDynamic();
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.vertices = _vertices.ToArray();
            _mesh.triangles = _faces.ToArray();
            
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        public void UpdateMesh() {
            Clear();
            
            MarchCubes();
            
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

        void MarchCubes()
        {
            // Clear edge cache
            ResetEdgeCache();
            
            for (int i = 0; i < sectionSize.x - 1; i++)
            {
                for (int j = 0; j < sectionSize.y - 1; j++)
                {
                    for (int k = 0; k < sectionSize.z - 1; k++)
                    {
                        MarchingCubesUtils.Cube cube = new MarchingCubesUtils.Cube
                        {
                            Corners = new Vector3Int[8],
                            Values = new float[8]
                        };

                        cube.Corners[0] = new Vector3Int(i, j, k);
                        cube.Corners[1] = new Vector3Int(i, j, k+1);
                        cube.Corners[2] = new Vector3Int(i+1, j, k+1);
                        cube.Corners[3] = new Vector3Int(i+1, j, k);
                        cube.Corners[4] = new Vector3Int(i, j+1, k);
                        cube.Corners[5] = new Vector3Int(i, j+1, k+1);
                        cube.Corners[6] = new Vector3Int(i+1, j+1, k+1);
                        cube.Corners[7] = new Vector3Int(i+1, j+1, k);
    
                        for (int x = 0; x < 8; x++)
                        {
                            cube.Values[x] = densityField.valueField[cube.Corners[x].x, cube.Corners[x].y, cube.Corners[x].z];
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
                if (!inverseMesh && cube.Values[i] < surfaceLevel)
                {
                    cubeIndex |= 1 << i;
                }
                if (inverseMesh && cube.Values[i] > surfaceLevel)
                {
                    cubeIndex |= 1 << i;
                }
            }
            
            for (int i = 0; MarchingCubesData.TriangleTable[cubeIndex, i] != -1; i+=3)
            {
                // int e0 = MarchingCubesData.TriangleTable[cubeIndex, i];
                // int e1 = MarchingCubesData.TriangleTable[cubeIndex, i + 1];
                // int e2 = MarchingCubesData.TriangleTable[cubeIndex, i + 2];
                //
                // int v0 = GetVertexIndex(cube, e0);
                // int v1 = GetVertexIndex(cube, e1);
                // int v2 = GetVertexIndex(cube, e2);
                //
                // _faces.Add(v0);
                // _faces.Add(v2);
                // _faces.Add(v1);
                
                // ----------- Deprecated -----------
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
                
                _faces.Add(currentIndex);
                _faces.Add(currentIndex + 1);
                _faces.Add(currentIndex + 2);
            }
        }
        
        private int GetVertexIndex(MarchingCubesUtils.Cube cube, int edge)
        {
            int[] pos = MarchingCubesData.edgeCubePosition[edge];
            
            int x = cube.Corners[0].x + pos[0];
            int y = cube.Corners[0].y + pos[1];
            int z = cube.Corners[0].z + pos[2];

            int cached = -1;

            if (edge is 0 or 2 or 4 or 6)
            {
                cached = _xEdges[x,y,z];
                if (cached != -1) return cached;
            }
            else if (edge is 1 or 5 or 3 or 7)
            {
                cached = _yEdges[x,y,z];
                if (cached != -1) return cached;
            }
            else
            {
                cached = _zEdges[x,y,z];
                if (cached != -1) return cached;
            }
            
            int a = MarchingCubesData.cornerIndexAFromEdge[edge];
            int b = MarchingCubesData.cornerIndexBFromEdge[edge];

            Vector3 v = MarchingCubesUtils.InterpolateVertices(
                cube.Corners[a], cube.Values[a],
                cube.Corners[b], cube.Values[b],
                surfaceLevel);

            int index = _vertices.Count;
            _vertices.Add(v);

            if (edge is 0 or 2 or 4 or 6)
            {
                _xEdges[x, y, z] = index;
            }
            else if (edge is 1 or 5 or 3 or 7)
            {
                _yEdges[x, y, z] = index;
            }
            else
            {
                _zEdges[x, y, z] = index;
            }
            
            return index;
        }

        private void ResetEdgeCache()
        {
            _xEdges = new int[sectionSize.x, sectionSize.y + 1, sectionSize.z + 1];
            _yEdges = new int[sectionSize.x + 1, sectionSize.y, sectionSize.z + 1];
            _zEdges = new int[sectionSize.x + 1, sectionSize.y + 1, sectionSize.z];

            MarchingCubesUtils.Fill3D(_xEdges, -1);
            MarchingCubesUtils.Fill3D(_yEdges, -1);
            MarchingCubesUtils.Fill3D(_zEdges, -1);
        }
    }
}
