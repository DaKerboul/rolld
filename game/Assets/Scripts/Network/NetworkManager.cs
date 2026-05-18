using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Colyseus;
using Colyseus.Schema;

/// <summary>
/// Singleton managing the Colyseus connection, room lifecycle, remote player spawning,
/// and game-phase events (eliminated, qualified, roundStart, roundEnd, gameEnd).
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private const string serverURL = "wss://game.rolld.kerboul.me";

    [Header("Prefab")]
    [Tooltip("Prefab for remote players (must have RemotePlayerController)")]
    public GameObject remotePlayerPrefab;

    // --- Public state for UI ---
    public bool IsConnected { get; private set; }
    public string RoomId { get; private set; } = "";
    public string LocalSessionId { get; private set; } = "";
    public int PlayerCount { get; private set; }
    public string ConnectionStatus { get; private set; } = "Déconnecté";
    public string LastError { get; private set; } = "";

    // Expose remote players for debug UI
    public Dictionary<string, RemotePlayerController> RemotePlayers => _remotePlayers;

    // Local player info (set during join)
    public string LocalPlayerName { get; private set; } = "";
    public Color LocalPlayerColor { get; private set; } = Color.white;

    // --- Room listing ---
    [System.Serializable] public class RoomMeta { public string name; }
    [System.Serializable] public class RoomInfo  { public string roomId; public int clients; public int maxClients; public RoomMeta metadata; }
    [System.Serializable] private class RoomListWrapper { public List<RoomInfo> items; }

    public event Action<RoomInfo[]> OnRoomsRefreshed;

    public void FetchRooms() => StartCoroutine(DoFetchRooms());

    private IEnumerator DoFetchRooms()
    {
        using var req = UnityWebRequest.Get($"{serverURL.Replace("wss://", "https://").Replace("ws://", "http://")}/rooms");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { OnRoomsRefreshed?.Invoke(Array.Empty<RoomInfo>()); yield break; }
        var wrapper = JsonUtility.FromJson<RoomListWrapper>($"{{\"items\":{req.downloadHandler.text}}}");
        OnRoomsRefreshed?.Invoke(wrapper?.items?.ToArray() ?? Array.Empty<RoomInfo>());
    }

    // --- Events ---
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;

    // Game flow events
    public event Action<string> OnPhaseChanged;       // phase name
    public event Action<float>  OnCountdownChanged;   // seconds remaining
    public event Action<string, string> OnEliminated; // sessionId, reason
    public event Action<string> OnQualified;          // sessionId
    public event Action<int, string, int> OnRoundStart; // roundNumber, mode, totalRounds
    public event Action<int> OnRoundEnd;              // roundNumber
    public event Action<string> OnGameEnd;            // winnerName

    // --- Internals ---
    private Client _client;
    private Room<NetworkState> _room;
    private StateCallbackStrategy<NetworkState> _callbacks;
    private readonly Dictionary<string, RemotePlayerController> _remotePlayers = new();
    private float _broadcastTimer;
    private const float BROADCAST_INTERVAL = 0.01667f; // ~60/sec
    private bool _isJoining;

    private Transform _localPlayer;
    private Rigidbody _localPlayerRb;

    private Vector3 _lastSentPos;
    private Vector3 _lastSentVel;
    private Vector3 _lastSentAngVel;
    private const float POS_THRESHOLD = 0.005f;
    private const float VEL_THRESHOLD = 0.05f;

    private string _lastPhase = "";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!IsConnected || _room == null) return;

        _broadcastTimer += Time.deltaTime;
        if (_broadcastTimer >= BROADCAST_INTERVAL)
        {
            _broadcastTimer = 0f;
            BroadcastPosition();
        }
    }

    public NetworkPlayer GetLocalPlayerState()
    {
        if (_room == null || _room.State.players == null || string.IsNullOrEmpty(LocalSessionId)) return null;
        _room.State.players.TryGetValue(LocalSessionId, out var player);
        return player;
    }

    // ─── Join / Leave ────────────────────────────────────────────────────

    // ─── Join helpers ─────────────────────────────────────────────────────

    private Dictionary<string, object> BuildJoinOptions(string playerName, Color color) => new()
    {
        { "name",   playerName },
        { "colorR", color.r },
        { "colorG", color.g },
        { "colorB", color.b },
    };

    private void PrepareJoin(string playerName, Color color)
    {
        _isJoining = true;
        ConnectionStatus = "Connexion en cours...";
        LastError = "";
        LocalPlayerName = playerName;
        LocalPlayerColor = color;
        PlayerPrefs.SetString("rolld_player_name", playerName);
        _client = new Client(serverURL);
    }

    private void FinishJoin()
    {
        LocalSessionId = _room.SessionId;
        RoomId = _room.RoomId;
        IsConnected = true;
        ConnectionStatus = "Connecté";
        Debug.Log($"[Network] Joined room {RoomId} as {LocalSessionId}");

        _callbacks = Callbacks.Get(_room);
        _callbacks.OnAdd(state => state.players, (key, player) => OnPlayerAdd(key, player));
        _callbacks.OnRemove(state => state.players, (key, player) => OnPlayerRemove(key, player));
        _callbacks.Listen(state => state.phase, (v, _) => _OnPhaseChanged(v));
        _callbacks.Listen(state => state.countdown, (v, _) => OnCountdownChanged?.Invoke(v));
        _callbacks.Listen(state => state.playersAlive, (newVal, oldVal) => { if (GameHUD.Instance != null) GameHUD.Instance.SetPlayersAlive((int)newVal); });

        _room.OnMessage<EliminatedMsg>("eliminated", msg => { OnEliminated?.Invoke(msg.sessionId, msg.reason); });
        _room.OnMessage<QualifiedMsg> ("qualified",  msg => { OnQualified?.Invoke(msg.sessionId); });
        _room.OnMessage<RoundStartMsg>("roundStart", msg => { OnRoundStart?.Invoke(msg.round, msg.mode, msg.totalRounds); });
        _room.OnMessage<RoundEndMsg>  ("roundEnd",   msg => { OnRoundEnd?.Invoke(msg.round); });
        _room.OnMessage<GameEndMsg>   ("gameEnd",    msg => { OnGameEnd?.Invoke(msg.winner); });
        _room.OnMessage<ChatUI.ChatMessage>("chat",  msg => { ChatUI.Instance?.ReceiveChatMessage(msg); });
        _room.OnLeave += OnRoomLeave;

        // Seed players already present in the room (state decoded before callbacks were registered)
        if (_room.State.players != null)
        {
            foreach (var key in _room.State.players.Keys)
                OnPlayerAdd((string)key, (NetworkPlayer)_room.State.players[key]);
        }

        OnConnected?.Invoke();
    }

    private void HandleJoinError(Exception e)
    {
        Debug.LogError($"[Network] Failed to join: {e.Message}");
        ConnectionStatus = "Erreur de connexion";
        LastError = e.Message;
        IsConnected = false;
    }

    // ─── Public join methods ──────────────────────────────────────────────

    public async void JoinArena(string playerName, Color color)
    {
        if (_isJoining || IsConnected) return;
        PrepareJoin(playerName, color);
        try
        {
            _room = await _client.JoinOrCreate<NetworkState>("arena", BuildJoinOptions(playerName, color));
            FinishJoin();
        }
        catch (Exception e) { HandleJoinError(e); }
        finally { _isJoining = false; }
    }

    public async void JoinByRoomId(string roomId, string playerName, Color color)
    {
        if (_isJoining || IsConnected) return;
        PrepareJoin(playerName, color);
        try
        {
            _room = await _client.JoinById<NetworkState>(roomId, BuildJoinOptions(playerName, color));
            FinishJoin();
        }
        catch (Exception e) { HandleJoinError(e); }
        finally { _isJoining = false; }
    }

    public async void CreateRoom(string playerName, Color color, string roomName = null)
    {
        if (_isJoining || IsConnected) return;
        PrepareJoin(playerName, color);
        try
        {
            var opts = BuildJoinOptions(playerName, color);
            if (roomName != null) opts["roomName"] = roomName;
            _room = await _client.Create<NetworkState>("arena", opts);
            FinishJoin();
        }
        catch (Exception e) { HandleJoinError(e); }
        finally { _isJoining = false; }
    }

    public async void LeaveRoom()
    {
        if (_room != null) await _room.Leave();
        Cleanup();
    }

    public async void SendReady()
    {
        if (_room != null && IsConnected)
            await _room.Send("ready", null);
    }

    public async void SendCheckpoint(int index)
    {
        if (_room != null && IsConnected)
            await _room.Send("checkpointReached", new { index });
    }

    public async void SendChatMessage(string text)
    {
        if (_room != null && IsConnected)
            await _room.Send("chat", new { text });
    }

    // ─── State Callbacks ─────────────────────────────────────────────────

    private void _OnPhaseChanged(string phase)
    {
        if (phase == _lastPhase) return;
        _lastPhase = phase;
        Debug.Log($"[Network] Phase → {phase}");
        OnPhaseChanged?.Invoke(phase);
    }

    private void OnPlayerAdd(string sessionId, NetworkPlayer player)
    {
        Debug.Log($"[Network] Player joined: {sessionId} ({player.name})");
        PlayerCount = _room.State.players?.Count ?? 0;

        if (sessionId == LocalSessionId) return;
        if (_remotePlayers.ContainsKey(sessionId)) return; // prevent duplicate spawn

        {
            Vector3 spawnPos = new Vector3(player.x, player.y, player.z);
            GameObject remoteBall = remotePlayerPrefab != null
                ? Instantiate(remotePlayerPrefab, spawnPos, Quaternion.identity)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            remoteBall.transform.position = spawnPos;
            remoteBall.name = $"RemotePlayer_{player.name}_{sessionId[..6]}";

            var controller = remoteBall.GetComponent<RemotePlayerController>()
                          ?? remoteBall.AddComponent<RemotePlayerController>();

            controller.Initialize(sessionId, player.name,
                new Color(player.colorR, player.colorG, player.colorB));

            _remotePlayers[sessionId] = controller;
        }

        _callbacks.OnChange(player, () => OnPlayerChange(sessionId, player));
        OnPlayerJoined?.Invoke(sessionId);
    }

    private void OnPlayerRemove(string sessionId, NetworkPlayer player)
    {
        Debug.Log($"[Network] Player left: {sessionId}");
        PlayerCount = _room.State.players?.Count ?? 0;

        if (_remotePlayers.TryGetValue(sessionId, out var controller))
        {
            if (controller != null && controller.gameObject != null)
                Destroy(controller.gameObject);
            _remotePlayers.Remove(sessionId);
        }

        OnPlayerLeft?.Invoke(sessionId);
    }

    private void OnPlayerChange(string sessionId, NetworkPlayer player)
    {
        if (sessionId == LocalSessionId) return;

        if (_remotePlayers.TryGetValue(sessionId, out var controller))
        {
            controller.SetTargetState(
                new Vector3(player.x, player.y, player.z),
                new Vector3(player.vx, player.vy, player.vz),
                new Quaternion(player.rx, player.ry, player.rz, player.rw),
                player.t,
                new Vector3(player.avx, player.avy, player.avz)
            );

            controller.SetVisible(!player.isEliminated);
        }
    }

    // ─── Position Broadcasting ────────────────────────────────────────────

    private void BroadcastPosition()
    {
        if (_room == null || !IsConnected) return;

        if (_localPlayer == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                _localPlayer = pc.transform;
                _localPlayerRb = pc.GetComponent<Rigidbody>();
            }
            else return;
        }

        Vector3 pos = _localPlayer.position;
        Vector3 vel = _localPlayerRb != null ? _localPlayerRb.linearVelocity : Vector3.zero;
        Vector3 angVel = _localPlayerRb != null ? _localPlayerRb.angularVelocity : Vector3.zero;

        if (Vector3.Distance(pos, _lastSentPos) < POS_THRESHOLD &&
            Vector3.Distance(vel, _lastSentVel) < VEL_THRESHOLD &&
            Vector3.Distance(angVel, _lastSentAngVel) < VEL_THRESHOLD)
            return;

        _lastSentPos = pos;
        _lastSentVel = vel;
        _lastSentAngVel = angVel;

        Quaternion rot = _localPlayer.rotation;

        var data = new Dictionary<string, object>
        {
            { "x",  pos.x }, { "y", pos.y }, { "z", pos.z },
            { "vx", vel.x }, { "vy", vel.y }, { "vz", vel.z },
            { "rx", rot.x }, { "ry", rot.y }, { "rz", rot.z }, { "rw", rot.w },
            { "avx", angVel.x }, { "avy", angVel.y }, { "avz", angVel.z }
        };

        _ = _room.Send("position", data);
    }

    // ─── Room Lifecycle ───────────────────────────────────────────────────

    private void OnRoomLeave(int code)
    {
        Debug.Log($"[Network] Left room (code: {code})");
        OnDisconnected?.Invoke(); // before Cleanup so listeners still have LocalPlayerName
        Cleanup();
    }

    private void Cleanup()
    {
        IsConnected = false;
        ConnectionStatus = "Déconnecté";
        RoomId = "";
        PlayerCount = 0;
        LocalPlayerName = "";
        LocalPlayerColor = Color.white;
        _lastPhase = "";

        foreach (var kvp in _remotePlayers)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                Destroy(kvp.Value.gameObject);
        }
        _remotePlayers.Clear();

        _room = null;
        _client = null;
        _callbacks = null;
        _localPlayer = null;
        _localPlayerRb = null;
    }

    void OnDestroy()
    {
        if (_room != null) _ = _room.Leave(false);
    }
}

// ─── Message DTOs ─────────────────────────────────────────────────────────────

[Serializable] public class EliminatedMsg { public string sessionId; public string name; public string reason; }
[Serializable] public class QualifiedMsg   { public string sessionId; public string name; }
[Serializable] public class RoundStartMsg  { public int round; public string mode; public int totalRounds; }
[Serializable] public class RoundEndMsg    { public int round; }
[Serializable] public class GameEndMsg     { public string winner; }
