using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; // NavMesh icin gerekli

public class PhotonPlayerUnitSpawner : MonoBehaviourPunCallbacks
{
    [Header("Asker Prefab Listeleri (Resources/NetworkPrefabs icinde)")]
    public List<string> blueTeamUnitPrefabNames;
    public List<string> redTeamUnitPrefabNames;

    private Transform arenaTransform;
    // Arena henuz taranmamisken gelen karakterleri tutmak icin liste
    private List<WaitingUnit> waitingUnits = new List<WaitingUnit>();

    private struct WaitingUnit
    {
        public PhotonView view;
        public Vector3 localPos;
        public string team;
    }

    private void Update()
    {
        // Arena henuz bulunamadiysa surekli aramaya devam et (Dinamik Takip)
        if (arenaTransform == null)
        {
            FindArena();
        }
        else if (waitingUnits.Count > 0)
        {
            // Arena bulundugu anda bekleyen tum karakterleri uzerine yerlestir
            ProcessWaitingUnits();
        }
    }

    private void FindArena()
    {
        GameObject arenaObj = GameObject.FindGameObjectWithTag("Arena");
        if (arenaObj != null)
        {
            arenaTransform = arenaObj.transform;
            Debug.Log("<color=green>Arena Sahneye Geldi! Birimler Yerlestiriliyor.</color>");
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

        // Arena taranmissa yerel pozisyonu hesapla, taranmamissa dunya pozisyonunu gonder
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
            // Prefablar mutlaka Resources/NetworkPrefabs klasorunde olmali
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
            // Arena henuz yok, karakteri listeye al
            Debug.LogWarning("Arena taranmadigi icin birim bekleme listesine alindi.");
            waitingUnits.Add(new WaitingUnit { view = targetView, localPos = localPos, team = team });
        }
    }

    private void AttachToArena(PhotonView targetView, Vector3 localPos, string team)
    {
        targetView.transform.SetParent(arenaTransform);
        targetView.transform.localPosition = localPos;
        targetView.transform.localRotation = Quaternion.identity;
        // localScale artik degistirilmiyor - prefab boyutunu koruyor
        targetView.gameObject.tag = team + "Team";

        // --- NAVMESH SNAP (GUNCELENDI) ---
        NavMeshAgent agent = targetView.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // Agent'i gecici kapat
            agent.enabled = false;
            
            // baseOffset'i sifirla (havada durmayi onler)
            agent.baseOffset = 0f;

            // NavMesh uzerinde en yakin noktayi bul (Arama mesafesi arttirildi: 5.0f)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetView.transform.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                // Karakteri NavMesh yuzeyine yerlestir
                targetView.transform.position = hit.position;
                Debug.Log($"<color=green>[SPAWN] {targetView.gameObject.name} NavMesh'e yerlestirildi. Pozisyon: {hit.position}</color>");
            }
            else
            {
                // NavMesh bulunamadiysa, Y pozisyonunu arena yuzeyine ayarla
                Vector3 pos = targetView.transform.position;
                pos.y = arenaTransform.position.y;
                targetView.transform.position = pos;
                Debug.LogWarning($"<color=orange>[SPAWN] {targetView.gameObject.name} NavMesh bulunamadi! Arena yuzeyine yerlestirildi.</color>");
            }

            // Agent'i geri ac
            agent.enabled = true;
            
            // Agent'in NavMesh'e baglanmasini bekle
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning($"<color=orange>[SPAWN] {targetView.gameObject.name} hala NavMesh uzerinde degil!</color>");
            }
        }
        // --------------------

        Debug.Log($"Birim {team} takimi olarak Arena'ya yerlesti. (Snapped)");
    }

    private string GetPrefabName(string team, int unitIndex)
    {
        if (team == "Blue" && unitIndex < blueTeamUnitPrefabNames.Count) return blueTeamUnitPrefabNames[unitIndex];
        if (team == "Red" && unitIndex < redTeamUnitPrefabNames.Count) return redTeamUnitPrefabNames[unitIndex];
        return null;
    }
}