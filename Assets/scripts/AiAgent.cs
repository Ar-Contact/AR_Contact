using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AiAgent : MonoBehaviour
{
    // State Machine
    public AiStateMachine stateMachine;

    // BÝLEÞENLER
    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Health health; // Saðlýk scriptine eriþim

    // HEDEF BÝLGÝSÝ
    public string targetTag;
    [HideInInspector] public Transform targetTransform;

    // AI Mesafeleri (Inspector'dan ayarlanacak)
    [Header("AI Mesafeleri")]
    public float chaseDistance = 20.0f;  // Hedefi bu mesafeden görmeye baþla
    public float attackDistance = 5.0f;  // Bu mesafeye gelince saldýr
    public float attackDamage = 25.0f;

    void Start()
    {
        // Bileþenleri al
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>(); // Health bileþenini bul ve taný

        // NavMeshAgent'ýn durma mesafesini 'attackDistance' ile eþitle
        navMeshAgent.stoppingDistance = attackDistance;

        // Hedefi bul
        GameObject targetObject = GameObject.FindWithTag(targetTag);
        if (targetObject != null)
        {
            targetTransform = targetObject.transform;
        }
        else
        {
            Debug.LogError("'" + targetTag + "' tag'ine sahip bir obje bulunamadý!", this);
        }

        // --- State Machine Kurulumu ---
        stateMachine = new AiStateMachine(this);

        // Bütün olasý state'leri (durumlarý) kaydet
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiChaseState());
        stateMachine.RegisterState(new AiAttackState());

        // Baþlangýç durumunu ayarla (Idle olarak baþlýyor)
        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        // --- ÖLÜM KONTROLÜ (ZOMBÝ FIX) ---
        // Eðer karakterin caný bittiyse (isDead true ise)
        if (health != null && health.isDead)
        {
            // Hareket etmeyi durdur (NavMesh'i kapat)
            navMeshAgent.enabled = false;

            // Animasyonlarý veya beyni güncellemeyi býrak ve fonksiyondan çýk
            return;
        }
        // ---------------------------------

        // State machine'i her frame güncelle (Hangi durumdaysa onu çalýþtýrýr)
        stateMachine.Update();
    }
}