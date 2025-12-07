using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarlarý")]
    public GameObject unitPrefab;
    public int birimMaliyeti = 50; // BU ASKERÝN FÝYATI

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

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Eðer para yetmiyorsa sürüklemeye bile izin vermeyebiliriz (Ýsteðe baðlý)
        // Ama þimdilik sürüklesin, býrakýnca kontrol etsin.

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
        rectTransform.anchoredPosition = originalPos; // Ýkonu yerine koy

        // Býraktýðý yer geçerli mi VE Parasý yetiyor mu?
        TrySpawnUnit();
    }

    private void TrySpawnUnit()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            // ÖNCE PARA KONTROLÜ
            // CurrencyManager'a sor: "Bu kadar param var mý?"
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                // Para varsa ve kesildiyse askeri oluþtur
                GameObject newUnit = Instantiate(unitPrefab, hit.point, Quaternion.identity);

                UnityEngine.AI.NavMeshAgent agent = newUnit.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.Warp(hit.point);

                Debug.Log("Birim oluþturuldu. Kalan Bakiye güncellendi.");
            }
            else
            {
                // Para yoksa görsel bir uyarý verebilirsin (Ses çalmak vb.)
                Debug.Log("PARAN YETMEDÝ!");
            }
        }
    }
}