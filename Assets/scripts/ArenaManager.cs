using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI; // NavMesh için gerekli
using Photon.Pun;     // Multiplayer için gerekli

// PhotonView eklemeyi unutma!
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

        // Sadece Master Client (Odayı kuran) oyun döngüsünü yönetsin
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
            // Tüm oyunculara round bilgisini gönder
            photonView.RPC("RPC_UpdateRound", RpcTarget.All, currentRound);

            // --- HAZIRLIK ---
            photonView.RPC("RPC_SetWarState", RpcTarget.All, false); // Savaşı durdur, panelleri aç

            float time = 10f; // Test için 10 saniye (Normalde 30 yaparsın)
            while (time > 0)
            {
                photonView.RPC("RPC_UpdateTimer", RpcTarget.All, time);
                time -= Time.deltaTime;
                yield return null;
            }

            // --- SAVAŞ ---
            photonView.RPC("RPC_SetWarState", RpcTarget.All, true); // Savaşı başlat, panelleri kapat

            // !!! İŞTE EKSİK OLAN KISIM BURASIYDI !!!
            // Askerleri uyandır
            photonView.RPC("RPC_WakeUpSoldiers", RpcTarget.All);

            yield return new WaitForSeconds(30f); // Savaş süresi (90 yapabilirsin)

            currentRound++;
        }

        if (stateText != null) stateText.text = "MAÇ BİTTİ!";
    }

    [PunRPC]
    void RPC_UpdateRound(int round)
    {
        if (roundText != null) roundText.text = "ROUND: " + round;
    }

    [PunRPC]
    void RPC_UpdateTimer(float time)
    {
        if (timerText != null) timerText.text = Mathf.CeilToInt(time).ToString();
        if (stateText != null && !isWarStarted) stateText.text = "HAZIRLIK: " + Mathf.CeilToInt(time);
    }

    [PunRPC]
    void RPC_SetWarState(bool state)
    {
        isWarStarted = state;
        UpdatePanelVisibility(!state); // Savaş başladıysa paneli kapat (false), değilse aç (true)

        if (state) stateText.text = "SAVAŞ BAŞLADI!";
        else stateText.text = "BİRLİKLERİNİ YERLEŞTİR!";
    }

    [PunRPC]
    void RPC_WakeUpSoldiers()
    {
        Debug.Log("ASKERLER UYANDIRILIYOR!");

        // Mavi ve Kırmızı takımı bul ve hareket ettir
        ActivateTeam("BlueTeam");
        ActivateTeam("RedTeam");
    }

    void ActivateTeam(string teamTag)
    {
        GameObject[] soldiers = GameObject.FindGameObjectsWithTag(teamTag);
        foreach (GameObject soldier in soldiers)
        {
            Debug.Log($"<color=yellow>[ACTIVATE] {soldier.name} aktifleştiriliyor...</color>");
            
            // 1. NavMeshAgent'ı Aç ve Ayarla
            NavMeshAgent agent = soldier.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                // Önce NavMesh pozisyonunu bul
                NavMeshHit hit;
                if (NavMesh.SamplePosition(soldier.transform.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    // Agent'ı kapalıyken pozisyonu ayarla
                    soldier.transform.position = hit.position;
                    Debug.Log($"<color=green>[ACTIVATE] {soldier.name} NavMesh'e yerleştirildi: {hit.position}</color>");
                }
                
                // Agent'ı aç
                agent.enabled = true;
                
                // Agent ayarları
                agent.baseOffset = 0f;
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
                
                // NavMesh üzerinde olduğunu kontrol et
                if (!agent.isOnNavMesh)
                {
                    Debug.LogWarning($"<color=orange>[ACTIVATE] {soldier.name} hala NavMesh üzerinde değil! Warp deneniyor...</color>");
                    if (NavMesh.SamplePosition(soldier.transform.position, out hit, 10.0f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        Debug.Log($"<color=green>[ACTIVATE] {soldier.name} Warp ile NavMesh'e yerleştirildi: {hit.position}</color>");
                    }
                }
                else
                {
                    Debug.Log($"<color=green>[ACTIVATE] {soldier.name} NavMesh üzerinde! isOnNavMesh=true</color>");
                }
            }

            // 2. Dondurucu Scripti Kapat
            UnitAutoFreeze freezer = soldier.GetComponent<UnitAutoFreeze>();
            if (freezer != null) freezer.enabled = false;

            // 3. AI Scriptini Aç
            AiAgent ai = soldier.GetComponent<AiAgent>();
            if (ai != null) ai.enabled = true;
            
            // 4. GroundSnapper'ı tetikle
            GroundSnapper snapper = soldier.GetComponent<GroundSnapper>();
            if (snapper != null)
            {
                snapper.ForceSnapToGround();
                Debug.Log($"<color=cyan>[ACTIVATE] {soldier.name} GroundSnapper tetiklendi</color>");
            }
        }
    }

    private void UpdatePanelVisibility(bool show)
    {
        string team = PlayerSession.Team;
        if (team == "Blue")
        {
            if (dragBluePanel) dragBluePanel.SetActive(show);
            if (dragRedPanel) dragRedPanel.SetActive(false);
        }
        else if (team == "Red")
        {
            if (dragRedPanel) dragRedPanel.SetActive(show);
            if (dragBluePanel) dragBluePanel.SetActive(false);
        }
    }
}