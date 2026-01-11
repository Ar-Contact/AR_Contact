using UnityEngine;
using UnityEngine.AI;
using Photon.Pun; // MUTLAKA EKLE

public class AiArcherAgent : AiAgent
{
    [Header("Okçu Özel Ayarlarý")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;

    [Header("Animasyon Zamanlamasý")]
    [Range(0.0f, 1.0f)]
    public float shootTiming = 1.0f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // KRÝTÝK: Diðer birimlerdeki að kontrolü
        if (!GetComponent<PhotonView>().IsMine)
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
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiArcherAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        // KRÝTÝK: Sadece sahibi AI mantýðýný yönetsin
        if (!GetComponent<PhotonView>().IsMine) return;

        if (health != null && health.isDead)
        {
            navMeshAgent.enabled = false;
            return;
        }

        if (targetTransform == null)
        {
            FindClosestEnemy();
            return;
        }

        stateMachine.Update();
    }

    [Photon.Pun.PunRPC]
    public void RPC_ShootArrow(Vector3 direction, int targetViewID)
    {
        Photon.Pun.PhotonView targetView = Photon.Pun.PhotonView.Find(targetViewID);
        if (targetView == null) return;

        GameObject arrowObj = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.LookRotation(direction));
        ArrowProjectile arrowScript = arrowObj.GetComponent<ArrowProjectile>();
        if (arrowScript != null)
        {
            // 'gameObject' parametresini (yani okçuyu) ekledik
            arrowScript.Initialize(attackDamage, targetView.transform, gameObject);
        }
    }
}