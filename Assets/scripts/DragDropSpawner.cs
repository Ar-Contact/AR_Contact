using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarları")]
    public int unitIndex = 0;
    public int birimMaliyeti = 10;

    [Header("Gereklilikler")]
    public LayerMask groundLayer;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Maske kullanarak sadece Ground layer'ına çarpıyoruz
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            // Çarptığın objenin ismini temizle (boşlukları sil)
            string hitName = hit.transform.name.Trim();
            string playerTeam = PlayerSession.Team;

            // --- KRİTİK HATA AYIKLAMA (Console'da ne yazdığına bak) ---
            Debug.Log($"<color=yellow>DENEME:</color> Çarpılan: [{hitName}] | Takım: [{playerTeam}]");

            // KONTROL 1: Mavi takımsan ve isminde "Red" geçiyorsa ENGELLE
            if (playerTeam == "Blue" && hitName.Contains("Red"))
            {
                Debug.LogError("ENGELLENDİ: Mavi Takım Kırmızı Sahaya Koyamaz!");
                return;
            }

            // KONTROL 2: Kırmızı takımsan ve isminde "Blue" geçiyorsa ENGELLE
            if (playerTeam == "Red" && hitName.Contains("Blue"))
            {
                Debug.LogError("ENGELLENDİ: Kırmızı Takım Mavi Sahaya Koyamaz!");
                return;
            }

            // --- PARA VE SPAWN KONTROLÜ ---
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                GameObject mainPlayer = GameObject.Find("MainPlayer");
                if (mainPlayer != null)
                {
                    var spawner = mainPlayer.GetComponent<PhotonPlayerUnitSpawner>();
                    if (spawner != null)
                    {
                        spawner.RequestSpawnUnit(unitIndex, hit.point);
                    }
                }
            }
        }
    }
}