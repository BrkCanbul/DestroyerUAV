using UnityEditor.Animations;
using UnityEngine;

public class EnemyPlane : Entity
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EnemyAI aiController;


    [Header("Death Settings")]
    public float deathTorque = 500f;
    public float deathforce = 100f;
    public float minCrashSpeedForExplosion = 25f;
    public ParticleSystem explosionEffect;
    public ParticleSystem dieEffect;
    public LayerMask groundLayer;
    private bool isDead = false;

    //references
    private Rigidbody rb;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }
    
    public override void Die() {
        if(isDead) return;
        isDead = true;
        if(dieEffect!=null) {
            var fx = Instantiate(dieEffect,transform.position,Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject,fx.main.duration+2f);
        }
        
        aiController.OnDied();
        aiController.enabled=false;

        if(rb !=null) {
            rb.isKinematic=false;
            rb.constraints = RigidbodyConstraints.None;
        }
        rb.AddForce(transform.forward * deathforce,ForceMode.Impulse);
        Vector3 randTorq = new Vector3(
            Random.Range(-1f,1f),
            Random.Range(-1f,1f),
            Random.Range(-1f,1f)
        ).normalized*deathforce;
        rb.AddTorque(randTorq,ForceMode.Impulse);

        Debug.Log($"{gameObject.name} has been died.");


    }
    public override void TakeDamage(int damage) {
        base.TakeDamage(damage);
    }

    private bool IsInLayer(GameObject obj, LayerMask layerMask) {
        return (layerMask.value & (1 << obj.layer)) > 0;
    }
    private void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.tag=="Ammo") {

            return;
        }
        if(!isDead) {
            Debug.Log($"{gameObject.name} collided with {collision.gameObject.name}");
            Die();
        }

        if(!IsInLayer(collision.gameObject,groundLayer)) {

            Debug.Log("Not ground layer, no explosion");

            return;
            
        }
        Debug.Log(collision.gameObject.layer);
        Debug.Log(groundLayer);
        Debug.Log("Ground layer collision, checking for explosion");
        if(rb!=null&&rb.linearVelocity.magnitude<minCrashSpeedForExplosion)
            return;

        if(explosionEffect!=null) {
            var fx = Instantiate(explosionEffect,transform.position,Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject,fx.main.duration+2f);
        }
        Destroy(gameObject);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        UnityEditor.Handles.color=Color.green;
        UnityEditor.Handles.Label(transform.position+(Vector3.up+Vector3.left)*5f,$"Health: {Health}");
    }
#endif
}
