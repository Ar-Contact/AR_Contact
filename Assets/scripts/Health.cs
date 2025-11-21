using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    Ragdoll ragdoll;
    SkinnedMeshRenderer skinnedMeshRenderer;
    UIHealthBar healthBar;

    public float blinkIntensity;
    public float blinkDuration;
    float blinkTimer;

    // --- YENÝ EKLEME ---
    private bool isDead = false; // Karakterin ölü olup olmadýðýný takip eder

    void Start()
    {
        ragdoll = GetComponent<Ragdoll>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        healthBar = GetComponentInChildren<UIHealthBar>();
        currentHealth = maxHealth;
        isDead = false; // Oyuna canlý baþla

        var rigidBodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rigidBody in rigidBodies)
        {
            HitBox hitbox = rigidBody.gameObject.AddComponent<HitBox>();
            hitbox.health = this;
        }
    }

    public void TakeDamage(float amount, Vector3 direction)
    {
        // --- GÜNCELLEME ---
        // Eðer karakter zaten öldüyse, bu fonksiyondan hemen çýk
        // (Ölüler hasar alamaz)
        if (isDead)
        {
            return;
        }
        // --------------------

        currentHealth -= amount;
        healthBar.SetHealthBarPertencage(currentHealth / maxHealth);

        // --- GÜNCELLEME ---
        // Caný 0'ýn altýndaysa VE HENÜZ ÖLÜ DEÐÝLSE
        if (currentHealth <= 0.0f && !isDead)
        {
            Die(); // Die() fonksiyonunu sadece bir kez tetikle
        }
        // --------------------

        blinkTimer = blinkDuration;
    }
    private void Die()
    {
        // --- YENÝ EKLEME ---
        // Karakteri "Öldü" olarak iþaretle
        isDead = true;
        // --------------------

        ragdoll.ActivateRagdoll();
        healthBar.gameObject.SetActive(false);

        
    }

    private void Update()
    {
        // --- GÜNCELLEME ---
        // Eðer öldüyse, hasar alma efektini (blink) çalýþtýrma
        if (isDead)
        {
            if (!gameObject.CompareTag("Untagged"))
            {
                gameObject.tag = "Untagged";
            }
            return;
        }
        

        blinkTimer -= Time.deltaTime;
        float lerp = Mathf.Clamp01(blinkTimer / blinkDuration);
        float intensity = (lerp * blinkIntensity) + 1.0f;
        skinnedMeshRenderer.material.color = Color.white * intensity;
    }
}