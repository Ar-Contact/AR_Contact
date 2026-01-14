using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun; // MUTLAKA EKLE

public class AiMagicianAgent : AiAgent
{
    [Header("Büyücü Özel Ayarlarý")]
    public GameObject magicPrefab;
    public Transform magicSpawnPoint;

    [Header("Animasyon Zamanlamasý")]
    [Range(0.0f, 1.0f)]
    public float shootTiming = 1.0f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // KRÝTÝK: Ayýlardaki mantýðýn aynýsý
        if (!GetComponent<PhotonView>().IsMine)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }
        }

        navMeshAgent.stoppingDistance = attackDistance; // agent. yerine direkt deðiþken

        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiArcherChaseState());
        stateMachine.RegisterState(new AiMagicianAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        // KRÝTÝK: Sadece sahibi AI mantýðýný yönetsin
        if (!GetComponent<PhotonView>().IsMine) return;

        if (health != null && health.isDead)
        {
            navMeshAgent.enabled = false;
            return;
        }

        if (targetTransform == null)
        {
            FindClosestEnemy();
            return;
        }

        stateMachine.Update();
    }

    [PunRPC]
    public void RPC_CastSpell(Vector3 direction, int targetViewID)
    {
        // Sahibi olmayan ekranlarda büyü burada oluþur
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView == null) return;

        GameObject spellObj = Instantiate(magicPrefab, magicSpawnPoint.position, Quaternion.LookRotation(direction));
        SpellProjectile spell = spellObj.GetComponent<SpellProjectile>();
        if (spell != null)
        {
            spell.Initialize(attackDamage, targetView.transform);
        }
    }
}