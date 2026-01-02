using UnityEngine;

public class AiBearAttackState : AIState
{
    private bool hasDealtDamage;
    private float damageTiming = 0.5f;

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
        agent.animator.SetFloat("Speed", 0f);
        agent.animator.SetBool("IsAttacking", true);

        hasDealtDamage = false;
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

        FaceTarget(agent);
        agent.animator.SetFloat("Speed", 0f);

        float distance = Vector3.Distance(
            agent.transform.position,
            agent.targetTransform.position
        );

        if (distance > agent.attackDistance)
        {
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            return;
        }

        AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1.0f;

        if (normalizedTime >= damageTiming && !hasDealtDamage)
        {
            Vector3 direction =
                (agent.targetTransform.position - agent.transform.position).normalized;

            targetHealth.TakeDamage(agent.attackDamage, direction);
            hasDealtDamage = true;
        }

        // Yeni animasyon döngüsünde tekrar hasar vurabilsin
        if (normalizedTime < damageTiming)
        {
            hasDealtDamage = false;
        }
    }

    public void Exit(AiAgent agent)
    {
        agent.animator.SetBool("IsAttacking", false);
    }

    private void FaceTarget(AiAgent agent)
    {
        Vector3 direction =
            (agent.targetTransform.position - agent.transform.position).normalized;

        Quaternion lookRotation =
            Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        agent.transform.rotation =
            Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
}
