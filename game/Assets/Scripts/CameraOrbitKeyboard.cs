using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Adds ZQSD (AZERTY) / WASD (QWERTY) keyboard orbit for the Cinemachine camera.
/// Works in parallel with mouse orbit via CinemachineInputAxisController.
/// Attach to the CinemachineCamera GameObject alongside CinemachineOrbitalFollow.
/// </summary>
public class CameraOrbitKeyboard : MonoBehaviour
{
    [Header("Orbit Speed (degrees/sec)")]
    public float horizontalSpeed = 150f;
    public float verticalSpeed = 80f;

    private CinemachineOrbitalFollow _orbital;
    private CinemachineInputAxisController _axisController;

    void Start()
    {
        _orbital = GetComponent<CinemachineOrbitalFollow>();
        _axisController = GetComponent<CinemachineInputAxisController>();
        if (_orbital == null)
            Debug.LogWarning("[CameraOrbitKeyboard] CinemachineOrbitalFollow not found on this GameObject.");
    }

    void Update()
    {
        if (_orbital == null) return;

        // Enforce cursor lock while this script is active (Player hierarchy is active = in gameplay)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Freeze camera orbit (keyboard + mouse) when keybind menu is open
        if (KeyBindingUI.IsVisible)
        {
            if (_axisController != null && _axisController.enabled)
                _axisController.enabled = false;
            return;
        }
        else if (_axisController != null && !_axisController.enabled)
        {
            _axisController.enabled = true;
        }
        var kb = Keyboard.current;
        if (kb == null) return;

        // Physical-key mapping: W/A/S/D positions = Z/Q/S/D on AZERTY
        Key kUp    = KeyBindingUI.GetKey("CamUp",    Key.W);
        Key kDown  = KeyBindingUI.GetKey("CamDown",  Key.S);
        Key kLeft  = KeyBindingUI.GetKey("CamLeft",  Key.A);
        Key kRight = KeyBindingUI.GetKey("CamRight", Key.D);

        float h = 0f, v = 0f;
        if (kb[kRight].isPressed) h += 1f;
        if (kb[kLeft].isPressed)  h -= 1f;
        if (kb[kUp].isPressed)    v += 1f;
        if (kb[kDown].isPressed)  v -= 1f;

        if (Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f)
        {
            _orbital.HorizontalAxis.Value += h * horizontalSpeed * Time.deltaTime;
            _orbital.VerticalAxis.Value = Mathf.Clamp(
                _orbital.VerticalAxis.Value + v * verticalSpeed * Time.deltaTime,
                _orbital.VerticalAxis.Range.x,
                _orbital.VerticalAxis.Range.y
            );
        }
    }
}
