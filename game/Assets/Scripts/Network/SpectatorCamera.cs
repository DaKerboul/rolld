using UnityEngine;

/// <summary>
/// Spectator camera that slowly orbits around the arena center while the player
/// is in the lobby (not yet connected). Automatically disables itself and yields
/// to the gameplay Cinemachine camera once the player joins.
/// Attach to a dedicated GameObject with a Camera component.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SpectatorCamera : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Tooltip("World-space point the camera orbits around")]
    public Vector3 orbitCenter = Vector3.zero;

    [Tooltip("Radius of the orbit circle")]
    public float orbitRadius = 30f;

    [Tooltip("Height above the orbit center")]
    public float orbitHeight = 18f;

    [Tooltip("Degrees per second")]
    public float orbitSpeed = 12f;

    [Tooltip("Downward pitch angle in degrees")]
    public float pitchAngle = 30f;

    // Internal
    private float _angle;
    private Camera _cam;

    // Reference to the gameplay camera (CinemachineBrain) — set by LobbyUI
    [HideInInspector] public Camera gameplayCamera;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _angle = Random.Range(0f, 360f); // start at random angle for variety
    }

    void LateUpdate()
    {
        _angle += orbitSpeed * Time.deltaTime;
        if (_angle >= 360f) _angle -= 360f;

        float rad = _angle * Mathf.Deg2Rad;
        Vector3 pos = orbitCenter + new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            orbitHeight,
            Mathf.Sin(rad) * orbitRadius
        );

        transform.position = pos;
        transform.LookAt(orbitCenter + Vector3.up * 2f);
    }

    /// <summary>
    /// Switch to spectator view — enable this camera, disable gameplay camera.
    /// </summary>
    public void Activate()
    {
        _cam.enabled = true;
        gameObject.SetActive(true);

        // Disable the gameplay camera so we're the active one
        if (gameplayCamera != null)
            gameplayCamera.enabled = false;
    }

    /// <summary>
    /// Switch back to gameplay view — disable this camera, enable gameplay camera.
    /// </summary>
    public void Deactivate()
    {
        _cam.enabled = false;
        gameObject.SetActive(false);

        // Re-enable the gameplay camera
        if (gameplayCamera != null)
            gameplayCamera.enabled = true;
    }
}
