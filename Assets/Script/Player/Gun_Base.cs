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
    Player p;
    Text ammo_text;


    [Header("Status")]
    [Header("Delay")]
    [SerializeField] protected float damage;
    [SerializeField] protected float max_bullet_delay;
    [SerializeField] float max_rerode_delay;
    [SerializeField] float minus_rerode_delay;
    protected float cur_bullet_delay;
    protected float cur_rerode_delay;

    [Header("Ammo")]
    public int magazine;
    public int cur_ammo;
    public int remain_ammo;
    [SerializeField] int max_ammo;

    [Header("Min : Max")]
    [SerializeField] protected int dir_ran_min;
    [SerializeField] protected int dir_ran_max;

    bool isFest;
    bool isCilck;
    bool isRerode;
    public bool isActive = true;
    protected bool isRight;
    [SerializeField] protected bool isInfinite;

    [Header("Sound")]
    [SerializeField] protected AudioClip _fire;
    [SerializeField] private AudioClip relode;

    [Header("Pos")]
    public Vector3 _startpos;
    protected Vector3 dir_gun;
    protected float rot;

    [Header("Inhale")]
    public float suckRange = 3f;
    public float fieldOfView = 60f; // 원뿔 각도 (좌우로 30도씩)
    public float enemymoveSpeed = 5f;
    public LayerMask suckableLayer;
    public Transform suckPoint;
    public Vector2 suckDirection;
    public Vector3 lastPosition = Vector3.zero;

    protected virtual void Start()
    {
        //anim = me.gameObject.GetComponent<Animator>();
        p = Player.Instance;
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
        target = p.target.position;

        if (!isActive) return;

        Spin();

        if (isRerode) Reload();
        if (cur_bullet_delay < max_bullet_delay) cur_bullet_delay += Time.deltaTime;
    }

    public void UsingGun(bool curMode)
    {
        if (curMode) Fire();
        else Inhale();
    }

    void Reload()
    {
        cur_rerode_delay += Time.deltaTime;
        //canvas.rerode.SetFill(1 - cur_rerode_delay / max_rerode_delay);
        if (cur_rerode_delay > max_rerode_delay && isRerode)
        {
            isRerode = false;
            if (remain_ammo > magazine)
                cur_ammo = magazine;
            else
                cur_ammo = remain_ammo;

            if (isFest)
            {
                max_rerode_delay += minus_rerode_delay;
                isFest = false;
            }
            //_magazine.CurMagazin();
        }
    }

    private void Fire()
    {
        if (!isActive) return;

        if (!isRerode)
        {
            if ((cur_ammo == 0 && Input.GetMouseButtonDown(0)))
            {
                isRerode = true;
                //SoundManager.Instance.Sound(relode, false, 1);
                cur_rerode_delay = 0;
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                isRerode = true;
                isFest = true;
                max_rerode_delay -= minus_rerode_delay;
                //SoundManager.Instance.Sound(relode, false, 1);
                cur_rerode_delay = 0;
            }
        }
        else return;

        if (Input.GetMouseButtonUp(0) && !isCilck)
        {
            isCilck = true;
            Click();
        }
        if (!Input.GetMouseButton(0)) return;
        if (cur_bullet_delay < max_bullet_delay) return;
        Shot();
        isCilck = false;
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

            if (angle < fieldOfView / 2f)
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
            Destroy(col.gameObject);
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

    protected abstract void Click();
}
