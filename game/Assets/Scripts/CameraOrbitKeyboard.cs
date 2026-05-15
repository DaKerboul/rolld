using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Orbit camera via mouse delta direct (bypass CinemachineInputAxisController)
/// + keyboard fallback. Right-click toggles cursor lock.
/// Active uniquement quand le Player est actif (gameplay).
/// </summary>
public class CameraOrbitKeyboard : MonoBehaviour
{
    [Header("Keyboard Orbit Speed (deg/s)")]
    public float horizontalSpeed = 150f;
    public float verticalSpeed   = 80f;

    [Header("Mouse Sensitivity (deg/px)")]
    public float mouseSensitivity = 0.2f;

    private CinemachineOrbitalFollow       _orbital;
    private CinemachineInputAxisController _axisController;

    void Awake()
    {
        _orbital        = GetComponent<CinemachineOrbitalFollow>();
        _axisController = GetComponent<CinemachineInputAxisController>();
    }

    void OnEnable()
    {
        // On gère la souris nous-mêmes
        if (_axisController != null) _axisController.enabled = false;
        LockCursor();
    }

    void OnDisable()
    {
        UnlockCursor();
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void Update()
    {
        if (_orbital == null) return;

        var mouse = Mouse.current;

        // Clic droit = toggle lock
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                UnlockCursor();
            else
                LockCursor();
        }

        if (KeyBindingUI.IsVisible) return;

        // Souris — seulement quand locked (delta infini, sans accrochage au bord)
        if (Cursor.lockState == CursorLockMode.Locked && mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            _orbital.HorizontalAxis.Value += delta.x * mouseSensitivity;
            _orbital.VerticalAxis.Value    = Mathf.Clamp(
                _orbital.VerticalAxis.Value - delta.y * mouseSensitivity,
                _orbital.VerticalAxis.Range.x,
                _orbital.VerticalAxis.Range.y
            );
        }

        // Clavier
        var kb = Keyboard.current;
        if (kb == null) return;

        Key kUp    = KeyBindingUI.GetKey("CamUp",    Key.W);
        Key kDown  = KeyBindingUI.GetKey("CamDown",  Key.S);
        Key kLeft  = KeyBindingUI.GetKey("CamLeft",  Key.A);
        Key kRight = KeyBindingUI.GetKey("CamRight", Key.D);

        float h = 0f, v = 0f;
        if (kb[kRight].isPressed) h += 1f;
        if (kb[kLeft].isPressed)  h -= 1f;
        if (kb[kUp].isPressed)    v += 1f;
        if (kb[kDown].isPressed)  v -= 1f;

        if (h != 0f || v != 0f)
        {
            _orbital.HorizontalAxis.Value += h * horizontalSpeed * Time.deltaTime;
            _orbital.VerticalAxis.Value    = Mathf.Clamp(
                _orbital.VerticalAxis.Value + v * verticalSpeed * Time.deltaTime,
                _orbital.VerticalAxis.Range.x,
                _orbital.VerticalAxis.Range.y
            );
        }
    }
}
