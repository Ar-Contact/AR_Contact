using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent için bu satýr önemli

public class AiChaseState : AIState
{
    public AIStateId GetId()
    {
        return AIStateId.ChasePlayer;
    }

    public void Enter(AiAgent agent)
    {
        // Güvence için eklendi:
        agent.animator.SetBool("IsAttacking", false);
    }

    public void Update(AiAgent agent)
    {
        // --- GÜNCELLEME: Hedef geçerli mi? ---
        // Hedef yoksa VEYA hedefin tag'i artýk aradýðýmýz tag deðilse (örn: "Untagged" oldu)
        if (agent.targetTransform == null || !agent.targetTransform.CompareTag(agent.targetTag))
        {
            // Hedefi kaybettiðimizi bildir ve Idle'a dön
            agent.targetTransform = null; // Hedefi unut
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }
        // --- GÜNCELLEME SONU ---

        // (Kodun geri kalaný ayný...)

        float distance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);

        if (distance <= agent.attackDistance)
        {
            if (agent.navMeshAgent.velocity.magnitude < 0.1f)
            {
                agent.stateMachine.ChangeState(AIStateId.Attack);
            }
            else
            {
                agent.animator.SetFloat("Speed", agent.navMeshAgent.velocity.magnitude);
            }
        }
        else
        {
            agent.navMeshAgent.destination = agent.targetTransform.position;
            agent.animator.SetFloat("Speed", agent.navMeshAgent.velocity.magnitude);
        }
    }

    public void Exit(AiAgent agent)
    {
        // Takip durumundan çýkarken (saldýrýya veya idle'a geçerken)
        // AI'ýn o anki hedefini (path) sýfýrla ki durabilsin.
        agent.navMeshAgent.ResetPath();
    }
}