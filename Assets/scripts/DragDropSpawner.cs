using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int unitIndex = 0;
    public int birimMaliyeti = 10;
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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ArenaManager.Instance != null && (!ArenaManager.Instance.isArenaPlaced || ArenaManager.Instance.isWarStarted)) return;
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
        // AR Kamerası üzerinden ekrana dokunulan yere ışın atar
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            string hitName = hit.transform.name.Trim();
            string playerTeam = PlayerSession.Team;

            // Takım bölgesi kontrolü
            if (playerTeam == "Blue" && hitName.Contains("Red")) return;
            if (playerTeam == "Red" && hitName.Contains("Blue")) return;

            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                GameObject mainPlayer = GameObject.Find("MainPlayer");
                if (mainPlayer != null)
                {
                    var spawner = mainPlayer.GetComponent<PhotonPlayerUnitSpawner>();
                    if (spawner != null)
                        spawner.RequestSpawnUnit(unitIndex, hit.point);
                }
            }
        }
    }
}