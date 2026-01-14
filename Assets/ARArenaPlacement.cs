using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems; // UI'a týklamayý ayýrt etmek için gerekli kütüphane

public class ARArenaPlacement : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject arenaPrefab; // Yerleþecek Kale/Arena Modeli

    // Bu deðiþkenleri artýk Inspector'dan atamana gerek yok, kod kendisi bulacak
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;

    private GameObject spawnedObject; // Sahneye konulan obje
    private List<ARRaycastHit> hits = new List<ARRaycastHit>(); // Çarpýþma listesi

    void Start()
    {
        // 1. SORUN ÇÖZÜMÜ: Sahne deðiþince kaybolan referanslarý otomatik bul
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();

        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();

        // PC TESTÝ ÝÇÝN (Sadece Editörde çalýþýr, mobilde bu kod çalýþmaz)
#if UNITY_EDITOR
        if (arenaPrefab != null && spawnedObject == null)
        {
            // Editörde hemen kameranýn önüne koyar ki test edebilesin
            spawnedObject = Instantiate(arenaPrefab, new Vector3(0, -1, 3), Quaternion.identity);

            // Eðer Manager varsa (PC'de ArenaManager sahnedeyse) oyunu baþlat
            if (ArenaManager.Instance != null)
                ArenaManager.Instance.StartGameAfterPlacement();

            Debug.Log("PC Modu: Obje test için yerleþtirildi.");
        }
#endif
    }

    void Update()
    {
        // Ekrana dokunulmadýysa iþlem yapma
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        // 2. SORUN ÇÖZÜMÜ: UI ENGELÝ
        // Eðer parmak bir butona veya panele deðiyorsa, zemine yerleþtirme yapma!
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return;
        }

        // Sadece dokunma baþladýðýnda iþlem yap
        if (touch.phase == TouchPhase.Began)
        {
            // Raycast Manager bulunamadýysa hata vermesin, fonksiyondan çýksýn
            if (raycastManager == null) return;

            // Ekranda dokunduðun yerden bir ýþýn at
            // TrackableType.PlaneWithinPolygon: Sadece sarý alanlarýn içine yerleþtirir.
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                // Eðer sahnede obje yoksa oluþtur
                if (spawnedObject == null)
                {
                    spawnedObject = Instantiate(arenaPrefab, hitPose.position, hitPose.rotation);

                    // --- YÖN AYARLAMA (LookAt) ---
                    // 1. Arenayý önce dümdüz oyuncuya (Kameraya) baktýr
                    Vector3 lookPos = Camera.main.transform.position;
                    lookPos.y = spawnedObject.transform.position.y; // Yükseklik farkýný yok say, eðilmesin
                    spawnedObject.transform.LookAt(lookPos);

                    // 3. SORUN ÇÖZÜMÜ: TAKIMA GÖRE DÖNDÜRME
                    // Mavi takým ve Kýrmýzý takým masanýn karþýlýklý uçlarýnda gibi hissetmeli
                    string myTeam = PlayerSession.Team;

                    if (myTeam == "Blue")
                    {
                        // Mavi takým için arenayý 180 derece çevir (Modelin arkasýný/önünü kendine göre ayarla)
                        spawnedObject.transform.Rotate(0, 180, 0);
                    }
                    else
                    {
                        // Kýrmýzý takým için olduðu gibi kalsýn (veya tam tersi gerekiyorsa buraya 180 yaz)
                        spawnedObject.transform.Rotate(0, 0, 0);
                    }

                    // --- OYUNU BAÞLAT ---
                    if (ArenaManager.Instance != null)
                    {
                        ArenaManager.Instance.StartGameAfterPlacement();
                    }

                    // Zemini gizle (Görsel kirlilik gitmesi için)
                    TogglePlanes(false);
                }
                else
                {
                    // Obje zaten varsa, yeni týkladýðýn yere taþý (Yerini beðenmezsen deðiþtirebilmen için)
                    spawnedObject.transform.position = hitPose.position;
                }
            }
        }
    }

    // Zeminleri açýp kapatan yardýmcý fonksiyon
    void TogglePlanes(bool status)
    {
        if (planeManager != null)
        {
            planeManager.enabled = status; // Yeni zemin arama dursun/baþlasýn

            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(status); // Mevcut zeminleri gizle/göster
            }
        }
    }
}