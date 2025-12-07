using UnityEngine;

public class AiMagicianAttackState : AIState
{
    private bool hasCastSpell;
    private float previousFrameTime; // Bir önceki karenin zamanýný tutacaðýz

    public AIStateId GetId()
    {
        return AIStateId.Attack;
    }

    public void Enter(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
        agent.animator.SetFloat("Speed", 0f);
        agent.animator.SetBool("spell", true);

        hasCastSpell = false;
        previousFrameTime = 0f; // Giriþte sýfýrla
    }

    public void Update(AiAgent agent)
    {
        if (agent.targetTransform == null || !agent.targetTransform.CompareTag(agent.targetTag))
        {
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }

        // Hedefe Dön
        Vector3 direction = (agent.targetTransform.position - agent.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        float distance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);
        if (distance > agent.attackDistance)
        {
            agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            return;
        }

        // --- FIRLATMA MANTIÐI (GARANTÝ YÖNTEM) ---
        AiMagicianAgent magicianAgent = agent as AiMagicianAgent;

        if (magicianAgent != null)
        {
            AnimatorStateInfo stateInfo = agent.animator.GetCurrentAnimatorStateInfo(0);
            bool isSpellAnimation = stateInfo.IsName("spell") || stateInfo.IsTag("Attack");

            if (isSpellAnimation)
            {
                // Mevcut zamanýn 0-1 arasýndaki karþýlýðýný al
                float currentNormalizedTime = stateInfo.normalizedTime % 1.0f;

                // 1. KONTROL: Döngü baþa sardý mý?
                // Eðer þimdiki zaman, önceki kareden KÜÇÜKSE (örn: 0.99 -> 0.05), animasyon baþa sarmýþtýr.
                // Bu durumda atýþ hakkýný (flag) sýfýrla.
                if (currentNormalizedTime < previousFrameTime)
                {
                    hasCastSpell = false;
                }

                // 2. KONTROL: Atýþ zamaný geldi mi?
                // Zamanlama geçildiyse VE henüz atýlmadýysa at.
                if (currentNormalizedTime >= magicianAgent.shootTiming && !hasCastSpell)
                {
                    CastSpell(magicianAgent, direction);
                    hasCastSpell = true;
                }

                // Bu karenin zamanýný kaydet, bir sonraki karede "önceki" olarak kullanacaðýz.
                previousFrameTime = currentNormalizedTime;
            }
        }
    }

    public void Exit(AiAgent agent)
    {
        agent.animator.SetBool("spell", false);
    }

    private void CastSpell(AiMagicianAgent agent, Vector3 targetDirection)
    {
        if (agent.magicPrefab != null && agent.magicSpawnPoint != null)
        {
            // Atýþýn tam spawn noktasýndan ve hedefe doðru bakarak çýkmasýný saðlar
            GameObject spellObj = Object.Instantiate(agent.magicPrefab, agent.magicSpawnPoint.position, Quaternion.LookRotation(targetDirection));

            SpellProjectile spellScript = spellObj.GetComponent<SpellProjectile>();
            if (spellScript != null)
            {
                spellScript.Initialize(agent.attackDamage, agent.targetTag, targetDirection);
            }
        }
    }
}