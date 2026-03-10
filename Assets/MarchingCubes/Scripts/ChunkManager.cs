using System.Collections.Generic;
using MarchingCubes.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Vector3Int mapSize;
    [SerializeField, Range(4, 48)] private int resolution = 16;
    [SerializeField] private float chunkSize = 1f;
    [SerializeField] private float surfaceLevel = 1f;
    
    [Header("Reuse Mesh")]
    [SerializeField] private bool reuseMesh = false;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    
    [Header("Save Settings")]
    [SerializeField] private string meshSaveLocation = "Assets/MarchingCubes/Meshes";
    [SerializeField] private string meshSaveName = "MarchingCubesMesh";
    
    private int _lastResolution = 16;
    private float _lastChunkSize = 32f;
    private float _lastSurfaceLevel = 1f;
    
    private List<MarchingCubeChunk> _chunks = new();
    
    private static ChunkManager instance = null;
    public static ChunkManager Instance => instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }
    
#if UNITY_EDITOR
    [MenuItem("Chunk Manager/Save Mesh/.asset")]
    static void SaveMeshToAsset()
    {
        Debug.Log("Saving mesh as asset...");
        
        string path = Instance.meshSaveLocation + "/" + Instance.meshSaveName + ".asset";
        
        Mesh mesh = CombineMeshes();
        
        if (path.Length > 0)
        {
            AssetDatabase.CreateAsset(Instantiate(mesh), path);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("Saved mesh as asset !");
    }
    [MenuItem("Chunk Manager/Save Mesh/.obj")]
    static void SaveMeshToObj()
    {
        Debug.Log("Saving mesh as .obj...");
        
    }
    [MenuItem("Chunk Manager/Save Mesh/.fbx")]
    static void SaveMeshToFbx()
    {
        Debug.Log("Saving to .fbx not available yet...");
        //Debug.Log("Saving mesh as .fbx...");
    }

    static Mesh CombineMeshes() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        MeshFilter[] meshFilters = Instance.transform.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] instances = new CombineInstance[meshFilters.Length];
        
        for (int i = 0; i < meshFilters.Length; i++)
        {
            var meshFilter = meshFilters[i];
            
            instances[i] = new CombineInstance
            {
                mesh = meshFilter.sharedMesh,
                transform = meshFilter.transform.localToWorldMatrix,
            };

            meshFilter.gameObject.SetActive(false);
        }
        
        mesh.CombineMeshes(instances);

        return mesh;
    }
#endif
    
    void OnValidate()
    {
        if (resolution != _lastResolution)
        {
            _lastResolution = resolution;
            RebuildMap();
        }
        if (!Mathf.Approximately(chunkSize, _lastChunkSize))
        {
            _lastChunkSize = chunkSize;
            RebuildMap();
        }
        if (!Mathf.Approximately(surfaceLevel, _lastSurfaceLevel))
        {
            _lastSurfaceLevel = surfaceLevel;
            RebuildMap();
        }
    }
    
    void Start()
    {
        if (reuseMesh) {
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
        }
        else {
            BuildMap();
        }
    }

    void BuildMap()
    {
        Vector3 position = transform.position;
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int z = 0; z < mapSize.z; z++)
                {
                    Vector3 chunkPos = position + new Vector3(x, y, z) * chunkSize;
                    GameObject chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);
                    MarchingCubeChunk marchingCubeChunk = chunk.GetComponent<MarchingCubeChunk>();
                    marchingCubeChunk.resolution = resolution;
                    marchingCubeChunk.chunkSize = chunkSize;
                    marchingCubeChunk.surfaceLevel = surfaceLevel;
                    marchingCubeChunk.sampleSpacePosition = chunkPos;
                    _chunks.Add(marchingCubeChunk);
                }
            }
        }
    }

    void RebuildMap()
    {
        foreach (MarchingCubeChunk chunk in _chunks)
        {
            chunk.resolution = resolution;
            chunk.chunkSize = chunkSize;
            chunk.surfaceLevel = surfaceLevel;
            chunk.RebuildMesh();
        }
    }
    
    struct CaveEdge
    {
        public Vector3 a;
        public Vector3 b;
        public float radius;
    }
    
    private List<CaveEdge> _edges = new List<CaveEdge>()
    {
        new CaveEdge{a = new Vector3(10,10,10), b = new Vector3(20,10,10), radius = 1},
        new CaveEdge{a = new Vector3(20,10,10), b = new Vector3(30,12,15), radius = 1},
        new CaveEdge{a = new Vector3(20,10,10), b = new Vector3(20,5,20), radius = 1},
    };
    
    public float GetDensity(Vector3 p)
    {
        float density = 1f;

        foreach (var edge in _edges)
        {
            float d = DistancePointSegment(p, edge.a, edge.b);

            float influence = edge.radius - d;

            if (influence > 0)
                density -= influence;
        }

        return density;
    }
    
    float DistancePointSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 pa = p - a;
        Vector3 ba = b - a;

        float h = Mathf.Clamp01(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));

        return (pa - ba * h).magnitude;
    }
}
