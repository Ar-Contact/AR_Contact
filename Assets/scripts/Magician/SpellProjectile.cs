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

        // SADECE kendi hedefimizi vururuz
        if (other.transform != target) return;

        HitBox hitBox = other.GetComponent<HitBox>();
        if (hitBox != null && hitBox.health != null)
        {
            hitBox.OnArrowHit(damage, transform.forward);
        }

        Destroy(gameObject);
    }
}
