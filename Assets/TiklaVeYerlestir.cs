using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class TiklaVeYerleştir : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject secilenPrefab; // Buranın boş kalmadığından emin ol!
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

        if (secilenPrefab == null)
        {
            Debug.LogError("HATA: 'secilenPrefab' alanı boş! Lütfen Inspector'dan bir obje sürükleyin.");
        }
    }

    void Update()
    {
        if (objeYerlesildiMi) return;

        // Mobil Dokunma
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            ObjeYerleştir(Input.GetTouch(0).position);
        }

        // Editör Mouse Tıklama
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            ObjeYerleştir(Input.mousePosition);
        }
#endif
    }

    void ObjeYerleştir(Vector2 ekranPozisyonu)
    {
        Debug.Log("Tıklama algılandı, işlem başlıyor...");
        Pose hedefPose;

        // PlaneWithinBounds ve PlaneWithinPolygon'u kapsar, daha garantidir.
        if (raycastManager.Raycast(ekranPozisyonu, carpismaListesi, TrackableType.AllTypes))
        {
            hedefPose = carpismaListesi[0].pose;
            Debug.Log("Zemin algılandı: " + hedefPose.position);
        }
        else
        {
            Debug.LogWarning("Zemin bulunamadı! Obje kameranın önüne yerleştiriliyor.");
            Camera cam = Camera.main;
            Vector3 pozisyon = cam.transform.position + cam.transform.forward * kameraOnuMesafe;
            Quaternion rotasyon = Quaternion.LookRotation(-cam.transform.forward);
            hedefPose = new Pose(pozisyon, rotasyon);
        }

        if (secilenPrefab != null)
        {
            sahnedeOlanObje = Instantiate(secilenPrefab, hedefPose.position, hedefPose.rotation);

            // Objeyi dik tut ve kameraya baktır
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
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
        Debug.Log("İŞLEM TAMAM: Obje yerleştirildi ve sistem kilitlendi.");
    }
}