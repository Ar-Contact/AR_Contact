using UnityEngine;
using UnityEngine.AI;

public class AiChaseState : AIState
{
    public AIStateId GetId()
    {
        return AIStateId.ChasePlayer;
    }

    public void Enter(AiAgent agent)
    {
        agent.animator.SetBool("IsAttacking", false);
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

        float distance = Vector3.Distance(
            agent.transform.position,
            agent.targetTransform.position
        );

        if (distance <= agent.attackDistance)
        {
            if (agent.navMeshAgent.velocity.magnitude < 0.1f)
            {
                agent.stateMachine.ChangeState(AIStateId.Attack);
            }
        }
        else
        {
            agent.navMeshAgent.SetDestination(agent.targetTransform.position);
            agent.animator.SetFloat(
                "Speed",
                agent.navMeshAgent.velocity.magnitude
            );
        }
    }

    public void Exit(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
    }
}
