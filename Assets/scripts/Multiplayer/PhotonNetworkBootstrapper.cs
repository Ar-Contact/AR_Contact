using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections;

public class PhotonNetworkBootstrapper : MonoBehaviourPunCallbacks
{
    private static PhotonNetworkBootstrapper _instance;
    
    [Header("Connection Settings")]
    [Tooltip("Timeout in seconds for room join/create attempts")]
    public float connectionTimeout = 10f;
    
    [Header("Scene Configuration")]
    [Tooltip("Name of the game scene to load when both players are ready")]
    public string gameSceneName = "SampleScene";
    
    private bool _isConnecting = false;
    private Coroutine _timeoutCoroutine;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("PhotonNetworkBootstrapper already exists, destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Configure Photon settings
        PhotonNetwork.AutomaticallySyncScene = true; // Master client loads scene for all
    }

    private bool _hasStartedConnection = false;

    private void Start()
    {
        Debug.Log("PhotonNetworkBootstrapper: Initialized. Waiting for MatchMaker to set PlayerSession...");
    }

    private void Update()
    {
        // Wait for MatchMaker to complete matchmaking and set PlayerSession.NetworkStarted
        if (!_hasStartedConnection && PlayerSession.NetworkStarted)
        {
            _hasStartedConnection = true;
            
            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║ PHOTON: PlayerSession Ready Detected!    ║");
            Debug.Log("╚═══════════════════════════════════════════╝");
            Debug.Log($"Team: {PlayerSession.Team}");
            Debug.Log($"MatchId: {PlayerSession.MatchId}");
            Debug.Log("Starting Photon connection...");
            
            ConnectToPhoton();
        }
    }

    private void ConnectToPhoton()
    {
        // Validation
        if (string.IsNullOrEmpty(PlayerSession.MatchId))
        {
            Debug.LogError("PlayerSession.MatchId is EMPTY! Cannot create/join room!");
            return;
        }

        // Already connected/connecting check
        if (PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Already connected to Photon. Proceeding to room creation/join...");
            OnConnectedToMaster();
            return;
        }
        
        if (_isConnecting)
        {
            Debug.LogWarning("Already attempting to connect to Photon.");
            return;
        }

        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"PHOTON CONNECTION START");
        Debug.Log($"MatchId: {PlayerSession.MatchId}");
        Debug.Log("Team will be assigned after joining room");
        Debug.Log("═══════════════════════════════════════");

        _isConnecting = true;
        
        // Start connection timeout
        if (_timeoutCoroutine != null)
        {
            StopCoroutine(_timeoutCoroutine);
        }
        _timeoutCoroutine = StartCoroutine(ConnectionTimeoutCheck());
        
        // Connect to Photon Cloud
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Connecting to Photon Cloud...");
    }

    private IEnumerator ConnectionTimeoutCheck()
    {
        yield return new WaitForSeconds(connectionTimeout);
        
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError($"CONNECTION TIMEOUT! Failed to connect/join room within {connectionTimeout} seconds.");
            Debug.LogError("Please check:");
            Debug.LogError("  1. Internet connection");
            Debug.LogError("  2. Photon App ID is configured correctly");
            Debug.LogError("  3. Firewall settings");
            
            _isConnecting = false;
            PhotonNetwork.Disconnect();
        }
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("✓ Connected to Photon Master Server!");
        
        // Use Photon's JoinRandomRoom for automatic matchmaking
        Debug.Log("→ Attempting to join random available room...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"No available rooms found (Code: {returnCode}). Creating new room...");
        
        // No rooms available, create a new one
        string roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
        
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };
        
        Debug.Log($"→ Creating room: {roomName}");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"✓ Successfully joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        Debug.Log($"I am Master Client: {PhotonNetwork.IsMasterClient}");
        
        // TEAM ASSIGNMENT based on join order (replaces Firebase)
        if (PhotonNetwork.IsMasterClient)
        {
            // First player to join = Blue team (Host)
            PlayerSession.Team = "Blue";
            PlayerSession.IsHost = true;
            Debug.Log("→ Assigned as BLUE TEAM (Master Client / Host)");
        }
        else
        {
            // Second player = Red team (Client)
            PlayerSession.Team = "Red";
            PlayerSession.IsHost = false;
            Debug.Log("→ Assigned as RED TEAM (Client)");
        }
        
        // Stop timeout coroutine
        if (_timeoutCoroutine != null)
        {
            StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = null;
        }
        
        _isConnecting = false;
        
        // Check if both players are ready
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Both players in room! Loading game scene...");
            LoadGameScene();
        }
        else
        {
            Debug.Log("Waiting for second player to join...");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"✓ Player joined: {newPlayer.NickName} (ID: {newPlayer.ActorNumber})");
        Debug.Log($"Room now has {PhotonNetwork.CurrentRoom.PlayerCount} players");
        
        // When second player joins, load game scene
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Both players ready! Loading game scene...");
            LoadGameScene();
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"✗ Failed to create room! Code: {returnCode}, Message: {message}");
        _isConnecting = false;
        
        // Room might already exist (if reconnecting), try joining instead
        Debug.Log("Attempting to join existing room instead...");
        PhotonNetwork.JoinRoom(PlayerSession.MatchId);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"✗ Failed to join room! Code: {returnCode}, Message: {message}");
        _isConnecting = false;
        
        // TODO: Show error UI to user
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon. Cause: {cause}");
        _isConnecting = false;
        
        // TODO: Handle reconnection or show error UI
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"Player left room: {otherPlayer.NickName}");
        
        // TODO: Handle player disconnect (pause game, show message, etc.)
    }

    #endregion

    private void LoadGameScene()
    {
        // Only master client loads the scene (others will sync automatically)
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Master Client loading scene: {gameSceneName}");
            PhotonNetwork.LoadLevel(gameSceneName);
        }
        else
        {
            Debug.Log("Waiting for Master Client to load scene...");
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        
        if (_timeoutCoroutine != null)
        {
            StopCoroutine(_timeoutCoroutine);
        }
    }
}
