using UnityEngine;

public class AiMagicianAttackState : AIState
{
    private bool hasCastSpell;
    private float previousFrameTime;

    // --- YENİ EKLENDİ: Saldırı Hızı Ayarları ---
    private float nextAttackTime = 0f; // Bir sonraki saldırının yapılabileceği zaman
    private float attacksPerMinute = 6f; // Dakikada kaç saldırı yapsın? (Sen 6 istedin)
    // ------------------------------------------

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();

        if (agent.autoUpdateAnimatorSpeed)
        {
            agent.animator.SetFloat("Speed", 0f);
        }
        agent.animator.SetBool("spell", true);

        hasCastSpell = false;
        previousFrameTime = 0f;
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

        Vector3 direction =
            (agent.targetTransform.position - agent.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation =
                Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        float distance = Vector3.Distance(
            agent.transform.position,
            agent.targetTransform.position
        );

        if (distance > agent.attackDistance)
        {
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            return;
        }

        AiMagicianAgent magicianAgent = agent as AiMagicianAgent;
        if (magicianAgent == null) return;

        AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
        bool isSpellAnimation =
            stateInfo.IsName("spell") || stateInfo.IsTag("Attack");

        if (!isSpellAnimation) return;

        float currentNormalizedTime = stateInfo.normalizedTime % 1.0f;

        if (currentNormalizedTime < previousFrameTime)
        {
            hasCastSpell = false;
        }

        if (currentNormalizedTime >= magicianAgent.shootTiming && !hasCastSpell)
        {
            // --- GÜNCELLENDİ: Zaman kontrolü eklendi ---
            // Şu anki zaman, belirlediğimiz bir sonraki saldırı zamanından büyük mü?
            if (Time.time >= nextAttackTime)
            {
                CastSpell(magicianAgent, direction, agent.targetTransform);
                hasCastSpell = true;

                // Bir sonraki saldırı zamanını hesapla (60 saniye / 6 = 10 saniye sonra)
                float delayBetweenAttacks = 60f / attacksPerMinute;
                nextAttackTime = Time.time + delayBetweenAttacks;
            }
            // -------------------------------------------
        }

        previousFrameTime = currentNormalizedTime;
    }

    public void Exit(AiAgent agent)
    {
        agent.animator.SetBool("spell", false);
    }

    private void CastSpell(AiMagicianAgent agent, Vector3 direction, Transform target)
    {
        if (agent.magicPrefab == null || agent.magicSpawnPoint == null) return;

        int targetViewID = target.GetComponent<Photon.Pun.PhotonView>().ViewID;
        agent.GetComponent<Photon.Pun.PhotonView>().RPC("RPC_CastSpell", Photon.Pun.RpcTarget.All, direction, targetViewID);
    }
}