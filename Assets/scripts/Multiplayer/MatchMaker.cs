using Firebase;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;

public class MatchMaker : MonoBehaviour
{
    FirebaseFirestore db;
    FirebaseAuth auth;

    private SynchronizationContext _mainThreadContext;
    private bool _isGameReadyToStart = false;
    private string _sceneToLoad = "Scenes/SampleScene";

    private void Awake()
    {
        _mainThreadContext = SynchronizationContext.Current;
    }

    private void Update()
    {
        if (_isGameReadyToStart)
        {
            _isGameReadyToStart = false;
            Debug.Log(" Sahne yükleniyor...");
            SceneManager.LoadScene(_sceneToLoad);
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
                Debug.LogError($"Firebase Hatasý: {dependencyStatus}");
            }
        });
    }

    void InitializeFirebaseAndStart()
    {
        db = FirebaseFirestore.DefaultInstance;


        auth = FirebaseAuth.DefaultInstance;

#if UNITY_EDITOR
        // Editörde Rastgele ID
        PlayerSession.UserId = "Editor_User_" + Random.Range(100, 999);
        Debug.LogWarning($"Editör ID: {PlayerSession.UserId}");
        // async void yerine Task kullanýp hatayý yakalamak için özel çaðrý
        RunSafeJoin();
#else
        // Build
        if (auth.CurrentUser == null)
        {
            auth.SignInAnonymouslyAsync().ContinueWith(authTask => 
            {
                if (authTask.IsCanceled || authTask.IsFaulted) return;
                _mainThreadContext.Post(_ => {
                    PlayerSession.UserId = auth.CurrentUser.UserId;
                    RunSafeJoin();
                }, null);
            });
        }
        else
        {
            PlayerSession.UserId = auth.CurrentUser.UserId;
            RunSafeJoin();
        }
#endif
    }

    // async void hatasýný önlemek için sarmalayýcý fonksiyon
    async void RunSafeJoin()
    {
        try
        {
            await JoinOrCreateMatch();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HATA OLUÞTU: {e.Message}\n{e.StackTrace}");
        }
    }

    async Task JoinOrCreateMatch()
    {
        Debug.Log("Firestore sorgusu baþlýyor...");

        

        Debug.Log("Maç aranýyor...");
        QuerySnapshot matches = await db.Collection("matches")
            .WhereEqualTo("status", "waiting")
            .Limit(1)
            .GetSnapshotAsync();

        if (matches.Count > 0)
        {
            Debug.Log("Bekleyen maç bulundu!");
            DocumentSnapshot match = matches.Documents.First();
            bool joined = await AssignTeam(match.Reference, match.Id);

            if (!joined)
            {
                Debug.Log("Maça girilemedi (Dolu olabilir), yeni kuruluyor...");
                await CreateMatch();
            }
        }
        else
        {
            Debug.Log("Maç bulunamadý, yeni oluþturuluyor...");
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
            { "players", players }
        };

        await matchRef.SetAsync(data);

        PlayerSession.MatchId = matchRef.Id;
        PlayerSession.Team = "Blue";
        PlayerSession.IsHost = true;

        Debug.Log("HOST: Maç oluþturuldu ve bekliniyor.");
        _isGameReadyToStart = true;
    }

    async Task<bool> AssignTeam(DocumentReference matchRef, string matchId)
    {
        // Transaction kullanarak veri tutarlýlýðýný saðla
        return await db.RunTransactionAsync(async transaction =>
        {
            DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(matchRef);

            // Veriyi güvenli çekme
            Dictionary<string, object> players = null;
            if (snapshot.Exists && snapshot.ContainsField("players"))
            {
                players = snapshot.GetValue<Dictionary<string, object>>("players");
            }

            if (players == null) players = new Dictionary<string, object>();

            if (players.Count >= 2)
            {
                Debug.LogWarning("Maç dolmuþ!");
                return false;
            }

            // Takým belirleme
            string team = players.Values.Contains("Blue") ? "Red" : "Blue";

            // Local Dictionary güncellemesi
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

            // Session ayarlarý
            PlayerSession.MatchId = matchId;
            PlayerSession.Team = team;
            PlayerSession.IsHost = (team == "Blue");

            Debug.Log($"CLIENT: Maça katýldý. Takým: {team}");

            return true;
        }).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Transaction Baþarýsýz: " + task.Exception);
                return false;
            }

            if (task.Result)
            {
                // Baþarýlý olursa sahneyi yükle
                _isGameReadyToStart = true;
                return true;
            }
            return false;
        });
    }
}