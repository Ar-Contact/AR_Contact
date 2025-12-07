using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Büyü Ayarlarý")]
    public float speed = 20f; // Büyünün hýzý
    private float damage;
    private string targetTag;

    // Büyüyü oluþtururken (Instantiate) çaðýracaðýmýz fonksiyon
    public void Initialize(float damageAmount, string tagToHit, Vector3 direction)
    {
        damage = damageAmount;
        targetTag = tagToHit;

        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, 5f); // 5 saniye sonra bir yere çarpmazsa yok olsun
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // --- 1. HAVAYA/TRIGGERLARA ÇARPMA SORUNU ---
        if (other.isTrigger) return;

        HitBox hitBox = other.GetComponent<HitBox>();

        // Eðer bir canlýya çarptýysak
        if (hitBox != null && hitBox.health != null)
        {
            // VE bu canlý bizim hedefimizse (Tag kontrolü)
            if (hitBox.health.CompareTag(targetTag))
            {
                // NOT: HitBox scriptinde "OnMagicHit" olmadýðý için 
                // hasar vermek adýna mecburen "OnArrowHit" fonksiyonunu kullanýyoruz.
                // Ýsim farklý olsa da hasar verme iþini yapar.
                hitBox.OnArrowHit(damage, transform.forward);

                // Hedefi vurduk, büyü patladý/yok oldu.
                Destroy(gameObject);
                return;
            }
        }

        // --- 2. DUVAR/YER KONTROLÜ ---
        // Canlý deðilse (Hitbox yoksa) duvardýr, büyü duvarda patlasýn.
        if (hitBox == null)
        {
            Destroy(gameObject);
        }
    }
}