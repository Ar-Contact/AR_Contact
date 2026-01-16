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

        // Sadece dokunma baþladýðýnda çalýþsýn
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            ObjeYerlestir(Input.GetTouch(0).position);
        }
    }

    void ObjeYerlestir(Vector2 ekranPozisyonu)
    {
        // Sadece PLANE (Zemin) algýlandýysa iþlem yap
        if (raycastManager.Raycast(ekranPozisyonu, carpismaListesi, TrackableType.PlaneWithinPolygon))
        {
            Pose hedefPose = carpismaListesi[0].pose;

            if (secilenPrefab != null)
            {
                GameObject sahnedeOlanObje = Instantiate(secilenPrefab, hedefPose.position, hedefPose.rotation);

                // Kameraya bakma düzeltmesi (Sadece Y ekseninde döndür ki yamuk durmasýn)
                Vector3 lookPos = Camera.main.transform.position;
                lookPos.y = sahnedeOlanObje.transform.position.y; // Yükseklik ayný kalsýn
                sahnedeOlanObje.transform.LookAt(lookPos);
                sahnedeOlanObje.transform.Rotate(0, 180, 0);

                SistemiKilitle();
            }
        }
        else
        {
            // Zemin algýlanmadýysa kullanýcýya uyarý verebilirsin veya hiçbir þey yapmazsýn.
            Debug.LogWarning("Lütfen sarý zemin noktalarýnýn üzerine týklayýn!");
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

        // --- ARENA MANAGER'I TETÝKLE ---
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.StartGameAfterPlacement();
        }

        Debug.Log("Arena yerleþti ve ArenaManager tetiklendi.");
    }
}