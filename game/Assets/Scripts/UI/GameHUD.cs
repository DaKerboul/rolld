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
        // Race UI disabled — free-roam mode has no rounds/countdown/timer.
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
