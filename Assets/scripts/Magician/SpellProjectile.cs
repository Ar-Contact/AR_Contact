using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Büyü Ayarlarý")]
    public float speed = 20f;

    private float damage;
    private Transform target;

    // AI saldýrýrken çaðýrýr
    public void Initialize(float damageAmount, Transform targetTransform)
    {
        damage = damageAmount;
        target = targetTransform;

        Destroy(gameObject, 5f); // Güvenlik
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        // Hedefin kendisi mi yoksa vücudunun bir parçasý mý kontrol et
        HitBox hitBox = other.GetComponent<HitBox>();

        // Eðer çarptýðýmýz þeyin bir HitBox'ý varsa ve o HitBox bizim hedefimize aitse
        if (hitBox != null && hitBox.health != null)
        {
            if (hitBox.health.transform == target)
            {
                Debug.Log("Mermi Çarptý: " + other.name);
                hitBox.OnArrowHit(damage, transform.forward);
                Destroy(gameObject);
            }
        }
    }
}
