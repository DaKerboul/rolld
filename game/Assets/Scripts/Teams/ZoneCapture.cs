using UnityEngine;

/// <summary>
/// Teams mode: a central capture zone. The local player sends "inZone" messages
/// to the server while inside the zone. The server tallies scores.
/// Visualizes zone control by tinting the zone's renderer.
///
/// Setup: Create a flat Box/Plane in the center of the arena.
/// Add a BoxCollider set to "Is Trigger". Attach this script.
/// </summary>
public class ZoneCapture : MonoBehaviour
{
    public static ZoneCapture Instance { get; private set; }

    [Header("Visual")]
    [Tooltip("Neutral zone color")]
    public Color neutralColor = new Color(0.5f, 0.5f, 0.6f, 0.5f);
    [Tooltip("Red team controls color")]
    public Color redColor = new Color(1f, 0.2f, 0.2f, 0.6f);
    [Tooltip("Blue team controls color")]
    public Color blueColor = new Color(0.2f, 0.5f, 1f, 0.6f);

    [Header("Score Reporting")]
    [Tooltip("How often (seconds) to send inZone=true to server while inside")]
    public float reportInterval = 0.5f;

    private bool _isLocalPlayerInZone = false;
    private float _reportTimer = 0f;
    private Renderer _renderer;

    // Zone occupant counts (received via server state — approximated by remote player positions)
    private int _redInZone = 0;
    private int _blueInZone = 0;

    void Awake()
    {
        Instance = this;
        _renderer = GetComponent<Renderer>();
        SetZoneColor(neutralColor);
    }

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(string phase)
    {
        if (phase == "lobby" || phase == "roundEnd")
        {
            _isLocalPlayerInZone = false;
            _redInZone = 0;
            _blueInZone = 0;
            SetZoneColor(neutralColor);
        }
    }

    void Update()
    {
        if (!_isLocalPlayerInZone) return;

        _reportTimer += Time.deltaTime;
        if (_reportTimer >= reportInterval)
        {
            _reportTimer = 0f;
            NetworkManager.Instance?.SendInZone(true);
        }

        // Update zone tint based on dominance
        UpdateZoneColor();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            _isLocalPlayerInZone = true;
            _reportTimer = 0f;
            NetworkManager.Instance?.SendInZone(true);
        }

        // Count remote players in zone
        var remote = other.GetComponent<RemotePlayerController>();
        if (remote != null)
        {
            // team info would need to be tracked — skip for now, server handles scoring
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            _isLocalPlayerInZone = false;
            NetworkManager.Instance?.SendInZone(false);
        }
    }

    private void UpdateZoneColor()
    {
        // Read team scores from NetworkManager state for visual feedback
        // For now use a pulsing neutral tint when local player is inside
        float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 3f);
        Color c = neutralColor;
        c.a = pulse * 0.6f;
        SetZoneColor(c);
    }

    public void SetZoneColor(Color c)
    {
        if (_renderer == null) return;
        var mat = _renderer.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else mat.color = c;
    }
}
