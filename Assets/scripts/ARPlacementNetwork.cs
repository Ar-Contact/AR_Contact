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
    [Header("Ayarlar")]
    public GameObject secilenPrefab;
    public Text debugText;

    private const string NETWORK_PREFABS_PATH = "NetworkPrefabs";

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private ARAnchorManager anchorManager;
    private List<ARRaycastHit> carpismaListesi = new List<ARRaycastHit>();

    // --- KİLİT DEĞİŞKENİ ---
    private bool arenaYerlesti = false;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
        anchorManager = GetComponent<ARAnchorManager>();

        LogYaz("SCRIPT AKTİF! Lütfen sarı noktalara tıkla...");
    }

    void Update()
    {
        // EĞER ARENA ZATEN YERLEŞTİYSE HİÇBİR ŞEY YAPMA VE ÇIK
        if (arenaYerlesti) return;

        // Eğer ArenaManager tarafında oyun başladıysa da dur
        if (ArenaManager.Instance != null && ArenaManager.Instance.isArenaPlaced) return;

        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || (Application.isEditor && Input.GetMouseButtonDown(0)))
        {
            // 1. Master Client Kontrolü
            if (!PhotonNetwork.IsMasterClient)
            {
                LogYaz("❌ HATA: Sadece Master Client arena koyabilir!");
                return;
            }

            Vector2 touchPos = (Input.touchCount > 0) ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

            if (IsPointerOverUI(touchPos)) return;

            if (raycastManager.Raycast(touchPos, carpismaListesi, TrackableType.PlaneWithinPolygon))
            {
                LogYaz("✅ ZEMİN BULUNDU! Yerleştiriliyor...");
                ObjeYerlestir(carpismaListesi[0].pose);
            }
            else
            {
                LogYaz("⚠️ SARI ZEMİN BULUNAMADI! Sarı noktalara tıkla.");
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
        if (secilenPrefab == null) return;

        string yol = $"{NETWORK_PREFABS_PATH}/{secilenPrefab.name}";
        GameObject obj = PhotonNetwork.Instantiate(yol, pose.position, pose.rotation);

        if (obj != null)
        {
            LogYaz("🎉 ARENA YERLEŞTİ! Artık değiştirilemez.");

            // --- KİLİDİ KAPATIYORUZ ---
            arenaYerlesti = true;
            // --------------------------

            ARAnchor anchor = anchorManager.AddAnchor(pose);
            if (anchor != null) obj.transform.SetParent(anchor.transform);

            photonView.RPC("RPC_Kapat", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void RPC_Kapat()
    {
        // Diğer oyuncularda da kilidi kapat
        arenaYerlesti = true;

        LogYaz("🚀 Oyun Başladı RPC!");
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
        Debug.Log(msj);
    }
}