using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class PhotonPlayerUnitSpawner : MonoBehaviourPunCallbacks
{
    [Header("Mavi Takim Askerleri")]
    public List<string> blueTeamUnitPrefabNames;

    [Header("Kirmizi Takim Askerleri")]
    public List<string> redTeamUnitPrefabNames;

    private Transform arenaTransform;

    // UI'dan gelen istek buraya düşer
    public void RequestSpawnUnit(int unitIndex, Vector3 worldPosition)
    {
        if (!PhotonNetwork.InRoom) return;

        // Arenayı bul (Tag: Arena)
        if (arenaTransform == null)
        {
            GameObject arenaObj = GameObject.FindGameObjectWithTag("Arena");
            if (arenaObj != null) arenaTransform = arenaObj.transform;
        }

        string myTeam = PlayerSession.Team;
        string prefabName = GetPrefabName(myTeam, unitIndex);
        if (string.IsNullOrEmpty(prefabName)) return;

        // --- ÖNEMLİ KISIM: AR POZİSYON HESABI ---
        // Senin tıkladığın Dünya koordinatını, Arenanın içine göre (Local) çeviriyoruz.
        Vector3 localPos = worldPosition;
        if (arenaTransform != null)
        {
            localPos = arenaTransform.InverseTransformPoint(worldPosition);
        }

        // RPC ile herkese "Arenanın şurasına (Local) koy" diyoruz
        photonView.RPC("SpawnUnitRPC", RpcTarget.All, prefabName, localPos, myTeam);
    }

    [PunRPC]
    private void SpawnUnitRPC(string prefabName, Vector3 localPos, string team)
    {
        // 1. Kendi Arenamı bul
        if (arenaTransform == null)
        {
            GameObject arenaObj = GameObject.FindGameObjectWithTag("Arena");
            if (arenaObj != null) arenaTransform = arenaObj.transform;
        }

        // 2. Local -> World (Benim masamdaki konuma çevir)
        Vector3 spawnPos = localPos;
        if (arenaTransform != null)
        {
            spawnPos = arenaTransform.TransformPoint(localPos);
        }

        // 3. Sadece Master Client Instantiate yapsın
        if (PhotonNetwork.IsMasterClient)
        {
            GameObject unit = PhotonNetwork.Instantiate("NetworkPrefabs/" + prefabName, spawnPos, Quaternion.identity);

            if (unit != null)
            {
                PhotonView unitPV = unit.GetComponent<PhotonView>();
                // Herkeste Tag'i güncelle
                photonView.RPC("SetUnitTagRPC", RpcTarget.AllBuffered, unitPV.ViewID, team);
            }
        }
    }

    [PunRPC]
    private void SetUnitTagRPC(int viewID, string team)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            targetView.gameObject.tag = team + "Team";
        }
    }

    private string GetPrefabName(string team, int unitIndex)
    {
        if (team == "Blue" && unitIndex < blueTeamUnitPrefabNames.Count) return blueTeamUnitPrefabNames[unitIndex];
        if (team == "Red" && unitIndex < redTeamUnitPrefabNames.Count) return redTeamUnitPrefabNames[unitIndex];
        return null;
    }
}