using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MatchMaker : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject createRoomPanel;
    public GameObject roomListPanel;

    // YENİ EKLENEN: Renk Paneli Referansı
    public GameObject renkDedektoruPaneli;

    [Header("UI Elements")]
    public InputField roomNameInput;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    // Hafızada odaları tutmak için sözlük
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private void Start()
    {
        PlayerSession.NetworkStarted = false;

        // Başlangıçta panelleri sıfırla
        ShowPanel(mainPanel);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    #region Panel Kontrolleri

    // 1. ADIM: Ana Menüdeki "Oda Kur" butonu ARTIK BUNU ÇAĞIRMALI
    public void OdaKurButonunaBasildi()
    {
        // CreateRoomPanel yerine ÖNCE Renk panelini açıyoruz
        ShowPanel(renkDedektoruPaneli);
    }

    // 2. ADIM: Renk seçilip "Tamam" denince RenkDedektoru scripti BUNU ÇAĞIRACAK
    public void RenkSecimiTamamlandi()
    {
        // Renk bitti, şimdi oda kurma ekranını aç
        ShowPanel(createRoomPanel);
    }

    // Mevcut Oda Listesi Açma Fonksiyonu
    public void OpenRoomListPanel()
    {
        ShowPanel(roomListPanel);
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public void BackToMain() => ShowPanel(mainPanel);

    // TÜM PANELLERİ YÖNETEN FONKSİYON (Tıklama sorununu çözer)
    private void ShowPanel(GameObject panelToShow)
    {
        // Hangi panelin açılacağını belirle, diğerlerini kapat.
        // SetActive(false) yapmak butonların engellenmesini önler.

        if (mainPanel != null) mainPanel.SetActive(panelToShow == mainPanel);
        if (createRoomPanel != null) createRoomPanel.SetActive(panelToShow == createRoomPanel);
        if (roomListPanel != null) roomListPanel.SetActive(panelToShow == roomListPanel);
        if (renkDedektoruPaneli != null) renkDedektoruPaneli.SetActive(panelToShow == renkDedektoruPaneli);
    }
    #endregion

    public void CreateRoom()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) return;

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[LOBİ] Güncelleme geldi. Paket boyutu: {roomList.Count}");

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
                cachedRoomList.Remove(room.Name);
            else
                cachedRoomList[room.Name] = room;
        }

        UpdateRoomListView();
    }

    private void UpdateRoomListView()
    {
        foreach (Transform child in roomListContent) Destroy(child.gameObject);

        foreach (var entry in cachedRoomList)
        {
            RoomInfo room = entry.Value;

            if (!room.IsVisible || room.PlayerCount >= room.MaxPlayers)
                continue;

            GameObject item = Instantiate(roomItemPrefab, roomListContent);
            item.transform.localScale = Vector3.one;

            Text roomText = item.GetComponentInChildren<Text>();
            if (roomText != null)
                roomText.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";

            string rName = room.Name;
            item.GetComponent<Button>().onClick.AddListener(() => JoinRoom(rName));
        }
    }

    public override void OnJoinedRoom()
    {
        cachedRoomList.Clear();

        if (PhotonNetwork.IsMasterClient)
        {
            PlayerSession.Team = "Blue";
            PlayerSession.IsHost = true;
        }
        else
        {
            PlayerSession.Team = "Red";
            PlayerSession.IsHost = false;
        }

        PlayerSession.MatchId = PhotonNetwork.CurrentRoom.Name;
        PlayerSession.NetworkStarted = true;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LoadLevel("SampleScene");
        }
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
    }
}