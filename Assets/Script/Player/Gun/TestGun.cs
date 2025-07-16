using UnityEngine;

public class TestGun : Gun_Base
{
    [Header("Pistol")]
    [SerializeField] float lifeTime;
    [SerializeField] float moveSpeed;

    protected override void Shot()
    {
        if(curAmmo == 0 || remainAmmo == 0) return;

        float dir_ran = Random.Range(dir_ran_min, dir_ran_max + 1);
        var temp = Instantiate(bullet, startpos.transform.position, Quaternion.Euler(0, 0, rot + dir_ran)).GetComponent<Bullet_Base>();
        temp.Init(lifeTime, moveSpeed, damage);
        curAmmo--;
        if(!isInfinite) remainAmmo--;
        cur_bullet_delay = 0;
    }

    protected override void Click()
    {
        cur_bullet_delay += 0.1f;
    }

}
