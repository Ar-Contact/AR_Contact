using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AiMagicianAgent : AiAgent
{
    [Header("Büyücü Özel Ayarlarý")]
    public GameObject magicPrefab; // Büyü prefabý
    public Transform magicSpawnPoint; // Asa ucu

    [Header("Animasyon Zamanlamasý")]
    [Tooltip("Büyü animasyonun yüzde kaçýnda çýksýn? (0.0 = Baþlangýç, 1.0 = Bitiþ)")]
    [Range(0.0f, 1.0f)]
    public float shootTiming = 1.0f;

    AiAgent agent;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        navMeshAgent.stoppingDistance = 0f;

        Health targetHealth = agent.targetTransform.GetComponent<Health>();
        if (targetHealth != null && targetHealth.isDead)
        {
            agent.SetTarget(null);
            agent.stateMachine.ChangeState(AIStateId.idle);
            return;
        }


        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiMagicianAttackState()); // Saldýrý state'i

        stateMachine.ChangeState(AIStateId.idle);
    }
}