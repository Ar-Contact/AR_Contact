using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    // Sabit değer yerine public değişken kullanıyoruz.
    [Header("Damage Settings")]
    public float damage = 100f; // <-- Varsayılan değeri 100 olarak ayarladık.

    public float speed = 80f;
    public float lifespan = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("ArrowProjectile: Rigidbody bileşeni bulunamadı ve gereklidir!");
            Destroy(gameObject);
        }
    }

    // Eğer HumanArcherController'dan çağrılacaksa, bu metot kullanılabilir:
    public void Initialize(Vector3 direction, float arrowForce, float damageAmount)
    {
        // Koddan gelen hasar miktarını kullan (eğer varsa)
        this.damage = damageAmount;

        if (rb != null)
        {
            rb.velocity = direction * arrowForce;
            rb.angularVelocity = Vector3.zero;
        }

        Destroy(gameObject, lifespan);
    }


    void OnTriggerEnter(Collider other)
    {
        HitBox hitBox = other.GetComponent<HitBox>();

        if (hitBox != null)
        {
            // Hasarı değişkenin anlık değerinden al (Inspector veya Initialize metodu ile belirlenen)
            Vector3 hitDirection = rb.velocity.normalized;
            hitBox.OnArrowHit(this.damage, hitDirection); // <-- Güncel hasar değeri gönderilir

            Destroy(gameObject);
        }
        else if (!other.isTrigger && other.gameObject.isStatic)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
            }
            transform.SetParent(other.transform);
            Destroy(gameObject, 2f);
        }
        else if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}