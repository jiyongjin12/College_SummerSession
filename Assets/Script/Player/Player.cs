using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    private static Player _instance = null;
    public static Player Instance => _instance;

    [Header("Objects")]
    public VariableJoystick moveJoystick;
    public VariableJoystick targetJoystick;
    public RectTransform target;
    Rigidbody2D rigid;
    Animator anim;
    Vector3 pos;

    [Header("Status")]
    public int HP;
    public float moveSpeed;

    [Header("Weapon")]
    public Transform gunPos;
    public Gun_Base curWeapon;

    [Header("Values")]
    float x, y;


    bool isDamage;
    public bool isActive = false;
    public bool fireMode = true;
    public float radius;

    [Header("Pos")]
    [SerializeField] Vector3 localPosition;
    [SerializeField] Vector2 dir;

    public void _Instance()
    {
        _instance = this;
    }

    void Awake()
    {
        _Instance();
    }

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        pos = transform.position;
        if (!isActive) return;
        if (targetJoystick.HendleMove != Vector2.zero) target.anchoredPosition = targetJoystick.HendleMove * radius;

        Move();
        curWeapon.UsingGun(fireMode);
    }

    public void ChangeGunMode(){ fireMode = !fireMode; } 

    void Move()
    {
        //if (isSlow) return;
        // x = Input.GetAxisRaw("Horizontal");
        // y = Input.GetAxisRaw("Vertical");
        x = moveJoystick.Horizontal;
        y = moveJoystick.Vertical;

        dir = target.position - pos;
        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector3 nor = new Vector3(x, y, 0f).normalized;
        rigid.linearVelocity = new Vector2(nor.x * moveSpeed, nor.y * moveSpeed);

    }

    public void Damage()
    {

    }
}
