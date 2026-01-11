using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonNetworkBootstrapper : MonoBehaviourPunCallbacks
{
    private static PhotonNetworkBootstrapper _instance;

    [Header("Scene Configuration")]
    public string gameSceneName = "SampleScene";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // ESKİ UPDATE VE CONNECT FONKSİYONLARINI SİLDİK
    // MatchMaker zaten bağlantıyı yönetiyor.

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Oyuncu katıldı: {newPlayer.NickName}");

        // İkinci oyuncu geldiğinde Master sahneyi yükler
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Oda doldu, arena yükleniyor...");
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }
}