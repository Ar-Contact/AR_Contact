using Photon.Pun;
using UnityEngine;

public class AiArcherAttackState : AIState
{
    private bool hasShotArrow;

    // Artýk buradaki gizli deðiþkene ihtiyacýmýz yok, Agent'tan okuyacaðýz.

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        // Güvenli ResetPath
        if (agent.navMeshAgent.isActiveAndEnabled && agent.navMeshAgent.isOnNavMesh)
        {
            agent.navMeshAgent.ResetPath();
        }

        agent.animator.SetFloat("Speed", 0f);
        agent.animator.SetBool("IsAttacking", true);
        hasShotArrow = false;
    }

    public void Update(AiAgent agent)
    {
        if (agent.targetTransform == null)
        {
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        Health targetHealth = agent.targetTransform.GetComponent<Health>();
        if (targetHealth == null || targetHealth.isDead)
        {
            agent.SetTarget(null);
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        // Hedefe Dön
        Vector3 direction = (agent.targetTransform.position - agent.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        float distance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);
        if (distance > agent.attackDistance)
        {
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            return;
        }

        // --- OK FIRLATMA ZAMANLAMASI ---
        AiArcherAgent archerAgent = agent as AiArcherAgent;

        if (archerAgent != null)
        {
            AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1.0f;

            // DÜZELTME: 'shootTiming' deðerini artýk archerAgent'ýn üzerinden alýyoruz.
            // Bu sayede Inspector'dan deðiþtirdiðin an buraya yansýr.
            if (normalizedTime >= archerAgent.shootTiming && !hasShotArrow)
            {
                ShootArrow(archerAgent, direction);
                hasShotArrow = true;
            }

            // Animasyon baþa sardýysa tekrar atýþa hazýrlan
            if (normalizedTime < 0.1f)
            {
                hasShotArrow = false;
            }
        }
    }

    public void Exit(AiAgent agent)
    {
        agent.animator.SetBool("IsAttacking", false);
    }

    // ShootArrow fonksiyonunu þu þekilde deðiþtir knk:
    private void ShootArrow(AiArcherAgent agent, Vector3 targetDirection)
    {
        if (agent.arrowPrefab != null && agent.arrowSpawnPoint != null && agent.targetTransform != null)
        {
            int targetViewID = agent.targetTransform.GetComponent<PhotonView>().ViewID;

            // Oku herkesin ekranýnda oluþturmasý için RPC gönderiyoruz
            agent.GetComponent<PhotonView>().RPC("RPC_ShootArrow", RpcTarget.All, targetDirection, targetViewID);
        }
    }
}