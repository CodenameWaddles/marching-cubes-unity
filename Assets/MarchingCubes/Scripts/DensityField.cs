using MarchingCubes.Scripts;
using UnityEngine;

public class DensityField : MonoBehaviour
{
    private float[][][] _valueField;

    public float[][][] ValueField => _valueField;

    public void GenerateField(Vector3Int sectionSize, Vector3 sampleSpacePosition, Density.DensityFunction densityFunction, float surfaceLevel, float step)
    {
        
    }
}
