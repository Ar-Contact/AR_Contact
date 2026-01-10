using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;

public class NetworkBootstrapper : MonoBehaviour
{
    // ADDED: Singleton pattern to prevent multiple instances
    private static NetworkBootstrapper _instance;
    
    [Header("Network Configuration")]
    [Tooltip("IP address to connect to as client")]
    public string hostIPAddress = "192.168.0.14";
    
    [Tooltip("Port for network connections")]
    public ushort networkPort = 7777;
    
    [Header("Connection Settings")]
    [Tooltip("Timeout in seconds for client connection attempts")]
    public float connectionTimeout = 10f;
    
    private void Awake()
    {
        // ADDED: Singleton pattern
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("NetworkBootstrapper already exists, destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // CRITICAL FIX: Check if PlayerSession is initialized by MatchMaker
        if (!PlayerSession.NetworkStarted)
        {
            Debug.Log("NetworkBootstrapper: Waiting for PlayerSession to be initialized by MatchMaker...");
            StartCoroutine(WaitForPlayerSessionAndStart());
            return;
        }
        
        InitializeNetwork();
    }
    
    // ADDED: Wait for MatchMaker to initialize PlayerSession
    private IEnumerator WaitForPlayerSessionAndStart()
    {
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!PlayerSession.NetworkStarted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (!PlayerSession.NetworkStarted)
        {
            Debug.LogError("TIMEOUT: PlayerSession was never initialized! Did MatchMaker run?");
            yield break;
        }
        
        Debug.Log("PlayerSession ready! Starting network...");
        InitializeNetwork();
    }
    
    // REFACTORED: Network initialization logic
    private void InitializeNetwork()
    {
        // Check for existing network
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is NULL! Make sure NetworkManager exists in scene.");
            return;
        }
        
        // Network already running check (prevent duplicate starts)
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network already running, skipping initialization.");
            return;
        }

        // Team safety check
        if (string.IsNullOrEmpty(PlayerSession.Team))
        {
            Debug.LogError("PlayerSession.Team is EMPTY! Matchmaking failed?");
            return;
        }

        // Get Unity Transport component
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport component not found on NetworkManager!");
            return;
        }

        // DIAGNOSTIC: Show what team is starting
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"NETWORK START: Team = '{PlayerSession.Team}'");
        Debug.Log($"IsHost = {PlayerSession.IsHost}");
        Debug.Log("═══════════════════════════════════════");

        // Start Host or Client based on team
        if (PlayerSession.Team == "Blue")
        {
            Debug.Log("→ Starting as HOST (Blue Team)");
            StartAsHost(transport);
        }
        else if (PlayerSession.Team == "Red")
        {
            Debug.Log("→ Starting as CLIENT (Red Team)");
            StartAsClient(transport);
        }
        else
        {
            Debug.LogError($"Unknown team: {PlayerSession.Team}");
        }
    }
    
    // ADDED: Separate method for Host initialization with error handling
    private void StartAsHost(UnityTransport transport)
    {
        Debug.Log($"HOST baslatiliyor (Blue Team) on port {networkPort}...");
        
        try
        {
            // CRITICAL: Check if port is already in use
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                Debug.LogWarning("HOST already running! Skipping duplicate start.");
                return;
            }
            
            // Configure transport for host (listen on all interfaces)
            transport.SetConnectionData("0.0.0.0", networkPort);
            
            // FIXED: Check if StartHost succeeded
            bool success = NetworkManager.Singleton.StartHost();
            
            if (success)
            {
                Debug.Log("HOST basarili bir sekilde basladi!");
                PlayerSession.NetworkStarted = true;
            }
            else
            {
                Debug.LogError($"HOST BASLATILIMADI! Port {networkPort} kullanımda olabilir veya baska bir hata olustu.");
                Debug.LogError("Cozum: Unity Editor'u ve build'i kapatip tekrar deneyin, veya farkli bir port kullananin.");
                PlayerSession.NetworkStarted = false;
                
                // ADDED: Shutdown to clean up failed state
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HOST baslatilirken hata olustu: {e.Message}");
            Debug.LogError(e.StackTrace);
            PlayerSession.NetworkStarted = false;
            
            // ADDED: Shutdown on exception
            try
            {
                NetworkManager.Singleton?.Shutdown();
            }
            catch { }
        }
    }
    
    // ADDED: Separate method for Client initialization with timeout
    private void StartAsClient(UnityTransport transport)
    {
        Debug.Log($"CLIENT baslatiliyor (Red Team) - Connecting to {hostIPAddress}:{networkPort}...");
        
        try
        {
            // CRITICAL: Check if already connected or connecting
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.LogWarning("CLIENT already running or connected! Skipping duplicate start.");
                return;
            }
            
            // FIXED: Use configurable IP address instead of hardcoded localhost
            transport.SetConnectionData(hostIPAddress, networkPort);
            
            // FIXED: Check if StartClient succeeded
            bool success = NetworkManager.Singleton.StartClient();
            
            if (success)
            {
                Debug.Log("CLIENT baglanti denemesi basladi...");
                // ADDED: Start connection timeout check
                StartCoroutine(CheckConnectionTimeout());
            }
            else
            {
                Debug.LogError("CLIENT BASLATILIMADI! Baglanti baslatma basarisiz.");
                Debug.LogError($"Cozum: Host'un {hostIPAddress}:{networkPort} adresinde calistigindan emin olun.");
                PlayerSession.NetworkStarted = false;
                
                // ADDED: Shutdown to clean up failed state
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CLIENT baslatilirken hata olustu: {e.Message}");
            Debug.LogError(e.StackTrace);
            PlayerSession.NetworkStarted = false;
            
            // ADDED: Shutdown on exception
            try
            {
                NetworkManager.Singleton?.Shutdown();
            }
            catch { }
        }
    }
    
    // ADDED: Connection timeout coroutine
    private IEnumerator CheckConnectionTimeout()
    {
        float elapsed = 0f;
        
        while (elapsed < connectionTimeout)
        {
            // Check if connected
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log($"CLIENT basarili bir sekilde baglandi! (Gecen sure: {elapsed:F1}s)");
                PlayerSession.NetworkStarted = true;
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Timeout reached without connection
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogError($"CONNECTION TIMEOUT! {connectionTimeout} saniye icinde Host'a baglan�lamadi.");
            Debug.LogError($"Host IP: {hostIPAddress}:{networkPort}");
            Debug.LogError("Kontrol edin:");
            Debug.LogError("  1. Host calisiy?? mu?");
            Debug.LogError("  2. IP adresi dogru mu?");
            Debug.LogError($"  3. Port {networkPort} acik mi?");
            Debug.LogError("  4. Firewall engelliyor mu?");
            
            // Shutdown the failed connection attempt
            NetworkManager.Singleton.Shutdown();
            PlayerSession.NetworkStarted = false;
            
            // TODO: Show error UI to user and allow retry
        }
    }
    
    private void OnDestroy()
    {
        // Clean up singleton reference
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
