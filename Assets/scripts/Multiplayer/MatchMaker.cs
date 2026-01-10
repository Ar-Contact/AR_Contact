using Firebase;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;

public class MatchMaker : MonoBehaviour
{
    FirebaseFirestore db;
    FirebaseAuth auth;

    private SynchronizationContext _mainThreadContext;
    private bool _isGameReadyToStart = false;
    
    private ListenerRegistration _matchListener;

    private void Awake()
    {
        _mainThreadContext = SynchronizationContext.Current;
    }

    private void Update()
    {
        if (_isGameReadyToStart)
        {
            _isGameReadyToStart = false;
            
            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║ MATCHMAKER: GAME READY TO START!        ║");
            Debug.Log("╚═══════════════════════════════════════════╝");
            
            // CRITICAL: Signal PhotonNetworkBootstrapper that PlayerSession is ready
            // PhotonNetworkBootstrapper will handle Photon connection and scene loading
            PlayerSession.NetworkStarted = true;
            Debug.Log($"✓ PlayerSession.NetworkStarted = true");
            Debug.Log($"✓ Team: {PlayerSession.Team}");
            Debug.Log($"✓ IsHost: {PlayerSession.IsHost}");
            Debug.Log($"✓ MatchId: {PlayerSession.MatchId}");
            Debug.Log("→ PhotonNetworkBootstrapper will now connect to Photon and join room...");
        }
    }
    
    private void OnDestroy()
    {
        if (_matchListener != null)
        {
            _matchListener.Stop();
            _matchListener = null;
        }
    }

