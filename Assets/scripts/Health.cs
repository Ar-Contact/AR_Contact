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

    public bool isDead = false;

    [Tooltip("Öldükten kaç saniye sonra obje tamamen yok olsun?")]
    public float destroyAfterDeathTime = 5.0f;

    // --- YENÝ EKLENEN KISIMLAR ---
    [Header("Takým ve Ödül Ayarlarý")]
    [Tooltip("Bu mob hangi takýma ait? (Büyük harfle: 'Blue' veya 'Red' yazýn)")]
    public string mobTakimi = "Red"; // Varsayýlan Red olsun

    [Tooltip("Bu mob öldüðünde karþý takýma kaç puan versin?")]
    public int oldurmeOdulu = 10;
    // -----------------------------

    void Start()
    {
        ragdoll = GetComponent<Ragdoll>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        healthBar = GetComponentInChildren<UIHealthBar>();
        currentHealth = maxHealth;
        isDead = false;

        var rigidBodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rigidBody in rigidBodies)
        {
            HitBox hitbox = rigidBody.gameObject.AddComponent<HitBox>();
            hitbox.health = this;
        }
    }

    public void TakeDamage(float amount, Vector3 direction)
    {
        if (isDead) return;

        currentHealth -= amount;
        healthBar.SetHealthBarPertencage(currentHealth / maxHealth);

        if (currentHealth <= 0.0f && !isDead)
        {
            Die();
        }

        blinkTimer = blinkDuration;
    }

    private void Die()
    {
        isDead = true;

        ragdoll.ActivateRagdoll();
        healthBar.gameObject.SetActive(false);

        // --- YENÝ EKLENEN MANTIK ---
        // Oyuncu þu an hangi takýmda? (PlayerTeamer'dan öðreniyoruz)
        string oyuncuTakimi = PlayerTeamer.SecilenTakim;

        // EÐER: Oyuncu Mavi ise VE bu ölen mob Kýrmýzý ise -> Para Ver
        if (oyuncuTakimi == "Blue" && mobTakimi == "Red")
        {
            CurrencyManager.Instance.ParaKazan(oldurmeOdulu);
        }
        // EÐER: Oyuncu Kýrmýzý ise VE bu ölen mob Mavi ise -> Para Ver
        else if (oyuncuTakimi == "Red" && mobTakimi == "Blue")
        {
            CurrencyManager.Instance.ParaKazan(oldurmeOdulu);
        }
        // ---------------------------

        Destroy(gameObject, destroyAfterDeathTime);
    }

    private void Update()
    {
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