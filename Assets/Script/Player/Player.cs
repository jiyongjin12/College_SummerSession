using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Player : MonoBehaviour
{
    private static Player _instance = null;
    public static Player Instance => _instance;

    [Header("Objects")]
    public VariableJoystick moveJoystick;
    public VariableJoystick targetJoystick;
    Rigidbody2D rigid;
    GameCanvas canvas;
    Animator anim;
    Vector3 pos;

    [Header("Status")]
    public int HP;
    public int maxHP;
    public int O2;
    public int maxO2;
    public float moveSpeed;
    public int capacity;
    public int maxCapacity;
    public List<int> curFishList = new();

    [Header("Weapon")]
    public Transform gunPos;
    public Gun_Base curWeapon;

    [Header("Values")]
    float x, y;


    bool isDamage;
    public bool isActive = false;
    public bool isCantMove = false;
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
        canvas = GameCanvas.Instance;
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        DataManager d = DataManager.instance;
        maxHP = d.upgradeData.hpLVList[d.curPlayerData.hpLV];
        maxO2 = d.upgradeData.O2LVList[d.curPlayerData.O2LV];
        maxCapacity = d.upgradeData.capacityLVList[d.curPlayerData.capacityLV];

        HP = maxHP;
        O2 = maxO2;
        capacity = 0;

        canvas.interactionButton.onClick.AddListener(FireModeChangeButton);
    }

    void Update()
    {
        pos = transform.position;
        if (!isActive) return;
        if (targetJoystick.HendleMove != Vector2.zero) canvas.target.anchoredPosition = targetJoystick.HendleMove * radius;

        if (isCantMove)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }
        Move();
        if(targetJoystick.HendleInput.magnitude > 0.5f) curWeapon.UsingGun(fireMode);
    }

    public void FireModeChangeButton() { fireMode = !fireMode; }

    void Move()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        // x = moveJoystick.Horizontal;
        // y = moveJoystick.Vertical;

        dir = canvas.target.position - pos;
        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector3 nor = new Vector3(x, y, 0f).normalized;
        rigid.linearVelocity = new Vector2(nor.x * moveSpeed, nor.y * moveSpeed);

    }

    public void Damage()
    {

    }

}
