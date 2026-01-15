using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    public bool isWarStarted = false;
    public bool isArenaPlaced = false;

    void Awake() { Instance = this; }

    public void StartGameAfterPlacement()
    {
        if (isArenaPlaced) return;
        isArenaPlaced = true;
        if (stateText != null) stateText.text = "SAVAŞ ALANI HAZIR!";
        StartCoroutine(SystemLoop());
    }

    IEnumerator SystemLoop()
    {
        int currentRound = 1;
        while (currentRound <= 5)
        {
            if (roundText != null) roundText.text = "ROUND: " + currentRound;

            // --- HAZIRLIK ---
            isWarStarted = false;
            UpdatePanelVisibility(true);
            stateText.text = "BİRLİKLERİNİ YERLEŞTİR!";

            float time = 30f;
            while (time > 0)
            {
                timerText.text = Mathf.CeilToInt(time).ToString();
                time -= Time.deltaTime;
                yield return null;
            }

            // --- SAVAŞ ---
            isWarStarted = true;
            UpdatePanelVisibility(false);
            stateText.text = "SAVAŞ BAŞLADI!";
            yield return new WaitForSeconds(90f);

            currentRound++;
        }
        stateText.text = "MAÇ BİTTİ!";
    }

    private void UpdatePanelVisibility(bool show)
    {
        string team = PlayerSession.Team;
        if (team == "Blue")
        {
            dragBluePanel.SetActive(show);
            dragRedPanel.SetActive(false);
        }
        else if (team == "Red")
        {
            dragRedPanel.SetActive(show);
            dragBluePanel.SetActive(false);
        }
    }
}