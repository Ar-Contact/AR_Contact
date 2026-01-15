using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Photon.Pun;

public class ARPlacementNetwork : MonoBehaviour
{
    [Header("AR Ayarlarý")]
    public GameObject arenaPrefab; // Yerleþtirilecek ana arena/zemin
    public float clickInterval = 0.5f; // Çift týk hýzý

    private ARRaycastManager raycastManager;
    private GameObject spawnedArena;
    private float lastClickTime;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // UI'dan seçilen güncel asker (Photon Resources içindeki isimle ayný olmalý)
    private string selectedAskerResourceName;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // 1. ADIM: Arena yerleþmediyse arena yerleþtirme mantýðý (Çift Týk)
        if (!ArenaManager.Instance.isArenaPlaced)
        {
            HandleArenaPlacement();
        }
        // 2. ADIM: Arena yerleþtiyse ve SAVAÞ BAÞLAMADIYSA (Hazýrlýk Evresi) asker koyma
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
                    // Arena sadece Master Client (Host) tarafýndan veya yerel olarak yerleþtirilir
                    if (spawnedArena == null)
                    {
                        spawnedArena = Instantiate(arenaPrefab, hitPose.position, hitPose.rotation);
                        // ArenaManager'a haber ver
                        ArenaManager.Instance.StartGameAfterPlacement();
                    }
                }
            }
            lastClickTime = Time.time;
        }
    }

    private void HandleUnitPlacement()
    {
        if (string.IsNullOrEmpty(selectedAskerResourceName)) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (raycastManager.Raycast(Input.mousePosition, hits, TrackableType.AllTypes))
            {
                // Photon ile askeri oluþtur (Resources klasöründe olmalý)
                PhotonNetwork.Instantiate(selectedAskerResourceName, hits[0].pose.position, hits[0].pose.rotation);
            }
        }
    }

    // UI Butonlarýna bu fonksiyonu baðla (Örn: "Archer", "Knight")
    public void SetSelectedAsker(string prefabName)
    {
        selectedAskerResourceName = prefabName;
    }
}