using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;

public class PlayerUnitSpawner : NetworkBehaviour
{
    [Header("Mavi Takim Askerleri (Sirasiyla)")]
    public List<GameObject> blueTeamUnits;

    [Header("Kirmizi Takim Askerleri (Sirasiyla)")]
    public List<GameObject> redTeamUnits;

    // DIAGNOSTIC: Check if NetworkObject is properly spawned
    private void Start()
    {
        Debug.Log("=== PlayerUnitSpawner Network Status ===");
        Debug.Log($"IsServer: {IsServer}");
        Debug.Log($"IsClient: {IsClient}");
        Debug.Log($"IsOwner: {IsOwner}");
        Debug.Log($"IsSpawned: {IsSpawned}");
        Debug.Log($"NetworkObjectId: {NetworkObjectId}");
        Debug.Log("========================================");
    }

    // UI (DragAndDrop) bu fonksiyonu cag�racak
    public void RequestSpawnUnit(int unitIndex, Vector3 position)
    {
        // DIAGNOSTIC: This should show on BOTH host AND client when called
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  RequestSpawnUnit CALLED!                            ║");
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
        Debug.Log($"[RequestSpawnUnit] UnitIndex: {unitIndex}, Position: {position}");
        Debug.Log($"[RequestSpawnUnit] IsServer: {IsServer}, IsClient: {IsClient}");
        Debug.Log($"[RequestSpawnUnit] IsOwner: {IsOwner}, IsSpawned: {IsSpawned}");
        Debug.Log($"[RequestSpawnUnit] PlayerSession.Team: {PlayerSession.Team}");
        
        // FIXED: Removed IsOwner check - use RequireOwnership=false in ServerRpc instead
        string myTeam = PlayerSession.Team;
        
        if (string.IsNullOrEmpty(myTeam))
        {
            Debug.LogError("PlayerSession.Team is NULL or EMPTY! Cannot spawn unit.");
            return;
        }
        
        Debug.Log($"[RequestSpawnUnit] Sending SpawnUnitServerRpc... Team: {myTeam}, UnitIndex: {unitIndex}");
        SpawnUnitServerRpc(unitIndex, position, myTeam);
        Debug.Log($"[RequestSpawnUnit] SpawnUnitServerRpc call sent!");
    }

    // FIXED: Added RequireOwnership = false to allow any client to spawn
    [ServerRpc(RequireOwnership = false)]
    private void SpawnUnitServerRpc(int unitIndex, Vector3 position, string team)
    {
        Debug.Log($"[SERVER] SpawnUnitServerRpc called! Team: {team}, UnitIndex: {unitIndex}");
        
        GameObject prefabToSpawn = null;

        if (team == "Blue")
        {
            if (unitIndex < blueTeamUnits.Count && unitIndex >= 0)
            {
                prefabToSpawn = blueTeamUnits[unitIndex];
                Debug.Log($"[SERVER] Selected Blue team unit: {prefabToSpawn?.name}");
            }
            else
            {
                Debug.LogError($"[SERVER] Invalid Blue team unitIndex {unitIndex}! List size: {blueTeamUnits.Count}");
            }
        }
        else if (team == "Red")
        {
            if (unitIndex < redTeamUnits.Count && unitIndex >= 0)
            {
                prefabToSpawn = redTeamUnits[unitIndex];
                Debug.Log($"[SERVER] Selected Red team unit: {prefabToSpawn?.name}");
            }
            else
            {
                Debug.LogError($"[SERVER] Invalid Red team unitIndex {unitIndex}! List size: {redTeamUnits.Count}");
            }
        }
        else
        {
            Debug.LogError($"[SERVER] Unknown team: {team}");
        }

        // ADDED: Comprehensive error checking
        if (prefabToSpawn != null)
        {
            Debug.Log($"[SERVER] Instantiating {prefabToSpawn.name} at {position}");
            GameObject newUnit = Instantiate(prefabToSpawn, position, Quaternion.identity);
            
            // Check for NetworkObject component
            NetworkObject netObj = newUnit.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"[SERVER] NetworkObject component missing on {prefabToSpawn.name}!");
                Debug.LogError("[SERVER] Solution: Add NetworkObject component to the prefab in Unity Inspector.");
                Destroy(newUnit);
                return;
            }
            
