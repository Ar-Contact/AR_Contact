using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarları")]
    public int unitIndex;
    public int birimMaliyeti = 10;

    [Header("Layer Ayarı")]
    public LayerMask groundLayer; // DİKKAT: Inspector'da "Ground" seçili olmalı!

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;
    private Canvas parentCanvas;
    private Camera arCamera;
    private PhotonPlayerUnitSpawner centralSpawner;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        arCamera = Camera.main;
        centralSpawner = FindObjectOfType<PhotonPlayerUnitSpawner>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ArenaManager.Instance != null && ArenaManager.Instance.isWarStarted) return;
        originalPos = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ArenaManager.Instance != null && ArenaManager.Instance.isWarStarted) return;
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = originalPos;

        if (ArenaManager.Instance != null && ArenaManager.Instance.isWarStarted) return;

        TrySpawnUnit();
    }

    private void TrySpawnUnit()
    {
        Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Işın atıyoruz
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            string playerTeam = PlayerSession.Team;
            string hitTag = hit.transform.tag;
            string hitLayer = LayerMask.LayerToName(hit.transform.gameObject.layer);

            // --- HATA AYIKLAMA LOGU ---
            // Nereye tıkladığını görmek için bunu ekledik
            Debug.Log($"Işın Çarptı -> Obje: {hit.transform.name}, Tag: {hitTag}, Layer: {hitLayer}");

            bool isPlacementValid = false;

            if (playerTeam == "Blue" && hitTag == "BlueGround") isPlacementValid = true;
            else if (playerTeam == "Red" && hitTag == "RedGround") isPlacementValid = true;

            if (!isPlacementValid)
            {
                Debug.LogWarning($"❌ YANLIŞ BÖLGE! Senin Takımın: {playerTeam}, Tıklanan Tag: {hitTag}");
                return;
            }

            // Para ve Oluşturma
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                if (centralSpawner != null)
                {
                    Vector3 spawnPos = hit.point;
                    spawnPos.y += 0.05f; // Yerin dibine girmesin
                    centralSpawner.RequestSpawnUnit(unitIndex, spawnPos);
                    Debug.Log("✅ Asker koyma isteği gönderildi.");
                }
            }
            else
            {
                Debug.Log("💰 Yetersiz Bakiye!");
            }
        }
        else
        {
            // BURASI ÇALIŞIYORSA SORUN LAYER VEYA COLLIDER AYARINDADIR
            Debug.LogError("🚨 HATA: Işın hiçbir 'Ground' objesine çarpmadı!");
            Debug.LogError("Kontrol Et: 1. Arena zeminlerinde Collider var mı? 2. Arena zeminlerinin Layer'ı 'Ground' mu? 3. Scriptteki Ground Layer 'Ground' seçili mi?");
        }
    }
}