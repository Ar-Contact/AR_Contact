using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarları")]
    public int unitIndex = 0;
    public int birimMaliyeti = 10;

    [Header("Layer Ayarı (DİKKAT)")]
    public LayerMask groundLayer; // Inspector'da "Ground" layerını seçmelisin!

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;
    private Canvas parentCanvas;
    private Camera arCamera; // AR Kamerası

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        arCamera = Camera.main; // Sahnedeki XR Origin kamerasını bulur
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Savaş başladıysa (veya hazırlık değilse) sürükletme
        if (ArenaManager.Instance != null && ArenaManager.Instance.isWarStarted) return;

        originalPos = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // Arkadaki zemine tıklayabilelim
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
        rectTransform.anchoredPosition = originalPos; // İkon yerine döner

        if (ArenaManager.Instance != null && ArenaManager.Instance.isWarStarted) return;

        TrySpawnUnit();
    }

    private void TrySpawnUnit()
    {
        // Parmağın (veya mouse'un) olduğu yerden AR dünyasına ışın at
        Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Sadece "Ground" layer'ına çarp
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            string playerTeam = PlayerSession.Team;
            string hitTag = hit.transform.tag; // BlueGround veya RedGround

            Debug.Log($"Vurulan Zemin: {hitTag} | Benim Takımım: {playerTeam}");

            // --- BÖLGE KONTROLÜ (Senin Taglerinle) ---
            if (playerTeam == "Blue" && hitTag == "RedGround")
            {
                Debug.LogWarning("Mavi Takım Kırmızı Sahaya Koyamaz!");
                return;
            }
            if (playerTeam == "Red" && hitTag == "BlueGround")
            {
                Debug.LogWarning("Kırmızı Takım Mavi Sahaya Koyamaz!");
                return;
            }

            // --- PARA VE DOĞURMA ---
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                var spawner = FindObjectOfType<PhotonPlayerUnitSpawner>();
                if (spawner != null)
                {
                    // Tıklanan noktayı gönder
                    spawner.RequestSpawnUnit(unitIndex, hit.point);
                }
            }
        }
    }
}