using UnityEngine;

/// <summary>
/// Controls a remote player's ball using snapshot interpolation.
/// Maintains a ring buffer of recent network snapshots and interpolates
/// between them with a fixed delay, producing smooth motion even with jitter.
/// Uses Rigidbody.MovePosition for proper physics collision detection.
/// </summary>
public class RemotePlayerController : MonoBehaviour
{
    [Header("Interpolation")]
    [Tooltip("Interpolation delay in seconds (higher = smoother, more latency)")]
    public float interpolationDelay = 0.083f; // ~83ms = 5 frames at 60Hz

    [Tooltip("Max extrapolation time when no new data arrives")]
    public float maxExtrapolation = 0.08f; // 80ms — short to avoid overshoot

    [Tooltip("If distance exceeds this, snap instead of interpolate")]
    public float snapDistance = 8f;

    [Tooltip("Final smoothing factor (higher = tighter follow, lower = smoother)")]
    public float smoothingSpeed = 24f;

    [Tooltip("Rotation slerp speed")]
    public float rotationSpeed = 24f;

    // Public info
    public string SessionId { get; private set; }
    public string PlayerName { get; private set; }
    public Color PlayerColor { get; private set; }

    // --- Snapshot buffer ---
    private struct Snapshot
    {
        public double serverTime;  // server timestamp (ms)
        public float localTime;    // Time.time when received
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;
        public Vector3 angularVelocity;
    }

    private const int BUFFER_SIZE = 16;
    private readonly Snapshot[] _buffer = new Snapshot[BUFFER_SIZE];
    private int _bufferCount;
    private int _newestIndex;
    private float _firstLocalTime; // local time of first snapshot received
    private double _firstServerTime; // server time of first snapshot received
    private bool _initialized;
    private Quaternion _currentRotation = Quaternion.identity;
    private Rigidbody _rb; // Cached for MovePosition

    // Optional: floating name label
    private TextMesh _nameLabel;

    /// <summary>
    /// Called by NetworkManager when spawning this remote player.
    /// </summary>
    public void Initialize(string sessionId, string playerName, Color color)
    {
        SessionId = sessionId;
        PlayerName = playerName;
        PlayerColor = color;
        _currentRotation = transform.rotation;
        _bufferCount = 0;
        _initialized = true;

        // Apply color tint (multiply blend to keep pattern visible)
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(renderer.sharedMaterial);
            Color original = Color.white;
            if (mat.HasProperty("_BaseColor"))  original = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) original = mat.GetColor("_Color");

