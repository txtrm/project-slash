using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int Health;
    private int Damage;

    private float DMGtimer = 0f;

    void Update()
    {
        DMGtimer -= Time.deltaTime;
        if (Health <= 0)
            Death();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Weapon") && DMGtimer <= 0)
        {
            TakeDamage();
            DMGtimer = 0.5f;
        }
    }

    private void TakeDamage() {
        Health -= 1;
    }

    private void Death() {
        Destroy(gameObject);
    }
}
