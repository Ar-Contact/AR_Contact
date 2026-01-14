using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore; // Eklendi
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Dictionary için eklendi

public class FirebaseAuthUI : MonoBehaviour
{
    [Header("UI")]
    public InputField emailInput;
    public InputField passwordInput;
    public InputField nicknameInput; // Yeni: Kullanýcýdan nickname almak için
    public Text statusText;
    public Text status_2;

    private FirebaseAuth auth;
    private FirebaseFirestore db; // Firestore referansý

    void Start()
    {
        statusText.text = "Firebase kontrol ediliyor...";

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance; // Firestore baþlatýldý
                statusText.text = "Firebase hazýr";
            }
            else
            {
                statusText.text = "Firebase hata: " + task.Result;
            }
        });
    }

    public void Login()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Email ve þifre gir";
            return;
        }

        statusText.text = "Giriþ yapýlýyor...";

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                statusText.text = "Giriþ baþarýsýz";
                return;
            }

            // Giriþ baþarýlý, sahne deðiþtir
            SceneManager.LoadScene("StartGame");
        });
    }

    public void Register()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string nickname = nicknameInput != null ? nicknameInput.text : "Yeni Oyuncu";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Email ve þifre gir";
            return;
        }

        statusText.text = "Kayýt oluþturuluyor...";

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                statusText.text = "Kayýt baþarýsýz";
                return;
            }

            FirebaseUser user = task.Result.User;

            // AUTH BAÞARILI -> ÞÝMDÝ VERÝTABANINA KAYDET
            CreateUserInFirestore(user.UserId, user.Email, nickname);
        });
    }

    private void CreateUserInFirestore(string uid, string email, string nickname)
    {
        // /users/{uid} yoluna döküman referansý al
        DocumentReference userRef = db.Collection("users").Document(uid);

        // Senin istediðin þema
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "email", email },
            { "nickname", nickname },
            { "level", 1 },
            { "totalScore", 0 },
            { "matchesPlayed", 0 },
            { "createdAt", FieldValue.ServerTimestamp }, // Sunucu saati
        };

        userRef.SetAsync(userData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                statusText.text = "Kayýt ve Profil Baþarýlý!";
                status_2.text = "Bilgilerinizle giriþ yapabilirsiniz.";
                Debug.Log("Firestore: Kullanýcý dökümaný oluþturuldu.");
            }
            else
            {
                statusText.text = "Auth baþarýlý ama veri yazýlamadý!";
                Debug.LogError(task.Exception);
            }
        });
    }
}