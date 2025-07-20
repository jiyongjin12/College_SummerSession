using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class Gun_Base : MonoBehaviour
{
    [Header("Gun_Base")]
    [Header("Obj")]
    public Transform me;

    [SerializeField] protected Transform startpos;
    [SerializeField] protected Bullet_Base bullet;

    //protected Magazine _magazine;
    protected Animator anim;
    protected Vector3 target;
    GameCanvas canvas;
    Player p;
    Text ammo_text;


    [Header("Status")]
    [Header("Delay")]
    [SerializeField] protected float damage;
    [SerializeField] protected float maxBulletDelay;
    [SerializeField] float waitRerodeTime;
    [SerializeField] float rerodeDelay;
    protected float curBulletDelay;
    protected float curRerodeDelay;
    protected float curWaitRerodeTime;

    [Header("Ammo")]
    public int magazine;
    public int curAmmo;
    public int remainAmmo;
    public int maxAmmo;

    [Header("Min : Max")]
    [SerializeField] protected float dirRanMin;
    [SerializeField] protected float dirRanMax;

    bool isRerode;
    bool isFire;
    public bool isActive = true;
    protected bool isRight;
    [SerializeField] protected bool isInfinite;

    [Header("Sound")]
    [SerializeField] protected AudioClip _fire;
    [SerializeField] private AudioClip relode;

    [Header("Pos")]
    public Vector3 _startpos;
    [SerializeField] protected Vector3 dir_gun;
    protected float rot;

    [Header("Inhale")]
    public float suckRange;
    public float fieldOfView;
    public float enemymoveSpeed;
    public LayerMask suckableLayer;
    public Transform suckPoint;
    public Vector2 suckDirection;
    public Vector3 lastPosition = Vector3.zero;

    protected virtual void Start()
    {
        canvas = GameCanvas.Instance;
        p = Player.Instance;

        DataManager d = DataManager.instance;
        List<GunData> datas = d.gunUpgradeData[d.curPlayerData.gunID].LV;
        damage = datas[d.curPlayerData.gunLV[0]].damage;
        maxBulletDelay = datas[d.curPlayerData.gunLV[1]].bulletDelay;
        rerodeDelay = datas[d.curPlayerData.gunLV[2]].rerodeDelay;
        magazine = datas[d.curPlayerData.gunLV[3]].magazine;
        maxAmmo = datas[d.curPlayerData.gunLV[4]].maxAmmo;
        curAmmo = magazine;
        remainAmmo = maxAmmo;
        dirRanMin = datas[d.curPlayerData.gunLV[5]].dirRanMin;
        dirRanMax = datas[d.curPlayerData.gunLV[6]].dirRanMax;

        //anim = me.gameObject.GetComponent<Animator>();
        // canvas = MainCanvas.Instance;

        // ammo_text = canvas.curAmmo;
        // _magazine = canvas.magazine;
        // left = p.left_Hend;
        // right = p.right_Hend;
        // main_camera = canvas.main_camera;

        // canvas.rerode.SetFill(0);
    }

    protected virtual void Update()
    {
        target = canvas.target.position;

        if (!isActive) return;

        Spin();

        if (curBulletDelay < maxBulletDelay) curBulletDelay += Time.deltaTime;
        if (curAmmo < magazine && isFire)
        {
            if (curWaitRerodeTime < waitRerodeTime) curWaitRerodeTime += Time.deltaTime;
            else Reload();
        }

    }

    public void UsingGun(bool curMode)
    {
        isFire = curMode;
        if (curMode) Fire();
        else Inhale();
    }

    void Reload()
    {
        isRerode = true;
        curRerodeDelay += Time.deltaTime;
        canvas.FillRerode(curRerodeDelay / rerodeDelay);
        if (curRerodeDelay > rerodeDelay && isRerode)
        {
            isRerode = false;
            if (remainAmmo > magazine)
                curAmmo = magazine;
            else
                curAmmo = remainAmmo;

            curRerodeDelay = 0;
            //_magazine.CurMagazin();
        }
    }

    private void Fire()
    {
        if (!isActive) return;

        if (isRerode)
        {
            curWaitRerodeTime = 0;
            curRerodeDelay = 0;
            canvas.FillRerode(0);
            isRerode = false;
        }

        if (!Input.GetMouseButton(0)) return;
        if (curBulletDelay < maxBulletDelay) return;
        Shot();
        curWaitRerodeTime = 0;
    }

    protected void Spin()
    {
        dir_gun = new Vector3(target.x, target.y - 0.5f) - me.transform.position;
        float z = Mathf.Atan2(dir_gun.y, dir_gun.x) * Mathf.Rad2Deg;
        rot = z - 90f;
        float angle = Vector2.SignedAngle(Vector2.right, dir_gun);
        //Debug.Log($"{dir_gun} / {rot} / {z} / {angle}");
        me.eulerAngles = new Vector3(0, 0, angle);
    }

    private void Inhale()
    {
        if (!isActive) return;

        suckDirection = dir_gun;

        if (!Input.GetMouseButton(1)) { return; }
        FindObj();
    }

    void FindObj()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(suckPoint.position, suckRange, suckableLayer);
        Collider2D[] InhaleCollider = Physics2D.OverlapCircleAll(suckPoint.position, 0.3f, suckableLayer);

        foreach (Collider2D col in colliders)
        {
            Vector2 toTarget = (col.transform.position - suckPoint.position).normalized;

            float angle = Vector2.Angle(suckDirection.normalized, toTarget);
            col.TryGetComponent<Fish>(out var fish);
            if (angle < fieldOfView / 2f && fish.isDie)
            {
                Transform target = col.transform;
                //target.position = Vector2.MoveTowards(target.position, suckPoint.position, enemymoveSpeed * Time.deltaTime);
                Rigidbody2D rb = col.GetComponent<Rigidbody2D>();

                //float distance = Vector2.Distance(target.position, suckPoint.position);
                Vector2 dir = ((Vector2)suckPoint.position - rb.position).normalized;
                rb.AddForce(dir * enemymoveSpeed, ForceMode2D.Force);
            }
        }

        foreach (Collider2D col in InhaleCollider)
        {
            if (col.TryGetComponent<Fish>(out var fish))
            {
                p.curFishList.Add(fish.fishData.fishID);
                if(fish.isDie) Destroy(col.gameObject);
            }
            else Debug.Log("This Obj is TestFish? Fish Component is NULL");
        }


    }

    void OnDrawGizmosSelected()
    {
        if (suckPoint != null)
        {
            Vector3 dir = suckDirection.normalized;
            float halfFOV = fieldOfView / 2f;

            Vector3 leftBoundary = Quaternion.Euler(0, 0, -halfFOV) * dir;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, halfFOV) * dir;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(suckPoint.position, suckPoint.position + leftBoundary * suckRange);
            Gizmos.DrawLine(suckPoint.position, suckPoint.position + rightBoundary * suckRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(suckPoint.position, 0.3f);
        }
    }

    protected abstract void Shot();
}
