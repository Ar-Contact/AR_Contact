using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiIdleState : AIState
{
    public AIStateId GetId()
    {
        return AIStateId.idle;
    }

    public void Enter(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
        agent.animator.SetFloat("Speed", 0f);
    }

    public void Update(AiAgent agent)
    {
        // --- GÜNCELLEME: Yeni hedef ara ---
        // 1. Bir hedefimiz yoksa, yeni bir tane bulmaya çalýþ
        if (agent.targetTransform == null)
        {
            FindNewTarget(agent);
        }

        // 2. Hala bir hedefimiz yoksa (çünkü etrafta kimse yok), beklemeye devam et
        if (agent.targetTransform == null)
        {
            return;
        }
        // --- GÜNCELLEME SONU ---

        // 3. Artýk bir hedefimiz var (ya önceden vardý ya da yeni bulduk). Mesafeyi ölç.
        float distance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);

        // 4. Eðer hedef "takip mesafesi" (chaseDistance) içine girerse,
        //    ChasePlayer durumuna geç.
        if (distance < agent.chaseDistance)
        {
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
        }
    }

    public void Exit(AiAgent agent)
    {
        // Çýkarken bir þey yapmaya gerek yok
    }

    // --- YENÝ FONKSÝYON ---
    private void FindNewTarget(AiAgent agent)
    {
        // agent'ýn aradýðý 'targetTag' ile bir obje bul
        GameObject targetObject = GameObject.FindWithTag(agent.targetTag);
        if (targetObject != null)
        {
            // Bulursak, onu hedefimiz olarak ata
            agent.targetTransform = targetObject.transform;
        }
    }
}