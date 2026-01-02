using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 20f;

    private float damage;
    private Transform target;

    public void Initialize(float damageAmount, Transform targetTransform)
    {
        damage = damageAmount;
        target = targetTransform;

        Destroy(gameObject, 5f);
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

        HitBox hitBox = other.GetComponent<HitBox>();

        if (hitBox != null && hitBox.health != null)
        {
            if (hitBox.health.transform == target)
            {
                hitBox.OnArrowHit(damage, transform.forward);
                Destroy(gameObject);
                return;
            }
        }

        // Duvar / zemin
        Destroy(gameObject);
    }
}
