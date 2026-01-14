using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class TiklaVeYerleştir : MonoBehaviour
{
    [Header("Yerleştirilecek Prefab")]
    public GameObject secilenPrefab;

    [Header("Plane yoksa kamera önü mesafe")]
    public float kameraOnuMesafe = 1.0f;

    private GameObject sahnedeOlanObje;

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
            Vector2 dokunmaPozisyonu = Input.GetTouch(0).position;
            ObjeYerleştir(dokunmaPozisyonu);
        }
#if UNITY_EDITOR
        else if (Input.GetMouseButtonDown(0))
        {
            ObjeYerleştir(Input.mousePosition);
        }
#endif
    }

    void ObjeYerleştir(Vector2 ekranPozisyonu)
    {
        Pose hedefPose;

        // 1️⃣ Önce plane dene
        if (raycastManager.Raycast(ekranPozisyonu, carpismaListesi, TrackableType.PlaneWithinPolygon))
        {
            hedefPose = carpismaListesi[0].pose;
        }
        else
        {
            // 2️⃣ Plane yoksa → kameranın önüne koy
            Camera cam = Camera.main;
            Vector3 pozisyon = cam.transform.position + cam.transform.forward * kameraOnuMesafe;
            Quaternion rotasyon = Quaternion.LookRotation(-cam.transform.forward);

            hedefPose = new Pose(pozisyon, rotasyon);
        }

        sahnedeOlanObje = Instantiate(secilenPrefab, hedefPose.position, hedefPose.rotation);

        // Kameraya baktır
        Vector3 lookPos = Camera.main.transform.position;
        lookPos.y = sahnedeOlanObje.transform.position.y;
        sahnedeOlanObje.transform.LookAt(lookPos);
        sahnedeOlanObje.transform.Rotate(0, 180, 0);

        SistemiKilitle();
    }

    void SistemiKilitle()
    {
        objeYerlesildiMi = true;

        if (planeManager != null)
        {
            planeManager.enabled = false;

            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }

        Debug.Log("Obje yerleştirildi (plane olsa da olmasa da).");
    }
}
