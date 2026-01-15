using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class PhotonPlayerUnitSpawner : MonoBehaviourPunCallbacks
{
    [Header("Birim Prefab Listeleri")]
    public List<string> blueTeamUnitPrefabNames;
    public List<string> redTeamUnitPrefabNames;

    private Transform arenaTransform;

    private void FindArena()
    {
        // Sahneye AR ile yeni kurulan Arena'yı tag üzerinden bulur
        if (arenaTransform == null)
        {
            GameObject arenaObj = GameObject.FindGameObjectWithTag("Arena");
            if (arenaObj != null) arenaTransform = arenaObj.transform;
        }
    }

    // DragAndDropSpawner bu fonksiyonu çağırır
    public void RequestSpawnUnit(int unitIndex, Vector3 worldPosition)
    {
        FindArena();

        if (arenaTransform == null)
        {
            Debug.LogError("Hata: Sahne yerleştirilmeden asker koyulamaz!");
            return;
        }

        // 1. ADIM: DÜNYA KONUMUNU ARENA'YA GÖRE YERELLEŞTİR
        // (Oyuncu A'nın masanın neresine tıkladığını Arena'nın merkezine göre hesaplar)
        Vector3 localPos = arenaTransform.InverseTransformPoint(worldPosition);

        // 2. ADIM: Sadece yerel konumu RPC ile diğer oyunculara gönder
        photonView.RPC("RPC_SpawnUnitSynchronized", RpcTarget.AllBuffered, unitIndex, localPos, PlayerSession.Team);
    }

    [PunRPC]
    private void RPC_SpawnUnitSynchronized(int unitIndex, Vector3 localPos, string team)
    {
        FindArena();
        if (arenaTransform == null) return;

        // 3. ADIM: YEREL KONUMU DÜNYA KONUMUNA GERİ ÇEVİR
        // (Oyuncu B, gelen yerel veriyi kendi dünyasındaki Arena merkezine göre konumlandırır)
        Vector3 worldPosAtClient = arenaTransform.TransformPoint(localPos);

        string prefabName = GetPrefabName(team, unitIndex);

        // Sadece objenin sahibi veya Master Client nesneyi oluşturur
        if (photonView.IsMine || PhotonNetwork.IsMasterClient)
        {
            SpawnUnit(prefabName, worldPosAtClient, team);
        }
    }

    private string GetPrefabName(string team, int index)
    {
        if (team == "Blue" && index < blueTeamUnitPrefabNames.Count) return blueTeamUnitPrefabNames[index];
        if (team == "Red" && index < redTeamUnitPrefabNames.Count) return redTeamUnitPrefabNames[index];
        return null;
    }

    private void SpawnUnit(string prefabName, Vector3 position, string team)
    {
        string resourcePath = $"NetworkPrefabs/{prefabName}";

        // Askerin yönünü de Arena'nın yönüne (Rotation) eşitleyerek oluşturuyoruz
        GameObject spawnedUnit = PhotonNetwork.Instantiate(resourcePath, position, arenaTransform.rotation);

        if (spawnedUnit != null)
        {
            photonView.RPC("SetUnitTagRPC", RpcTarget.AllBuffered, spawnedUnit.GetComponent<PhotonView>().ViewID, team);
        }
    }

    [PunRPC]
    private void SetUnitTagRPC(int viewID, string team)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view != null) view.gameObject.tag = team + "Team";
    }
}