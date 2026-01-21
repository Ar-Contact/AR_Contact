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

        // UYARI: attackDamage 0 ise hasar veremez!
        if (attackDamage <= 0)
        {
            Debug.LogError($"<color=red>[AGENT] {gameObject.name} - UYARI: attackDamage = {attackDamage}! Prefab'da düzelt!</color>");
        }

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

        // Hareket hızı ve baseOffset ayarları (AiAgent'tan miras)
        navMeshAgent.speed = moveSpeed;
        navMeshAgent.baseOffset = baseOffset;
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