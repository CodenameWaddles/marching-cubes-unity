using MarchingCubes.Scripts;
using UnityEditor;
using UnityEngine;

namespace Tool.Scripts {
    [CreateAssetMenu(fileName = "DensityFieldMesh", menuName = "Scriptable Objects/DensityFieldMesh")]
    public class DensityFieldMeshObject : ScriptableObject
    {
        public DensityField DensityField { get; set; }
        public Mesh Mesh { get;  set; }
        public string MeshSaveName  { get; set; }
        public string MeshSaveLocation  { get; set; }
    
        public string GetAssetPath()
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAssetPath(this);
#else
        return null;
#endif
        }

        public void SaveMesh()
        {
            Debug.Log("Saving mesh as asset...");
        
            string path = MeshSaveLocation + "/" + MeshSaveName + ".asset";
        
            if (path.Length > 0)
            {
                AssetDatabase.CreateAsset(Instantiate(Mesh), path);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("Saved mesh as asset !");
        }
    }
}
