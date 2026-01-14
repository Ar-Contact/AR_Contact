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

    [Header("UI Elements")]
    public InputField roomNameInput;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    // Hafızada odaları tutmak için sözlük
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private void Start()
    {
        PlayerSession.NetworkStarted = false;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        ShowPanel(mainPanel);
    }

    #region Panel Kontrolleri
    public void OpenCreateRoomPanel() => ShowPanel(createRoomPanel);
    public void OpenRoomListPanel()
    {
        ShowPanel(roomListPanel);
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }
    public void BackToMain() => ShowPanel(mainPanel);

    private void ShowPanel(GameObject panelToShow)
    {
        mainPanel.SetActive(panelToShow == mainPanel);
        createRoomPanel.SetActive(panelToShow == createRoomPanel);
        roomListPanel.SetActive(panelToShow == roomListPanel);
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

    // KRİTİK DÜZELTME BURADA
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[LOBİ] Güncelleme geldi. Paket boyutu: {roomList.Count}");

        // 1. Önce hafızadaki listeyi (cache) güncelle
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                cachedRoomList.Remove(room.Name);
            }
            else
            {
                cachedRoomList[room.Name] = room;
            }
        }

        // 2. UI listesini temizle ve hafızadaki güncel listeye göre yeniden çiz
        UpdateRoomListView();
    }

    private void UpdateRoomListView()
    {
        // Mevcut butonları temizle
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // Hafızadaki tüm geçerli odaları ekrana bas
        foreach (var entry in cachedRoomList)
        {
            RoomInfo room = entry.Value;

            // Oda gizli veya doluysa gösterme
            if (!room.IsVisible || room.PlayerCount >= room.MaxPlayers)
                continue;

            GameObject item = Instantiate(roomItemPrefab, roomListContent);
            item.transform.localScale = Vector3.one;

            Text roomText = item.GetComponentInChildren<Text>();
            if (roomText != null)
            {
                roomText.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
            }

            string rName = room.Name;
            item.GetComponent<Button>().onClick.AddListener(() => JoinRoom(rName));
        }
    }

    public override void OnJoinedRoom()
    {
        // Odaya girince hafızayı temizle (lobiye dönünce taze başlasın)
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