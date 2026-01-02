using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // BU KÜTÜPHANEYÝ EKLE

public class NetworkBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Network zaten çalýþýyorsa durdur (Çakýþma önlemi)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;

        // Team güvenliði
        if (string.IsNullOrEmpty(PlayerSession.Team))
        {
            PlayerSession.Team = "Blue"; // Test için varsayýlan
        }

        // --- BAÐLANTI AYARLARI ---
        // Unity Transport bileþenine eriþiyoruz
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (PlayerSession.Team == "Blue")
        {
            Debug.Log("HOST baþlatýlýyor (Blue)...");
            // Host için ayara gerek yok, otomatik bind eder.
            NetworkManager.Singleton.StartHost();
        }
        else if (PlayerSession.Team == "Red")
        {
            Debug.Log("CLIENT baþlatýlýyor (Red)...");

            // --- KRÝTÝK DÜZELTME ---
            // Ýkinci oyuncu (Client), nereye baðlanacaðýný bilmeli.
            // Ayný PC'de test ediyorsan "127.0.0.1" (Localhost) olmalý.
            // Farklý cihazlarda test ediyorsan Host'un Yerel IP'si (örn: 192.168.1.35) olmalý.

            // Test için Localhost'a sabitliyoruz:
            transport.SetConnectionData("127.0.0.1", 7777);

            NetworkManager.Singleton.StartClient();
        }

        PlayerSession.NetworkStarted = true;
    }
}