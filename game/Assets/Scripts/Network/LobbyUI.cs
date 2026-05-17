using UnityEngine;

/// <summary>
/// Lobby UI displayed at scene start. Player enters a name, picks a color,
/// and clicks "Rejoindre" to connect to the arena.
/// Manages the full pre-game → in-game transition:
///  - Hides the Player hierarchy until connected
///  - Activates a spectator camera while in lobby
///  - Teleports the player ball to the server spawn position on join
/// Uses Dear ImGui–style skin via ImGuiSkin.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The root 'Player' GameObject (contains PlayerSphere + cameras). Will be deactivated until connected.")]
    public GameObject playerRoot;

    [Tooltip("The spectator camera GameObject (SpectatorCamera component).")]
    public SpectatorCamera spectatorCamera;

    // Preset colors for selection
    private static readonly Color[] PresetColors = new Color[]
    {
        new Color(1f, 0.35f, 0.2f),    // Orange-red
        new Color(0.2f, 0.6f, 1f),     // Blue
        new Color(0.3f, 1f, 0.4f),     // Green
        new Color(1f, 0.85f, 0.1f),    // Yellow
        new Color(0.8f, 0.3f, 1f),     // Purple
        new Color(1f, 0.5f, 0.7f),     // Pink
    };

    private static readonly string[] ColorNames = new string[]
    {
        "Rouge", "Bleu", "Vert", "Jaune", "Violet", "Rose"
    };

    // UI state
    private bool _lobbyActive = true;
    private string _playerName = "";
    private int _selectedColorIndex = 0;
    private string _statusMessage = "";
    private bool _isConnecting = false;
    private bool _isReady = false;

    // Cached color preview texture (avoid per-frame leak)
    private Texture2D _colorPreviewTex;
    private int _lastPreviewColorIndex = -1;

    void Start()
    {
        // Generate a default name
        _playerName = "Joueur" + Random.Range(100, 999);

        // --- Hide the player hierarchy until connected ---
        if (playerRoot != null)
            playerRoot.SetActive(false);

        // --- Activate spectator camera ---
        if (spectatorCamera != null)
        {
            // Wire the gameplay camera reference so spectator knows what to re-enable
            var gameplayCam = playerRoot?.GetComponentInChildren<Camera>(true);
            if (gameplayCam != null)
                spectatorCamera.gameplayCamera = gameplayCam;

            spectatorCamera.Activate();
        }

        // Subscribe to network events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnected += OnConnected;
            NetworkManager.Instance.OnDisconnected += OnDisconnected;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnected -= OnConnected;
            NetworkManager.Instance.OnDisconnected -= OnDisconnected;
        }
    }

    private void OnConnected()
    {
        _lobbyActive = false;
        _isConnecting = false;
        _statusMessage = "";
        CancelInvoke(nameof(CheckConnectionTimeout));

        // --- Activate the player hierarchy ---
        if (playerRoot != null)
            playerRoot.SetActive(true);

        // Teleport player ball to the server-assigned spawn position
        var nm = NetworkManager.Instance;
        if (nm != null && playerRoot != null)
        {
            var pc = playerRoot.GetComponentInChildren<PlayerController>(true);
            if (pc != null)
            {
                // Get spawn pos from the local player's state in the room
                var localState = nm.GetLocalPlayerState();
                if (localState != null)
                {
                    Vector3 spawnPos = new Vector3(localState.x, localState.y, localState.z);
                    var rb = pc.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.position = spawnPos;
                    }
                    pc.transform.position = spawnPos;
                    pc.SetSpawnPosition(spawnPos);
                    Debug.Log($"[Lobby] Player teleported to spawn: {spawnPos}");
                }
                pc.enabled = true;

                // Setup local player visuals: 50% color tint + floating name label
                pc.SetupLocalPlayer(nm.LocalPlayerName, nm.LocalPlayerColor);
            }
        }

        // --- Switch from spectator to gameplay camera ---
        if (spectatorCamera != null)
            spectatorCamera.Deactivate();

        // Unlock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisconnected()
    {
        _lobbyActive = true;
        _isConnecting = false;
        _isReady = false;
        _statusMessage = "Déconnecté du serveur";

        // Show cursor for lobby
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- Deactivate the player hierarchy ---
        if (playerRoot != null)
        {
            var pc = playerRoot.GetComponentInChildren<PlayerController>(true);
            if (pc != null) pc.enabled = false;
            playerRoot.SetActive(false);
        }

        // --- Re-enable spectator camera ---
        if (spectatorCamera != null)
            spectatorCamera.Activate();
    }

    void OnGUI()
    {
        if (!_lobbyActive) return;

        ImGuiSkin.EnsureReady();

        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ImGuiSkin.DrawOverlay();

        bool isConnected = NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;

        if (!isConnected)
        {
            // ── Pre-connect panel ────────────────────────────────────────
            float panelWidth = 420;
            float panelHeight = 440;
            ImGuiSkin.BeginWindow(panelWidth, panelHeight, "ROLL'D");

            GUILayout.Label("Rejoindre l'arène multijoueur", ImGuiSkin.WindowSubtitle);
            GUILayout.Space(16);

            ImGuiSkin.DrawSectionHeader("PSEUDO");
            GUILayout.Space(4);
            _playerName = GUILayout.TextField(_playerName, 16, ImGuiSkin.TextField, GUILayout.Height(30));
            GUILayout.Space(12);

            ImGuiSkin.DrawSectionHeader("COULEUR");
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < PresetColors.Length; i++)
            {
                Color c = PresetColors[i];
                bool selected = _selectedColorIndex == i;
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = selected ? c : c * 0.7f;
                GUIStyle btnStyle = new GUIStyle(ImGuiSkin.ButtonSmall)
                {
                    fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
                };
                if (selected) btnStyle.normal.textColor = Color.white;
                string label = selected ? $"▸ {ColorNames[i]}" : ColorNames[i];
                if (GUILayout.Button(label, btnStyle, GUILayout.Height(32), GUILayout.Width(60)))
                    _selectedColorIndex = i;
                GUI.backgroundColor = prevBg;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

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
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(_colorPreviewTex, GUIStyle.none, GUILayout.Width(80), GUILayout.Height(16));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(16);

            GUI.enabled = !_isConnecting && !string.IsNullOrWhiteSpace(_playerName);
            string buttonText = _isConnecting ? "Connexion..." : "▶ Rejoindre l'arène";
            if (GUILayout.Button(buttonText, ImGuiSkin.ButtonAccent, GUILayout.Height(44)))
                JoinArena();
            GUI.enabled = true;
            GUILayout.Space(8);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                bool isError = _statusMessage.Contains("Erreur") || _statusMessage.Contains("Déconnecté");
                GUIStyle statusStyle = isError ? ImGuiSkin.StatusRed : new GUIStyle(ImGuiSkin.Hint);
                if (!isError) statusStyle.normal.textColor = ImGuiSkin.ColYellow;
                GUILayout.Label(_statusMessage, statusStyle);
            }

            ImGuiSkin.EndWindow();
        }
        else
        {
            // ── Waiting room panel (connected, waiting for game to start) ──
            float panelWidth = 380;
            float panelHeight = 320;
            ImGuiSkin.BeginWindow(panelWidth, panelHeight, "SALLE D'ATTENTE");

            GUILayout.Label("En attente des joueurs...", ImGuiSkin.WindowSubtitle);
            GUILayout.Space(12);

            // Player list
            ImGuiSkin.DrawSectionHeader("JOUEURS CONNECTÉS");
            GUILayout.Space(4);
            var nm = NetworkManager.Instance;
            if (nm != null && nm.IsConnected)
            {
                // We can't directly iterate NetworkState.players from here easily,
                // so show basic count
                var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                style.normal.textColor = new Color(0.75f, 0.75f, 0.85f);
                GUILayout.Label($"  {nm.PlayerCount} joueur(s) dans la salle", style);
            }
            GUILayout.Space(16);

            // Ready button
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
                var readyStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                };
                readyStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);
                GUILayout.Label("✔ Prêt ! En attente des autres...", readyStyle, GUILayout.Height(44));
            }

            GUILayout.Space(8);
            var hintStyle = new GUIStyle(ImGuiSkin.Hint);
            hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.6f);
            GUILayout.Label("La partie démarre quand tout le monde est prêt\nou automatiquement après 30 secondes.", hintStyle);

            ImGuiSkin.EndWindow();
        }
    }

    private void JoinArena()
    {
        if (NetworkManager.Instance == null)
        {
            _statusMessage = "Erreur : NetworkManager introuvable";
            return;
        }

        if (string.IsNullOrWhiteSpace(_playerName))
        {
            _statusMessage = "Entrez un pseudo";
            return;
        }

        _isConnecting = true;
        _statusMessage = "Connexion au serveur...";

        Color selectedColor = PresetColors[_selectedColorIndex];
        NetworkManager.Instance.JoinArena(_playerName.Trim(), selectedColor);

        // Monitor for errors after a delay
        Invoke(nameof(CheckConnectionTimeout), 10f);
    }

    private void CheckConnectionTimeout()
    {
        if (_isConnecting && !NetworkManager.Instance.IsConnected)
        {
            _isConnecting = false;
            _statusMessage = "Erreur : Impossible de joindre rolld.io. Réessayez dans quelques instants.";
            if (!string.IsNullOrEmpty(NetworkManager.Instance.LastError))
            {
                _statusMessage += $"\n{NetworkManager.Instance.LastError}";
            }
        }
    }
}
