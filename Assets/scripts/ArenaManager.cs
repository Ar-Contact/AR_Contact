using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.XR.ARFoundation; // AR Kütüphanesi Eklendi
using UnityEngine.XR.ARSubsystems; // AR Durumları için Eklendi

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance;

    [Header("UI Panelleri")]
    public GameObject dragBluePanel;
    public GameObject dragRedPanel;

    [Header("UI Textleri")]
    public Text timerText;
    public Text stateText;
    public Text roundText;

    [HideInInspector]
    public bool isWarStarted = false;
    public bool isArenaPlaced = false;

    private float timeRemaining;
    private int currentRound = 1;
    private const int MAX_ROUNDS = 5;
    private bool gameOver = false;

    // LOGLAMA İÇİN YENİ DEĞİŞKENLER
    private ARSession arSession;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Panelleri kapat
        dragBluePanel.SetActive(false);
        dragRedPanel.SetActive(false);

        // --- AR DIAGNOSTIC (TANI) BAŞLATIYORUZ ---
        arSession = FindObjectOfType<ARSession>();
        StartCoroutine(CheckARStatusLoop());
    }

    // --- KAMERA DURUMUNU EKRANA YAZAN FONKSİYON ---
    IEnumerator CheckARStatusLoop()
    {
        // Oyun başlayana veya arena yerleşene kadar durumu kontrol et
        while (!isArenaPlaced)
        {
            if (arSession == null)
            {
                arSession = FindObjectOfType<ARSession>();
                stateText.text = "HATA: AR Session Bulunamadı!\nXR Origin ekli mi?";
                stateText.color = Color.red;
            }
            else
            {
                // AR Session Durumunu Kontrol Et
                if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                {
                    stateText.text = "AR Sistemi Kontrol Ediliyor...";
                }
                else if (ARSession.state == ARSessionState.NeedsInstall)
                {
                    stateText.text = "UYARI: ARCore/ARKit Yüklenmeli!";
                }
                else if (ARSession.state == ARSessionState.Installing)
                {
                    stateText.text = "AR Yazılımı Yükleniyor...";
                }
                else if (ARSession.state == ARSessionState.Ready)
                {
                    stateText.text = "AR Hazır! Kamera Başlatılıyor...";
                }
                else if (ARSession.state == ARSessionState.SessionInitializing)
                {
                    stateText.text = "Kamera Görüntüsü Bekleniyor...";
                }
                else if (ARSession.state == ARSessionState.SessionTracking)
                {
                    // Her şey yolunda!
                    stateText.text = "KAMERA AKTİF!\nLütfen Zemini Tara ve Çift Tıkla.";
                    stateText.color = Color.green; // Yazıyı yeşil yap
                }
            }
            yield return new WaitForSeconds(0.5f); // Yarım saniyede bir güncelle
        }
    }

    public void StartGameAfterPlacement()
    {
        isArenaPlaced = true;
        stateText.color = Color.white; // Yazı rengini düzelt

        string team = string.IsNullOrEmpty(PlayerSession.Team) ? "Blue" : PlayerSession.Team;
        Debug.Log($"[ArenaManager] Oyun Başlatılıyor. Takım: {team}");

        StartCoroutine(SystemLoop());
    }

    IEnumerator SystemLoop()
    {
        // 1. Arenanın yerleşmesini bekle
        while (!isArenaPlaced) yield return null;

        Debug.Log("[ArenaManager] Sistem Döngüsü Başladı.");

        while (currentRound <= MAX_ROUNDS && !gameOver)
        {
            if (roundText != null) roundText.text = "ROUND: " + currentRound + " / " + MAX_ROUNDS;

            // --- EKONOMİ ---
            if (currentRound > 1 && CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.ParaKazan(100);
            }

            // --- HAZIRLIK ---
            isWarStarted = false;
            UpdatePanelVisibility(true);
            ToggleAllUnits(false);

            stateText.text = "HAZIRLIK ZAMANI";
            Debug.Log($"[Round {currentRound}] Hazırlık Başladı.");

            timeRemaining = 30f;
            while (timeRemaining > 0)
            {
                timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // --- SAVAŞ ---
            isWarStarted = true;
            UpdatePanelVisibility(false);
            ToggleAllUnits(true);

            stateText.text = "SAVAŞ BAŞLADI!";
            Debug.Log($"[Round {currentRound}] Savaş Başladı.");

            timeRemaining = (currentRound == MAX_ROUNDS) ? 999999f : 90f;

            while (timeRemaining > 0)
            {
                if (currentRound == MAX_ROUNDS) timerText.text = "∞";
                else timerText.text = Mathf.CeilToInt(timeRemaining).ToString();

                timeRemaining -= Time.deltaTime;
                if (CheckUnitsRemaining()) break;
                yield return null;
            }

            // --- ROUND SONU ---
            if (currentRound == MAX_ROUNDS)
            {
                DetermineWinner();
                gameOver = true;
            }
            else
            {
                currentRound++;
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void UpdatePanelVisibility(bool show)
    {
        string team = string.IsNullOrEmpty(PlayerSession.Team) ? "Blue" : PlayerSession.Team;
        if (team == "Blue") dragBluePanel.SetActive(show);
        else dragRedPanel.SetActive(show);
    }

    private bool CheckUnitsRemaining()
    {
        if (timeRemaining > 88f && currentRound != MAX_ROUNDS) return false;
        int blueCount = GameObject.FindGameObjectsWithTag("BlueTeam").Length;
        int redCount = GameObject.FindGameObjectsWithTag("RedTeam").Length;
        return (blueCount == 0 || redCount == 0);
    }

    private void DetermineWinner()
    {
        int blueCount = GameObject.FindGameObjectsWithTag("BlueTeam").Length;
        int redCount = GameObject.FindGameObjectsWithTag("RedTeam").Length;
        if (blueCount > redCount) stateText.text = "MAVİ TAKIM KAZANDI!";
        else if (redCount > blueCount) stateText.text = "KIRMIZI TAKIM KAZANDI!";
        else stateText.text = "BERABERE!";
    }

    public void ToggleAllUnits(bool activate)
    {
        GameObject[] blueUnits = GameObject.FindGameObjectsWithTag("BlueTeam");
        GameObject[] redUnits = GameObject.FindGameObjectsWithTag("RedTeam");
        foreach (var unit in blueUnits) ApplyUnitState(unit, activate);
        foreach (var unit in redUnits) ApplyUnitState(unit, activate);
    }

    private void ApplyUnitState(GameObject unit, bool state)
    {
        NavMeshAgent nma = unit.GetComponent<NavMeshAgent>();
        MonoBehaviour ai = unit.GetComponent("AiAgent") as MonoBehaviour;
        if (nma != null) nma.enabled = state;
        if (ai != null) ai.enabled = state;
    }
}