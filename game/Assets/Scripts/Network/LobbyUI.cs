using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lobby UI: character setup + room list side by side.
/// - T to open/close chat, Tab for keybinds (handled elsewhere)
/// - Lists available rooms, lets the player create or join one
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Scene References")]
    public GameObject playerRoot;
    public SpectatorCamera spectatorCamera;

    private static readonly Color[] PresetColors =
    {
        new Color(1f, 0.35f, 0.2f),
        new Color(0.2f, 0.6f, 1f),
        new Color(0.3f, 1f, 0.4f),
        new Color(1f, 0.85f, 0.1f),
        new Color(0.8f, 0.3f, 1f),
        new Color(1f, 0.5f, 0.7f),
    };
    private static readonly string[] ColorNames = { "Rouge", "Bleu", "Vert", "Jaune", "Violet", "Rose" };

    // UI state
    private bool   _lobbyActive  = true;
    private string _playerName   = "";
    private int    _selectedColorIndex = 0;
    private string _statusMessage = "";
    private bool   _isConnecting = false;
    private bool   _isReady      = false;

    // Room list
    private NetworkManager.RoomInfo[] _rooms = new NetworkManager.RoomInfo[0];
    private bool   _roomsFetching = false;
    private float  _refreshTimer  = 0f;
    private const float REFRESH_INTERVAL = 4f;
    private Vector2 _roomsScroll;

    // Color preview texture
    private Texture2D _colorPreviewTex;
    private int _lastPreviewColorIndex = -1;

    void Start()
    {
        _playerName = PlayerPrefs.GetString("rolld_player_name", "Joueur" + Random.Range(100, 999));

        if (playerRoot != null)
            playerRoot.SetActive(false);

        if (spectatorCamera != null)
        {
            var gameplayCam = playerRoot?.GetComponentInChildren<Camera>(true);
            if (gameplayCam != null)
                spectatorCamera.gameplayCamera = gameplayCam;
            spectatorCamera.Activate();
        }

        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.OnConnected      += OnConnected;
            nm.OnDisconnected   += OnDisconnected;
            nm.OnRoomsRefreshed += OnRoomsRefreshed;
        }

        RefreshRooms();
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Instance;
        if (nm != null)
        {
            nm.OnConnected      -= OnConnected;
            nm.OnDisconnected   -= OnDisconnected;
            nm.OnRoomsRefreshed -= OnRoomsRefreshed;
        }
    }

    void Update()
    {
        if (!_lobbyActive || _isConnecting) return;
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= REFRESH_INTERVAL)
        {
            _refreshTimer = 0f;
            RefreshRooms();
        }
    }

    private void RefreshRooms()
    {
        if (_roomsFetching) return;
        _roomsFetching = true;
        NetworkManager.Instance?.FetchRooms();
    }

    private void OnRoomsRefreshed(NetworkManager.RoomInfo[] rooms)
    {
        _rooms = rooms;
        _roomsFetching = false;
    }

    // ─── Network callbacks ────────────────────────────────────────────────

    private void OnConnected()
    {
        _lobbyActive  = false;
        _isConnecting = false;
        _statusMessage = "";
        CancelInvoke(nameof(ConnectionTimeout));

        if (playerRoot != null)
            playerRoot.SetActive(true);

        var nm = NetworkManager.Instance;
        if (nm != null && playerRoot != null)
        {
            var setup = playerRoot.GetComponentInChildren<VehicleLocalSetup>(true);
            if (setup != null)
            {
                var vehicle = setup.GetComponent<NWH.VehiclePhysics2.VehicleController>();
                var rb = vehicle != null ? vehicle.vehicleRigidbody : setup.GetComponent<Rigidbody>();
                var localState = nm.GetLocalPlayerState();
                if (localState != null && rb != null)
                {
                    Vector3 spawnPos = new Vector3(localState.x, localState.y, localState.z);
                    rb.linearVelocity  = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = spawnPos;
                    setup.transform.position = spawnPos;
                }
                if (vehicle != null) vehicle.enabled = true;
                setup.SetupLocal(nm.LocalPlayerName, nm.LocalPlayerColor);
            }
        }

        if (spectatorCamera != null)
            spectatorCamera.Deactivate();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnDisconnected()
    {
        _lobbyActive   = true;
        _isConnecting  = false;
        _isReady       = false;
        _statusMessage = "Déconnecté du serveur";
        _refreshTimer  = REFRESH_INTERVAL; // force immediate refresh

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (playerRoot != null)
        {
            var vehicle = playerRoot.GetComponentInChildren<NWH.VehiclePhysics2.VehicleController>(true);
            if (vehicle != null) vehicle.enabled = false;
            playerRoot.SetActive(false);
        }

        if (spectatorCamera != null)
            spectatorCamera.Activate();
    }

    // ─── OnGUI ────────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (!_lobbyActive) return;

        ImGuiSkin.EnsureReady();
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        ImGuiSkin.DrawOverlay();

        bool isConnected = NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;

        if (!isConnected)
            DrawSetupAndRoomList();
        else
            DrawWaitingRoom();
    }

    // ─── Setup + room list ────────────────────────────────────────────────

    private void DrawSetupAndRoomList()
    {
        const float W = 620f, H = 520f;
        float x = (Screen.width  - W) * 0.5f;
        float y = (Screen.height - H) * 0.5f;

        ImGuiSkin.BeginWindowAt(x, y, W, H, "ROLL'D");
        GUILayout.Label("Choisir une salle et configurer son personnage", ImGuiSkin.WindowSubtitle);
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        // ── Left column : character setup ─────────────────────────────
        GUILayout.BeginVertical(GUILayout.Width(240));
        ImGuiSkin.DrawSectionHeader("PERSONNAGE");
        GUILayout.Space(4);
        _playerName = GUILayout.TextField(_playerName, 16, ImGuiSkin.TextField, GUILayout.Height(30));
        GUILayout.Space(10);

        ImGuiSkin.DrawSectionHeader("COULEUR");
        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        for (int i = 0; i < PresetColors.Length; i++)
        {
            Color c        = PresetColors[i];
            bool  selected = _selectedColorIndex == i;
            Color prevBg   = GUI.backgroundColor;
            GUI.backgroundColor = selected ? c : c * 0.6f;
            var btnStyle = new GUIStyle(ImGuiSkin.ButtonSmall)
                { fontStyle = selected ? FontStyle.Bold : FontStyle.Normal };
            if (selected) btnStyle.normal.textColor = Color.white;
            if (GUILayout.Button(selected ? $"▸{ColorNames[i][0]}" : $"{ColorNames[i][0]}",
                    btnStyle, GUILayout.Height(30), GUILayout.Width(34)))
                _selectedColorIndex = i;
            GUI.backgroundColor = prevBg;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        // Color swatch
        if (_colorPreviewTex == null || _lastPreviewColorIndex != _selectedColorIndex)
        {
            if (_colorPreviewTex == null)
            {
                _colorPreviewTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _colorPreviewTex.hideFlags = HideFlags.HideAndDontSave;
            }
            _colorPreviewTex.SetPixel(0, 0, PresetColors[_selectedColorIndex]);
            _colorPreviewTex.Apply();
            _lastPreviewColorIndex = _selectedColorIndex;
        }
        var swatchStyle = new GUIStyle(ImGuiSkin.LabelDim) { alignment = TextAnchor.MiddleLeft, fontSize = 11 };
        GUILayout.Label($"▌ {ColorNames[_selectedColorIndex]}", swatchStyle);

        GUILayout.FlexibleSpace();

        // Create room button
        GUI.enabled = !_isConnecting && !string.IsNullOrWhiteSpace(_playerName);
        if (GUILayout.Button("+ Créer une salle", ImGuiSkin.Button, GUILayout.Height(36)))
            DoCreate();

        GUILayout.Space(4);

        // Join any (join or create fallback)
        if (GUILayout.Button("▶ Rejoindre n'importe", ImGuiSkin.ButtonAccent, GUILayout.Height(36)))
            DoJoinAny();

        GUI.enabled = true;
        GUILayout.EndVertical();

        GUILayout.Space(12);

        // ── Right column : room list ───────────────────────────────────
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        ImGuiSkin.DrawSectionHeader("SALLES DISPONIBLES");
        GUILayout.FlexibleSpace();
        GUI.enabled = !_roomsFetching;
        if (GUILayout.Button(_roomsFetching ? "…" : "↻", ImGuiSkin.ButtonSmall,
                GUILayout.Width(28), GUILayout.Height(22)))
        {
            _refreshTimer = 0f;
            RefreshRooms();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        float listH = H - 160f;
        _roomsScroll = GUILayout.BeginScrollView(_roomsScroll, ImGuiSkin.ScrollView,
            GUILayout.Height(listH));

        if (_rooms.Length == 0)
        {
            var emptyStyle = new GUIStyle(ImGuiSkin.LabelDim) { alignment = TextAnchor.MiddleCenter };
            GUILayout.FlexibleSpace();
            GUILayout.Label(_roomsFetching ? "Chargement…" : "Aucune salle ouverte.", emptyStyle);
            GUILayout.FlexibleSpace();
        }
        else
        {
            foreach (var room in _rooms)
            {
                string roomName = room.metadata?.name ?? ("Salle #" + room.roomId.Substring(0, 6));
                int    clients  = room.clients;
                int    maxCli   = room.maxClients;

                GUILayout.BeginHorizontal();

                var nameStyle = new GUIStyle(ImGuiSkin.LabelBold) { fontSize = 12 };
                GUILayout.Label(roomName, nameStyle, GUILayout.Width(140));

                var countStyle = new GUIStyle(ImGuiSkin.LabelDim) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
                GUILayout.Label($"{clients} / {maxCli}", countStyle, GUILayout.Width(48));

                GUILayout.FlexibleSpace();

                bool full = clients >= maxCli;
                GUI.enabled = !_isConnecting && !full && !string.IsNullOrWhiteSpace(_playerName);
                if (GUILayout.Button(full ? "Pleine" : "▶ Rejoindre",
                        ImGuiSkin.ButtonSmall, GUILayout.Width(90), GUILayout.Height(26)))
                    DoJoinRoom(room.roomId);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                ImGuiSkin.Separator();
                GUILayout.Space(2);
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal(); // end columns

        // ── Status bar ────────────────────────────────────────────────
        GUILayout.Space(4);
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            bool isError = _statusMessage.Contains("Erreur") || _statusMessage.Contains("Déconnecté");
            GUILayout.Label(_statusMessage, isError ? ImGuiSkin.StatusRed : ImGuiSkin.Hint);
        }

        ImGuiSkin.EndWindow();
    }

    // ─── Waiting room ─────────────────────────────────────────────────────

    private void DrawWaitingRoom()
    {
        ImGuiSkin.BeginWindow(400f, 300f, "SALLE D'ATTENTE");

        var nm = NetworkManager.Instance;
        string roomDisplay = nm != null ? ("Salle #" + nm.RoomId.Substring(0, Mathf.Min(6, nm.RoomId.Length))) : "—";
        GUILayout.Label(roomDisplay, ImGuiSkin.WindowSubtitle);
        GUILayout.Space(12);

        ImGuiSkin.DrawSectionHeader("JOUEURS CONNECTÉS");
        GUILayout.Space(4);
        if (nm != null)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            s.normal.textColor = new Color(0.75f, 0.75f, 0.85f);
            GUILayout.Label($"  {nm.PlayerCount} joueur(s) dans la salle", s);
        }
        GUILayout.Space(16);

        if (!_isReady)
        {
            if (GUILayout.Button("✔ Je suis prêt !", ImGuiSkin.ButtonAccent, GUILayout.Height(44)))
            {
                _isReady = true;
                NetworkManager.Instance?.SendReady();
            }
        }
        else
        {
            var rs = new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            rs.normal.textColor = new Color(0.3f, 1f, 0.5f);
            GUILayout.Label("✔ Prêt ! En attente des autres…", rs, GUILayout.Height(44));
        }

        GUILayout.Space(8);
        GUILayout.Label("La partie démarre quand tout le monde est prêt\nou automatiquement après 30 secondes.", ImGuiSkin.Hint);

        ImGuiSkin.EndWindow();
    }

    // ─── Actions ──────────────────────────────────────────────────────────

    private string ValidateName()
    {
        string n = _playerName.Trim();
        if (string.IsNullOrEmpty(n)) { _statusMessage = "Entre un pseudo d'abord."; return null; }
        return n;
    }

    private void DoJoinRoom(string roomId)
    {
        string n = ValidateName(); if (n == null) return;
        _isConnecting  = true;
        _statusMessage = "Connexion à la salle…";
        NetworkManager.Instance?.JoinByRoomId(roomId, n, PresetColors[_selectedColorIndex]);
        Invoke(nameof(ConnectionTimeout), 10f);
    }

    private void DoCreate()
    {
        string n = ValidateName(); if (n == null) return;
        _isConnecting  = true;
        _statusMessage = "Création d'une salle…";
        NetworkManager.Instance?.CreateRoom(n, PresetColors[_selectedColorIndex]);
        Invoke(nameof(ConnectionTimeout), 10f);
    }

    private void DoJoinAny()
    {
        string n = ValidateName(); if (n == null) return;
        _isConnecting  = true;
        _statusMessage = "Connexion…";
        NetworkManager.Instance?.JoinArena(n, PresetColors[_selectedColorIndex]);
        Invoke(nameof(ConnectionTimeout), 10f);
    }

    private void ConnectionTimeout()
    {
        if (!_isConnecting) return;
        _isConnecting  = false;
        var nm = NetworkManager.Instance;
        _statusMessage = "Impossible de se connecter. Réessaie.";
        if (nm != null && !string.IsNullOrEmpty(nm.LastError))
            _statusMessage += $"\n{nm.LastError}";
    }
}
