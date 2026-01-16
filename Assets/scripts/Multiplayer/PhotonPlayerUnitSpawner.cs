using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class PhotonPlayerUnitSpawner : MonoBehaviourPunCallbacks
{
    [Header("Asker Prefab Listeleri (Resources/NetworkPrefabs İçinde)")]
    public List<string> blueTeamUnitPrefabNames;
    public List<string> redTeamUnitPrefabNames;

    private Transform arenaTransform;
    // Arena henüz taranmamışken gelen karakterleri tutmak için liste
    private List<WaitingUnit> waitingUnits = new List<WaitingUnit>();

    private struct WaitingUnit
    {
        public PhotonView view;
        public Vector3 localPos;
        public string team;
    }

    private void Update()
    {
        // Arena henüz bulunamadıysa sürekli aramaya devam et (Dinamik Takip)
        if (arenaTransform == null)
        {
            FindArena();
        }
        else if (waitingUnits.Count > 0)
        {
            // Arena bulunduğu anda bekleyen tüm karakterleri üzerine yerleştir
            ProcessWaitingUnits();
        }
    }

    private void FindArena()
    {
        GameObject arenaObj = GameObject.FindGameObjectWithTag("Arena");
        if (arenaObj != null)
        {
            arenaTransform = arenaObj.transform;
            Debug.Log("<color=green>Arena Sahneye Geldi! Birimler Yerleştiriliyor.</color>");
        }
    }

    private void ProcessWaitingUnits()
    {
        for (int i = waitingUnits.Count - 1; i >= 0; i--)
        {
            var unit = waitingUnits[i];
            if (unit.view != null)
            {
                AttachToArena(unit.view, unit.localPos, unit.team);
            }
            waitingUnits.RemoveAt(i);
        }
    }

    public void RequestSpawnUnit(int unitIndex, Vector3 worldPosition)
    {
        if (!PhotonNetwork.InRoom) return;

        string myTeam = PlayerSession.Team;
        string prefabName = GetPrefabName(myTeam, unitIndex);

        if (string.IsNullOrEmpty(prefabName)) return;

        // Arena taranmışsa yerel pozisyonu hesapla, taranmamışsa dünya pozisyonunu gönder
        Vector3 localPos = worldPosition;
        if (arenaTransform != null)
        {
            localPos = arenaTransform.InverseTransformPoint(worldPosition);
        }

        photonView.RPC("SpawnUnitRPC", RpcTarget.MasterClient, prefabName, localPos, myTeam);
    }

    [PunRPC]
    private void SpawnUnitRPC(string prefabName, Vector3 localPos, string team)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Prefablar mutlaka Resources/NetworkPrefabs klasöründe olmalı
            GameObject unit = PhotonNetwork.Instantiate("NetworkPrefabs/" + prefabName, Vector3.zero, Quaternion.identity);

            if (unit != null)
            {
                int viewID = unit.GetComponent<PhotonView>().ViewID;
                photonView.RPC("SyncUnitToArenaRPC", RpcTarget.AllBuffered, viewID, localPos, team);
            }
        }
    }

    [PunRPC]
    private void SyncUnitToArenaRPC(int viewID, Vector3 localPos, string team)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView == null) return;

        FindArena();

        if (arenaTransform != null)
        {
            AttachToArena(targetView, localPos, team);
        }
        else
        {
            // Arena henüz yok, karakteri listeye al
            Debug.LogWarning("Arena taranmadığı için birim bekleme listesine alındı.");
            waitingUnits.Add(new WaitingUnit { view = targetView, localPos = localPos, team = team });
        }
    }

    private void AttachToArena(PhotonView targetView, Vector3 localPos, string team)
    {
        targetView.transform.SetParent(arenaTransform);
        targetView.transform.localPosition = localPos;
        targetView.transform.localRotation = Quaternion.identity;
        targetView.transform.localScale = Vector3.one;
        targetView.gameObject.tag = team + "Team";
        Debug.Log($"Birim {team} takımı olarak Arena'ya yerleşti.");
    }

    private string GetPrefabName(string team, int unitIndex)
    {
        if (team == "Blue" && unitIndex < blueTeamUnitPrefabNames.Count) return blueTeamUnitPrefabNames[unitIndex];
        if (team == "Red" && unitIndex < redTeamUnitPrefabNames.Count) return redTeamUnitPrefabNames[unitIndex];
        return null;
    }
}