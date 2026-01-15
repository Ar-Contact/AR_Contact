using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class ARPlacement : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject arenaPrefab;

    private GameObject spawnedArena;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    private void Update()
    {
        // 1. Arena zaten kurulduysa iþlem yapma
        if (spawnedArena != null) return;

        // 2. Giriþ Kontrolü (Mobil Dokunma veya PC Fare Týklamasý)
        bool isPressed = false;
        Vector2 screenPos = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            isPressed = true;
            screenPos = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            isPressed = true;
            screenPos = Input.mousePosition;
        }

        if (isPressed)
        {
            // 3. AR Yüzey Taramasý (Mobil için)
            if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                SpawnArena(hitPose.position, hitPose.rotation);
            }
            // 4. Bilgisayar (Unity Editor) Testi için Simülasyon
#if UNITY_EDITOR
            else
            {
                // Bilgisayarda yüzey algýlanamayacaðý için kameranýn 3 metre önüne koyar
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 3f;
                // Yere paralel durmasý için Y eksenini düzeltiyoruz (isteðe baðlý)
                spawnPos.y = 0;
                SpawnArena(spawnPos, Quaternion.identity);
                Debug.Log("Editor Modu: Arena kameranýn önüne yerleþtirildi.");
            }
#endif
        }
    }

    private void SpawnArena(Vector3 position, Quaternion rotation)
    {
        // Arenayý oluþtur
        spawnedArena = Instantiate(arenaPrefab, position, rotation);

        // NavMesh'i piþir (Askerlerin yürümesi için þart)
        NavMeshSurface surface = spawnedArena.GetComponent<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("NavMesh baþarýyla oluþturuldu.");
        }

        // ArenaManager'ý baþlat
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.StartGameLoop();
        }

        DisablePlaneDetection();
    }

    private void DisablePlaneDetection()
    {
        var planeManager = GetComponent<ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }
}