using UnityEngine;
using NWH.VehiclePhysics2;

/// <summary>
/// Remote vehicle controller. Attached to remote players spawned from the network.
/// - Disables NWH local simulation (VehicleController + WheelControllers + AudioSources).
/// - Sets the Rigidbody kinematic so we can drive it with snapshot interpolation
///   via <see cref="Rigidbody.MovePosition"/> / <see cref="Rigidbody.MoveRotation"/>.
/// - Local dynamic vehicles bounce naturally off the kinematic remote's colliders.
/// - Adds a floating name label (distance-scaled) and a colored capsule marker.
/// </summary>
public class RemoteVehicleSync : MonoBehaviour
{
    [Header("Interpolation")]
    public float interpolationDelay = 0.083f;
    public float maxExtrapolation = 0.08f;
    public float snapDistance = 12f;
    public float smoothingSpeed = 24f;
    public float rotationSpeed = 24f;

    [Header("Spawn")]
    [Tooltip("Seconds after spawn during which colliders are disabled to avoid ejecting overlapping locals at connect.")]
    public float spawnGrace = 1.5f;

    public string SessionId { get; private set; }
    public string PlayerName { get; private set; }
    public Color PlayerColor { get; private set; }
    public float SpawnTime { get; private set; }

    private struct Snapshot
    {
        public double serverTime;
        public float localTime;
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;
        public Vector3 angularVelocity;
    }

    private const int BUFFER_SIZE = 16;
    private readonly Snapshot[] _buffer = new Snapshot[BUFFER_SIZE];
    private int _bufferCount;
    private int _newestIndex;
    private bool _initialized;

    private Rigidbody _rb;
    private VehicleController _vehicle;
    private Collider[] _allColliders;
    private bool _collidersReenabled;
    private Quaternion _currentRotation = Quaternion.identity;

    private GameObject _nameLabelObj;
    private TextMesh _nameLabel;
    private GameObject _markerObj;

    public void Initialize(string sessionId, string playerName, Color color)
    {
        SessionId = sessionId;
        PlayerName = playerName;
        PlayerColor = color;
        SpawnTime = Time.time;
        _bufferCount = 0;
        _currentRotation = transform.rotation;

        // Disable NWH driving simulation: this remote is purely networked, no local AI/input.
        _vehicle = GetComponent<VehicleController>();
        if (_vehicle != null) _vehicle.enabled = false;

        // Disable all wheel controllers so they don't apply suspension forces.
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            var t = mb.GetType();
            if (t.FullName == "NWH.WheelController3D.WheelController" || t.FullName.Contains(".WheelController"))
                mb.enabled = false;
        }

        // Mute audio sources (engine, skid, etc.) on remotes.
        foreach (var src in GetComponentsInChildren<AudioSource>(true))
            src.enabled = false;

