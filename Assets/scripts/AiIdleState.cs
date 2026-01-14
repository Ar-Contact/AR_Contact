using UnityEngine;

public class AiIdleState : AIState
{
    private float waitAfterKillDuration = 1.0f;
    private float timer;

    public AIStateId GetId()
    {
        return AIStateId.idle;
    }

    public void Enter(AiAgent agent)
    {
        // EKLENEN KONTROL: Agent aktifse ve NavMesh üzerindeyse yolu sýfýrla
        if (agent.navMeshAgent.isActiveAndEnabled && agent.navMeshAgent.isOnNavMesh)
        {
            agent.navMeshAgent.ResetPath();
        }

        agent.animator.SetFloat("Speed", 0f);
        timer = 0f;
    }

    public void Update(AiAgent agent)
    {
        // HEDEF VAR MI?
        if (agent.targetTransform != null)
        {
            Health targetHealth = agent.targetTransform.GetComponent<Health>();

            if (targetHealth != null && targetHealth.isDead)
            {
                timer += Time.deltaTime;

                if (timer < waitAfterKillDuration)
                    return;

                agent.SetTarget(null);
                timer = 0f;
                return;
            }

            float distance = Vector3.Distance(
                agent.transform.position,
                agent.targetTransform.position
            );

            if (distance < agent.chaseDistance)
            {
                agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            }
        }
    }

    public void Exit(AiAgent agent)
    {
        timer = 0f;
    }
}
