using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class PhotonPlayerUnitSpawner : MonoBehaviourPunCallbacks
{
    [Header("Mavi Takim Askerleri (Sirasiyla)")]
    public List<string> blueTeamUnitPrefabNames; // Prefab names in Resources folder
    
    [Header("Kirmizi Takim Askerleri (Sirasiyla)")]
    public List<string> redTeamUnitPrefabNames; // Prefab names in Resources folder
    
    // Note: photonView is inherited from MonoBehaviourPun base class

    private void Awake()
    {
        // Verify PhotonView exists (inherited photonView property)
        if (photonView == null)
        {
            Debug.LogError("PhotonView component missing! Please add PhotonView component to this GameObject.");
        }
    }

    private void Start()
    {
        Debug.Log("=== PhotonPlayerUnitSpawner Status ===");
        Debug.Log($"PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"PhotonNetwork.IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"PlayerSession.Team: {PlayerSession.Team}");
        Debug.Log("======================================");
    }

    /// <summary>
    /// Called by UI (DragAndDrop) to spawn a unit
    /// </summary>
    public void RequestSpawnUnit(int unitIndex, Vector3 position)
    {
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  RequestSpawnUnit CALLED!                            ║");
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
        Debug.Log($"[RequestSpawnUnit] UnitIndex: {unitIndex}, Position: {position}");
        Debug.Log($"[RequestSpawnUnit] PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"[RequestSpawnUnit] PlayerSession.Team: {PlayerSession.Team}");
        
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Cannot spawn unit - not in a Photon room!");
            return;
        }
        
        string myTeam = PlayerSession.Team;
        
        if (string.IsNullOrEmpty(myTeam))
        {
            Debug.LogError("PlayerSession.Team is NULL or EMPTY! Cannot spawn unit.");
            return;
        }
        
        // Get the prefab name based on team and index
        string prefabName = GetPrefabName(myTeam, unitIndex);
        
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError($"Invalid unit index {unitIndex} for team {myTeam}");
            return;
        }
        
        Debug.Log($"[RequestSpawnUnit] Spawning {prefabName} for team {myTeam}");
        
        // Spawn using Photon - all clients will see it automatically
        SpawnUnit(prefabName, position, myTeam);
    }

    private string GetPrefabName(string team, int unitIndex)
    {
        if (team == "Blue")
        {
            if (unitIndex >= 0 && unitIndex < blueTeamUnitPrefabNames.Count)
            {
                return blueTeamUnitPrefabNames[unitIndex];
            }
        }
        else if (team == "Red")
        {
            if (unitIndex >= 0 && unitIndex < redTeamUnitPrefabNames.Count)
            {
                return redTeamUnitPrefabNames[unitIndex];
            }
        }
        
        return null;
    }

    private void SpawnUnit(string prefabName, Vector3 position, string team)
    {
        Debug.Log($"[PHOTON] PhotonNetwork.Instantiate: {prefabName} at {position}");
        
        try
        {
            // PhotonNetwork.Instantiate requires prefab to be in Resources folder
            // Path format: "NetworkPrefabs/PrefabName" if stored in Resources/NetworkPrefabs/
            string resourcePath = $"NetworkPrefabs/{prefabName}";
            
            GameObject spawnedUnit = PhotonNetwork.Instantiate(
                resourcePath,
                position,
                Quaternion.identity
            );
            
            if (spawnedUnit != null)
            {
                Debug.Log($"✓ SUCCESS! Unit '{prefabName}' spawned via Photon!");
                Debug.Log($"  PhotonView ID: {spawnedUnit.GetComponent<PhotonView>()?.ViewID}");
                Debug.Log($"  Position: {spawnedUnit.transform.position}");
                
                // CRITICAL: Set team tag for AI enemy detection
                string tagName = team + "Team"; // Converts "Blue" → "BlueTeam", "Red" → "RedTeam"
                spawnedUnit.tag = tagName;
                Debug.Log($"  ✓ Tag set to: {tagName}");
                
                // Notify all players via RPC to set tags on their clients too
                PhotonView unitPhotonView = spawnedUnit.GetComponent<PhotonView>();
                if (unitPhotonView != null)
                {
                    photonView.RPC("SetUnitTagRPC", RpcTarget.AllBuffered, unitPhotonView.ViewID, team);
                }
                
                // Notify all players for debugging
                photonView.RPC("NotifySpawnRPC", RpcTarget.All, prefabName, position, team);
            }
            else
            {
                Debug.LogError($"✗ PhotonNetwork.Instantiate returned NULL for '{resourcePath}'!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Photon spawn FAILED: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            Debug.LogError($"SOLUTION:");
            Debug.LogError($"  1. Ensure '{prefabName}' exists in Resources/NetworkPrefabs/ folder");
            Debug.LogError($"  2. Prefab MUST have PhotonView component");
            Debug.LogError($"  3. PhotonView must have unique View ID (can be 0 for dynamic spawning)");
        }
    }

    [PunRPC]
    private void SetUnitTagRPC(int photonViewID, string team)
    {
        // Find the spawned unit by PhotonView ID
        PhotonView unitView = PhotonView.Find(photonViewID);
        if (unitView != null)
        {
            string tagName = team + "Team"; // Converts "Blue" → "BlueTeam", "Red" → "RedTeam"
            unitView.gameObject.tag = tagName;
            Debug.Log($"[RPC] Set tag '{tagName}' on {unitView.gameObject.name} (ViewID: {photonViewID})");
        }
        else
        {
            Debug.LogWarning($"[RPC] Could not find PhotonView {photonViewID} to set tag!");
        }
    }
    
    [PunRPC]
    private void NotifySpawnRPC(string unitName, Vector3 spawnPosition, string spawnTeam)
    {
        Debug.Log($"[RPC RECEIVED] Unit spawned notification:");
        Debug.Log($"  Unit: {unitName}");
        Debug.Log($"  Team: {spawnTeam}");
        Debug.Log($"  Position: {spawnPosition}");
        Debug.Log($"  Received by: {PlayerSession.Team} team");
    }
}
