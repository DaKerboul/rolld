using UnityEngine;
using NWH.VehiclePhysics2;

/// <summary>
/// Equivalent of <c>PlayerController.SetupLocalPlayer</c> for the NWH vehicle local player.
/// Attaches a floating name label, a colored marker above the car so other players can
/// spot us at distance, and registers the vehicle's Rigidbody with the NetworkManager
/// so it is broadcast over the wire.
/// </summary>
[RequireComponent(typeof(VehicleController))]
public class VehicleLocalSetup : MonoBehaviour
{
    private GameObject _nameLabelObj;
    private TextMesh _nameLabel;
    private GameObject _markerObj;
    private VehicleController _vehicle;

    void Awake()
    {
        _vehicle = GetComponent<VehicleController>();
    }

    public void SetupLocal(string playerName, Color playerColor)
    {
        if (_vehicle == null) _vehicle = GetComponent<VehicleController>();
        var rb = _vehicle.vehicleRigidbody != null ? _vehicle.vehicleRigidbody : GetComponent<Rigidbody>();

        // Register with NetworkManager so it broadcasts position from THIS Rigidbody.
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.RegisterLocalVehicle(transform, rb);

        BuildNameLabel(playerName, playerColor);
        BuildColorMarker(playerColor);

        Debug.Log($"[VehicleLocal] Setup complete: {playerName} color={playerColor}");
    }

    private void BuildNameLabel(string playerName, Color color)
    {
        if (_nameLabelObj != null) Destroy(_nameLabelObj);
        _nameLabelObj = new GameObject("LocalNameLabel");
        _nameLabelObj.transform.SetParent(transform.parent, false);
        _nameLabelObj.transform.localScale = Vector3.one * 0.1f;

        _nameLabel = _nameLabelObj.AddComponent<TextMesh>();
        _nameLabel.text = playerName;
        _nameLabel.fontSize = 144;
        _nameLabel.characterSize = 0.15f;
        _nameLabel.anchor = TextAnchor.MiddleCenter;
        _nameLabel.alignment = TextAlignment.Center;
        _nameLabel.color = color;
        if (PlayerController.LabelFont != null) _nameLabel.font = PlayerController.LabelFont;

        var renderer = _nameLabel.GetComponent<MeshRenderer>();
        if (PlayerController.LabelFont != null && PlayerController.LabelFont.material != null)
            renderer.material = PlayerController.LabelFont.material;
        else
        {
            var textShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Texture");
            if (textShader != null) renderer.material = new Material(textShader);
        }
    }

    private void BuildColorMarker(Color color)
    {
        // Cone-ish marker above the car so other players can identify us at distance.
        if (_markerObj != null) Destroy(_markerObj);
        _markerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _markerObj.name = "LocalMarker";
        DestroyImmediate(_markerObj.GetComponent<Collider>());
        _markerObj.transform.SetParent(transform, false);
        _markerObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        _markerObj.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);

        var r = _markerObj.GetComponent<Renderer>();
        if (r != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 0.6f);
            mat.EnableKeyword("_EMISSION");
            r.material = mat;
        }
    }

    void LateUpdate()
    {
        if (_nameLabelObj != null)
        {
            _nameLabelObj.transform.position = transform.position + Vector3.up * 2.8f;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - _nameLabelObj.transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                    _nameLabelObj.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    void OnDestroy()
    {
        if (_nameLabelObj != null) Destroy(_nameLabelObj);
        if (_markerObj != null) Destroy(_markerObj);
    }
}
