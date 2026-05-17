using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Tracks player statistics and uploads them to the game server every 30s + on disconnect.
/// No dependency on round events — works even if Colyseus callbacks are broken.
/// </summary>
public class StatsTracker : MonoBehaviour
{
    public static StatsTracker Instance { get; private set; }

    private const string SERVER_URL    = "https://game.rolld.kerboul.me";
    private const float  SEND_INTERVAL    = 30f;
    private const float  MIN_SEND_INTERVAL = 6f; // juste au-dessus du rate-limit serveur (5s)

    // Cumulative stats
    private float _totalDistance;
    private int   _totalJumps;
    private float _maxSpeed;
    private int   _bumpsGiven;

    // Playtime
    private float _sessionStart;
    private float _playtimeSentSoFar; // how much playtime we already sent

    // Tracking
    private Vector3 _lastPos;
    private bool    _tracking;
    private string  _cachedName = "";
    private float   _lastSentTime = -999f;

    private PlayerController _pc;
    private Rigidbody        _rb;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();

        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.OnConnected    += OnConnected;
            nm.OnDisconnected += OnDisconnected;
        }
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.OnConnected    -= OnConnected;
            nm.OnDisconnected -= OnDisconnected;
        }
    }

    void FixedUpdate()
    {
        if (!_tracking || _rb == null) return;

        Vector3 pos   = transform.position;
        float   delta = Vector3.Distance(pos, _lastPos);
        if (delta < 20f) // filtre téléportations
            _totalDistance += delta;
        _lastPos = pos;

        float speed = _rb.linearVelocity.magnitude;
        if (speed > _maxSpeed) _maxSpeed = speed;
    }

    // ─── Public hooks ─────────────────────────────────────────────────────

    public void RegisterJump() => _totalJumps++;
    public void RegisterBump() => _bumpsGiven++;

    // ─── Connection events ────────────────────────────────────────────────

    private void OnConnected()
    {
        _cachedName   = NetworkManager.Instance?.LocalPlayerName ?? "";
        _lastPos      = transform.position;
        _sessionStart = Time.time;
        _tracking     = true;
        StartCoroutine(PeriodicSend());
    }

    private void OnDisconnected()
    {
        _tracking = false;
        StopAllCoroutines();
        SendStats(); // envoi final best-effort
    }

    // ─── Periodic send ────────────────────────────────────────────────────

    private IEnumerator PeriodicSend()
    {
        while (_tracking)
        {
            yield return new WaitForSeconds(SEND_INTERVAL);
            if (_tracking) SendStats();
        }
    }

    // ─── HTTP send ────────────────────────────────────────────────────────

    private void SendStats()
    {
        if (Time.time - _lastSentTime < MIN_SEND_INTERVAL) return;
        var nm = NetworkManager.Instance;
        string name = (nm != null && !string.IsNullOrEmpty(nm.LocalPlayerName))
            ? nm.LocalPlayerName
            : _cachedName;
        if (string.IsNullOrEmpty(name)) return;
        _lastSentTime = Time.time;
        StartCoroutine(DoSendStats(name));
    }

    private IEnumerator DoSendStats(string playerName)
    {
        float now          = Time.time;
        float sessionSecs  = now - _sessionStart;
        float playtimeToSend = sessionSecs - _playtimeSentSoFar;
        _playtimeSentSoFar = sessionSecs;

        var payload = new StatsPayload
        {
            name  = playerName,
            stats = new StatsData
            {
                totalDistance = _totalDistance,
                totalJumps    = _totalJumps,
                maxSpeed      = _maxSpeed,
                bumpsGiven    = _bumpsGiven,
                totalPlaytime = playtimeToSend,
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
            Debug.Log($"[Stats] Sent for {playerName} — dist:{_totalDistance:F0}m spd:{_maxSpeed:F1}m/s jumps:{_totalJumps}");
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
        public int   bumpsGiven;
        public float totalPlaytime;
    }
}
