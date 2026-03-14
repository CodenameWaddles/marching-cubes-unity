using System;
using System.Collections.Generic;
using MarchingCubes.Scripts;
using NUnit.Framework;
using Tool.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class MeshEditorWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material brushMaterial;
    [SerializeField] private int minVertexCount = 100;
    [SerializeField] private string objectSaveLocation = "Assets/Tool/Save";
    [SerializeField] private string meshSaveName = "SavedMesh";
    
    private DensityFieldMeshObject  densityFieldMeshObject;

    private float[][][] _valueField;
    private Vector3Int _fieldSize;
    private float _surfaceLevel = 0f;
    private float _step = 1f;
    private Density.DensityFunction _densityFunction;

    private PreviewRenderUtility _preview;
    private Material _material;
    private MeshCollider _collider;
    private Matrix4x4 _meshMatrix;
    
    // Brush stuff
    private Mesh _rayPointMesh;
    private bool _rayCollided = false;
    private Vector3 _rayCollidedPos = Vector3.zero;
    private Vector3 _minRayCollidedScale = new Vector3(1f, 1f, 1f);
    private Vector3 _maxRayCollidedScale = new Vector3(5f, 5f, 5f);
    private Vector3 _rayCollidedScale = new Vector3(0.1f, 0.1f, 0.1f);
    private Matrix4x4 _rayCollidedMatrix;
    private float _brushSize;
    private float _brushStrength;
    private DensityField.FieldModificationType _modificationType = DensityField.FieldModificationType.Add;
    
    private Vector2 _rotation = new Vector2(20, 30);
    private float _zoom = 5f;
    private bool _mouseHeld;
    
    private Vector3IntField _fieldSizeField;

    private VisualElement _previewContainer;
    
    private Button _redButton;
    private Button _blueButton;
    private Button _greenButton;
    private Button _generateButton;
    private Button _addButton;
    private Button _subtractButton;
    private Button _saveButton;
    
    private Slider _brushSizeSlider;
    private Slider _brushStrengthSlider;
    private Slider _surfaceLevelSlider;

    private Toggle _invertToggle;
    
    private Label _nbVerticesLabel;
    private Label _nbFacesLabel;
    
    private DropdownField _densityTypeDropdown;

    private bool _meshGenerated = false;
    private GameObject _previewObject;
    private MarchingCubesSection _marchingCubesSection;

    void OnEnable()
    {
        // preview window setup
        _preview = new PreviewRenderUtility();

        _preview.camera.transform.position = new Vector3(0, 0, -5);
        _preview.camera.transform.LookAt(Vector3.zero);

        _preview.camera.nearClipPlane = 0.1f;
        _preview.camera.farClipPlane = 500f;

        _preview.lights[0].intensity = 1.4f;
        _preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
        _preview.lights[1].intensity = 1.4f;
        
        // materials
        _material = new Material(Shader.Find("Universal Render Pipeline/Lit")) {
            color = Color.red
        };

        // Brush setup
        _rayPointMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        _rayCollidedMatrix = Matrix4x4.TRS(_rayCollidedPos, Quaternion.identity, _rayCollidedScale);
        
        _previewObject =  new GameObject();
        _previewObject.hideFlags = HideFlags.HideAndDontSave;
        _marchingCubesSection = _previewObject.AddComponent<MarchingCubesSection>();
    }

    void OnDisable()
    {
        _preview.Cleanup();
        
        DestroyImmediate(_material);
        DestroyImmediate(_previewObject);
        
        if (mesh != null)
        {
            mesh.Clear();
            DestroyImmediate(mesh, true);
        }
        
        _brushSizeSlider.UnregisterValueChangedCallback(OnBrushSizeChanged);
        _fieldSizeField.UnregisterValueChangedCallback(OnFieldSizeChanged);
        _surfaceLevelSlider.UnregisterValueChangedCallback(OnSurfaceLevelChanged);
        _brushStrengthSlider.UnregisterValueChangedCallback(OnBrushStrengthChanged);
        _densityTypeDropdown.UnregisterValueChangedCallback(OnDensityTypeChanged);
        
        if (_redButton != null)
            _redButton.clicked -= OnClickRed;
        if (_blueButton != null)
            _blueButton.clicked -= OnClickBlue;
        if (_greenButton != null)
            _greenButton.clicked -= OnClickGreen;
        if (_generateButton != null)
            _generateButton.clicked -= GenerateMesh;
        if (_addButton != null)
            _addButton.clicked -= OnClickAdd;
        if (_subtractButton != null)
            _subtractButton.clicked -= OnClickSubtract;
        if (_saveButton != null)
            _saveButton.clicked -= OnClickSave;
    }
    
    [MenuItem("Tools/Mesh Editor")]
    public static void Open()
    {
        MeshEditorWindow wnd = GetWindow<MeshEditorWindow>();
        wnd.titleContent = new GUIContent("Mesh Editor");
    }

    private void Update() {
        Repaint();
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        
        // Instantiate UXML
        VisualElement labelFromUxml = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUxml);

        _redButton = root.Q<Button>("red");
        _blueButton = root.Q<Button>("blue");
        _greenButton = root.Q<Button>("green");
        _generateButton = root.Q<Button>("generate");
        _addButton = root.Q<Button>("add");
        _subtractButton = root.Q<Button>("subtract");
        _saveButton = root.Q<Button>("save");

        if (_redButton != null)
            _redButton.clicked += OnClickRed;
        if (_blueButton != null)
            _blueButton.clicked += OnClickBlue;
        if (_greenButton != null)
            _greenButton.clicked += OnClickGreen;
        if (_generateButton != null)
            _generateButton.clicked += GenerateMesh;
        if (_addButton != null)
            _addButton.clicked += OnClickAdd;
        if (_subtractButton != null)
            _subtractButton.clicked += OnClickSubtract;
        if (_saveButton != null)
            _saveButton.clicked += OnClickSave;
        
        _invertToggle = root.Q<Toggle>("invert");
            
        _brushSizeSlider = root.Q<Slider>("brush_size");
        _brushSizeSlider.RegisterValueChangedCallback(OnBrushSizeChanged);
        _brushSize = _brushSizeSlider.value;
        
        _brushStrengthSlider = root.Q<Slider>("brush_strength");
        _brushStrengthSlider.RegisterValueChangedCallback(OnBrushStrengthChanged);
        _brushStrength = _brushStrengthSlider.value;
        
        _surfaceLevelSlider = root.Q<Slider>("surface_level");
        _surfaceLevelSlider.RegisterValueChangedCallback(OnSurfaceLevelChanged);
        _surfaceLevel = _surfaceLevelSlider.value;

        _fieldSizeField = root.Q<Vector3IntField>("field_size");
        _fieldSizeField.RegisterValueChangedCallback(OnFieldSizeChanged);
        _fieldSize = _fieldSizeField.value;
        
        _nbVerticesLabel = root.Q<Label>("vertices");
        _nbFacesLabel = root.Q<Label>("faces");
        
        _densityTypeDropdown = root.Q<DropdownField>("density_type");
        _densityTypeDropdown.RegisterValueChangedCallback(OnDensityTypeChanged);
        _densityFunction = Density.DensityFunctions["Flat"];
        
        _previewContainer = root.Q<VisualElement>("preview_container");
        
        var preview = new IMGUIContainer(DrawPreview);
        preview.style.flexGrow = 1;
        _previewContainer.Add(preview);
    }

    private void DrawPreview()
    {
        Rect r = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );
        GUI.Box(r, "Edit");
        
        _preview.BeginPreview(r, GUIStyle.none);

        if (_meshGenerated)
        {
            _preview.DrawMesh(mesh, _meshMatrix, _material, 0);
        }

        UpdateCamera(r);
        HandleMouse(r);
        UpdateCollider();

        if (mesh)
            UpdateLabels();

        if (_rayCollided) {
            _preview.DrawMesh(_rayPointMesh, _rayCollidedMatrix, brushMaterial, 0);
        }
        
        _preview.camera.Render();
        
        Texture result = _preview.EndPreview();
        GUI.DrawTexture(r, result);
    }

    void UpdateCamera(Rect r)
    {
        Event e = Event.current;

        if (r.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDrag && e.button == 1)
            {
                _rotation.x += e.delta.y;
                _rotation.y += e.delta.x;
                _rotation.x = Mathf.Clamp(_rotation.x, -95f, 95f);
                e.Use();
            }

            if (e.type == EventType.ScrollWheel)
            {
                _zoom += e.delta.y * 0.3f;
                _zoom = Mathf.Clamp(_zoom, 1f, 200f);
                e.Use();
            }
        }
        
        Vector3 camPos = Quaternion.Euler(_rotation.x, _rotation.y, 0) * new Vector3(0, 0, -_zoom);
        _preview.camera.transform.position = camPos;
        _preview.camera.transform.LookAt(Vector3.zero);
    }
    
    void HandleMouse(Rect r)
    {
        Event e = Event.current;
        if (r.Contains(e.mousePosition))
        {
            Ray ray = GetRay(r, e.mousePosition);

            if (e.type == EventType.MouseLeaveWindow) {
                Debug.Log("yes");
                _mouseHeld = false;
            }
            if (e.type == EventType.MouseUp && e.button == 0) {
                _mouseHeld = false;
                e.Use();
            }

            if (_collider.Raycast(ray, out RaycastHit hit, 500f))
            {
                Vector3 p = hit.point;
                _rayCollidedPos = p;
                _rayCollidedScale = Vector3.Lerp(_minRayCollidedScale, _maxRayCollidedScale, (_brushSize - _brushSizeSlider.lowValue) / (_brushSizeSlider.highValue - _brushSizeSlider.lowValue) );
                _rayCollidedMatrix = Matrix4x4.TRS(_rayCollidedPos, Quaternion.identity, _rayCollidedScale);
                _rayCollided = true;
                if (e.type == EventType.MouseDown && e.button == 0) {
                    _mouseHeld = true;
                    e.Use();
                }
                if (_mouseHeld) {
                    HandleClick(p);
                }
            }
            else {
                _rayCollided = false;
            }
            
        }
    }
    
    void UpdateCollider()
    {
        if (_collider == null)
        {
            _previewObject.transform.position = _meshMatrix.GetPosition();
            _collider = _previewObject.AddComponent<MeshCollider>();
        }
        
        _previewObject.transform.position = _meshMatrix.GetPosition();
        _collider.sharedMesh = mesh;
    }
    
    Ray GetRay(Rect rect, Vector2 mousePos)
    {
        Vector2 local = mousePos - rect.position;

        local.y = rect.height - local.y;

        Vector3 screenPoint = new Vector3(
            local.x / rect.width * _preview.camera.pixelWidth,
            local.y / rect.height * _preview.camera.pixelHeight,
            0
        );

        return _preview.camera.ScreenPointToRay(screenPoint);
    }

    private void HandleClick(Vector3 point)
    {
        if (_modificationType == DensityField.FieldModificationType.Subtract && mesh.vertexCount <= minVertexCount) return;

        _marchingCubesSection.densityField.ModifyFieldSphere(point - _previewObject.transform.position, _modificationType, Mathf.RoundToInt(_rayCollidedScale.x), _brushStrength, _surfaceLevel);
        _marchingCubesSection.UpdateMesh();
    }

    public void GenerateMesh()
    {
        Debug.Log("Generating Mesh");
        _meshGenerated = true;

        _marchingCubesSection.sectionSize = _fieldSize;
        _marchingCubesSection.step = _step;
        _marchingCubesSection.surfaceLevel = _surfaceLevel;
        _marchingCubesSection.densityFunction = _densityFunction;
        _marchingCubesSection.sampleSpacePosition = new Vector3(-15f, -15f, -15f);
        _marchingCubesSection.inverseMesh = _invertToggle.value;
        
        _marchingCubesSection.BuildMesh();

        if (mesh != null)
        {
            mesh.Clear();
            DestroyImmediate(mesh, true);
        }
        
        mesh = _marchingCubesSection.Mesh;
        
        // Mesh position based on bounds
        Vector3 meshPos = -mesh.bounds.center;
        _meshMatrix = Matrix4x4.TRS(meshPos, Quaternion.identity, Vector3.one);

        UpdateCollider();
        Debug.Log("Mesh Generated with " + _marchingCubesSection.Mesh.vertices.Length + " vertices");
    }

    void UpdateLabels()
    {
        if(mesh.triangles.Length > 0)
            _nbFacesLabel.text = "Faces : " + (mesh.triangles.Length / 3);
        if(mesh.vertexCount > 0)
            _nbVerticesLabel.text = "Vertices : " + mesh.vertexCount;
    }
    
    private void OnBrushSizeChanged(ChangeEvent<float> evt) {
        _brushSize = evt.newValue;
    }
    
    private void OnBrushStrengthChanged(ChangeEvent<float> evt) {
        _brushStrength = evt.newValue;
    }

    private void OnSurfaceLevelChanged(ChangeEvent<float> evt)
    {
        _surfaceLevel = evt.newValue;
    }
    
    private void OnFieldSizeChanged(ChangeEvent<Vector3Int> evt) {
        _fieldSize = evt.newValue;
    }

    public void OnClickAdd() {
        _modificationType = DensityField.FieldModificationType.Add;
    }
    
    public void OnClickSubtract() {
        _modificationType = DensityField.FieldModificationType.Subtract;
    }

    public void OnClickSave()
    {
        densityFieldMeshObject = CreateInstance<DensityFieldMeshObject>();
        AssetDatabase.CreateAsset(densityFieldMeshObject, objectSaveLocation + "/DensityFieldMeshObject.asset");

        densityFieldMeshObject.Mesh = mesh;
        densityFieldMeshObject.DensityField = _marchingCubesSection.densityField;
        densityFieldMeshObject.MeshSaveName = meshSaveName;
        densityFieldMeshObject.MeshSaveLocation = objectSaveLocation;

        densityFieldMeshObject.SaveMesh();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void OnDensityTypeChanged(ChangeEvent<string> evt)
    {
        _densityFunction = Density.DensityFunctions[evt.newValue];
    }
    
    public void OnClickRed()
    {
        _material.color = Color.red;
    }
    
    public void OnClickBlue()
    {
        _material.color = Color.blue;
    }
    
    public void OnClickGreen()
    {
        _material.color = Color.green;
    }
}
