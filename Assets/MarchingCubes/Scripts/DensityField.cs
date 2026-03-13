using UnityEngine;

namespace MarchingCubes.Scripts
{
    public class DensityField
    {
        public enum FieldModificationType {
            Add = 1,
            Subtract = -1
        }
        
        public float[,,] valueField;
        public float step;

        public float threshold = 0.05f;

        public void GenerateField(Vector3Int sectionSize, Vector3 sampleSpacePosition, Density.DensityFunction densityFunction, float stepValue)
        {
            valueField = new float[sectionSize.x, sectionSize.y, sectionSize.z];
            step = stepValue;

            for (int x = 0; x < sectionSize.x; x++) {
                for (int y = 0; y < sectionSize.y; y++) {
                    for (int z = 0; z < sectionSize.z; z++) {
                        Vector3 position = sampleSpacePosition + new Vector3(x * step, y * step, z * step);
                        valueField[x, y, z] = densityFunction(position);
                    }
                }
            }
        }

        public void ModifyFieldSphere(Vector3 point, FieldModificationType modificationType, float size, float strength, float surfaceLevel) {
            for (float x = -size; x < size; x++) {
                for (float y = -size; y < size; y++) {
                    for (float z = -size; z < size; z++) {
                        float xi = point.x + x;
                        float yi = point.y + y;
                        float zi = point.z + z;
                        float distance = MarchingCubesUtils.SphereDistance(point, new Vector3(xi, yi, zi), size);
                        if (distance < 0) {
                            int xj = Mathf.RoundToInt(xi);
                            int yj = Mathf.RoundToInt(yi);
                            int zj = Mathf.RoundToInt(zi);
                            
                            if (valueField.GetLength(0) > xj && valueField.GetLength(1) > yj && valueField.GetLength(2) > zj && xj >= 0 && yj >= 0 && zj >= 0)
                            {
                                valueField[xj, yj, zj] += (int)modificationType * distance * strength;
                            }
                        }
                    }
                }
            }
        }
    }
}
