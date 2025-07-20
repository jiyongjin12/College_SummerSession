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
    [SerializeField] Image O2Image;
    [SerializeField] Image hpImage;
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
    float O2Timer;

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
        maxHP = d.upgradeData[0].LV[d.curPlayerData.playerLV[0]];
        maxO2 = d.upgradeData[1].LV[d.curPlayerData.playerLV[1]];
        moveSpeed = d.upgradeData[2].LV[d.curPlayerData.playerLV[2]];
        maxCapacity = d.upgradeData[3].LV[d.curPlayerData.playerLV[3]];

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

        if (Input.GetKeyDown(KeyCode.Space)) HP -= 10;
        if (isCantMove)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }
        Move();
        O2Timer += Time.deltaTime;
        if (O2Timer >= 1)
        {
            O2Timer = 0;
            O2--;
        }
        O2Image.fillAmount = (float)O2 / maxO2;
        hpImage.fillAmount = (float)HP / maxHP;
        if (targetJoystick.HendleInput.magnitude > 0.5f) curWeapon.UsingGun(fireMode);
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
