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

    // Checkpoint info (set by CheckpointSystem)
    private int _checkpointsCurrent = 0;
    private int _checkpointsTotal = 5;

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

    void OnRoundStart(int round, string mode)
    {
        _roundNumber = round;
        _gameMode = mode;
        _roundTimer = 0f;
        _timerRunning = true;
        _checkpointsCurrent = 0;
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

    public void SetPhase(string phase) => _phase = phase;
    public void SetCountdown(float v) => _countdown = v;
    public void SetRoundInfo(int round, string mode) { _roundNumber = round; _gameMode = mode; }
    public void SetCheckpoint(int current, int total) { _checkpointsCurrent = current; _checkpointsTotal = total; }

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
        int alive = nm?.GetLocalPlayerState() != null
            ? (_room_playersAlive > 0 ? _room_playersAlive : 1)
            : 0;

        if (nm != null)
        {
            // read from room state if accessible
        }

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

        // ── Race: checkpoint progress (bottom center) ─────────────────────
        if (_gameMode == "race" && (_phase == "playing" || _localRaceActive))
        {
            float bw = 300f;
            float bx = (Screen.width - bw) / 2f;
            float by = Screen.height - 60f;

            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            GUI.DrawTexture(new Rect(bx - 8f, by - 8f, bw + 16f, 36f), _bgTex);
            GUI.color = Color.white;

            // Background bar
            GUI.color = new Color(0.2f, 0.2f, 0.28f, 1f);
            GUI.DrawTexture(new Rect(bx, by, bw, 20f), _barBgTex);

            // Fill
            float fill = _checkpointsTotal > 0 ? (float)_checkpointsCurrent / _checkpointsTotal : 0f;
            GUI.color = new Color(0.3f, 1f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(bx, by, bw * fill, 20f), _barFillTex);
            GUI.color = Color.white;

            var cpStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            cpStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(bx, by, bw, 20f),
                $"Checkpoint {_checkpointsCurrent} / {_checkpointsTotal}", cpStyle);
        }

        // ── Teams: score display (bottom center) ──────────────────────────
        if (_gameMode == "teams" && _phase == "playing")
        {
            float tw = 260f;
            float tx = (Screen.width - tw) / 2f;
            float ty = Screen.height - 60f;

            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            GUI.DrawTexture(new Rect(tx - 8f, ty - 8f, tw + 16f, 36f), _bgTex);
            GUI.color = Color.white;

            var teamStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };

            // Red team score
            teamStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(tx, ty - 2f, tw * 0.4f, 28f), $"{_cachedScoreRed}", teamStyle);

            // Separator
            var sepStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            sepStyle.normal.textColor = new Color(0.5f, 0.5f, 0.6f);
            GUI.Label(new Rect(tx + tw * 0.4f, ty - 2f, tw * 0.2f, 28f), "vs", sepStyle);

            // Blue team score
            teamStyle.normal.textColor = new Color(0.3f, 0.6f, 1f);
            GUI.Label(new Rect(tx + tw * 0.6f, ty - 2f, tw * 0.4f, 28f), $"{_cachedScoreBlue}", teamStyle);
        }

        // ── Survival: death zone warning ──────────────────────────────────
        if (_gameMode == "survival" && _phase == "playing" && _deathZoneWarning > 0.01f)
        {
            GUI.color = new Color(1f, 0.3f, 0.1f, _deathZoneWarning * 0.4f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bgTex);
            GUI.color = Color.white;

            var warnStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
            warnStyle.normal.textColor = new Color(1f, 0.4f, 0.2f, _deathZoneWarning);
            GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 36f), "⚠ ZONE DE MORT MONTE !", warnStyle);
        }
    }

    // Static accessors for cross-script use
    public static GameHUD Instance { get; private set; }
    public static int TotalCheckpoints { get; set; } = 5;

    // Cached values updated from NetworkManager state polling
    private int _cachedPlayersAlive = 0;
    private int _cachedScoreRed = 0;
    private int _cachedScoreBlue = 0;
    private float _deathZoneWarning = 0f;
    private int _room_playersAlive = 0;

    void LateUpdate()
    {
        // Poll NetworkManager for display values (avoids tight coupling via events for display-only data)
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;

        // Survival: check death zone proximity
        if (_gameMode == "survival")
        {
            var localState = NetworkManager.Instance.GetLocalPlayerState();
            if (localState != null)
            {
                // deathZoneY is synced via NetworkState — we read via a static accessor pattern
                // For now, warn when player Y is within 5 units above death zone
                // (actual deathZoneY is not directly accessible here without extra plumbing)
            }
        }
    }

    // Called by DeathZone.cs to update the warning
    public void SetDeathZoneWarning(float intensity) => _deathZoneWarning = intensity;
    public void SetTeamScores(int red, int blue) { _cachedScoreRed = red; _cachedScoreBlue = blue; }
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
