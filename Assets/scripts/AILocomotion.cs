using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AILocomotion : MonoBehaviour
{
    NavMeshAgent agent;
    Animator animator;

    // DEGISTIRILDI: 'playerTransform' yerine hedef objenin tag'ini (etiketini)
    // Inspector'dan alacagiz.
    public string targetTag;

    // EKLENDI: Hiz otomatik guncellemeyi kontrol etmek icin
    public bool useAutoSpeed = true;

    // EKLENDI: Buldugumuz hedefin Transform'unu saklamak icin bir degisken.
    private Transform targetTransform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // EKLENDI: Oyunu baslatirken belirledigimiz tag'e sahip objeyi bul.
        GameObject targetObject = GameObject.FindWithTag(targetTag);

        // EKLENDI: Objeyi bulup bulamadigimizi kontrol et.
        if (targetObject != null)
        {
            // Eger bulduysak, transform'unu sakla.
            targetTransform = targetObject.transform;
        }
        else
        {
            // Eger bu tag'e sahip bir obje sahnede yoksa, hata mesaji ver.
            Debug.LogError("'" + targetTag + "' tag'ine sahip bir obje bulunamadi! " +
                             "Lutfen AI karakterinin takip edecegi objeyi kontrol edin.", this);
        }
    }

    void Update()
    {
        // DEGISTIRILDI: Sadece bir hedef bulduysak (targetTransform null degilse)
        // hedefi takip etmesini soyle.
        if (targetTransform != null)
        {
            agent.destination = targetTransform.position;
            
            if (useAutoSpeed)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude);
            }
        }
        else
        {
            // Eger bir hedefimiz yoksa (bulunamadiysa), hizi sifirla.
            if (useAutoSpeed)
            {
                animator.SetFloat("Speed", 0f);
            }
        }
    }
}
