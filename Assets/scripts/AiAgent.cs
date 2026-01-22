using UnityEngine;
using UnityEngine.AI;

public class AiAgent : MonoBehaviour
{
    public AiStateMachine stateMachine;

    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Health health;

    [HideInInspector] public Transform targetTransform;

    [Header("AI Mesafeleri")]
    public float chaseDistance = 10f;    // Düşürüldü: 20 -> 10
    public float attackDistance = 2f;    // Düşürüldü: 5 -> 2
    public float attackDamage = 10f;     // Düşürüldü: 25 -> 10

    [Header("Hareket Ayarları")]
    [Tooltip("Karakterin hareket hızı")]
    public float moveSpeed = 1.5f;       // Düşürüldü: 2.5 -> 1.5
    
    [Tooltip("Karakterin zemine oturma offset'i (0 = zemine oturur)")]
    public float baseOffset = 0f;

    [Header("Debug")]
    public bool autoUpdateAnimatorSpeed = true;

    void Start()
    {
        Debug.Log($"<color=white>[AGENT] {gameObject.name} - AiAgent BASLATILIYOR...</color>");
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // Bilesenleri kontrol et
        if (navMeshAgent == null) Debug.LogError($"<color=red>[AGENT] {gameObject.name} - NavMeshAgent BULUNAMADI!</color>");
        if (animator == null) Debug.LogError($"<color=red>[AGENT] {gameObject.name} - Animator BULUNAMADI!</color>");
        if (health == null) Debug.LogWarning($"<color=orange>[AGENT] {gameObject.name} - Health bileşeni yok.</color>");

        // UYARI: attackDamage 0 ise hasar veremez!
        if (attackDamage <= 0)
        {
            Debug.LogError($"<color=red>[AGENT] {gameObject.name} - UYARI: attackDamage = {attackDamage}! Bu karakter HASAR VEREMEZ! Prefab'da değeri düzelt!</color>");
        }

        // PhotonView kontrolu
        var photonView = GetComponent<Photon.Pun.PhotonView>();
        if (photonView == null)
        {
            Debug.LogError($"<color=red>[AGENT] {gameObject.name} - PhotonView BULUNAMADI!</color>");
            return;
        }

        Debug.Log($"<color=white>[AGENT] {gameObject.name} - IsMine: {photonView.IsMine}, Tag: {gameObject.tag}</color>");

        // NavMeshAgenti kapatmak yerine, durdurmayi deneyelim
        if (!photonView.IsMine)
        {
            Debug.Log($"<color=gray>[AGENT] {gameObject.name} - Bu agent bize ait DEGIL, NavMesh kapatiliyor.</color>");
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }
            return;
        }

        // NavMesh durumunu kontrol et
        if (navMeshAgent != null)
        {
            Debug.Log($"<color=white>[AGENT] {gameObject.name} - NavMesh durumu: isOnNavMesh={navMeshAgent.isOnNavMesh}, isActiveAndEnabled={navMeshAgent.isActiveAndEnabled}</color>");
            
            if (!navMeshAgent.isOnNavMesh)
            {
                Debug.LogError($"<color=red>[AGENT] {gameObject.name} - KARAKTER NAVMESH UZERINDE DEGIL! Pozisyon: {transform.position}</color>");
            }
        }

        // Hareket hızı ve baseOffset ayarları
        navMeshAgent.speed = moveSpeed;
        // baseOffset'i her zaman 0 yap - karakterlerin havada kalmasını önler
        navMeshAgent.baseOffset = 0f;
        navMeshAgent.stoppingDistance = attackDistance;
        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiChaseState());
        stateMachine.RegisterState(new AiAttackState());
        stateMachine.ChangeState(AIStateId.idle);
        
        Debug.Log($"<color=white>[AGENT] {gameObject.name} - AiAgent HAZIRLANDI! ChaseDistance: {chaseDistance}, AttackDistance: {attackDistance}, AttackDamage: {attackDamage}, MoveSpeed: {moveSpeed}</color>");
    }

    private float enemyScanInterval = 0.5f;
    private float lastEnemyScanTime = 0f;

    void Update()
    {
        var photonView = GetComponent<Photon.Pun.PhotonView>();
        if (!photonView.IsMine) return;

        if (health != null && health.isDead)
        {
            navMeshAgent.enabled = false;
            return;
        }

        // Automatically search for enemies if no target
        if (targetTransform == null)
        {
            // Only scan periodically to save performance
            if (Time.time - lastEnemyScanTime > enemyScanInterval)
            {
                lastEnemyScanTime = Time.time;
                FindClosestEnemy();
            }
            return;
        }

        stateMachine.Update();
    }
    
    public void FindClosestEnemy()
    {
        var photonView = GetComponent<Photon.Pun.PhotonView>();
        if (!photonView.IsMine) return;

        string myTag = gameObject.tag;
        string enemyTag = "";
        
        if (myTag == "BlueTeam")
        {
            enemyTag = "RedTeam";
        }
        else if (myTag == "RedTeam")
        {
            enemyTag = "BlueTeam";
        }
        else
        {
            Debug.LogWarning($"<color=orange>[AGENT] {gameObject.name} - Gecersiz tag: '{myTag}'. BlueTeam veya RedTeam olmali!</color>");
            return;
        }
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        if (enemies.Length == 0)
        {
            // Her 60 frame'de bir log at
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"<color=gray>[AGENT] {gameObject.name} - {enemyTag} dusm­ani bulunamadi.</color>");
            }
            return;
        }
        
        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.isDead)
            {
                continue;
            }
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        
        if (closestEnemy != null)
        {
            SetTarget(closestEnemy.transform);
            Debug.Log($"<color=cyan>[AGENT] {gameObject.name} ({myTag}) - DUSMAN BULUNDU: {closestEnemy.name} ({enemyTag}), Uzaklik: {closestDistance:F1}</color>");
        }
    }

    public void SetTarget(Transform target)
    {
        targetTransform = target;
        Debug.Log($"<color=cyan>[AGENT] {gameObject.name} - Hedef AYARLANDI: {(target != null ? target.name : "NULL")}</color>");

        var photonView = GetComponent<Photon.Pun.PhotonView>();
        if (photonView.IsMine && target != null)
        {
            Photon.Pun.PhotonView targetView = target.GetComponent<Photon.Pun.PhotonView>();
            if (targetView != null)
            {
                photonView.RPC("SyncTargetRPC", Photon.Pun.RpcTarget.Others, targetView.ViewID);
            }
        }
    }

    [Photon.Pun.PunRPC]
    public void SyncTargetRPC(int targetViewID)
    {
        Photon.Pun.PhotonView targetView = Photon.Pun.PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            targetTransform = targetView.transform;
            Debug.Log($"<color=cyan>[AGENT] {gameObject.name} - RPC ile hedef senkronize edildi: {targetTransform.name}</color>");
        }
    }
}
