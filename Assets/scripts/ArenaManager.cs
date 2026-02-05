using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // List ve koleksiyonlar için gerekli
using UnityEngine.AI;
using Photon.Pun;

public class ArenaManager : MonoBehaviourPunCallbacks
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

    void Awake()
    {
        Instance = this;
    }

    public void StartGameAfterPlacement()
    {
        if (isArenaPlaced) return;
        isArenaPlaced = true;

        if (stateText != null) stateText.text = "SAVAŞ ALANI HAZIR!";

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SystemLoop());
        }
    }

    IEnumerator SystemLoop()
    {
        int currentRound = 1;
        while (currentRound <= 5)
        {
            photonView.RPC("RPC_UpdateRound", RpcTarget.All, currentRound);

            // --- HAZIRLIK ---
            photonView.RPC("RPC_SetWarState", RpcTarget.All, false);

            float time = 10f;
            while (time > 0)
            {
                photonView.RPC("RPC_UpdateTimer", RpcTarget.All, time);
                time -= Time.deltaTime;
                yield return null;
            }

            // --- SAVAŞ ---
            photonView.RPC("RPC_SetWarState", RpcTarget.All, true);
            photonView.RPC("RPC_WakeUpSoldiers", RpcTarget.All);

            // Takımlardan biri bitene kadar bekle (Maksimum 90 saniye)
            float maxWarDuration = 90f;
            float currentWarTime = 0f;

            while (AreBothTeamsAlive() && currentWarTime < maxWarDuration)
            {
                currentWarTime += Time.deltaTime;
                yield return null;
            }

            Debug.Log("Round bitti, bir takım elendi.");
            yield return new WaitForSeconds(3f); // Bir sonraki round'a geçmeden önce bekleme

            currentRound++;
        }

        if (stateText != null) stateText.text = "MAÇ BİTTİ!";
    }

    bool AreBothTeamsAlive()
    {
        // Eğer askerler öldüğünde Destroy(gameObject) yapılıyorsa bu yeterlidir
        GameObject[] blueTeam = GameObject.FindGameObjectsWithTag("BlueTeam");
        GameObject[] redTeam = GameObject.FindGameObjectsWithTag("RedTeam");

        return blueTeam.Length > 0 && redTeam.Length > 0;
    }

    [PunRPC]
    void RPC_SetWarState(bool state)
    {
        isWarStarted = state;
        UpdatePanelVisibility(!state);

        if (state)
        {
            stateText.text = "SAVAŞ BAŞLADI!";
        }
        else
        {
            stateText.text = "BİRLİKLERİNİ YERLEŞTİR!";

            // Hazırlık aşamasında hayatta kalan askerleri dondur
            FreezeAllUnits();
        }
    }

    void FreezeAllUnits()
    {
        List<GameObject> allSoldiers = new List<GameObject>();
        allSoldiers.AddRange(GameObject.FindGameObjectsWithTag("BlueTeam"));
        allSoldiers.AddRange(GameObject.FindGameObjectsWithTag("RedTeam"));

        foreach (GameObject soldier in allSoldiers)
        {
            UnitAutoFreeze freezer = soldier.GetComponent<UnitAutoFreeze>();
            if (freezer != null)
            {
                freezer.enabled = true;
                freezer.CheckAndFreeze();
            }

            // Güvenlik: NavMesh ve AI'yı kapat
            NavMeshAgent nma = soldier.GetComponent<NavMeshAgent>();
            if (nma != null) nma.enabled = false;

            AiAgent ai = soldier.GetComponent<AiAgent>();
            if (ai != null) ai.enabled = false;
        }
    }

    [PunRPC]
    void RPC_WakeUpSoldiers()
    {
        ActivateTeam("BlueTeam");
        ActivateTeam("RedTeam");
    }

    void ActivateTeam(string teamTag)
    {
        GameObject[] soldiers = GameObject.FindGameObjectsWithTag(teamTag);
        foreach (GameObject soldier in soldiers)
        {
            NavMeshAgent agent = soldier.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(soldier.transform.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    agent.Warp(hit.position);
                }
            }

            UnitAutoFreeze freezer = soldier.GetComponent<UnitAutoFreeze>();
            if (freezer != null) freezer.enabled = false;

            AiAgent ai = soldier.GetComponent<AiAgent>();
            if (ai != null) ai.enabled = true;
        }
    }

    // Timer ve Round RPC'leri
    [PunRPC] void RPC_UpdateRound(int round) { if (roundText != null) roundText.text = "ROUND: " + round; }
    [PunRPC] void RPC_UpdateTimer(float time) { if (timerText != null) timerText.text = Mathf.CeilToInt(time).ToString(); }

    private void UpdatePanelVisibility(bool show)
    {
        // PlayerSession.Team üzerinden hangi panelin açılacağını belirler
        string team = PlayerSession.Team;
        if (dragBluePanel) dragBluePanel.SetActive(team == "Blue" && show);
        if (dragRedPanel) dragRedPanel.SetActive(team == "Red" && show);
    }
}