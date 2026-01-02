using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarlarý")]
    public int unitIndex = 0; // Listede kaçýncý sýrada? (0: Okçu/Büyücü)
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            // Para Kontrolü
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                // LOCAL PLAYER'I BUL VE EMÝR VER
                if (NetworkManager.Singleton.LocalClient != null &&
                    NetworkManager.Singleton.LocalClient.PlayerObject != null)
                {
                    var myPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
                    var spawner = myPlayerObject.GetComponent<PlayerUnitSpawner>();

                    if (spawner != null)
                    {
                        spawner.RequestSpawnUnit(unitIndex, hit.point);
                        Debug.Log("Sunucuya emir verildi!");
                    }
                }
            }
            else
            {
                Debug.Log("PARAN YETMEDÝ!");
            }
        }
    }
}