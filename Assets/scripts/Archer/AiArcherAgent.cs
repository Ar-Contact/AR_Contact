using UnityEngine;
using UnityEngine.AI;
using Photon.Pun; // MUTLAKA EKLE

public class AiArcherAgent : AiAgent
{
    [Header("Ok�u �zel Ayarlar�")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;

    [Header("Animasyon Zamanlamas�")]
    [Range(0.0f, 1.0f)]
    public float shootTiming = 1.0f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // KR�T�K: Di�er birimlerdeki a� kontrol�
        if (!GetComponent<PhotonView>().IsMine)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }
        }

        // Hareket hızı ve baseOffset ayarları
        navMeshAgent.speed = moveSpeed;
        // baseOffset'i her zaman 0 yap - karakterlerin havada kalmasını önler
        navMeshAgent.baseOffset = 0f;
        navMeshAgent.stoppingDistance = attackDistance;

        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiArcherAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        // KR�T�K: Sadece sahibi AI mant���n� y�netsin
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
            // 'gameObject' parametresini (yani ok�uyu) ekledik
            arrowScript.Initialize(attackDamage, targetView.transform, gameObject);
        }
    }
}
