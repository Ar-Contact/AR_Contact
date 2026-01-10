using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AiArcherAgent : AiAgent
{
    [Header("Okçu Özel Ayarlarý")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;

    // --- YENÝ AYAR ---
    [Header("Animasyon Zamanlamasý")]
    [Tooltip("Ok animasyonun yüzde kaçýnda çýksýn? (0.0 = Baþlangýç, 0.5 = Orta, 1.0 = Bitiþ)")]
    [Range(0.0f, 1.0f)]
    public float shootTiming = 1.0f; // DEÐÝÞTÝ: 1.0f yaparak animasyonun tam bitiþine aldým
    // -----------------

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
            Debug.LogError("Okçu için hedef bulunamadý: " + targetTag);
        }

        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiArcherAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }
}