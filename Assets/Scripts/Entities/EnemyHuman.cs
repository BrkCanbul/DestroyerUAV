using UnityEngine;

public class EnemyHuman : Entity
{
    public Animator animator;


    public  override void Die() {
        animator.enabled = false;
    }
    public override void TakeDamage(int damage) {
        base.TakeDamage(damage);
    }
}
