using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Tracks per-session and per-round player statistics and uploads them to the game server.
/// All HTTP calls use UnityWebRequest coroutines (WebGL-safe, no async/await).
/// </summary>
public class StatsTracker : MonoBehaviour
{
    public static StatsTracker Instance { get; private set; }

    private const string SERVER_URL = "https://game.rolld.kerboul.me";

    // Cumulative session stats (accumulate across rounds)
    private float _totalDistance;
    private int   _totalJumps;
    private float _maxSpeed;
    private float _bestRaceTime;   // 0 = not set
    private int   _racesPlayed;
    private int   _qualifications;
    private int   _eliminations;
    private int   _checkpointsTotal;
    private int   _bumpsGiven;
    private float _totalPlaytime;

    // Per-round deltas (reset after each send)
    private float _roundDistance;
    private float _roundMaxSpeed;
    private float _sessionStart;

    private Vector3 _lastPos;
    private bool    _trackingActive;
    private string  _cachedName = "";
    private PlayerController _pc;
    private Rigidbody _rb;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _sessionStart = Time.time;
    }

    void Start()
    {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();

        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.OnRoundStart  += OnRoundStart;
            nm.OnRoundEnd    += OnRoundEnd;
            nm.OnQualified   += OnQualified;
            nm.OnEliminated  += OnEliminated;
            nm.OnConnected   += OnConnected;
            nm.OnDisconnected += OnDisconnected;
        }
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.OnRoundStart  -= OnRoundStart;
            nm.OnRoundEnd    -= OnRoundEnd;
            nm.OnQualified   -= OnQualified;
            nm.OnEliminated  -= OnEliminated;
            nm.OnConnected   -= OnConnected;
            nm.OnDisconnected -= OnDisconnected;
        }
    }

    void FixedUpdate()
    {
        if (!_trackingActive || _rb == null || _pc == null || !_pc.enabled) return;

        Vector3 pos = transform.position;
        float delta = Vector3.Distance(pos, _lastPos);
        if (delta < 20f) // sanity cap against teleports
        {
            _roundDistance  += delta;
            _totalDistance  += delta;
        }
        _lastPos = pos;

        float speed = _rb.linearVelocity.magnitude;
        if (speed > _roundMaxSpeed) _roundMaxSpeed = speed;
        if (speed > _maxSpeed)      _maxSpeed      = speed;
    }

    // ─── Public hooks ────────────────────────────────────────────────────

    public void RegisterJump()
    {
        _totalJumps++;
    }

    public void RegisterBump()
    {
        _bumpsGiven++;
    }

    public void RegisterCheckpoint()
    {
        _checkpointsTotal++;
    }

    public void RegisterFinish(float raceTime)
    {
        if (raceTime <= 0f) return;
        if (_bestRaceTime <= 0f || raceTime < _bestRaceTime)
            _bestRaceTime = raceTime;
    }

    // ─── Event handlers ──────────────────────────────────────────────────

    private void OnConnected()
    {
        _cachedName = NetworkManager.Instance?.LocalPlayerName ?? "";
        _lastPos = transform.position;
        _trackingActive = true;
    }

    private void OnDisconnected()
    {
        _trackingActive = false;
        _totalPlaytime += Time.time - _sessionStart;
        SendStats(); // best-effort on disconnect
    }

    private void OnRoundStart(int round, string mode, int totalRounds)
    {
        _racesPlayed++;
        _roundDistance = 0f;
        _roundMaxSpeed = 0f;
        _lastPos = transform.position;
        _trackingActive = true;
    }

    private void OnRoundEnd(int round)
    {
        _trackingActive = false;
        SendStats();
        _roundDistance = 0f;
        _roundMaxSpeed = 0f;
    }

    private void OnQualified(string sessionId)
    {
        if (sessionId == NetworkManager.Instance?.LocalSessionId)
            _qualifications++;
    }

    private void OnEliminated(string sessionId, string reason)
    {
        if (sessionId == NetworkManager.Instance?.LocalSessionId)
            _eliminations++;
    }

    // ─── HTTP send ───────────────────────────────────────────────────────

    private void SendStats()
    {
        // Prefer live name, fall back to cached (useful on disconnect where name is cleared)
        var nm = NetworkManager.Instance;
        string name = (nm != null && !string.IsNullOrEmpty(nm.LocalPlayerName))
            ? nm.LocalPlayerName
            : _cachedName;
        if (string.IsNullOrEmpty(name)) return;
        StartCoroutine(DoSendStats(name));
    }

    private IEnumerator DoSendStats(string playerName)
    {
        _totalPlaytime += Time.time - _sessionStart;
        _sessionStart = Time.time;

        var payload = new StatsPayload
        {
            name = playerName,
            stats = new StatsData
            {
                totalDistance    = _totalDistance,
                totalJumps       = _totalJumps,
                maxSpeed         = _maxSpeed,
                bestRaceTime     = _bestRaceTime > 0f ? _bestRaceTime : 0f,
                racesPlayed      = _racesPlayed,
                qualifications   = _qualifications,
                eliminations     = _eliminations,
                checkpointsTotal = _checkpointsTotal,
                bumpsGiven       = _bumpsGiven,
                totalPlaytime    = _totalPlaytime,
            }
        };

        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest($"{SERVER_URL}/stats/update", "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[Stats] Upload failed: {req.error}");
        else
            Debug.Log($"[Stats] Uploaded for {playerName}");
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────

    [System.Serializable]
    private class StatsPayload { public string name; public StatsData stats; }

    [System.Serializable]
    private class StatsData
    {
        public float totalDistance;
        public int   totalJumps;
        public float maxSpeed;
        public float bestRaceTime;
        public int   racesPlayed;
        public int   qualifications;
        public int   eliminations;
        public int   checkpointsTotal;
        public int   bumpsGiven;
        public float totalPlaytime;
    }
}
