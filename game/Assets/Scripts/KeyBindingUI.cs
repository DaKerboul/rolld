using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime key rebinding UI. Toggle with F2.
/// Manages rebinding for InputSystem actions (movement, jump)
/// and custom camera orbit keys (ZQSD).
/// All overrides persist in PlayerPrefs.
/// </summary>
public class KeyBindingUI : MonoBehaviour
{
    private bool _visible = false;
    private string _rebindingAction = null;

    /// <summary>True when the keybind config menu is open. Used to freeze camera + unlock cursor.</summary>
    public static bool IsVisible { get; private set; }
    private InputActionRebindingExtensions.RebindingOperation _rebindOp;

    // Default camera keys (physical WASD positions = ZQSD on AZERTY)
    private static readonly Dictionary<string, Key> _defaultCameraKeys = new()
    {
        { "CamUp",    Key.W },
        { "CamDown",  Key.S },
        { "CamLeft",  Key.A },
        { "CamRight", Key.D },
    };

    private static readonly string[] _cameraKeyNames = { "CamUp", "CamDown", "CamLeft", "CamRight" };
    private static readonly string[] _cameraKeyLabels = { "Caméra haut", "Caméra bas", "Caméra gauche", "Caméra droite" };

    private static Dictionary<string, Key> _currentCameraKeys;

    private PlayerInput _playerInput;

    void Awake()
    {
        LoadCameraKeys();
    }

    void Start()
    {
        _playerInput = FindFirstObjectByType<PlayerInput>();
        LoadBindingOverrides();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[Key.F2].wasPressedThisFrame)
        {
            _visible = !_visible;
            IsVisible = _visible;
            if (_visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                CancelRebind();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void LateUpdate()
    {
        // Process camera key rebinding
        if (_rebindingAction == null) return;
        if (!_defaultCameraKeys.ContainsKey(_rebindingAction)) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        foreach (Key k in Enum.GetValues(typeof(Key)))
        {
            if (k == Key.None) continue;
            try
            {
                if (kb[k].wasPressedThisFrame)
                {
                    if (k == Key.Escape)
                    {
                        _rebindingAction = null;
                        return;
                    }
                    _currentCameraKeys[_rebindingAction] = k;
                    SaveCameraKeys();
                    _rebindingAction = null;
                    return;
                }
            }
            catch { }
        }
    }

    void OnGUI()
    {
        if (!_visible) return;

        ImGuiSkin.EnsureReady();

        // Overlay
        ImGuiSkin.DrawOverlay();

        float w = 480, h = 540;

        ImGuiSkin.BeginWindow(w, h, "Configuration des touches");

        // --- Movement ---
        ImGuiSkin.DrawSectionHeader("DÉPLACEMENT");
        GUILayout.Space(4);
        DrawActionBinding("forward",   "Avancer");
        DrawActionBinding("backwards", "Reculer");
        DrawActionBinding("left",      "Gauche");
        DrawActionBinding("right",     "Droite");
        DrawActionBinding("jump",      "Sauter");

        GUILayout.Space(10);

        // --- Camera ---
        ImGuiSkin.DrawSectionHeader("CAMÉRA (+ SOURIS)");
        GUILayout.Space(4);
        for (int i = 0; i < _cameraKeyNames.Length; i++)
            DrawCameraKeyBinding(_cameraKeyNames[i], _cameraKeyLabels[i]);

        GUILayout.Space(16);

        // Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Réinitialiser tout", ImGuiSkin.Button, GUILayout.Height(32)))
            ResetAllBindings();
        if (GUILayout.Button("Fermer (F2)", ImGuiSkin.Button, GUILayout.Height(32)))
        {
            _visible = false;
            CancelRebind();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        if (_rebindingAction != null)
        {
            GUILayout.Label("Appuyez sur une touche pour assigner...", ImGuiSkin.Hint);
        }

        // F2 hint
        GUILayout.Label("F2 — Ouvrir / Fermer ce menu", ImGuiSkin.Footer);

        ImGuiSkin.EndWindow();
    }

    // --- Drawing helpers ---

    private void DrawActionBinding(string actionName, string label)
    {
        if (_playerInput == null) return;
        var action = _playerInput.actions.FindAction(actionName);
        if (action == null) return;

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, ImGuiSkin.Label, GUILayout.Width(150));

        string currentBinding = "---";
        if (action.bindings.Count > 0)
            currentBinding = InputControlPath.ToHumanReadableString(
                action.bindings[0].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);

        bool isRebinding = _rebindingAction == actionName;
        string btnText = isRebinding ? "[ ... ]" : currentBinding;

        GUIStyle btnStyle = isRebinding ? ImGuiSkin.ButtonAccent : ImGuiSkin.ButtonSmall;
        if (GUILayout.Button(btnText, btnStyle, GUILayout.Width(170), GUILayout.Height(26)))
        {
            if (!isRebinding) StartActionRebind(actionName);
            else CancelRebind();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawCameraKeyBinding(string keyName, string label)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, ImGuiSkin.Label, GUILayout.Width(150));

        Key current = GetKey(keyName, _defaultCameraKeys[keyName]);
        bool isRebinding = _rebindingAction == keyName;
        string btnText = isRebinding ? "[ ... ]" : current.ToString();

        GUIStyle btnStyle = isRebinding ? ImGuiSkin.ButtonAccent : ImGuiSkin.ButtonSmall;
        if (GUILayout.Button(btnText, btnStyle, GUILayout.Width(170), GUILayout.Height(26)))
        {
            if (!isRebinding) StartCameraKeyRebind(keyName);
            else CancelRebind();
        }
        GUILayout.EndHorizontal();
    }

    // --- Rebinding logic ---

    private void StartActionRebind(string actionName)
    {
        CancelRebind();
        var action = _playerInput?.actions.FindAction(actionName);
        if (action == null) return;

        _rebindingAction = actionName;
        action.Disable();

        _rebindOp = action.PerformInteractiveRebinding(0)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                action.Enable();
                SaveBindingOverrides();
                _rebindingAction = null;
                op.Dispose();
                _rebindOp = null;
            })
            .OnCancel(op =>
            {
                action.Enable();
                _rebindingAction = null;
                op.Dispose();
                _rebindOp = null;
            })
            .Start();
    }

