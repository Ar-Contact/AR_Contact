using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun; // Photon kütüphanesi şart

public class DragAndDropSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Birim Ayarları")]
    // ARTIK BURADAN PREFABI SEÇEBİLİRSİN
    // Önemli: Bu prefab Assets/Resources klasöründe olmalı!
    public GameObject unitPrefab;
    public int birimMaliyeti = 10;

    [Header("Layer Ayarı")]
    public LayerMask groundLayer;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;
    private Canvas parentCanvas;
    private Camera arCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        arCamera = Camera.main;
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

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            string playerTeam = PlayerSession.Team;
            string hitTag = hit.transform.tag;

            // Bölge Kontrolü
            if ((playerTeam == "Blue" && hitTag == "RedGround") ||
                (playerTeam == "Red" && hitTag == "BlueGround"))
            {
                Debug.LogWarning("Düşman sahasına asker koyamazsın!");
                return;
            }

            // Para ve Doğurma Kontrolü
            if (CurrencyManager.Instance.ParaHarcayabilirMi(birimMaliyeti))
            {
                if (unitPrefab != null)
                {
                    // ARTIK DOĞRUDAN İSİM GÖNDERİYORUZ
                    SpawnManager(unitPrefab.name, hit.point);
                }
                else
                {
                    Debug.LogError("HATA: Inspector'dan Unit Prefab atanmamış!");
                }
            }
        }
    }

    private void SpawnManager(string prefabName, Vector3 spawnPos)
    {
        // Photon ile ağına gönderiyoruz. 
        // Prefabın Resources klasöründe olması hayati önem taşır!
        PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
        Debug.Log(prefabName + " başarıyla spawn edildi.");
    }
}