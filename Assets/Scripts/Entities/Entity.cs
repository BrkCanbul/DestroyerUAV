using System.Runtime.CompilerServices;
using UnityEditor.Compilation;
using UnityEngine;

public class Entity : MonoBehaviour
{

    [Header("Entity Stats")]
    [Tooltip("Health Point")]
    public int Health = 100;

    void Start()
    {
        
    }


    virtual public void TakeDamage(int damage) {
        Debug.Log($"{gameObject.name} took {damage} damage.");
        Health -= damage;
        if(Health <= 0){
            Die();
            return;
        }


    }
     public virtual  void Die() {
        Destroy(gameObject);
    }
}
