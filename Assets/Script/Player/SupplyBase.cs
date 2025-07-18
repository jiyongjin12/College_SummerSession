using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SupplyBase : InteractionOBJ
{
    public SupplyData data;
    public ItemBullet item;

    [Header("Bezier_Shot")]
    [SerializeField] private float bezier_moveSpeed;
    [SerializeField] private float bezier_minRadius;
    [SerializeField] private float bezier_maxRadius;
    [SerializeField] private float bezier_maxMoveValue;
    [SerializeField] bool stopThrow;
    [SerializeField] bool startThrow;

    protected override void Start()
    {
        base.Start();
        add += UsingSupplyBaseButton;
    }

    protected override void TriggerEvent(bool isOut = false)
    {
        if (isOut)
        {
            stopThrow = true;
            startThrow = false;
        }else stopThrow = false;
    }
    
    public void UsingSupplyBaseButton()
    {
        Debug.Log($"{startThrow}");
        if (!startThrow)
        {
            startThrow = true;
            StartCoroutine(StartThrow());
        }
    }

    IEnumerator StartThrow()
    {
        yield return StartCoroutine(ThrowItem(ItemType.HP, data.hp - (p.maxHP - p.HP) < 0 ? data.hp : p.maxHP - p.HP));
        yield return StartCoroutine(ThrowItem(ItemType.Ammo, data.ammo - (p.curWeapon.maxAmmo - p.curWeapon.remainAmmo) < 0 ? data.ammo : p.curWeapon.maxAmmo - p.curWeapon.remainAmmo));
        yield return StartCoroutine(ThrowItem(ItemType.O2, data.O2 - (p.maxO2 - p.O2) < 0 ? data.O2 : p.maxO2 - p.O2));
        yield return new WaitForSeconds(1f);
        startThrow = false;
        //StartCoroutine(ThrowItem(ItemType.HP, data.hp - (p.maxHP - p.HP) < 0 ? data.hp : p.maxHP - p.HP, true));
    }

    IEnumerator ThrowItem(ItemType type, int throwCount, bool isPlayer = false)
    {
        if (throwCount == 0) yield break;
        Vector3 start;
        Transform end;
        if (!isPlayer) { start = transform.position; end = p.transform; }
        else { start = p.transform.position; end = transform; }

        // switch (type)
        // {
        //     case ItemType.HP: data.hp -= throwCount; break;
        //     case ItemType.Ammo: data.ammo -= throwCount; break;
        //     case ItemType.O2: data.O2 -= throwCount; break;
        //     case ItemType.Fish: data.capacity -= throwCount; break; //Temporary
        // }

        for (int i = 0; i < throwCount; i++)
        {
            if (stopThrow) yield break;
            var temp = Instantiate(item, start, Quaternion.identity).GetComponent<ItemBullet>();
            temp.Init(99, bezier_moveSpeed);
            temp.InitBezier(start, end, bezier_minRadius, bezier_maxRadius, bezier_maxMoveValue, type, p);
            switch (type)
            {
                case ItemType.HP: data.hp--; break;
                case ItemType.Ammo: data.ammo--; break;
                case ItemType.O2: data.O2--; break;
                case ItemType.Fish: data.capacity--; break; //Temporary
            }
            yield return new WaitForSeconds(0.025f);
        }
    }
}

public enum ItemType
{
    HP,
    Ammo,
    O2,
    Fish
}