        // Make the rigidbody kinematic so we drive it from network snapshots.
        _rb = _vehicle != null ? _vehicle.vehicleRigidbody : GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        // Cache colliders and disable them during the grace window to avoid spawn-time ejection.
        _allColliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in _allColliders)
            if (c != null && !c.isTrigger) c.enabled = false;

        BuildNameLabel(playerName, color);
        BuildColorMarker(color);

        _initialized = true;
        Debug.Log($"[RemoteVehicle] Initialized: {playerName} ({sessionId[..6]}) color={color}");
    }

    public void SetTargetState(Vector3 position, Vector3 velocity, Quaternion rotation, double serverTime, Vector3 angularVelocity = default)
    {
        _newestIndex = (_newestIndex + 1) % BUFFER_SIZE;
        _buffer[_newestIndex] = new Snapshot
        {
            serverTime = serverTime,
            localTime = Time.time,
            position = position,
            velocity = velocity,
            rotation = rotation,
            angularVelocity = angularVelocity,
        };
        if (_bufferCount < BUFFER_SIZE) _bufferCount++;
    }

    public void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r != null) r.enabled = visible;
        if (_nameLabelObj != null) _nameLabelObj.SetActive(visible);
        if (_markerObj != null) _markerObj.SetActive(visible);
    }

    void Update()
    {
        if (!_initialized) return;

        // Re-enable colliders once grace window has elapsed.
        if (!_collidersReenabled && Time.time - SpawnTime > spawnGrace)
        {
            if (_allColliders != null)
                foreach (var c in _allColliders)
                    if (c != null && !c.isTrigger) c.enabled = true;
            _collidersReenabled = true;
        }

        if (_bufferCount == 0) return;

        float renderTime = Time.time - interpolationDelay;
        int oldestIdx = (_newestIndex - _bufferCount + 1 + BUFFER_SIZE) % BUFFER_SIZE;

        Snapshot older = default, newer = default;
        bool found = false;
        for (int i = 0; i < _bufferCount - 1; i++)
        {
            int a = (oldestIdx + i) % BUFFER_SIZE;
            int b = (oldestIdx + i + 1) % BUFFER_SIZE;
            if (_buffer[a].localTime <= renderTime && _buffer[b].localTime >= renderTime)
            {
                older = _buffer[a];
                newer = _buffer[b];
                found = true;
                break;
            }
        }

        Vector3 targetPos;
        Quaternion targetRot;
        if (found)
        {
            float span = newer.localTime - older.localTime;
            float t = span > 0.001f ? (renderTime - older.localTime) / span : 1f;
            t = Mathf.Clamp01(t);
            targetPos = Vector3.Lerp(older.position, newer.position, t);
            targetRot = Quaternion.Slerp(older.rotation, newer.rotation, t);
        }
        else
        {
            var newest = _buffer[_newestIndex];
            float elapsed = renderTime - newest.localTime;
            if (elapsed < 0)
            {
                targetPos = _buffer[oldestIdx].position;
                targetRot = _buffer[oldestIdx].rotation;
            }
            else
            {
                float extTime = Mathf.Min(elapsed, maxExtrapolation);
                float damp = 1f - Mathf.Clamp01(elapsed / (maxExtrapolation * 2f));
                targetPos = newest.position + newest.velocity * extTime * damp;
                targetRot = newest.rotation;
            }
        }

        float dist = Vector3.Distance(transform.position, targetPos);
        Vector3 newPos;
        if (dist > snapDistance)
        {
            newPos = targetPos;
            _currentRotation = targetRot;
        }
        else
        {
            float lerpT = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            newPos = Vector3.Lerp(transform.position, targetPos, lerpT);
        }
        float rotLerpT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
        _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, rotLerpT);

        if (_rb != null)
        {
            _rb.MovePosition(newPos);
            _rb.MoveRotation(_currentRotation);
        }
        else
        {
            transform.position = newPos;
            transform.rotation = _currentRotation;
        }

        if (_nameLabelObj != null)
        {
            _nameLabelObj.transform.position = transform.position + Vector3.up * 2.8f;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - _nameLabelObj.transform.position;
                float camDist = lookDir.magnitude;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                    _nameLabelObj.transform.rotation = Quaternion.LookRotation(lookDir);
                float scale = Mathf.Clamp(camDist / 8f, 1f, 8f);
                _nameLabelObj.transform.localScale = Vector3.one * (0.1f * scale);
            }
        }
    }

    private void BuildNameLabel(string playerName, Color color)
    {
        _nameLabelObj = new GameObject("NameLabel");
        _nameLabelObj.transform.position = transform.position + Vector3.up * 2.8f;
        _nameLabelObj.transform.localScale = Vector3.one * 0.1f;

        _nameLabel = _nameLabelObj.AddComponent<TextMesh>();
        _nameLabel.text = playerName;
        _nameLabel.fontSize = 144;
        _nameLabel.characterSize = 0.15f;
        _nameLabel.anchor = TextAnchor.MiddleCenter;
        _nameLabel.alignment = TextAlignment.Center;
        _nameLabel.color = color;
        if (PlayerController.LabelFont != null) _nameLabel.font = PlayerController.LabelFont;

        var mr = _nameLabel.GetComponent<MeshRenderer>();
        if (PlayerController.LabelFont != null && PlayerController.LabelFont.material != null)
            mr.material = PlayerController.LabelFont.material;
        else
        {
            var s = Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Texture");
            if (s != null) mr.material = new Material(s);
        }
    }

    private void BuildColorMarker(Color color)
    {
        _markerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _markerObj.name = "RemoteMarker";
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

    void OnDestroy()
    {
        if (_nameLabelObj != null) Destroy(_nameLabelObj);
        if (_markerObj != null) Destroy(_markerObj);
        Debug.Log($"[RemoteVehicle] Destroyed: {PlayerName}");
    }
}
