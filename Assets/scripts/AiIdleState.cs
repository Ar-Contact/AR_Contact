using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiIdleState : AIState
{
    // --- AYAR: Kaç saniye beklesin? ---
    // Burayý 1.0f ile 2.0f arasýnda deðiþtirebilirsin.
    private float waitAfterKillDuration = 1.0f;

    private float timer; // Sayacý tutan deðiþken

    public AIStateId GetId()
    {
        return AIStateId.idle;
    }

    public void Enter(AiAgent agent)
    {
        agent.navMeshAgent.ResetPath();
        agent.animator.SetFloat("Speed", 0f);

        // Idle moduna her girdiðimizde sayacý sýfýrla
        timer = 0f;
    }

    public void Update(AiAgent agent)
    {
        // 1. MEVCUT HEDEF KONTROLÜ (BEKLEME MANTIÐI BURADA)
        if (agent.targetTransform != null)
        {
            Health targetHealth = agent.targetTransform.GetComponent<Health>();

            // Eðer hedef öldüyse...
            if (targetHealth != null && targetHealth.isDead)
            {
                // Sayacý çalýþtýr
                timer += Time.deltaTime;

                // Henüz süre dolmadýysa, hiçbir þey yapma (Cesede bakmaya devam et)
                if (timer < waitAfterKillDuration)
                {
                    return;
                }

                // SÜRE DOLDU! Artýk hedefi unutabiliriz.
                agent.targetTransform = null;
                timer = 0f;
            }
            // Hedef ölmedi ama Tag'i deðiþtiyse (örn: oyundan çýktýysa)
            else if (!agent.targetTransform.CompareTag(agent.targetTag))
            {
                agent.targetTransform = null;
            }
        }

        // --- 2. YENÝ HEDEF ARAMA ---
        // (Sadece hedefimiz yoksa burasý çalýþýr)
        if (agent.targetTransform == null)
        {
            FindClosestTarget(agent);
        }

        // Hala hedef yoksa bekle
        if (agent.targetTransform == null) return;

        // --- 3. HAREKETE GEÇÝÞ ---
        float distance = Vector3.Distance(agent.transform.position, agent.targetTransform.position);

        // Hedef yaþýyorsa ve menzildeyse saldýrýya/kovalamaya geç
        // (Ekstra kontrol: Ölü hedefe tekrar saldýrmasýn)
        Health checkHealth = agent.targetTransform.GetComponent<Health>();
        if (checkHealth != null && !checkHealth.isDead)
        {
            if (distance < agent.chaseDistance)
            {
                agent.stateMachine.ChangeState(AIStateId.ChasePlayer);
            }
        }
    }

    public void Exit(AiAgent agent)
    {
    }

    // --- EN YAKIN HEDEFÝ BULMA (Ayný kalýyor) ---
    private void FindClosestTarget(AiAgent agent)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(agent.targetTag);

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == agent.gameObject) continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            // Ölüleri asla yeni hedef olarak seçme
            if (enemyHealth != null && enemyHealth.isDead) continue;

            float distance = Vector3.Distance(agent.transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            agent.targetTransform = closestEnemy.transform;
        }
    }
}