            // Check if we're actually server
            if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("[SERVER] Cannot spawn! Not running as Server or Host!");
                Destroy(newUnit);
                return;
            }
            
            // Attempt to spawn
            try
            {
                Debug.Log($"[SERVER] Spawning NetworkObject for {prefabToSpawn.name}...");
                netObj.Spawn();
                Debug.Log($"[SERVER] SUCCESS! Unit spawned. NetworkObjectId: {netObj.NetworkObjectId}");
                
                // CRITICAL FIX: Check if NetworkTransform exists
                NetworkTransform netTransform = newUnit.GetComponent<NetworkTransform>();
                if (netTransform == null)
                {
                    Debug.LogError($"[SERVER] CRITICAL: {prefabToSpawn.name} is missing NetworkTransform component!");
                    Debug.LogError($"[SERVER] Position will NOT sync to clients - they will see object at (0,0,0)!");
                    Debug.LogError($"[SERVER] FIX: Add NetworkTransform component to the prefab in Unity Inspector!");
                }
                else
                {
                    Debug.Log($"[SERVER] NetworkTransform found - position will sync correctly to clients");
                }
                
                // Notify all clients about the spawn for debugging
                NotifySpawnClientRpc(netObj.NetworkObjectId, position, team, prefabToSpawn.name);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SERVER] Spawn FAILED: {e.Message}");
                Debug.LogError($"[SERVER] Stack trace: {e.StackTrace}");
                Debug.LogError("[SERVER] Check: Is prefab in NetworkManager's NetworkPrefabsList?");
                Destroy(newUnit);
            }
        }
        else
        {
            Debug.LogError($"[SERVER] prefabToSpawn is NULL! Team: {team}, UnitIndex: {unitIndex}");
        }
    }
    
    // ADDED: ClientRpc to notify all clients about spawned objects for debugging
    [ClientRpc]
    private void NotifySpawnClientRpc(ulong networkObjectId, Vector3 spawnPosition, string spawnTeam, string unitName)
    {
        Debug.Log($"[CLIENT] Received spawn notification! " +
                  $"NetworkObjectId: {networkObjectId}, " +
                  $"Team: {spawnTeam}, " +
                  $"Unit: {unitName}, " +
                  $"Expected Position: {spawnPosition}");
        
        // Try to find the spawned object on this client
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject spawnedObj))
        {
            Vector3 actualPosition = spawnedObj.transform.position;
            Debug.Log($"[CLIENT] ✓ Found spawned object! Actual position: {actualPosition}");
            
            float positionDifference = Vector3.Distance(actualPosition, spawnPosition);
            if (positionDifference > 0.5f)
            {
                Debug.LogError($"[CLIENT] ✗ POSITION MISMATCH! Distance: {positionDifference:F2} units");
                Debug.LogError($"[CLIENT] Server position: {spawnPosition}");
                Debug.LogError($"[CLIENT] Client position: {actualPosition}");
                Debug.LogError($"[CLIENT] → This means NetworkTransform is MISSING or NOT WORKING!");
                Debug.LogError($"[CLIENT] → FIX: Add NetworkTransform component to '{unitName}' prefab!");
            }
            else
            {
                Debug.Log($"[CLIENT] ✓ Position synchronized correctly (difference: {positionDifference:F3} units)");
            }
        }
        else
        {
            Debug.LogError($"[CLIENT] ✗ NetworkObject {networkObjectId} NOT FOUND on this client!");
            Debug.LogError($"[CLIENT] → This means '{unitName}' prefab is NOT in NetworkManager's NetworkPrefabsList!");
            Debug.LogError($"[CLIENT] → FIX: Add '{unitName}' to NetworkManager → Network Prefabs List in Inspector!");
        }
    }
}
