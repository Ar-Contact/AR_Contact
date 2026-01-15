using UnityEngine;
using UnityEngine.AI;

public class UnitAutoFreeze : MonoBehaviour
{
    void Start()
    {
        // Eðer hazýrlýk aþamasýndaysak (Savaþ baþlamadýysa)
        if (ArenaManager.Instance != null && !ArenaManager.Instance.isWarStarted)
        {
            // Hareket sistemini kapat
            NavMeshAgent nma = GetComponent<NavMeshAgent>();
            if (nma != null) nma.enabled = false;

            // Zeka/Saldýrý sistemini kapat
            MonoBehaviour ai = GetComponent("AiAgent") as MonoBehaviour;
            if (ai != null) ai.enabled = false;

            Debug.Log(gameObject.name + " hazýrlýk için donduruldu.");
        }
    }
}