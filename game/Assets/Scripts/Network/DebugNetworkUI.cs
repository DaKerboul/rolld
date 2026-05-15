using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug overlay:
///   – Always-visible HUD strip at the top (player name, status, room, FPS)
///   – Detailed panel toggled with F1 (full network + physics info)
/// Uses Dear ImGui–style skin via ImGuiSkin.
/// </summary>
public class DebugNetworkUI : MonoBehaviour
{
    private bool _detailsVisible = false;
    private Vector2 _scrollPos;

    // FPS tracking
    private float _fpsTimer;
    private int _fpsCount;
    private float _currentFps;

    void Update()
    {
        // FPS counter
        _fpsTimer += Time.unscaledDeltaTime;
        _fpsCount++;
        if (_fpsTimer >= 0.5f)
        {
            _currentFps = _fpsCount / _fpsTimer;
            _fpsTimer = 0f;
            _fpsCount = 0;
        }

        // Toggle detailed panel with F1
        if (Keyboard.current != null && Keyboard.current[Key.F1].wasPressedThisFrame)
            _detailsVisible = !_detailsVisible;
    }

    void OnGUI()
    {
        ImGuiSkin.EnsureReady();

        var nm = NetworkManager.Instance;
        if (nm == null) return;

        DrawHUDStrip(nm);

        if (_detailsVisible)
            DrawDetailPanel(nm);

        // Hint
        GUI.Label(new Rect(10, Screen.height - 25, 300, 20), "F1 — Debug details", ImGuiSkin.Footer);
    }

    // ───────── HUD Strip (always visible) ─────────

    private void DrawHUDStrip(NetworkManager nm)
    {
        float h = 28;
        ImGuiSkin.DrawHudStripBg(h);

        string dot = nm.IsConnected
            ? "<color=#44FF44>\u25CF</color>"
            : "<color=#FF4444>\u25CF</color>";

        string info;
        if (nm.IsConnected)
        {
            string name  = !string.IsNullOrEmpty(nm.LocalPlayerName) ? nm.LocalPlayerName : "\u2014";
            string room  = !string.IsNullOrEmpty(nm.RoomId) ? nm.RoomId[..Mathf.Min(8, nm.RoomId.Length)] : "\u2014";
            string sess  = !string.IsNullOrEmpty(nm.LocalSessionId) ? nm.LocalSessionId[..Mathf.Min(6, nm.LocalSessionId.Length)] : "\u2014";
            info = $"  {dot} <b>{name}</b>  |  Room {room}  |  Sess {sess}  |  {nm.PlayerCount}P  |  {"wss://game.rolld.kerboul.me"}  |  {_currentFps:F0} FPS";
        }
        else
        {
            info = $"  {dot} {nm.ConnectionStatus}  |  {"wss://game.rolld.kerboul.me"}  |  {_currentFps:F0} FPS";
        }

        GUI.Label(new Rect(0, 0, Screen.width, h), info, ImGuiSkin.HudLabel);
    }

    // ───────── Detail Panel (F1) ─────────

    private void DrawDetailPanel(NetworkManager nm)
    {
        float w = 360, h = 480;
        float x = Screen.width - w - 12;
        float y = 38;

        ImGuiSkin.BeginWindowAt(x, y, w, h, "Network Debug");

        // ── Connection ──
        ImGuiSkin.DrawSectionHeader("CONNECTION");
        GUILayout.Space(2);
        GUIStyle statusStyle = nm.IsConnected ? ImGuiSkin.StatusGreen : ImGuiSkin.StatusRed;
        GUILayout.Label($"\u25CF {nm.ConnectionStatus}", statusStyle);

        ImGuiSkin.DrawField("Server",  "wss://game.rolld.kerboul.me");
        ImGuiSkin.DrawField("Room ID", string.IsNullOrEmpty(nm.RoomId) ? "\u2014" : nm.RoomId);
        ImGuiSkin.DrawField("Session", string.IsNullOrEmpty(nm.LocalSessionId) ? "\u2014" : nm.LocalSessionId);
        ImGuiSkin.DrawField("Players", nm.PlayerCount.ToString());
        ImGuiSkin.DrawField("FPS",     $"{_currentFps:F0}");

        if (!string.IsNullOrEmpty(nm.LastError))
        {
            GUILayout.Space(2);
            GUILayout.Label($"\u26A0 {nm.LastError}", ImGuiSkin.StatusRed);
        }

        GUILayout.Space(6);

        // ── Local Player ──
        ImGuiSkin.DrawSectionHeader("LOCAL PLAYER");
        GUILayout.Space(2);
        ImGuiSkin.DrawField("Name", string.IsNullOrEmpty(nm.LocalPlayerName) ? "\u2014" : nm.LocalPlayerName);

        var state = nm.GetLocalPlayerState();
        if (state != null)
            ImGuiSkin.DrawField("Server Pos", $"({state.x:F1}, {state.y:F1}, {state.z:F1})");

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null && pc.isActiveAndEnabled)
        {
            var pos = pc.transform.position;
            ImGuiSkin.DrawField("Live Pos", $"({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            var rb = pc.GetComponent<Rigidbody>();
            if (rb != null)
            {
                var v = rb.linearVelocity;
                ImGuiSkin.DrawField("Velocity", $"({v.x:F1}, {v.y:F1}, {v.z:F1})  [{v.magnitude:F1} m/s]");
            }
        }

        GUILayout.Space(6);

        // ── Remote Players ──
        ImGuiSkin.DrawSectionHeader("REMOTE PLAYERS");
        GUILayout.Space(2);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, ImGuiSkin.ScrollView, GUILayout.Height(100));

        if (nm.RemotePlayers != null && nm.RemotePlayers.Count > 0)
        {
            foreach (var kvp in nm.RemotePlayers)
            {
                if (kvp.Value == null) continue;
                var rp = kvp.Value;
                string dist = "";
                if (pc != null && pc.isActiveAndEnabled)
                {
                    float d = Vector3.Distance(pc.transform.position, rp.transform.position);
                    dist = $" [{d:F1}m]";
                }
                GUILayout.Label($"  {rp.PlayerName} ({kvp.Key[..Mathf.Min(6, kvp.Key.Length)]}){dist}", ImGuiSkin.Label);
            }
        }
        else
        {
            GUILayout.Label("  (aucun joueur distant)", ImGuiSkin.LabelDim);
        }

        GUILayout.EndScrollView();
        GUILayout.FlexibleSpace();

        if (nm.IsConnected)
        {
            if (GUILayout.Button("Déconnecter", ImGuiSkin.Button, GUILayout.Height(28)))
                nm.LeaveRoom();
        }

        ImGuiSkin.EndWindow();
    }

}
