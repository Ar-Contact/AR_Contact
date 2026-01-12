using UnityEngine;
using UnityEngine.AI;

public class UnitAutoFreeze : MonoBehaviour
{
    void Start()
    {
        // ArenaManager'a bakýyoruz: Savaþ henüz baþlamadýysa (Hazýrlýk aþamasýndaysak)
        if (ArenaManager.Instance != null && !ArenaManager.Instance.isWarStarted)
        {
            // 1. TÝK: NavMeshAgent (Hareket bileþeni) kapatýlýr
            NavMeshAgent nma = GetComponent<NavMeshAgent>();
            if (nma != null)
            {
                nma.enabled = false; // IsTrigger zeminde olsa bile hareket etmez
            }

            // 2. TÝK: AiAgent (Saldýrý ve Zeka scripti) kapatýlýr
            // Inspector'daki tam ismiyle çaðýrýyoruz
            MonoBehaviour ai = GetComponent("AiAgent") as MonoBehaviour;
            if (ai != null)
            {
                ai.enabled = false; // Hedef aramaz ve ateþ etmez
            }

            Debug.Log(gameObject.name + " donduruldu. Savaþ baþladýðýnda ArenaManager tarafýndan uyandýrýlacak.");
        }
    }
}