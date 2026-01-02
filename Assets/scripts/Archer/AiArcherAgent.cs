using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AiArcherAgent : AiAgent
{
    [Header("Okçu Özel Ayarlarý")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;

    [Header("Animasyon Zamanlamasý")]
    [Tooltip("Ok animasyonun yüzde kaçýnda çýksýn? (0.0 = Baþlangýç, 1.0 = Bitiþ)")]
    [Range(0.0f, 1.0f)]
    public float shootTiming = 1.0f;

    // public AiAgent agent; // BU SATIRI SÝLDÝK. Hatanýn sebebi buydu.

    void Start()
    {
        // Miras alýnan (AiAgent) deðiþkenleri dolduruyoruz
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        navMeshAgent.stoppingDistance = 0f;

        // --- HATA DÜZELTME KISMI ---
        // Oyun baþladýðýnda henüz bir hedef (targetTransform) atanmamýþ olabilir.
        // Bu yüzden önce null kontrolü yapýyoruz.
        if (targetTransform != null)
        {
            Health targetHealth = targetTransform.GetComponent<Health>();

            // Hedef varsa ve ölü ise durumu sýfýrla
            if (targetHealth != null && targetHealth.isDead)
            {
                SetTarget(null); // 'agent.SetTarget' yerine direkt fonksiyonu kullanýyoruz
                stateMachine.ChangeState(AIStateId.idle);
                return;
            }
        }
        else
        {
            // Hedef yoksa güvenli bir þekilde IDLE (Bekleme) moduna geç
            // Debug.Log("Baþlangýçta hedef yok, IDLE moduna geçiliyor.");
        }
        // ---------------------------

        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiArcherAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }
}