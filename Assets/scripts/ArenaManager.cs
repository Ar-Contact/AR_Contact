using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance;

    [Header("UI Panelleri")]
    public GameObject dragBluePanel;
    public GameObject dragRedPanel;

    [Header("Kameralar")]
    public GameObject blueCamera;
    public GameObject redCamera;

    [Header("Zamanlayıcı & Round UI")]
    public Text timerText;
    public Text stateText;
    public Text roundText;

    [HideInInspector]
    public bool isWarStarted = false;

    private float timeRemaining;
    private int currentRound = 1;
    private const int MAX_ROUNDS = 5;
    private bool gameOver = false;

    void Awake() { Instance = this; }

    void Start()
    {
        SetInitialCameraAndPanels();
        StartCoroutine(SystemLoop());
    }

    private void SetInitialCameraAndPanels()
    {
        blueCamera.SetActive(false);
        redCamera.SetActive(false);
        dragBluePanel.SetActive(false);
        dragRedPanel.SetActive(false);

        string team = string.IsNullOrEmpty(PlayerSession.Team) ? "Blue" : PlayerSession.Team;

        if (team == "Blue") blueCamera.SetActive(true);
        else redCamera.SetActive(true);
    }

    IEnumerator SystemLoop()
    {
        while (currentRound <= MAX_ROUNDS && !gameOver)
        {
            if (roundText != null) roundText.text = "ROUND: " + currentRound + " / " + MAX_ROUNDS;

            // --- EKONOMİ GÜNCELLEMESİ (İsteğin Burası) ---
            // Eğer 1. round değilse, hazırlık başlarken 100 puan ver
            if (currentRound > 1)
            {
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.ParaKazan(100);
                    Debug.Log("Yeni Round Bonusu: 100 Puan Eklendi!");
                }
            }

            // --- HAZIRLIK SÜRESİ (30 Saniye) ---
            isWarStarted = false;
            UpdatePanelVisibility(true);
            ToggleAllUnits(false);

            stateText.text = "HAZIRLIK ZAMANI";
            timeRemaining = 30f;
            while (timeRemaining > 0)
            {
                timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // --- SAVAŞ SÜRESİ (90 Saniye) ---
            isWarStarted = true;
            UpdatePanelVisibility(false);
            ToggleAllUnits(true);

            stateText.text = "SAVAŞ BAŞLADI!";
            timeRemaining = (currentRound == MAX_ROUNDS) ? 999999f : 90f;

            while (timeRemaining > 0)
            {
                if (currentRound == MAX_ROUNDS) timerText.text = "∞";
                else timerText.text = Mathf.CeilToInt(timeRemaining).ToString();

                timeRemaining -= Time.deltaTime;

                if (CheckUnitsRemaining()) break;

                yield return null;
            }

            if (currentRound == MAX_ROUNDS)
            {
                DetermineWinner();
                gameOver = true;
            }
            else
            {
                currentRound++;
                // Round biterken kısa bir bekleme (isteğe bağlı)
                yield return new WaitForSeconds(1f);
            }
        }
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

    private void UpdatePanelVisibility(bool show)
    {
        string team = string.IsNullOrEmpty(PlayerSession.Team) ? "Blue" : PlayerSession.Team;
        if (team == "Blue") dragBluePanel.SetActive(show);
        else dragRedPanel.SetActive(show);
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