    public void StartGame()
    {
        PlayerSession.NetworkStarted = false;
        PlayerSession.Team = "";
        PlayerSession.IsHost = false;
        PlayerSession.MatchId = "";

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                _mainThreadContext.Post(_ => InitializeFirebaseAndStart(), null);
            }
            else
            {
                Debug.LogError($"Firebase Hatasi: {dependencyStatus}");
            }
        });
    }

    void InitializeFirebaseAndStart()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // FIXED: Always use Firebase Auth (both Editor and Build)
        if (auth.CurrentUser == null)
        {
            Debug.Log("Firebase Anonymous Auth baslatiliyor...");
            auth.SignInAnonymouslyAsync().ContinueWith(authTask => 
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    Debug.LogError("Firebase Auth BASARISIZ!");
                    if (authTask.Exception != null)
                    {
                        Debug.LogError(authTask.Exception);
                    }
                    return;
                }
                
                _mainThreadContext.Post(_ => {
                    PlayerSession.UserId = auth.CurrentUser.UserId;
                    Debug.Log($"Auth basarili! User ID: {PlayerSession.UserId}");
                    RunSafeJoin();
                }, null);
            });
        }
        else
        {
            PlayerSession.UserId = auth.CurrentUser.UserId;
            Debug.Log($"Mevcut kullanici: {PlayerSession.UserId}");
            RunSafeJoin();
        }
    }

    async void RunSafeJoin()
    {
        try
        {
            // SOLUTION: Use Photon's matchmaking instead of Firebase
            // Firebase Firestore is broken in Windows builds (async crash)
            Debug.Log("Using Photon matchmaking (Firebase Firestore disabled for builds)...");
            
            // Generate a random match ID for Photon room
            PlayerSession.MatchId = System.Guid.NewGuid().ToString().Substring(0, 8);
            
            // Signal that we're ready to connect to Photon
            // PhotonNetworkBootstrapper will handle team assignment based on IsMasterClient
            _isGameReadyToStart = true;
            
            Debug.Log($"Match ID generated: {PlayerSession.MatchId}");
            Debug.Log("Ready to connect to Photon. Team will be assigned after joining room.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error: {e.Message}\n{e.StackTrace}");
            _isGameReadyToStart = false;
        }
    }

    async Task JoinOrCreateMatch()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("MATCHMAKER: JoinOrCreateMatch started");
        Debug.Log("═══════════════════════════════════════");

        try
        {
            Debug.Log("[STEP 1] Checking Firestore connection...");
            
            if (db == null)
            {
                Debug.LogError("CRITICAL: Firestore db is NULL! Cannot query.");
                Debug.LogError("Solution: Ensure Firebase is initialized properly.");
                return;
            }
            
            Debug.Log("[STEP 2] Building Firestore query...");
            var matchesQuery = db.Collection("matches")
                .WhereEqualTo("status", "waiting")
                .Limit(1);
            
            Debug.Log("[STEP 3] Executing GetSnapshotAsync...");
            Debug.Log("WARNING: If build freezes here, check Firestore permissions!");
            
            QuerySnapshot matches = null;
            
            try
            {
                matches = await matchesQuery.GetSnapshotAsync();
                Debug.Log($"[STEP 4] ✓ Query completed successfully! Found {matches.Count} matches");
            }
            catch (System.Exception queryEx)
            {
                Debug.LogError($"[STEP 4] ✗ GetSnapshotAsync FAILED!");
                Debug.LogError($"Exception Type: {queryEx.GetType().Name}");
                Debug.LogError($"Message: {queryEx.Message}");
                Debug.LogError($"Stack: {queryEx.StackTrace}");
                
                if (queryEx.InnerException != null)
                {
                    Debug.LogError($"Inner Exception: {queryEx.InnerException.Message}");
                }
                
                Debug.Log("[FALLBACK] Creating new match instead of querying...");
                await CreateMatch();
                return;
            }

            if (matches.Count > 0)
            {
                Debug.Log("[STEP 5] Waiting match found! Attempting to join...");
                DocumentSnapshot match = matches.Documents.First();
                
                Debug.Log($"Match ID: {match.Id}");
                Debug.Log($"Match exists: {match.Exists}");
                
                bool joined = await AssignTeam(match.Reference, match.Id);

                if (!joined)
                {
                    Debug.Log("[STEP 6] Join failed (match full?), creating new match...");
                    await CreateMatch();
                }
                else
                {
                    Debug.Log("[STEP 6] ✓ Successfully joined existing match!");
                }
            }
            else
            {
                Debug.Log("[STEP 5] No waiting matches found, creating new match...");
                await CreateMatch();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("╔═══════════════════════════════════════════╗");
            Debug.LogError("║ CRITICAL ERROR IN JoinOrCreateMatch      ║");
            Debug.LogError("╚═══════════════════════════════════════════╝");
            Debug.LogError($"Exception Type: {e.GetType().Name}");
            Debug.LogError($"Message: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            
            if (e.InnerException != null)
            {
                Debug.LogError($"Inner Exception Type: {e.InnerException.GetType().Name}");
                Debug.LogError($"Inner Message: {e.InnerException.Message}");
            }
            
            Debug.Log("[EMERGENCY FALLBACK] Attempting to create new match...");
            
            try
            {
                await CreateMatch();
            }
            catch (System.Exception createEx)
            {
                Debug.LogError($"EMERGENCY FALLBACK ALSO FAILED: {createEx.Message}");
                Debug.LogError("Application is in an unrecoverable state!");
            }
        }
    }

    async Task CreateMatch()
    {
        DocumentReference matchRef = db.Collection("matches").Document();

        var players = new Dictionary<string, object>
        {
            { PlayerSession.UserId, "Blue" }
        };

        var data = new Dictionary<string, object>
        {
            { "status", "waiting" },
            { "players", players },
            { "createdAt", FieldValue.ServerTimestamp },
            { "expiresAt", FieldValue.ServerTimestamp }
        };

        await matchRef.SetAsync(data);

        PlayerSession.MatchId = matchRef.Id;
        PlayerSession.Team = "Blue";
        PlayerSession.IsHost = true;

        Debug.Log($"HOST: Mac olusturuldu (ID: {matchRef.Id}). Bekleniyor...");
        
        ListenForMatchReady(matchRef);
    }

    async Task<bool> AssignTeam(DocumentReference matchRef, string matchId)
    {
        bool transactionSuccess = false;
        
        try
        {
            transactionSuccess = await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(matchRef);

                Dictionary<string, object> players = null;
                if (snapshot.Exists && snapshot.ContainsField("players"))
                {
                    players = snapshot.GetValue<Dictionary<string, object>>("players");
                }

                if (players == null) players = new Dictionary<string, object>();

                if (players.Count >= 2)
                {
                    Debug.LogWarning("Mac dolmus!");
                    return false;
                }

                string team = players.Values.Contains("Blue") ? "Red" : "Blue";

                if (players.ContainsKey(PlayerSession.UserId))
                {
                    players[PlayerSession.UserId] = team;
                }
                else
                {
                    players.Add(PlayerSession.UserId, team);
                }

                var updates = new Dictionary<string, object>
                {
                    { "players", players }
                };

                if (players.Count == 2)
                {
                    updates.Add("status", "playing");
                }

                transaction.Update(matchRef, updates);

                PlayerSession.MatchId = matchId;
                PlayerSession.Team = team;
                PlayerSession.IsHost = (team == "Blue");

                Debug.Log($"CLIENT: Maca katildi. Takim: {team}");

                return true;
            });
            
            if (transactionSuccess)
            {
                _isGameReadyToStart = true;
                return true;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Transaction BASARISIZ: " + e.Message);
            if (e.InnerException != null)
            {
                Debug.LogError("Inner Exception: " + e.InnerException.Message);
            }
            
            _isGameReadyToStart = false;
            return false;
        }
    }
    
    private void ListenForMatchReady(DocumentReference matchRef)
    {
        Debug.Log("Ikinci oyuncu bekleniyor...");
        
        _matchListener = matchRef.Listen(snapshot => 
        {
            if (snapshot.Exists && snapshot.ContainsField("status"))
            {
                string status = snapshot.GetValue<string>("status");
                Debug.Log($"Match status guncellendi: {status}");
                
                if (status == "playing")
                {
                    _mainThreadContext.Post(_ => {
                        Debug.Log("Iki oyuncu da hazir! Oyun basliyor...");
                        _isGameReadyToStart = true;
                        
                        if (_matchListener != null)
                        {
                            _matchListener.Stop();
                            _matchListener = null;
                        }
                        
                        // ADDED: Delete match after 10 seconds
                        StartCoroutine(DeleteMatchAfterDelay(matchRef, 10f));
                    }, null);
                }
            }
        });
    }
    
    // ADDED: Auto-delete match document to prevent reuse
    private IEnumerator DeleteMatchAfterDelay(DocumentReference matchRef, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        matchRef.DeleteAsync().ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Match document deleted from Firebase (cleanup)");
            }
            else if (task.IsFaulted)
            {
                Debug.LogWarning("Failed to delete match: " + task.Exception);
            }
        });
    }
}
