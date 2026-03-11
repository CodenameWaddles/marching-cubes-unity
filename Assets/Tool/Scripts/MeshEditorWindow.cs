using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MeshEditorWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material brushMaterial;

    private float[][][] _valueField;
    private Vector3Int _fieldSize;

    private PreviewRenderUtility _preview;
    private Material _material;
    private MeshCollider _collider;
    private Matrix4x4 _meshMatrix;
    
    // Brush stuff
    private Mesh _rayPointMesh;
    private bool _rayCollided = false;
    private Vector3 _rayCollidedPos = Vector3.zero;
    private Vector3 _minRayCollidedScale = new Vector3(0.1f, 0.1f, 0.1f);
    private Vector3 _maxRayCollidedScale = new Vector3(1f, 1f, 1f);
    private Vector3 _rayCollidedScale = new Vector3(0.1f, 0.1f, 0.1f);
    private Matrix4x4 _rayCollidedMatrix;
    
    private float _brushSize;
    
    private Vector2 _rotation = new Vector2(20, 30);
    private float _zoom = 5f;

    private Slider _brushSizeSlider;
    private Vector3IntField _fieldSizeField;

    private VisualElement _previewContainer;
    
    private Button _redButton;
    private Button _blueButton;
    private Button _greenButton;

    void OnEnable()
    {
        // preview window setup
        _preview = new PreviewRenderUtility();

        _preview.camera.transform.position = new Vector3(0, 0, -5);
        _preview.camera.transform.LookAt(Vector3.zero);

        _preview.camera.nearClipPlane = 0.1f;
        _preview.camera.farClipPlane = 100f;

        _preview.lights[0].intensity = 1.4f;
        _preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
        _preview.lights[1].intensity = 1.4f;
        
        // Mesh position based on bounds
        Vector3 meshPos = -mesh.bounds.center;
        _meshMatrix = Matrix4x4.TRS(meshPos, Quaternion.identity, Vector3.one);
        
        // materials
        _material = new Material(Shader.Find("Universal Render Pipeline/Lit")) {
            color = Color.red
        };

        // Brush setup
        _rayPointMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        _rayCollidedMatrix = Matrix4x4.TRS(_rayCollidedPos, Quaternion.identity, _rayCollidedScale);
        
        // if not mesh display default
        if (mesh == null) {
            mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }
    }

    void OnDisable()
    {
        _preview.Cleanup();
        
        DestroyImmediate(_material);
        
        _brushSizeSlider.UnregisterValueChangedCallback(OnBrushSizeChanged);
        _fieldSizeField.UnregisterValueChangedCallback(OnFieldSizeChanged);
        
        if (_redButton != null)
            _redButton.clicked -= OnClickRed;
        if (_blueButton != null)
            _blueButton.clicked -= OnClickBlue;
        if (_greenButton != null)
            _greenButton.clicked -= OnClickGreen;
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
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        _redButton = root.Q<Button>("red");
        _blueButton = root.Q<Button>("blue");
        _greenButton = root.Q<Button>("green");

        if (_redButton != null)
            _redButton.clicked += OnClickRed;
        if (_blueButton != null)
            _blueButton.clicked += OnClickBlue;
        if (_greenButton != null)
            _greenButton.clicked += OnClickGreen;
        
        _brushSizeSlider = root.Q<Slider>("brush_size");
        _brushSizeSlider.RegisterValueChangedCallback(OnBrushSizeChanged);
        _brushSize = _brushSizeSlider.value;

        _fieldSizeField = root.Q<Vector3IntField>("field_size");
        _fieldSizeField.RegisterValueChangedCallback(OnFieldSizeChanged);
        _fieldSize = _fieldSizeField.value;
        
        _previewContainer = root.Q<VisualElement>("preview_container");
        
        var preview = new IMGUIContainer(DrawPreview);
        _previewContainer.Add(preview);
    }

    private void DrawPreview()
    {
        Rect r = GUILayoutUtility.GetRect(600, 400);
        GUI.Box(r, "Preview");
        
        _preview.BeginPreview(r, GUIStyle.none);
        
        _preview.DrawMesh(mesh, _meshMatrix, _material, 0);

        UpdateCamera(r);
        HandleMouse(r);
        UpdateCollider();

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
                _rotation.x = Mathf.Clamp(_rotation.x, -80f, 80f);
                e.Use();
            }

            if (e.type == EventType.ScrollWheel)
            {
                _zoom += e.delta.y * 0.1f;
                _zoom = Mathf.Clamp(_zoom, 1f, 20f);
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

            if (_collider.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 p = hit.point;
                _rayCollidedPos = p;
                _rayCollidedScale = Vector3.Lerp(_minRayCollidedScale, _maxRayCollidedScale, (_brushSize - _brushSizeSlider.lowValue) / (_brushSizeSlider.highValue - _brushSizeSlider.lowValue) );
                _rayCollidedMatrix = Matrix4x4.TRS(_rayCollidedPos, Quaternion.identity, _rayCollidedScale);
                _rayCollided = true;
                if (e.type == EventType.MouseDown && e.button == 0) {
                    e.Use();
                }
                //Debug.Log(p);
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
            GameObject go = new GameObject("PreviewCollider");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.position = _meshMatrix.GetPosition();
            _collider = go.AddComponent<MeshCollider>();
        }

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

    private void OnBrushSizeChanged(ChangeEvent<float> evt) {
        _brushSize = evt.newValue;
    }
    
    private void OnFieldSizeChanged(ChangeEvent<Vector3Int> evt) {
        _fieldSize = evt.newValue;
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
