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

    PreviewRenderUtility _preview;
    private Material _material;
    MeshCollider _collider;
    
    private Vector2 _rotation = new Vector2(20, 30);
    private float _zoom = 5f;
    
    private Button _redButton;
    private Button _blueButton;
    private Button _greenButton;

    void OnEnable()
    {
        _preview = new PreviewRenderUtility();

        _preview.camera.transform.position = new Vector3(0, 0, -5);
        _preview.camera.transform.LookAt(Vector3.zero);

        _preview.camera.nearClipPlane = 0.1f;
        _preview.camera.farClipPlane = 100f;

        _preview.lights[0].intensity = 1.4f;
        _preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
        _preview.lights[1].intensity = 1.4f;
        
        _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _material.color = Color.red;
    }

    void OnDisable()
    {
        _preview.Cleanup();
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
        
        var preview = new IMGUIContainer(DrawPreview);
        rootVisualElement.Add(preview);
    }

    private void DrawPreview()
    {
        Rect r = GUILayoutUtility.GetRect(400, 400);
        GUI.Box(r, "Preview");
        
        _preview.BeginPreview(r, GUIStyle.none);
        // if (mesh != null)
        //     _preview.DrawMesh(mesh, Matrix4x4.identity, _material, 0);

        mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        _preview.DrawMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Matrix4x4.identity, _material, 0);

        UpdateCamera(r);
        HandleClick(r);
        UpdateCollider();

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
    
    void HandleClick(Rect r)
    {
        Event e = Event.current;
        if (r.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = PreviewRay(r, e.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2);
            //Debug.Log(ray.direction);

            if (_collider.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 p = hit.point;
                
                Debug.Log(p);
            }
            
            // if (Physics.Raycast(ray, out RaycastHit hit))
            // {
            //     Vector3 p = hit.point;
            //
            //     Debug.Log(p);
            // }
            
            e.Use();
        }
    }
    
    void UpdateCollider()
    {
        if (_collider == null)
        {
            GameObject go = new GameObject("PreviewCollider");
            go.hideFlags = HideFlags.HideAndDontSave;
            _collider = go.AddComponent<MeshCollider>();
        }

        _collider.sharedMesh = mesh;
    }
    
    Ray PreviewRay(Rect rect, Vector2 mousePos)
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
