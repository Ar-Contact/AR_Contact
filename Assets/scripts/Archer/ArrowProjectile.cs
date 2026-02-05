using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 20f;
    private float damage;
    private Transform target;
    private GameObject owner; // EKLENDÝ: Oku atan kiþi

    // Initialize fonksiyonuna 'GameObject shooter' parametresini ekledik
    public void Initialize(float damageAmount, Transform targetTransform, GameObject shooter)
    {
        damage = damageAmount;
        target = targetTransform;
        owner = shooter; // Oku ataný kaydet

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
            if (hitBox.health.isDead) return;

            // KRÝTÝK KONTROL: Eðer çarptýðýmýz þey, oku atan kiþinin ta kendisiyse HASAR VERME
            if (hitBox.health.gameObject == owner)
            {
                return; // Kendi kendimizi vurmayý engelledik
            }

            hitBox.OnArrowHit(damage, transform.forward);
            Debug.Log("Ok Gerçek Hedefe Çarptý: " + other.name);
            Destroy(gameObject);
            return;
        }

        // Engel veya zemin
        Destroy(gameObject);
    }
}