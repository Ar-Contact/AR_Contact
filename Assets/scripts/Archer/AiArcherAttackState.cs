using UnityEngine;

public class AiArcherAttackState : AIState
{
    private bool hasShotArrow;

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        if (agent.navMeshAgent.isActiveAndEnabled && agent.navMeshAgent.isOnNavMesh)
        {
            agent.navMeshAgent.ResetPath();
        }

        if (agent.autoUpdateAnimatorSpeed)
        {
            agent.animator.SetFloat("Speed", 0f);
        }
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

        AiArcherAgent archerAgent = agent as AiArcherAgent;

        if (archerAgent != null)
        {
            AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1.0f;

            if (normalizedTime >= archerAgent.shootTiming && !hasShotArrow)
            {
                ShootArrow(archerAgent, direction);
                hasShotArrow = true;
            }

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

    private void ShootArrow(AiArcherAgent agent, Vector3 targetDirection)
    {
        if (agent.arrowPrefab != null && agent.arrowSpawnPoint != null && agent.targetTransform != null)
        {
            int targetViewID = agent.targetTransform.GetComponent<Photon.Pun.PhotonView>().ViewID;
            agent.GetComponent<Photon.Pun.PhotonView>().RPC("RPC_ShootArrow", Photon.Pun.RpcTarget.All, targetDirection, targetViewID);
        }
    }
}
