using UnityEngine;
using UnityEngine.AI;

public class AiArcherChaseState : AIState
{
    public AIStateId GetId()
    {
        return AIStateId.ChasePlayer; // ID ayný kalýr
    }

    public void Enter(AiAgent agent)
    {
        // Kovalamaya baþlarken animasyonu ata
        agent.animator.SetBool("IsAttacking", false);
    }

    public void Update(AiAgent agent)
    {
        // 1. Hedef Kontrolü (Klasik güvenlik önlemi)
        if (agent.targetTransform == null || agent.targetTransform.GetComponent<Health>()?.isDead == true)
        {
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }


        // 2. ÖKLÝD MESAFESÝ HESABI (Vector3.Distance kuþ uçuþu ölçer)
        float euclideanDistance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);

        // 3. Mesafe Kontrolü
        // Eðer kuþ uçuþu mesafemiz, saldýrý menziline girdiyse DUR ve SALDIR.
        if (euclideanDistance <= agent.attackDistance)
        {
            // NavMesh'i durdur ki titreme yapmasýn
            agent.navMeshAgent.ResetPath();
            agent.stateMachine.ChangeState(AIStateId.Attack);
        }
        else
        {
            // Menzilde deðilsek hedefe doðru yürümeye devam et
            agent.navMeshAgent.destination = agent.targetTransform.position;

            // Hýz animasyonu
            agent.animator.SetFloat("Speed", agent.navMeshAgent.velocity.magnitude);
        }
    }

    public void Exit(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
    }
}