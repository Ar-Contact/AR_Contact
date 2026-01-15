using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using Photon.Pun;

public class ARPlacementNetwork : MonoBehaviourPunCallbacks
{
    [Header("AR Ayarlarý")]
    public GameObject arenaPrefab;
    public float clickInterval = 0.5f;

    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private ARPlaneManager planeManager;
    private float lastClickTime;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private string selectedAskerResourceName;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        anchorManager = GetComponent<ARAnchorManager>();
        planeManager = GetComponent<ARPlaneManager>();
    }

    void Update()
    {
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;

        if (!ArenaManager.Instance.isArenaPlaced)
        {
            if (PhotonNetwork.IsMasterClient) HandleArenaPlacement();
        }
        else if (!ArenaManager.Instance.isWarStarted)
        {
            HandleUnitPlacement();
        }
    }

    private void HandleArenaPlacement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick <= clickInterval)
            {
                if (raycastManager.Raycast(Input.mousePosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    ARAnchor anchor = anchorManager.AddAnchor(hitPose);

                    if (anchor != null)
                    {
                        // ÖNEMLÝ: Prefab ismini Resources/ içindeki tam yoluna göre ver
                        GameObject spawnedArena = PhotonNetwork.Instantiate(arenaPrefab.name, hitPose.position, hitPose.rotation);
                        spawnedArena.transform.SetParent(anchor.transform);

                        photonView.RPC("RPC_StartGameForAll", RpcTarget.AllBuffered);
                    }
                }
            }
            lastClickTime = Time.time;
        }
    }

    [PunRPC]
    private void RPC_StartGameForAll()
    {
        // 1. AR Çizgilerini Kapat
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);
        }

        // 2. Kamerayý Hizala
        HizalaKamerayiArenaya();

        // 3. Döngüyü Baþlat
        if (ArenaManager.Instance != null) ArenaManager.Instance.StartGameAfterPlacement();
    }

    private void HizalaKamerayiArenaya()
    {
        // Arena prefabýna "Arena" tag'i vermeyi unutma!
        GameObject arena = GameObject.FindWithTag("Arena");
        if (arena == null) return;

        string team = PlayerSession.Team;
        Transform targetPos = null;

        // Arena prefabý içinde "Pos_Blue" ve "Pos_Red" isimli boþ objeler olmalý
        if (team == "Blue") targetPos = arena.transform.Find("Pos_Blue");
        else if (team == "Red") targetPos = arena.transform.Find("Pos_Red");

        if (targetPos != null)
        {
            // XR Origin'i bul (Ýsmi hiyerarþide neyse o olmalý)
            GameObject xrOrigin = GameObject.Find("XR Origin (AR)");
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = targetPos.position;
                xrOrigin.transform.rotation = targetPos.rotation;
                Debug.Log($"Kamera {team} konumuna baþarýyla taþýndý.");
            }
        }
    }

    private void HandleUnitPlacement()
    {
        if (string.IsNullOrEmpty(selectedAskerResourceName)) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (raycastManager.Raycast(Input.mousePosition, hits, TrackableType.AllTypes))
            {
                // Resources/NetworkPrefabs/AskerIsmi þeklinde yolunu kontrol et
                string path = "NetworkPrefabs/" + selectedAskerResourceName;
                PhotonNetwork.Instantiate(path, hits[0].pose.position, hits[0].pose.rotation);
            }
        }
    }

    public void SetSelectedAsker(string prefabName) => selectedAskerResourceName = prefabName;
}