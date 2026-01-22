using UnityEngine;
using UnityEngine.AI;

public class UnitAutoFreeze : MonoBehaviour
{
    private NavMeshAgent nma;
    private MonoBehaviour ai;

    void Awake()
    {
        nma = GetComponent<NavMeshAgent>();
        ai = GetComponent("AiAgent") as MonoBehaviour;
    }

    void OnEnable()
    {
        CheckAndFreeze();
    }

    public void CheckAndFreeze()
    {
        // Savaþ baþlamadýysa dondur
        if (ArenaManager.Instance != null && !ArenaManager.Instance.isWarStarted)
        {
            if (nma != null) nma.enabled = false;
            if (ai != null) ai.enabled = false;
        }
    }
}