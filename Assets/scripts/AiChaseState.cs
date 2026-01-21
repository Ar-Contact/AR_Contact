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
        Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - CHASE STATE'E GIRILDI</color>");
        agent.animator.SetBool("IsAttacking", false);
        
        // NavMesh durumunu kontrol et
        if (!agent.navMeshAgent.isActiveAndEnabled)
        {
            Debug.LogError($"<color=red>[CHASE] {agent.gameObject.name} - NavMeshAgent AKTIF DEGIL!</color>");
        }
        if (!agent.navMeshAgent.isOnNavMesh)
        {
            Debug.LogError($"<color=red>[CHASE] {agent.gameObject.name} - Karakter NavMesh UZERINDE DEGIL!</color>");
        }
    }

    public void Update(AiAgent agent)
    {
        if (agent.targetTransform == null)
        {
            Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - Hedef YOK, IDLE'a donuluyor</color>");
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        Health targetHealth = agent.targetTransform.GetComponent<Health>();
        if (targetHealth == null || targetHealth.isDead)
        {
            Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - Hedef OLDU, IDLE'a donuluyor</color>");
            agent.SetTarget(null);
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        float distance = Vector3.Distance(
            agent.transform.position,
            agent.targetTransform.position
        );

        Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - Hedefe uzaklik: {distance:F2}, AttackDist: {agent.attackDistance}, Velocity: {agent.navMeshAgent.velocity.magnitude:F2}</color>");

        if (distance <= agent.attackDistance)
        {
            Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - Saldiri mesafesinde! Velocity: {agent.navMeshAgent.velocity.magnitude:F2}</color>");
            
            if (agent.navMeshAgent.velocity.magnitude < 0.1f)
            {
                Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - Durdu, ATTACK STATE'E GECILIYOR!</color>");
                agent.stateMachine.ChangeState(AIStateId.Attack);
            }
        }
        else
        {
            // Hedefe dogru git
            agent.navMeshAgent.SetDestination(agent.targetTransform.position);
            
            // Path durumunu kontrol et
            if (agent.navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogError($"<color=red>[CHASE] {agent.gameObject.name} - PATH GECERSIZ! Hedefe yol bulunamiyor!</color>");
            }
            else if (agent.navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                Debug.LogWarning($"<color=orange>[CHASE] {agent.gameObject.name} - PATH KISMI! Tam yol yok.</color>");
            }
            
            if (agent.autoUpdateAnimatorSpeed)
            {
                agent.animator.SetFloat(
                    "Speed",
                    agent.navMeshAgent.velocity.magnitude
                );
            }
            
            Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - Hareket ediliyor. Speed: {agent.navMeshAgent.velocity.magnitude:F2}, HasPath: {agent.navMeshAgent.hasPath}</color>");
        }
    }

    public void Exit(AiAgent agent)
    {
        Debug.Log($"<color=blue>[CHASE] {agent.gameObject.name} - CHASE STATE'DEN CIKILIYOR</color>");
        if (agent.navMeshAgent.isActiveAndEnabled && agent.navMeshAgent.isOnNavMesh)
        {
            agent.navMeshAgent.ResetPath();
        }
    }
}
