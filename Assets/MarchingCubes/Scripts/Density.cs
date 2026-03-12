using UnityEngine;

namespace MarchingCubes.Scripts
{
    public static class Density
    {
        public delegate float DensityFunction(Vector3 position);

        public static float PlaneDensity(Vector3 position)
        {
            float density = position.y;
            return density;
        }
        
        public static float TestDensity(Vector3 position) {
            //float density = position.y;
            float density = 0;
            density += MarchingCubesUtils.ThreeDNoise(position * 4.05f) * 0.25f;
            density += MarchingCubesUtils.ThreeDNoise(position * 2.03f) * 0.5f;
            density += MarchingCubesUtils.ThreeDNoise(position * 1.02f);
            density += MarchingCubesUtils.ThreeDNoise(position * 0.52f) * 2.05f;
            density += MarchingCubesUtils.ThreeDNoise(position * 0.245f) * 4.03f;
            density += MarchingCubesUtils.ThreeDNoise(position * 0.125f) * 8.05f;
            density += MarchingCubesUtils.ThreeDNoise(position * 0.0625f) * 16.03f;
            density += MarchingCubesUtils.ThreeDNoise(position * 0.03126f) * 32.06f;
            density += MarchingCubesUtils.ThreeDNoise(position * 0.015624f) * 64.04f;
            return density;
        }

        public static float SphereDensity(Vector3 position)
        {
            float r = 2.5f;
            return position.x*position.x + position.y*position.y + position.z*position.z - r*r;
        }
    }
}
