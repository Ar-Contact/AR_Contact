using UnityEngine;
using UnityEngine.AI;

public class AiArcherChaseState : AIState
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
        if (agent.targetTransform == null || agent.targetTransform.GetComponent<Health>()?.isDead == true)
        {
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        float euclideanDistance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);

        if (euclideanDistance <= agent.attackDistance)
        {
            agent.navMeshAgent.ResetPath();
            agent.stateMachine.ChangeState(AIStateId.Attack);
        }
        else
        {
            agent.navMeshAgent.destination = agent.targetTransform.position;

            if (agent.autoUpdateAnimatorSpeed)
            {
                agent.animator.SetFloat("Speed", agent.navMeshAgent.velocity.magnitude);
            }
        }
    }

    public void Exit(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
    }
}
