using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 20f;
    private float damage;
    private string targetTag;

    public void Initialize(float damageAmount, string tagToHit, Vector3 direction)
    {
        damage = damageAmount;
        targetTag = tagToHit;

        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, 5f); // 5 saniye sonra kimseyi vuramazsa yok olsun
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // --- 1. HAVAYA ÇARPMA SORUNU ÇÖZÜMÜ ---
        // Eðer çarptýðýmýz þey "Trigger" ise (yani görünmez bir alansa),
        // bunu görmezden gel ve uçmaya devam et.
        if (other.isTrigger) return;
        // --------------------------------------

        HitBox hitBox = other.GetComponent<HitBox>();

        // Eðer bir canlýya çarptýysak
        if (hitBox != null && hitBox.health != null)
        {
            // VE bu canlý bizim hedefimizse (Tag kontrolü)
            if (hitBox.health.CompareTag(targetTag))
            {
                hitBox.OnArrowHit(damage, transform.forward);

                // Hedefi vurduk, görev tamamlandý, artýk yok olabilirsin.
                Destroy(gameObject);
                return;
            }
        }

        // --- 2. DUVAR/YER KONTROLÜ ---
        // Eðer çarptýðýmýz þey Hedef DEÐÝLSE ama bir DUAR veya YER ise yok ol.
        // (Bunu yapmazsak ok duvarlarýn içinden geçer)
        // Basit mantýk: Hitbox'ý yoksa cansýz mankendir (Duvar, zemin vs.)
        if (hitBox == null)
        {
            Destroy(gameObject);
        }
    }
}