            float strength = 0.7f;
            Color tint = new Color(
                Mathf.Lerp(original.r, original.r * color.r * 2f, strength),
                Mathf.Lerp(original.g, original.g * color.g * 2f, strength),
                Mathf.Lerp(original.b, original.b * color.b * 2f, strength),
                original.a
            );

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))     mat.color = tint;
            renderer.material = mat;
        }

        // Kinematic rigidbody with MovePosition for proper collision detection
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        // Add a trigger collider slightly larger than the physics collider
        // so the local player can detect bumps
        var existingCollider = GetComponent<SphereCollider>();
        float baseRadius = existingCollider != null ? existingCollider.radius : 0.5f;
        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = baseRadius * 1.15f; // 15% larger

        // Disable any player input on remote balls
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = false;

        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        // Create floating name label
        CreateNameLabel();

        Debug.Log($"[RemotePlayer] Initialized: {playerName} ({sessionId[..6]}) color={color}");
    }

    /// <summary>
    /// Called by NetworkManager when a state update arrives from the server.
    /// Pushes a new snapshot into the interpolation buffer.
    /// </summary>
    public void SetTargetState(Vector3 position, Vector3 velocity, Quaternion rotation, double serverTime, Vector3 angularVelocity = default)
    {
        // Bootstrap time mapping on first snapshot
        if (_bufferCount == 0)
        {
            _firstLocalTime = Time.time;
            _firstServerTime = serverTime;
        }

        // Advance ring buffer
        _newestIndex = (_newestIndex + 1) % BUFFER_SIZE;
        _buffer[_newestIndex] = new Snapshot
        {
            serverTime = serverTime,
            localTime = Time.time,
            position = position,
            velocity = velocity,
            rotation = rotation,
            angularVelocity = angularVelocity
        };
        if (_bufferCount < BUFFER_SIZE) _bufferCount++;
    }

    void Update()
    {
        if (!_initialized || _bufferCount == 0) return;

        // Render time = current time minus interpolation delay
        float renderTime = Time.time - interpolationDelay;

        // Build a sorted view of the buffer (oldest → newest by localTime)
        // to safely find the two bracketing snapshots
        int oldestIdx = (_newestIndex - _bufferCount + 1 + BUFFER_SIZE) % BUFFER_SIZE;

        Snapshot older = default;
        Snapshot newer = default;
        bool found = false;

        for (int i = 0; i < _bufferCount - 1; i++)
        {
            int idxA = (oldestIdx + i) % BUFFER_SIZE;
            int idxB = (oldestIdx + i + 1) % BUFFER_SIZE;
            if (_buffer[idxA].localTime <= renderTime && _buffer[idxB].localTime >= renderTime)
            {
                older = _buffer[idxA];
                newer = _buffer[idxB];
                found = true;
                break;
            }
        }

        Vector3 targetPos;
        Quaternion targetRot;

        if (found)
        {
            // Interpolate between the two bounding snapshots
            float span = newer.localTime - older.localTime;
            float t = span > 0.001f ? (renderTime - older.localTime) / span : 1f;
            t = Mathf.Clamp01(t);

            targetPos = Vector3.Lerp(older.position, newer.position, t);
            targetRot = Quaternion.Slerp(older.rotation, newer.rotation, t);
        }
        else
        {
            // No bracketing pair found
            var newest = _buffer[_newestIndex];
            float elapsed = renderTime - newest.localTime;

            if (elapsed < 0)
            {
                // Render time is earlier than all snapshots — use oldest, don't extrapolate backwards
                targetPos = _buffer[oldestIdx].position;
                targetRot = _buffer[oldestIdx].rotation;
            }
            else
            {
                // Extrapolate forward from newest, but with velocity damping
                float extTime = Mathf.Min(elapsed, maxExtrapolation);
                float dampFactor = 1f - Mathf.Clamp01(elapsed / (maxExtrapolation * 2f)); // fade to 0
                targetPos = newest.position + newest.velocity * extTime * dampFactor;
                targetRot = newest.rotation;
            }
        }

        // Final smoothing layer: lerp from current position toward computed target
        float dist = Vector3.Distance(transform.position, targetPos);
        Vector3 newPos;
        if (dist > snapDistance)
        {
            // Teleport for large distances (spawn, reconnect)
            newPos = targetPos;
            _currentRotation = targetRot;
        }
        else
        {
            float lerpT = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            newPos = Vector3.Lerp(transform.position, targetPos, lerpT);
        }

        // Smooth rotation
        float rotLerpT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
        _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, rotLerpT);

        // Use MovePosition/MoveRotation for proper collision detection
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
            _nameLabelObj.transform.position = transform.position + Vector3.up * 1.5f;
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

    private GameObject _nameLabelObj; // Keep reference for billboard update

    private void CreateNameLabel()
    {
        GameObject labelObj = new GameObject("NameLabel");
        // Do NOT parent to transform — ball rotation would spin the label
        labelObj.transform.position = transform.position + Vector3.up * 1.5f;
        labelObj.transform.localScale = Vector3.one * 0.1f;
        _nameLabelObj = labelObj;

        _nameLabel = labelObj.AddComponent<TextMesh>();
        _nameLabel.text = PlayerName;
        _nameLabel.fontSize = 144;
        _nameLabel.characterSize = 0.15f;
        _nameLabel.anchor = TextAnchor.MiddleCenter;
        _nameLabel.alignment = TextAlignment.Center;
        _nameLabel.color = Color.white;
        var font = PlayerController.LabelFont;
        if (font != null) _nameLabel.font = font;

        var meshRenderer = _nameLabel.GetComponent<MeshRenderer>();
        if (font != null && font.material != null)
            meshRenderer.material = font.material;
        else
        {
            var textShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Texture");
            if (textShader != null) meshRenderer.material = new Material(textShader);
        }
    }

    /// <summary>Called by NetworkManager when the player's team or color changes (e.g. teams mode).</summary>
    public void UpdateTeamColor(int team, Color serverColor)
    {
        // Only re-tint if the color actually changed significantly
        if (PlayerColor == serverColor) return;
        PlayerColor = serverColor;

        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        var mat = renderer.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", serverColor);
        else mat.color = serverColor;

        // Update name label color to match team
        if (_nameLabel != null)
        {
            _nameLabel.color = team == 1
                ? new Color(1f, 0.5f, 0.5f)
                : team == 2
                    ? new Color(0.5f, 0.7f, 1f)
                    : Color.white;
        }
    }

    /// <summary>Show or hide this remote player (used when eliminated).</summary>
    public void SetVisible(bool visible)
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = visible;
        if (_nameLabelObj != null) _nameLabelObj.SetActive(visible);

        // Disable physics interactions when hidden
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = visible;
    }

    void OnDestroy()
    {
        if (_nameLabelObj != null) Destroy(_nameLabelObj);
        Debug.Log($"[RemotePlayer] Destroyed: {PlayerName}");
    }
}
