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
        Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - IDLE STATE'E GIRILDI</color>");
        
        // EKLENEN KONTROL: Agent aktifse ve NavMesh uzerindeyse yolu sifirla
        if (agent.navMeshAgent.isActiveAndEnabled && agent.navMeshAgent.isOnNavMesh)
        {
            agent.navMeshAgent.ResetPath();
            Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - NavMesh path sifirlandi</color>");
        }
        else
        {
            Debug.LogWarning($"<color=red>[IDLE] {agent.gameObject.name} - NavMeshAgent AKTIF DEGIL veya NavMesh UZERINDE DEGIL!</color>");
        }

        if (agent.autoUpdateAnimatorSpeed)
        {
            agent.animator.SetFloat("Speed", 0f);
        }
        timer = 0f;
    }

    public void Update(AiAgent agent)
    {
        // HEDEF VAR MI?
        if (agent.targetTransform != null)
        {
            Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - Hedef VAR: {agent.targetTransform.name}</color>");
            
            Health targetHealth = agent.targetTransform.GetComponent<Health>();

            if (targetHealth != null && targetHealth.isDead)
            {
                Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - Hedef OLDU, bekleniyor...</color>");
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

            Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - Hedefe uzaklik: {distance:F2}, ChaseDistance: {agent.chaseDistance}</color>");

            if (distance < agent.chaseDistance)
            {
                Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - ChaseDistance icinde, CHASE STATE'E GECILIYOR!</color>");
                agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            }
        }
        else
        {
            // Her 60 frame'de bir log at (performans icin)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"<color=gray>[IDLE] {agent.gameObject.name} - Hedef YOK, bekleniyor...</color>");
            }
        }
    }

    public void Exit(AiAgent agent)
    {
        Debug.Log($"<color=green>[IDLE] {agent.gameObject.name} - IDLE STATE'DEN CIKILIYOR</color>");
        timer = 0f;
    }
}
