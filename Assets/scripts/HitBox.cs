using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public Health health;

    public void OnArrowHit(float damageAmount, Vector3 hitDirection)
    {
        
        if (health != null)
        {
            health.TakeDamage(damageAmount, hitDirection);
        }
        else
        {
            Debug.LogError("HitBox.health atanmamýþ!");
        }
    }
}