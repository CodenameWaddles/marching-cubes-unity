using UnityEngine;

namespace MarchingCubes.Scripts
{
    public static class MarchingCubesUtils
    {
        public static Vector3 InterpolateVertices(Vector3 a, float va, Vector3 b, float vb, float surfaceLevel)
        {
            float t = (surfaceLevel - va) / (vb - va);
            return a + t * (b - a);
        }
        
        public struct Cube
        {
            public float[] Values;
            public Vector3Int[] Corners;
        }
        
        public struct DeprecatedCube
        {
            public float[] Values;
            public Vector3[] Corners;
        }
        
        public static float ThreeDNoise(Vector3 position)
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
        
        public static float SphereDistance(Vector3 spherePos, Vector3 point, float radius) {
            return Vector3.Distance(spherePos, point) - radius;
        }
    }
}
