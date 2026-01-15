using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class TiklaVeYerlestir : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject secilenPrefab;
    public float kameraOnuMesafe = 1.0f;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> carpismaListesi = new List<ARRaycastHit>();
    private bool objeYerlesildiMi = false;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
    }

    void Update()
    {
        if (objeYerlesildiMi) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            ObjeYerlestir(Input.GetTouch(0).position);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            ObjeYerlestir(Input.mousePosition);
        }
#endif
    }

    void ObjeYerlestir(Vector2 ekranPozisyonu)
    {
        Pose hedefPose;

        if (raycastManager.Raycast(ekranPozisyonu, carpismaListesi, TrackableType.PlaneWithinPolygon))
        {
            hedefPose = carpismaListesi[0].pose;
        }
        else
        {
            Camera cam = Camera.main;
            Vector3 pozisyon = cam.transform.position + cam.transform.forward * kameraOnuMesafe;
            Quaternion rotasyon = Quaternion.LookRotation(-cam.transform.forward);
            hedefPose = new Pose(pozisyon, rotasyon);
        }

        if (secilenPrefab != null)
        {
            GameObject sahnedeOlanObje = Instantiate(secilenPrefab, hedefPose.position, hedefPose.rotation);

            // Kameraya bakma düzeltmesi
            Vector3 lookPos = Camera.main.transform.position;
            lookPos.y = sahnedeOlanObje.transform.position.y;
            sahnedeOlanObje.transform.LookAt(lookPos);
            sahnedeOlanObje.transform.Rotate(0, 180, 0);

            SistemiKilitle();
        }
    }

    void SistemiKilitle()
    {
        objeYerlesildiMi = true;

        // Plane'leri kapat
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }

        // --- ARENA MANAGER'I TETİKLE ---
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.StartGameAfterPlacement();
        }

        Debug.Log("Arena yerleşti ve ArenaManager tetiklendi.");
    }
}