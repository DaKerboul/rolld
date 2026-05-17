using UnityEngine;

/// <summary>
/// Singleton game state machine. Drives all game UI and player state transitions
/// based on server events received from NetworkManager.
/// States: Lobby → Countdown → Playing → Eliminated/Qualified → RoundEnd → GameEnd
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    public GameObject playerRoot;
    public SpectatorCamera spectatorCamera;
    public GameHUD gameHUD;
    public EliminationOverlay eliminationOverlay;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Lobby;
    public bool IsLocalEliminated { get; private set; } = false;
    public string CurrentMode { get; private set; } = "race";
    public int CurrentRound { get; private set; } = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        var nm = NetworkManager.Instance;
        if (nm == null) return;
        nm.OnPhaseChanged     += HandlePhaseChanged;
        nm.OnCountdownChanged += HandleCountdownChanged;
        nm.OnEliminated       += HandleEliminated;
        nm.OnQualified        += HandleQualified;
        nm.OnRoundStart       += HandleRoundStart;
        nm.OnRoundEnd         += HandleRoundEnd;
        nm.OnGameEnd          += HandleGameEnd;
        nm.OnDisconnected     += HandleDisconnected;
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Instance;
        if (nm == null) return;
        nm.OnPhaseChanged     -= HandlePhaseChanged;
        nm.OnCountdownChanged -= HandleCountdownChanged;
        nm.OnEliminated       -= HandleEliminated;
        nm.OnQualified        -= HandleQualified;
        nm.OnRoundStart       -= HandleRoundStart;
        nm.OnRoundEnd         -= HandleRoundEnd;
        nm.OnGameEnd          -= HandleGameEnd;
        nm.OnDisconnected     -= HandleDisconnected;
    }

    // ─── Event Handlers ───────────────────────────────────────────────────

    void HandlePhaseChanged(string phase)
    {
        switch (phase)
        {
            case "countdown":
                TransitionTo(GamePhase.Countdown);
                break;
            case "playing":
                if (!IsLocalEliminated)
                    TransitionTo(GamePhase.Playing);
                break;
            case "roundEnd":
                TransitionTo(GamePhase.RoundEnd);
                break;
            case "gameEnd":
                TransitionTo(GamePhase.GameEnd);
                break;
            case "lobby":
                // New round lobby — reset eliminated state
                IsLocalEliminated = false;
                TransitionTo(GamePhase.Lobby);
                break;
        }
    }

    void HandleCountdownChanged(float value)
    {
        gameHUD?.SetCountdown(value);
    }

    void HandleEliminated(string sessionId, string reason)
    {
        if (sessionId == NetworkManager.Instance?.LocalSessionId)
        {
            IsLocalEliminated = true;
            TransitionTo(GamePhase.Eliminated);
            eliminationOverlay?.ShowEliminated();
        }
    }

    void HandleQualified(string sessionId)
    {
        if (sessionId == NetworkManager.Instance?.LocalSessionId)
        {
            TransitionTo(GamePhase.Qualified);
            eliminationOverlay?.ShowQualified();
        }
    }

    void HandleRoundStart(int round, string mode, int totalRounds)
    {
        CurrentRound = round;
        CurrentMode = mode;
        gameHUD?.SetRoundInfo(round, mode);
        gameHUD?.SetTotalRounds(totalRounds);
        IsLocalEliminated = false;
    }

    void HandleRoundEnd(int round)
    {
        // Overlay already shown by elimination/qualification handlers
    }

    void HandleGameEnd(string winner)
    {
        eliminationOverlay?.ShowGameEnd(winner);
    }

    void HandleDisconnected()
    {
        IsLocalEliminated = false;
        TransitionTo(GamePhase.Lobby);
    }

    // ─── State Transitions ────────────────────────────────────────────────

    void TransitionTo(GamePhase phase)
    {
        CurrentPhase = phase;
        Debug.Log($"[GameManager] → {phase}");

        switch (phase)
        {
            case GamePhase.Lobby:
                SetPlayerActive(NetworkManager.Instance?.IsConnected ?? false);
                SetSpectatorActive(false);
                gameHUD?.SetPhase("lobby");
                break;

            case GamePhase.Countdown:
                gameHUD?.SetPhase("countdown");
                break;

            case GamePhase.Playing:
                SetPlayerActive(true);
                SetSpectatorActive(false);
                gameHUD?.SetPhase("playing");
                break;

            case GamePhase.Eliminated:
                SetPlayerActive(false);
                SetSpectatorActive(true);
                gameHUD?.SetPhase("eliminated");
                break;

            case GamePhase.Qualified:
                // Keep player active but freeze input briefly
                gameHUD?.SetPhase("qualified");
                break;

            case GamePhase.RoundEnd:
                gameHUD?.SetPhase("roundEnd");
                break;

            case GamePhase.GameEnd:
                SetPlayerActive(false);
                SetSpectatorActive(true);
                gameHUD?.SetPhase("gameEnd");
                break;
        }
    }

    void SetPlayerActive(bool active)
    {
        if (playerRoot == null) return;
        playerRoot.SetActive(active);
        var pc = playerRoot.GetComponentInChildren<PlayerController>(true);
        if (pc != null) pc.enabled = active;

        if (active)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void SetSpectatorActive(bool active)
    {
        if (spectatorCamera == null) return;
        if (active) spectatorCamera.Activate();
        else spectatorCamera.Deactivate();
    }
}

public enum GamePhase
{
    Lobby,
    Countdown,
    Playing,
    Eliminated,
    Qualified,
    RoundEnd,
    GameEnd,
}
