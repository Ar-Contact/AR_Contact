using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun; // Photon kütüphanesini eklemeyi unutma

public class AiBearAgent : AiAgent
{
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        if (!GetComponent<PhotonView>().IsMine)
        {
            // Sahibi değilsek NavMesh'i tamamen kapatıyoruz ki 
            // PhotonTransformView pozisyonu güncelleyebilsin.
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }
        }

        navMeshAgent.stoppingDistance = attackDistance;

        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiChaseState());
        stateMachine.RegisterState(new AiBearAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        // KRİTİK: Sadece sahibi AI mantığını ve hedef aramayı yürütsün
        if (!GetComponent<PhotonView>().IsMine) return;

        if (health != null && health.isDead)
        {
            navMeshAgent.enabled = false;
            return;
        }

        // Hedef yoksa otomatik tara (AiAgent'dan gelen mantık)
        if (targetTransform == null)
        {
            FindClosestEnemy(); // Bu fonksiyon AiAgent içinde IsMine kontrolü içeriyor
            return;
        }

        stateMachine.Update();
    }
}