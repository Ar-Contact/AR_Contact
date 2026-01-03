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
    private string _sceneToLoad = "Scenes/SampleScene";
    
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
            Debug.Log("Sahne yukleniyor...");
            
            // CRITICAL: Signal NetworkBootstrapper that PlayerSession is ready
            PlayerSession.NetworkStarted = true;
            Debug.Log($"PlayerSession ready - Team: {PlayerSession.Team}, IsHost: {PlayerSession.IsHost}");
            
            SceneManager.LoadScene(_sceneToLoad);
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
            await JoinOrCreateMatch();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HATA OLUSTU: {e.Message}\n{e.StackTrace}");
            _isGameReadyToStart = false;
        }
    }

    async Task JoinOrCreateMatch()
    {
        Debug.Log("Firestore sorgusu basliyor...");

        try
        {
            Debug.Log("Mac araniyor...");
            
            QuerySnapshot matches = await db.Collection("matches")
                .WhereEqualTo("status", "waiting")
                .Limit(1)
                .GetSnapshotAsync();
            
            Debug.Log($"Query completed! Found {matches.Count} matches");

            if (matches.Count > 0)
            {
                Debug.Log("Bekleyen mac bulundu!");
                DocumentSnapshot match = matches.Documents.First();
                bool joined = await AssignTeam(match.Reference, match.Id);

                if (!joined)
                {
                    Debug.Log("Maca girilemedi (Dolu olabilir), yeni kuruluyor...");
                    await CreateMatch();
                }
            }
            else
            {
                Debug.Log("Mac bulunamadi, yeni olusturuluyor...");
                await CreateMatch();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FIRESTORE ERROR: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            
            // Fallback: create new match
            Debug.Log("Creating new match after error...");
            await CreateMatch();
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
