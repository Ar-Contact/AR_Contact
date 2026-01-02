using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// BURADAKİ ": AiAgent" İFADESİ ZORUNLUDUR.
// Bunu silersek StateMachine, bu scripti kabul etmez ve hata verir.
public class AiBearAgent : AiAgent
{
    // Değişkenleri (navMeshAgent vb.) burada tekrar tanımlamıyoruz
    // çünkü ": AiAgent" diyerek onları zaten almış olduk.

    void Start()
    {
        // 1. BİLEŞENLERİ BAĞLA
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // NavMesh ayarı
        navMeshAgent.stoppingDistance = attackDistance;

        

        // 3. BEYNİ (STATE MACHINE) KUR
        // ": AiAgent" sayesinde 'this' komutu hata vermiyor.
        stateMachine = new AiStateMachine(this);

        // 4. DURUMLARI EKLE
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiChaseState());

        // ÖZEL: Normal Attack yerine Ayı Saldırısını yüklüyoruz
        stateMachine.RegisterState(new AiBearAttackState());

        // Başlangıç durumu
        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        // ÖLÜM KONTROLÜ
        if (health != null && health.isDead)
        {
            navMeshAgent.enabled = false;
            return; // Öldüyse aşağıya inme, çık.
        }

        // BEYNİ GÜNCELLE (Saldıracak mı, kovalayacak mı karar verir)
        stateMachine.Update();
    }
}