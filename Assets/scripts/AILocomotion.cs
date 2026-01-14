using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AILocomotion : MonoBehaviour
{
    NavMeshAgent agent;
    Animator animator;

    // DEÐÝÞTÝRÝLDÝ: 'playerTransform' yerine hedef objenin tag'ini (etiketini)
    // Inspector'dan alacaðýz.
    public string targetTag;

    // EKLENDÝ: Bulduðumuz hedefin Transform'unu saklamak için bir deðiþken.
    private Transform targetTransform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // EKLENDÝ: Oyunu baþlatýrken belirlediðimiz tag'e sahip objeyi bul.
        GameObject targetObject = GameObject.FindWithTag(targetTag);

        // EKLENDÝ: Objeyi bulup bulamadýðýmýzý kontrol et.
        if (targetObject != null)
        {
            // Eðer bulduysak, transform'unu sakla.
            targetTransform = targetObject.transform;
        }
        else
        {
            // Eðer bu tag'e sahip bir obje sahnede yoksa, hata mesajý ver.
            Debug.LogError("'" + targetTag + "' tag'ine sahip bir obje bulunamadý! " +
                             "Lütfen AI karakterinin takip edeceði objeyi kontrol edin.", this);
        }
    }

    void Update()
    {
        // DEÐÝÞTÝRÝLDÝ: Sadece bir hedef bulduysak (targetTransform null deðilse)
        // hedefi takip etmesini söyle.
        if (targetTransform != null)
        {
            agent.destination = targetTransform.position;
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
        else
        {
            // Eðer bir hedefimiz yoksa (bulunamadýysa), hýzý sýfýrla.
            animator.SetFloat("Speed", 0f);
        }
    }
}