using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDamager : MonoBehaviour
{
    [Header("Temel Ayarlar")]
    public float damageAmount = 25.0f; // Kýlýcýn vuracaðý hasar
    public string targetTag; // Hasar verilecek hedef tag'i (örn: "RedTeam")

    [Header("Cooldown Ayarý")]
    [Tooltip("Her vuruþ arasýnda geçmesi gereken minimum süre (saniye)")]
    public float attackCooldown = 1.0f; // 1 saniyede sadece 1 kez hasar ver

    private float nextHitTime; // Bir sonraki vuruþun yapýlabileceði zaman
    private Health myHealth;

    void Awake()
    {
        // Kendi canýmýzý bul (kendimize vurmayalým)
        myHealth = GetComponentInParent<Health>();

        // Oyuna baþlarken hemen vurabilmek için zamaný sýfýrla
        nextHitTime = 0f;
    }

    // Animasyon Eventleri (Start/EndDealDamage) artýk GEREKLÝ DEÐÝL.
    // Bu fonksiyonun içindeki zamanlayýcý her þeyi halledecek.

    private void OnTriggerEnter(Collider other)
    {
        // --- 1. COOLDOWN KONTROLÜ ---
        // Þu anki oyun zamaný (Time.time), bir sonraki vurabileceðimiz zamandan (nextHitTime)
        // daha mý küçük?
        if (Time.time < nextHitTime)
        {
            // Cevap evet ise, cooldown hala devam ediyor demektir.
            // Bu temasý YOK SAY ve fonksiyondan çýk.
            return;
        }

        // --- 2. HEDEF FÝLTRELEME ---
        // Temas ettiðimiz objeden HitBox'ý almayý dene
        HitBox hitBox = other.GetComponent<HitBox>();

        // Eðer bir HitBox'a çarptýysak...
        if (hitBox != null)
        {
            // 1. Çarptýðýmýz hedefin (Health bileþenine sahip olan ana obje) tag'i 
            //    bizim belirlediðimiz 'targetTag' ile eþleþiyor mu?
            if (hitBox.health.CompareTag(targetTag))
            {
                // 2. Çarptýðýmýz þeyin caný (health) bizim kendi canýmýz deðilse...
                if (hitBox.health != myHealth)
                {
                    // --- VURUÞ BAÞARILI! ---

                    // 3. Hasarý uygula
                    Debug.Log(gameObject.name + " kýlýcý, " + other.name + " objesine vurdu!");
                    Vector3 hitDirection = (other.transform.position - transform.position).normalized;
                    hitBox.health.TakeDamage(damageAmount, hitDirection);

                    // 4. COOLDOWN'U SIFIRLA
                    // Bir sonraki vurabileceðimiz zaman = þu anki zaman + cooldown süresi
                    // (Yani 1 saniye boyunca bir daha hasar veremez)
                    nextHitTime = Time.time + attackCooldown;
                }
            }
        }
    }
}