using System.Collections;
using UnityEngine;

public class ProjectileBehavior:MonoBehaviour {
    [Header("Bomb Settings")]
    public int damage = 100;
    public float explosionRadius = 5f;
    public float explosionForce = 500f;

    [Header("FX")]
    [Tooltip("Patlama efektinin prefab'ýný buraya sürükle.")]
    public ParticleSystem explosionEffect;

    [Tooltip("Efektin sahnede kalma süresi (saniye). 0 veya negatif ise ParticleSystem süresi kullanýlýr.")]
    public float explosionEffectLifetime = 0f;

    private bool hasExploded = false;

    private void Start() {
        
        StartCoroutine(DestroyAfterTime(10f));

    }

    private void OnCollisionEnter(Collision collision) {
        // Ayný bombanýn birden fazla kez patlamasýný önle
        if(hasExploded)
            return;
        hasExploded=true;

        Explode();
    }

    private IEnumerator DestroyAfterTime(float time) {
        yield return new WaitForSeconds(time);
        if(!hasExploded) {
            hasExploded=true;
            Explode();
        }

    }
    private void Explode() {
        // 1) Patlama efektini üret
        if(explosionEffect!=null) {
            ParticleSystem ps = Instantiate(
                explosionEffect,
                transform.position,
                Quaternion.identity
            );

            // Efekt ne kadar yaþayacak?
            float lifeTime = explosionEffectLifetime;

            if(lifeTime<=0f) {
                // Eðer inspector'dan süre vermediysen,
                // ParticleSystem'in ayarlarýndan otomatik hesapla.
                var main = ps.main;
                lifeTime=main.duration+main.startLifetimeMultiplier;
            }

            // Belirlenen süre sonunda efekti yok et
            Destroy(ps.gameObject,lifeTime);
        }
        else {
            Debug.LogWarning("[BombBehaviour] ExplosionEffect atanmadý!",this);
        }

        // 2) Buraya damage & force mantýðýný yazacaksýn
        Collider[] hits = Physics.OverlapSphere(transform.position,explosionRadius);
        foreach(var hit in hits)
            ExplosionHit(hit);


        // 3) Bombayý yok et
        Destroy(gameObject);
    }

    private void ExplosionHit(Collider hit) {
        // Burada hasar verme ve kuvvet uygulama iþlemlerini yapacaksýn
        if(explosionEffect!=null) {
            if(hit.TryGetComponent<Rigidbody>(out var rigidbody)) {
                rigidbody.AddExplosionForce(explosionForce,transform.position,explosionRadius);
            }
            
        }
    }

    // Sahnede radius’u görmek için:
    private void OnDrawGizmosSelected() {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(transform.position,explosionRadius);
    }
}
