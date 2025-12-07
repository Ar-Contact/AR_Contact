using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiBearAttackState : AIState
{
    // --- YENÝ DEÐÝÞKENLER ---
    private bool hasDealtDamage; // Bu saldýrý döngüsünde hasar vurduk mu?

    // Animasyonun yüzde kaçýnda hasar vereceðini belirler (0.5 = %50'si, yani tam ortasý)
    private float damageTiming = 0.5f;
    // ------------------------

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        // Saldýrý durumuna girince hareketi durdur.
        agent.navMeshAgent.ResetPath();
        agent.animator.SetFloat("Speed", 0f);
        // Animator'daki IsAttacking parametresini true yapar.
        agent.animator.SetBool("IsAttacking", true);

        // --- YENÝ ---
        // Saldýrýya her girdiðimizde, "daha hasar vurmadýk" olarak ayarla
        hasDealtDamage = false;
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

        FaceTarget(agent);
        agent.animator.SetFloat("Speed", 0f);

        float distance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);
        if (distance > agent.attackDistance)
        {
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
        }

        // Animasyon Hasar Logiði (Buraya dokunmuyoruz)
        AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1.0f;
        if (normalizedTime >= damageTiming)
        {
            if (!hasDealtDamage)
            {
                Health targetHealth = agent.targetTransform.GetComponent<Health>();
                if (targetHealth != null)
                {
                    Vector3 direction = (agent.targetTransform.position - agent.transform.position).normalized;
                    targetHealth.TakeDamage(agent.attackDamage, direction);
                    hasDealtDamage = true;
                }
            }
        }
        else
        {
            hasDealtDamage = false;
        }
    }

    public void Exit(AiAgent agent)
    {
        // Saldýrýdan çýkarken animatör bool'unu düzelt
        agent.animator.SetBool("IsAttacking", false);
    }

    private void FaceTarget(AiAgent agent)
    {
        Vector3 direction = (agent.targetTransform.position - agent.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
}