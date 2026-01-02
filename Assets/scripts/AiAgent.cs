using UnityEngine;
using UnityEngine.AI;

public class AiAgent : MonoBehaviour
{
    public AiStateMachine stateMachine;

    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Health health;

    [HideInInspector] public Transform targetTransform;

    [Header("AI Mesafeleri")]
    public float chaseDistance = 20f;
    public float attackDistance = 5f;
    public float attackDamage = 25f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        navMeshAgent.stoppingDistance = attackDistance;

        stateMachine = new AiStateMachine(this);

        stateMachine.RegisterState(new AiIdleState());
        stateMachine.RegisterState(new AiChaseState());
        stateMachine.RegisterState(new AiAttackState());

        stateMachine.ChangeState(AIStateId.idle);
    }

    void Update()
    {
        if (health != null && health.isDead)
        {
            navMeshAgent.enabled = false;
            return;
        }


        if (targetTransform == null)
            return;

        stateMachine.Update();
    }

    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }
}
