using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Bullet_Base : MonoBehaviour
{
    public SpriteRenderer me;

    float cur_lifeTime;
    [SerializeField] protected float lifeTime;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float damage;

    [SerializeField] bool isHit;

    public void Init(float _lifeTime, float _moveSpeed, float _damage)
    {
        lifeTime = _lifeTime;
        moveSpeed = _moveSpeed;
        damage = _damage;
    }

    public void Init(float _lifeTime, float _moveSpeed)
    {
        lifeTime = _lifeTime;
        moveSpeed = _moveSpeed;
    }

    protected virtual void Update()
    {
        cur_lifeTime += Time.deltaTime;
        if (cur_lifeTime > lifeTime) Destroy(gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D hit) {
        if (!isHit && hit.collider.TryGetComponent<Fish>(out var e_hit))
        {
            e_hit.TakeDamage(this.transform, damage);
            Hit_Event();
        }
        // else if (hit.collider.CompareTag("Wall"))
        // {
        //     Hit_Wall(hit);
        // }
    }

    protected abstract void Hit_Event();
    protected abstract void Hit_Wall(Collision2D hit);
}
