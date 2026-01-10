using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarları")]
    public int unitIndex = 0; // Listede kaçıncı sırada? (0: Okçu/Büyücü)
    public int birimMaliyeti = 50;

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
        originalPos = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = originalPos;
        TrySpawnUnit();
    }

    private void TrySpawnUnit()
    {
        Debug.Log("►►► TrySpawnUnit CALLED! ◄◄◄");
        Debug.Log($"Camera.main: {(Camera.main != null ? "EXISTS" : "NULL")}");
        Debug.Log($"Input.mousePosition: {Input.mousePosition}");
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        Debug.Log($"Raycast starting from: {ray.origin}, direction: {ray.direction}");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Debug.Log($"✓ Raycast HIT! Position: {hit.point}, Object: {hit.collider.name}");
            // Para Kontrolü
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                // FIXED: Find PlayerUnitSpawner on MainPlayer (scene object)
                // MainPlayer is NOT a spawned PlayerObject, it's in the scene
                GameObject mainPlayer = GameObject.Find("MainPlayer");
                
                if (mainPlayer == null)
                {
                    Debug.LogError("╔═════════════════════════════════════════╗");
                    Debug.LogError("║ MainPlayer GameObject NOT FOUND!       ║");
                    Debug.LogError("╚═════════════════════════════════════════╝");
                    return;
                }
                
                var spawner = mainPlayer.GetComponent<PhotonPlayerUnitSpawner>();

                if (spawner != null)
                {
                    Debug.Log($"╔═════════════════════════════════════════╗");
                    Debug.Log($"║ DragDrop: Spawner FOUND!               ║");
                    Debug.Log($"║ Calling RequestSpawnUnit(index: {unitIndex})   ║");
                    Debug.Log($"╚═════════════════════════════════════════╝");
                    
                    spawner.RequestSpawnUnit(unitIndex, hit.point);
                    Debug.Log("✓ Sunucuya emir verildi!");
                }
                else
                {
                    Debug.LogError("╔═════════════════════════════════════════╗");
                    Debug.LogError("║ PhotonPlayerUnitSpawner NOT on MainPlayer! ║");
                    Debug.LogError("╚═════════════════════════════════════════╝");
                }
            }
            else
            {
                Debug.Log("PARAN YETMEDI!");
            }
        }
        else
        {
            Debug.LogWarning("✗ Raycast MISSED! No ground hit detected.");
            Debug.LogWarning($"Ground Layer Mask: {groundLayer.value}");
        }
    }
}