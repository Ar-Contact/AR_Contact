using UnityEngine;

public class AiAttackState : AIState
{
    private bool hasDealtDamage;
    private float damageTiming = 0.5f;

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - ATTACK STATE'E GIRILDI</color>");
        
        // Guvenli ResetPath
        if (agent.navMeshAgent.isActiveAndEnabled && agent.navMeshAgent.isOnNavMesh)
        {
            agent.navMeshAgent.ResetPath();
        }

        if (agent.autoUpdateAnimatorSpeed)
        {
             agent.animator.SetFloat("Speed", 0f);
        }
        
        agent.animator.SetBool("IsAttacking", true);
        hasDealtDamage = false;
        
        Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - IsAttacking=true, Speed=0, AttackDamage={agent.attackDamage}</color>");
    }

    public void Update(AiAgent agent)
    {
        if (agent.targetTransform == null)
        {
            Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - Hedef YOK, IDLE'a donuluyor</color>");
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        Health targetHealth = agent.targetTransform.GetComponent<Health>();
        if (targetHealth == null || targetHealth.isDead)
        {
            Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - Hedef OLDU, IDLE'a donuluyor</color>");
            agent.SetTarget(null);
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        FaceTarget(agent);
        
        if (agent.autoUpdateAnimatorSpeed)
        {
             agent.animator.SetFloat("Speed", 0f);
        }

        float distance = Vector3.Distance(
            agent.transform.position,
            agent.targetTransform.position
        );

        Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - Hedefe uzaklik: {distance:F2}, AttackDist: {agent.attackDistance}</color>");

        if (distance > agent.attackDistance)
        {
            Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - Hedef uzaklasti, CHASE STATE'E GECILIYOR!</color>");
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            return;
        }

        AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1.0f;

        Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - AnimState: {stateInfo.shortNameHash}, NormTime: {normalizedTime:F2}, DamageTiming: {damageTiming}, HasDealt: {hasDealtDamage}</color>");

        if (normalizedTime >= damageTiming && !hasDealtDamage)
        {
            Vector3 direction =
                (agent.targetTransform.position - agent.transform.position).normalized;

            Debug.Log($"<color=magenta>[ATTACK] {agent.gameObject.name} - HASAR VERILIYOR! Miktar: {agent.attackDamage}</color>");
            targetHealth.TakeDamage(agent.attackDamage, direction);
            hasDealtDamage = true;
        }

        // Animasyon dongusu reset
        if (normalizedTime < damageTiming)
        {
            hasDealtDamage = false;
        }
    }

    public void Exit(AiAgent agent)
    {
        Debug.Log($"<color=red>[ATTACK] {agent.gameObject.name} - ATTACK STATE'DEN CIKILIYOR</color>");
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
