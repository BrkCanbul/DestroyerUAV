using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class AmmoBehavior : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int damage = 50;
    public float lifetime = 5f;
    public float sphereRadius = 0.02f;
    public LayerMask HitMask;
    [Range(0f,1f)]
    public float ricochetChance = 0.3f;


    //fields - references
    private Rigidbody rb;
    private Vector3 lastPosition;
    private float timer;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = rb.position;
        timer = lifetime;
    }
    private void FixedUpdate() {
        timer -= Time.fixedDeltaTime;
        if(timer<=0f) {
            Destroy(gameObject);
            return;
        }
        Vector3 currentPosition = rb.position;
        Vector3 move = currentPosition - lastPosition;
        float distance = move.magnitude;
        if(distance>0f) {
            if(Physics.SphereCast(lastPosition,sphereRadius,move.normalized,out var raycastHit,distance,HitMask) && Random.value>ricochetChance) {
                rb.position=raycastHit.point;
                Debug.Log("Ammo hit: " + raycastHit.collider.name);
                var entity = raycastHit.collider.GetComponentInParent<Entity>();
                
                if(entity!= null) {
                    entity.TakeDamage(damage);
                }
                Destroy(gameObject);
                return;
            }

        }
        lastPosition = currentPosition;

    }

}
