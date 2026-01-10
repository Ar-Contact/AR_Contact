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

    new void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        navMeshAgent.stoppingDistance = 0f;

        GameObject targetObject = GameObject.FindWithTag(targetTag);
        if (targetObject != null)
        {
            targetTransform = targetObject.transform;
        }
        else
        {
            Debug.LogError("Büyücü için hedef bulunamadý: " + targetTag);
        }

        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiMagicianAttackState()); // Saldýrý state'i

        stateMachine.ChangeState(AIStateId.idle);
    }
}