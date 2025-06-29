using UnityEngine;

public class Bullet : Bullet_Base
{
    protected override void Update()
    {
        base.Update();
        transform.Translate(Vector3.up * Time.deltaTime * moveSpeed);
    }

    protected override void Hit_Event()
    {
        Destroy(gameObject);
    }

    protected override void Hit_Wall(Collision2D hit){
        Hit_Event();
    }
}
