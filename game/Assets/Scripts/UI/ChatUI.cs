using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

/// <summary>
/// General chat panel. Toggle with F3.
/// Polls GET /chat/history every 3s and sends via POST /chat/send (or Colyseus if connected).
/// Uses ImGuiSkin for visual consistency.
/// </summary>
public class ChatUI : MonoBehaviour
{
    public static ChatUI Instance { get; private set; }
    public static bool IsVisible { get; private set; }

    private const string SERVER_URL = "https://game.rolld.kerboul.me";
    private const float POLL_INTERVAL = 3f;
    private const int MAX_DISPLAY = 50;

    private bool _visible;
    private string _inputText = "";
    private Vector2 _scrollPos;
    private float _pollTimer;
    private long _lastTimestamp;
    private bool _autoScroll = true;

    private readonly List<ChatMessage> _messages = new();
    private int _unreadCount;

    // Cached textures for badge
    private static Texture2D _badgeTex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Initial load
        StartCoroutine(DoPoll());
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[Key.F3].wasPressedThisFrame)
            Toggle();

        if (_visible)
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer >= POLL_INTERVAL) { _pollTimer = 0f; StartCoroutine(DoPoll()); }

            // Send on Enter (only when chat input has text)
            if (!string.IsNullOrWhiteSpace(_inputText) &&
                Keyboard.current != null && Keyboard.current[Key.Enter].wasPressedThisFrame)
            {
                TrySend();
            }
        }
    }

    private void Toggle()
    {
        _visible = !_visible;
        IsVisible = _visible;

        if (_visible)
        {
            _unreadCount = 0;
            _autoScroll = true;
            _pollTimer = POLL_INTERVAL; // poll immediately
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Only re-lock if no other UI is open
            if (!KeyBindingUI.IsVisible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void OnGUI()
    {
        if (!_visible)
        {
            DrawBadge();
            return;
        }

        ImGuiSkin.EnsureReady();

        // Panel bottom-right, doesn't obstruct the center
        float w = 460f;
        float h = 440f;
        float x = Screen.width - w - 12f;
        float y = Screen.height - h - 12f;

        ImGuiSkin.BeginWindowAt(x, y, w, h, "CHAT GÉNÉRAL");

        // ── Message history ───────────────────────────────────────────
        float listH = h - 130f;
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, ImGuiSkin.ScrollView, GUILayout.Height(listH));

        foreach (var msg in _messages)
        {
            var ts = System.DateTimeOffset.FromUnixTimeMilliseconds(msg.timestamp).ToLocalTime();
            string timeStr = ts.ToString("HH:mm");

            var timeStyle = new GUIStyle(ImGuiSkin.LabelDim) { fontSize = 10, fixedWidth = 36f };
            var nameStyle = new GUIStyle(ImGuiSkin.LabelBold);
            nameStyle.normal.textColor = ImGuiSkin.ColAccent;
            var textStyle = new GUIStyle(ImGuiSkin.LabelRich);

            GUILayout.BeginHorizontal();
            GUILayout.Label(timeStr, timeStyle);
            GUILayout.Label(msg.name + " :", nameStyle, GUILayout.Width(100f));
            GUILayout.Label(msg.text, textStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(1f);
        }

        if (_autoScroll) _scrollPos.y = float.MaxValue;
        GUILayout.EndScrollView();

        ImGuiSkin.Separator();
        GUILayout.Space(4f);

        // ── Input row ────────────────────────────────────────────────
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("ChatInput");
        _inputText = GUILayout.TextField(_inputText, 200, ImGuiSkin.TextField, GUILayout.Height(28f));

        bool canSend = !string.IsNullOrWhiteSpace(_inputText) && PlayerName.Length > 0;
        GUI.enabled = canSend;
        if (GUILayout.Button("Envoyer", ImGuiSkin.Button, GUILayout.Width(80f), GUILayout.Height(28f)))
            TrySend();
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.Label("F3 — Ouvrir / Fermer  ·  Entrée — Envoyer", ImGuiSkin.Footer);

        ImGuiSkin.EndWindow();

        // Auto-focus input field
        GUI.FocusControl("ChatInput");
    }

    private void DrawBadge()
    {
        if (_unreadCount <= 0) return;
        if (_badgeTex == null)
        {
            _badgeTex = new Texture2D(1, 1);
            _badgeTex.SetPixel(0, 0, Color.white);
            _badgeTex.Apply();
        }
        float bx = Screen.width - 68f;
        float by = Screen.height - 32f;
        GUI.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(bx, by, 56f, 22f), _badgeTex);
        GUI.color = Color.white;
        var s = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11, fontStyle = FontStyle.Bold };
        s.normal.textColor = Color.white;
        GUI.Label(new Rect(bx, by, 56f, 22f), $"💬 {_unreadCount}", s);
    }

    // ─── Send ────────────────────────────────────────────────────────────

    private string PlayerName => NetworkManager.Instance?.LocalPlayerName ?? "";

    private void TrySend()
    {
        string text = _inputText.Trim();
        if (string.IsNullOrEmpty(text) || PlayerName.Length == 0) return;
        _inputText = "";
        _autoScroll = true;

        var nm = NetworkManager.Instance;
        if (nm != null && nm.IsConnected)
        {
            // Fast path: through Colyseus (room broadcasts it back to all players AND saves to ChatManager)
            nm.SendChatMessage(text);
        }
        else
        {
            // Fallback: direct HTTP (for frontend-only visitors or disconnected state)
            StartCoroutine(DoSend(PlayerName, text));
        }
    }

    // ─── HTTP polling ─────────────────────────────────────────────────────

    private IEnumerator DoPoll()
    {
        string url = $"{SERVER_URL}/chat/history?since={_lastTimestamp}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var wrapper = JsonUtility.FromJson<MessageListWrapper>($"{{\"items\":{req.downloadHandler.text}}}");
        if (wrapper?.items == null) yield break;

        int added = 0;
        foreach (var msg in wrapper.items)
        {
            if (msg.timestamp > _lastTimestamp)
            {
                _messages.Add(msg);
                _lastTimestamp = msg.timestamp;
                added++;
            }
        }
        if (_messages.Count > MAX_DISPLAY)
            _messages.RemoveRange(0, _messages.Count - MAX_DISPLAY);

        if (added > 0 && !_visible)
            _unreadCount += added;
    }

    private IEnumerator DoSend(string name, string text)
    {
        var payload = new SendPayload { name = name, text = text };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest($"{SERVER_URL}/chat/send", "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }

    // Called by NetworkManager when a "chat" message arrives via Colyseus
    public void ReceiveChatMessage(ChatMessage msg)
    {
        if (_messages.Count >= MAX_DISPLAY)
            _messages.RemoveAt(0);
        _messages.Add(msg);
        if (msg.timestamp > _lastTimestamp) _lastTimestamp = msg.timestamp;
        if (!_visible) _unreadCount++;
        _autoScroll = true;
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────

    [System.Serializable]
    public class ChatMessage { public int id; public long timestamp; public string name; public string text; }

    [System.Serializable]
    private class SendPayload { public string name; public string text; }

    [System.Serializable]
    private class MessageListWrapper { public List<ChatMessage> items; }
}
