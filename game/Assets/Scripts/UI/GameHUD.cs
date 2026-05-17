using UnityEngine;

/// <summary>
/// In-game HUD: round info, countdown, players alive, timer, checkpoints.
/// Uses ImGuiSkin for visual consistency with LobbyUI.
/// Only shown when a game is active (not in lobby).
/// </summary>
public class GameHUD : MonoBehaviour
{
    // State
    private string _phase = "lobby";
    private float _countdown = 0f;
    private int _roundNumber = 1;
    private int _totalRounds = 4;
    private string _gameMode = "race";
    private float _roundTimer = 0f;
    private bool _timerRunning = false;


    // Local race state (activated when CP0 gate is crossed, independent of server phase)
    private bool _localRaceActive = false;
    private float _localRaceTimer = 0f;

    // Countdown animation
    private float _lastCountdownShown = -1f;
    private float _countdownPulse = 0f;

    // --- Static textures ---
    private static Texture2D _bgTex;
    private static Texture2D _barFillTex;
    private static Texture2D _barBgTex;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureTextures();
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoundStart += OnRoundStart;
            NetworkManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoundStart -= OnRoundStart;
            NetworkManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    void OnRoundStart(int round, string mode, int totalRounds)
    {
        _roundNumber = round;
        _totalRounds = totalRounds;
        _gameMode = mode;
        _roundTimer = 0f;
        _timerRunning = true;
    }

    void OnPhaseChanged(string phase)
    {
        _phase = phase;
        if (phase == "playing") _timerRunning = true;
        else if (phase == "roundEnd" || phase == "gameEnd") _timerRunning = false;
    }

    void Update()
    {
        if (_timerRunning)
            _roundTimer += Time.deltaTime;
        if (_localRaceActive)
            _localRaceTimer += Time.deltaTime;

        if (_countdown > 0f && _countdown != _lastCountdownShown)
        {
            _countdownPulse = 1f;
            _lastCountdownShown = _countdown;
        }
        _countdownPulse = Mathf.Max(0f, _countdownPulse - Time.deltaTime * 3f);
    }

    public float LocalRaceTimer => _localRaceTimer;

    public void SetPhase(string phase) => _phase = phase;
    public void SetCountdown(float v) => _countdown = v;
    public void SetRoundInfo(int round, string mode) { _roundNumber = round; _gameMode = mode; }
    public void SetTotalRounds(int n) => _totalRounds = n;


    public void SetLocalRaceActive(bool active)
    {
        _localRaceActive = active;
        if (!active) _localRaceTimer = 0f;
    }

    void OnGUI()
    {
        if (_phase == "lobby" && !_localRaceActive) return;

        ImGuiSkin.EnsureReady();
        var nm = NetworkManager.Instance;

        // ── Countdown (center, large) ─────────────────────────────────────
        if (_phase == "countdown" && _countdown > 0f)
        {
            float scale = 1f + _countdownPulse * 0.4f;
            float fontSize = 96f * scale;
            var countStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(fontSize),
                fontStyle = FontStyle.Bold,
            };
            countStyle.normal.textColor = new Color(1f, 0.85f, 0.1f, 1f);
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 120f),
                Mathf.CeilToInt(_countdown).ToString(), countStyle);

            // "Préparez-vous !" label below
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
            };
            subStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            string modeLabel = _gameMode switch {
                "race"     => "COURSE",
                "survival" => "SURVIVAL",
                "teams"    => "ÉQUIPES",
                _ => _gameMode.ToUpper()
            };
            GUI.Label(new Rect(0, Screen.height * 0.3f + 110f, Screen.width, 36f),
                $"— {modeLabel} —", subStyle);
            return;
        }

        // ── Top-left: Round & Mode ─────────────────────────────────────────
        float panelX = 12f;
        float panelY = 12f;
        float panelW = 220f;
        float panelH = 70f;

        GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), _bgTex);
        GUI.color = Color.white;

        var roundStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
        };
        roundStyle.normal.textColor = new Color(1f, 0.85f, 0.1f);
        GUI.Label(new Rect(panelX + 8f, panelY + 4f, panelW - 16f, 28f),
            $"ROUND {_roundNumber} / {_totalRounds}", roundStyle);

        var modeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
        modeStyle.normal.textColor = new Color(0.7f, 0.7f, 0.85f);
        string modeFull = _gameMode switch {
            "race" => "COURSE", "survival" => "SURVIVAL", "teams" => "ÉQUIPES", _ => _gameMode.ToUpper()
        };
        GUI.Label(new Rect(panelX + 8f, panelY + 32f, panelW - 16f, 24f), modeFull, modeStyle);

        // ── Top-right: Players alive ──────────────────────────────────────
        float prX = Screen.width - 180f;
        GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        GUI.DrawTexture(new Rect(prX, panelY, 168f, panelH), _bgTex);
        GUI.color = Color.white;

        var aliveStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold,
        };
        aliveStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);
        GUI.Label(new Rect(prX, panelY + 2f, 168f, 40f), $"{_cachedPlayersAlive}", aliveStyle);

        var aliveLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
        aliveLabel.normal.textColor = new Color(0.6f, 0.6f, 0.7f);
        GUI.Label(new Rect(prX, panelY + 40f, 168f, 22f), "joueurs en jeu", aliveLabel);

        // ── Round timer (top center) ──────────────────────────────────────
        float displayTimer = _timerRunning ? _roundTimer : (_localRaceActive ? _localRaceTimer : -1f);
        if (displayTimer >= 0f)
        {
            int mins = Mathf.FloorToInt(displayTimer / 60f);
            int secs = Mathf.FloorToInt(displayTimer % 60f);
            var timerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };
            timerStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            GUI.Label(new Rect(Screen.width * 0.5f - 60f, panelY, 120f, 40f),
                $"{mins:00}:{secs:00}", timerStyle);
        }

    }

    // Static accessors for cross-script use
    public static GameHUD Instance { get; private set; }


    // Cached values updated from NetworkManager state polling
    private int _cachedPlayersAlive = 0;

    public void SetPlayersAlive(int count) => _cachedPlayersAlive = count;

    private static void EnsureTextures()
    {
        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, Color.white);
            _bgTex.Apply();
        }
        if (_barBgTex == null)
        {
            _barBgTex = new Texture2D(1, 1);
            _barBgTex.SetPixel(0, 0, Color.white);
            _barBgTex.Apply();
        }
        if (_barFillTex == null)
        {
            _barFillTex = new Texture2D(1, 1);
            _barFillTex.SetPixel(0, 0, Color.white);
            _barFillTex.Apply();
        }
    }
}
