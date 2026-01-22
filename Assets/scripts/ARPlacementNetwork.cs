using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
[RequireComponent(typeof(ARAnchorManager))]
public class ARPlacementNetwork : MonoBehaviourPunCallbacks
{
    [Header("Arena Prefabları")]
    public GameObject arenaSiyahPrefab;
    public GameObject arenaKirmiziPrefab;
    public GameObject arenaMaviPrefab;

    [Header("Debug")]
    public Text debugText;

    private const string NETWORK_PREFABS_PATH = "NetworkPrefabs";

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private ARAnchorManager anchorManager;
    private List<ARRaycastHit> carpismaListesi = new List<ARRaycastHit>();

    private bool arenaYerlesti = false;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
        anchorManager = GetComponent<ARAnchorManager>();

        // --- KRİTİK LOG ---
        // Oyun başladığında hafızada ne var?
        string gelenRenk = GlobalVeri.SecilenRenk;
        Debug.Log($"[AR START] Oyun Başladı. Hafızadan Gelen Renk: '{gelenRenk}'");

        LogYaz($"Mod: {gelenRenk}. Sarı noktalara tıkla...");
    }

    void Update()
    {
        if (arenaYerlesti) return;
        if (ArenaManager.Instance != null && ArenaManager.Instance.isArenaPlaced) return;

        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || (Application.isEditor && Input.GetMouseButtonDown(0)))
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                LogYaz("❌ HATA: Sadece Master Client arena koyabilir!");
                return;
            }

            Vector2 touchPos = (Input.touchCount > 0) ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

            if (IsPointerOverUI(touchPos)) return;

            if (raycastManager.Raycast(touchPos, carpismaListesi, TrackableType.PlaneWithinPolygon))
            {
                LogYaz("✅ Zemin bulundu. Yerleştirme başlıyor...");
                ObjeYerlestir(carpismaListesi[0].pose);
            }
        }
    }

    private bool IsPointerOverUI(Vector2 pos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = pos;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    void ObjeYerlestir(Pose pose)
    {
        // --- DETAYLI LOGLAMA ---
        string suankiRenk = GlobalVeri.SecilenRenk;
        Debug.Log($"[AR YERLEŞTİRME] Şu anki Global Renk: '{suankiRenk}'");

        GameObject kullanilacakPrefab = null;

        // Kontrol mantığını loglayarak yapalım
        if (suankiRenk == "Kirmizi")
        {
            Debug.Log("[AR] Kırmızı seçildi.");
            kullanilacakPrefab = arenaKirmiziPrefab;
        }
        else if (suankiRenk == "Mavi")
        {
            Debug.Log("[AR] Mavi seçildi.");
            kullanilacakPrefab = arenaMaviPrefab;
        }
        else
        {
            Debug.Log("[AR] Siyah veya Belirsiz. Varsayılan (Siyah) seçiliyor.");
            kullanilacakPrefab = arenaSiyahPrefab;
        }

        // Prefab kontrolü
        if (kullanilacakPrefab == null)
        {
            LogYaz($"❌ HATA: '{suankiRenk}' için Inspector'da Prefab atanmamış!");
            Debug.LogError($"[AR KRİTİK HATA] '{suankiRenk}' renginin prefabı NULL!");
            return;
        }

        Debug.Log($"[AR SPAWN] Yaratılacak Prefab: {kullanilacakPrefab.name}");

        string yol = $"{NETWORK_PREFABS_PATH}/{kullanilacakPrefab.name}";
        GameObject obj = PhotonNetwork.Instantiate(yol, pose.position, pose.rotation);

        if (obj != null)
        {
            LogYaz("🎉 " + suankiRenk + " ARENA YERLEŞTİ!");
            arenaYerlesti = true;
            ARAnchor anchor = anchorManager.AddAnchor(pose);
            if (anchor != null) obj.transform.SetParent(anchor.transform);

            photonView.RPC("RPC_Kapat", RpcTarget.AllBuffered);
        }
        else
        {
            LogYaz("❌ HATA: Photon Instantiate Başarısız!");
        }
    }

    [PunRPC]
    void RPC_Kapat()
    {
        arenaYerlesti = true;
        LogYaz("🚀 Oyun Başladı (RPC)");
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var p in planeManager.trackables) p.gameObject.SetActive(false);
        }
        Hizala();
        if (ArenaManager.Instance != null) ArenaManager.Instance.StartGameAfterPlacement();
    }

    void Hizala()
    {
        GameObject arena = GameObject.FindWithTag("Arena");
        if (arena == null) return;
        string team = PlayerSession.Team;
        Transform target = (team == "Blue") ? arena.transform.Find("Pos_Blue") : arena.transform.Find("Pos_Red");
        if (target != null)
        {
            GameObject xr = GameObject.Find("XR Origin");
            if (xr == null) xr = GameObject.Find("XR Origin (AR)");
            if (xr != null)
            {
                xr.transform.position = target.position;
                xr.transform.rotation = Quaternion.Euler(0, target.eulerAngles.y, 0);
            }
        }
    }

    void LogYaz(string msj)
    {
        if (debugText != null) debugText.text = msj;
        Debug.Log("[EKRAN LOG] " + msj);
    }
}