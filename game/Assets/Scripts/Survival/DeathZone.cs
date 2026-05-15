using UnityEngine;

/// <summary>
/// Survival mode: a rising death zone that climbs from below.
/// The server is authoritative on the Y position (broadcast via NetworkState.deathZoneY).
/// This component moves the visual/collider locally, and detects local player contact.
///
/// Setup: Create a large Plane or Cube GameObject, attach this script,
/// add a Box/Mesh Collider set to "Is Trigger".
/// The object will be positioned at deathZoneY each frame.
/// </summary>
public class DeathZone : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Half-size of the death zone plane (X and Z)")]
    public float halfExtent = 200f;

    [Tooltip("Thickness of the zone collider")]
    public float thickness = 2f;

    [Header("Warning")]
    [Tooltip("Distance above death zone where the red tint starts")]
    public float warningDistance = 8f;

    private bool _hitSent = false;
    private float _targetY = -100f;

    void Start()
    {
        // Scale the collider to cover the arena
        transform.localScale = new Vector3(halfExtent * 2f, thickness, halfExtent * 2f);

        // Subscribe to death zone Y changes from server
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnDeathZoneYChanged += OnDeathZoneYChanged;
            NetworkManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnDeathZoneYChanged -= OnDeathZoneYChanged;
            NetworkManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    void OnDeathZoneYChanged(float y)
    {
        _targetY = y;
    }

    void OnPhaseChanged(string phase)
    {
        if (phase == "playing" || phase == "lobby")
        {
            _hitSent = false; // reset for new round
        }
    }

    void Update()
    {
        // Smooth follow of the server Y value
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * 3f);
        transform.position = pos;

        // Update warning tint based on local player proximity
        var nm = NetworkManager.Instance;
        if (nm != null && nm.IsConnected && GameHUD.Instance != null)
        {
            var localState = nm.GetLocalPlayerState();
            if (localState != null)
            {
                float dist = localState.y - pos.y;
                float intensity = Mathf.Clamp01(1f - dist / warningDistance);
                GameHUD.Instance.SetDeathZoneWarning(intensity);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hitSent) return;
        if (other.GetComponent<PlayerController>() == null) return;

        _hitSent = true;
        Debug.Log("[DeathZone] Local player hit the death zone!");
        NetworkManager.Instance?.SendDeathZoneHit();
    }
}