    private void StartCameraKeyRebind(string keyName)
    {
        CancelRebind();
        _rebindingAction = keyName;
    }

    private void CancelRebind()
    {
        if (_rebindOp != null)
        {
            _rebindOp.Cancel();
            _rebindOp.Dispose();
            _rebindOp = null;
        }
        _rebindingAction = null;
    }

    // --- Persistence ---

    private void SaveBindingOverrides()
    {
        if (_playerInput == null) return;
        string json = _playerInput.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputOverrides", json);
        PlayerPrefs.Save();
    }

    private void LoadBindingOverrides()
    {
        if (_playerInput == null) return;
        string json = PlayerPrefs.GetString("InputOverrides", "");
        if (!string.IsNullOrEmpty(json))
            _playerInput.actions.LoadBindingOverridesFromJson(json);
    }

    private static void LoadCameraKeys()
    {
        _currentCameraKeys = new Dictionary<string, Key>();
        foreach (var kv in _defaultCameraKeys)
        {
            string saved = PlayerPrefs.GetString($"CamKey_{kv.Key}", "");
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse<Key>(saved, out Key parsed))
                _currentCameraKeys[kv.Key] = parsed;
            else
                _currentCameraKeys[kv.Key] = kv.Value;
        }
    }

    private static void SaveCameraKeys()
    {
        if (_currentCameraKeys == null) return;
        foreach (var kv in _currentCameraKeys)
            PlayerPrefs.SetString($"CamKey_{kv.Key}", kv.Value.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get a camera key binding. Usable globally (static).
    /// </summary>
    public static Key GetKey(string name, Key fallback)
    {
        if (_currentCameraKeys == null) LoadCameraKeys();
        return _currentCameraKeys != null && _currentCameraKeys.TryGetValue(name, out Key k) ? k : fallback;
    }

    private void ResetAllBindings()
    {
        // Reset InputSystem actions
        if (_playerInput != null)
        {
            foreach (var action in _playerInput.actions)
                action.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey("InputOverrides");
        }

        // Reset camera keys
        _currentCameraKeys = new Dictionary<string, Key>(_defaultCameraKeys);
        foreach (var kv in _defaultCameraKeys)
            PlayerPrefs.DeleteKey($"CamKey_{kv.Key}");

        PlayerPrefs.Save();
    }

}
