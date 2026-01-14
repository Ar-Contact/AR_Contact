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
    public float chaseDistance = 20f;
    public float attackDistance = 5f;
    public float attackDamage = 25f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // NavMeshAgent'ý kapatmak yerine, durdurmayý deneyelim
        if (!GetComponent<Photon.Pun.PhotonView>().IsMine)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }
        }

        navMeshAgent.stoppingDistance = attackDistance;
        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiChaseState());
        stateMachine.RegisterState(new AiAttackState());
        stateMachine.ChangeState(AIStateId.idle);
    }

    private float enemyScanInterval = 0.5f; // Scan for enemies every 0.5 seconds
    private float lastEnemyScanTime = 0f;

    void Update()
    {
        if (!GetComponent<Photon.Pun.PhotonView>().IsMine) return; // Sadece sahibi hareket ettirsin

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
        // Eðer IsMine deðilse bu objeyi biz yönetmiyoruz demektir, hedef aramayý býrak
        if (!GetComponent<Photon.Pun.PhotonView>().IsMine) return;

        // Determine enemy tag based on my tag
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
            // Unit doesn't have a team tag yet
            return;
        }
        
        // Find all enemies with opposite tag
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        if (enemies.Length == 0)
        {
            return; // No enemies found
        }
        
        // Find closest enemy
        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            // Check if enemy is alive
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.isDead)
            {
                continue; // Skip dead enemies
            }
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        
        // Set target if found
        if (closestEnemy != null)
        {
            SetTarget(closestEnemy.transform);
            Debug.Log($"{gameObject.name} ({myTag}) found enemy: {closestEnemy.name} ({enemyTag}) at distance {closestDistance:F1}");
        }
    }

    // AiAgent.cs içine bu RPC fonksiyonunu ekle
    public void SetTarget(Transform target)
    {
        targetTransform = target;

        // Eðer biz sahibiysek, hedefimizi diðer oyunculara da bildir
        if (GetComponent<Photon.Pun.PhotonView>().IsMine && target != null)
        {
            Photon.Pun.PhotonView targetView = target.GetComponent<Photon.Pun.PhotonView>();
            if (targetView != null)
            {
                GetComponent<Photon.Pun.PhotonView>().RPC("SyncTargetRPC", Photon.Pun.RpcTarget.Others, targetView.ViewID);
            }
        }
    }

    [Photon.Pun.PunRPC]
    public void SyncTargetRPC(int targetViewID)
    {
        // Diðer oyuncular sahibin seçtiði hedefi kendi ekranlarýnda bulur
        Photon.Pun.PhotonView targetView = Photon.Pun.PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            targetTransform = targetView.transform;
        }
    }
}
