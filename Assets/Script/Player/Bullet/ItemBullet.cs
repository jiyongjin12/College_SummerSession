using System.Collections.Generic;
using UnityEngine;

public class ItemBullet : Bullet_Base
{
    [SerializeField] private float move_Value;
    [SerializeField] private float maxStopValue;
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private List<Vector3> plusPos;
    [SerializeField] private Player p;
    [SerializeField] private ItemType type;
    public void InitBezier(Vector3 _startPos, Transform _endPos, float minRadius, float maxRadius, float _maxStopValue, ItemType _type, Player _p)
    {
        startPos = _startPos;
        plusPos.Add(GetCirclePos(startPos, Random.Range(minRadius, maxRadius + 1)));
        endPos = _endPos;
        maxStopValue = _maxStopValue;
        move_Value = 0;
        type = _type;
        p = _p;

        //test
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        switch (type)
        {
            case ItemType.HP: sprite.color = Color.red; break;
            case ItemType.Ammo: sprite.color = Color.yellow; break;
            case ItemType.O2: sprite.color = Color.cyan; break;
            case ItemType.Fish: sprite.color = Color.white; break; //Temporary
        }
    }

    protected override void Update()
    {
        //base.Update();
        if(move_Value > maxStopValue){
            Hit_Event();
        }else{
            transform.position = Bezier(startPos, endPos.position, plusPos, move_Value);
            move_Value += Time.deltaTime;
        }
    }


    Vector3 GetCirclePos(Vector3 center, float radius)
    {
        int angle = Random.Range(0, 360);

        if (angle % 2 == 0) return center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sign(angle * Mathf.Rad2Deg) * radius);
        else return center - new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sign(angle * Mathf.Rad2Deg) * radius);
    }

    Vector3 Bezier(Vector3 startPos, Vector3 endPos, List<Vector3> plusPos, float value)
    {
        List<Vector3> posList = new List<Vector3>();

        posList.Add(startPos);
        foreach (var n in plusPos)
        {
            posList.Add(n);
        }
        posList.Add(endPos);

        while (posList.Count > 1)
        {
            List<Vector3> curPos = new List<Vector3>();
            for (int i = 0; i < posList.Count - 1; i++)
            {
                curPos.Add(Vector3.LerpUnclamped(posList[i], posList[i + 1], value));
            }
            posList = curPos;
        }

        return posList[0];
    }

    protected override void Hit_Event()
    {
        switch (type)
        {
            case ItemType.HP: p.HP++; break;
            case ItemType.Ammo: p.curWeapon.remainAmmo++; break;
            case ItemType.O2: p.O2++; break;
            case ItemType.Fish: p.capacity--; break; //Temporary
        }
        Destroy(gameObject);
    }

    protected override void Hit_Wall(Collision2D hit)
    {
        Hit_Event();
    }
}
