using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MatchMaker : MonoBehaviourPunCallbacks
{
    [Header("UI Panelleri")]
    public GameObject mainPanel;
    public GameObject createRoomPanel;
    public GameObject roomListPanel;

    [Header("UI Elemanları")]
    public InputField roomNameInput;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    // Hafızada odaları tutmak için sözlük
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private void Start()
    {
        // ÖNEMLİ: Master sahne yükleyince diğeri de otomatik yüklensin
        PhotonNetwork.AutomaticallySyncScene = true;

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

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
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
        UpdateRoomListView();
    }

    private void UpdateRoomListView()
    {
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in cachedRoomList)
        {
            RoomInfo room = entry.Value;
            if (!room.IsVisible || room.PlayerCount >= room.MaxPlayers)
                continue;

            GameObject item = Instantiate(roomItemPrefab, roomListContent);
            item.transform.localScale = Vector3.one;
            item.GetComponentInChildren<Text>().text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";

            string rName = room.Name;
            item.GetComponent<Button>().onClick.AddListener(() => JoinRoom(rName));
        }
    }

    public override void OnJoinedRoom()
    {
        cachedRoomList.Clear();

        // Takım Belirleme
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

        // Odaya girince sayıyı kontrol et (İkinci oyuncu doğrudan burayı tetikler)
        CheckPlayersAndStart();
    }

    // Yeni bir oyuncu odaya girdiğinde (Master tarafında çalışır)
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CheckPlayersAndStart();
    }

    private void CheckPlayersAndStart()
    {
        // Sadece Master Client yükleme yapabilir ve sayı 2 olmalıdır
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Oda Doldu! Sahne yükleniyor...");
            PhotonNetwork.LoadLevel("SampleScene");
        }
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
    }
}