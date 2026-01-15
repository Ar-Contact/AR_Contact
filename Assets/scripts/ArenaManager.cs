using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.XR.ARFoundation; // AR durumlarını okumak için
using UnityEngine.XR.ARSubsystems;

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

    [HideInInspector] public bool isWarStarted = false;
    [HideInInspector] public bool isArenaPlaced = false;

    void Awake() { Instance = this; }

    // Arena yerleştiğinde TiklaVeYerlestir tarafından çağrılır
    public void StartGameAfterPlacement()
    {
        if (isArenaPlaced) return;
        isArenaPlaced = true;

        if (stateText != null) stateText.text = "ARENA YERLEŞTİ!";

        StopAllCoroutines();
        StartCoroutine(SystemLoop());
    }

    IEnumerator SystemLoop()
    {
        int currentRound = 1;
        while (currentRound <= 5)
        {
            if (roundText != null) roundText.text = "ROUND: " + currentRound;

            // --- HAZIRLIK AŞAMASI ---
            isWarStarted = false;
            UpdatePanelVisibility(true); // Takımına göre paneli aç
            if (stateText != null) stateText.text = "HAZIRLIK ZAMANI";

            float time = 30f;
            while (time > 0)
            {
                if (timerText != null) timerText.text = Mathf.CeilToInt(time).ToString();
                time -= Time.deltaTime;
                yield return null;
            }

            // --- SAVAŞ AŞAMASI ---
            isWarStarted = true;
            UpdatePanelVisibility(false); // Sürükleme panellerini kapat
            if (stateText != null) stateText.text = "SAVAŞ BAŞLADI!";

            yield return new WaitForSeconds(90f);

            currentRound++;
        }

        if (stateText != null) stateText.text = "OYUN BİTTİ!";
    }

    private void UpdatePanelVisibility(bool show)
    {
        string team = PlayerSession.Team; // "Blue" veya "Red" olmalı

        if (string.IsNullOrEmpty(team))
        {
            Debug.LogWarning("Takım henüz seçilmemiş!");
            return;
        }

        if (team == "Blue")
        {
            if (dragBluePanel != null) dragBluePanel.SetActive(show);
            if (dragRedPanel != null) dragRedPanel.SetActive(false);
        }
        else if (team == "Red")
        {
            if (dragRedPanel != null) dragRedPanel.SetActive(show);
            if (dragBluePanel != null) dragBluePanel.SetActive(false);
        }
    }
}