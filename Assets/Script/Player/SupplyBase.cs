using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SupplyBase : MonoBehaviour
{
    public SupplyData data;
    public ItemBullet item;
    public Player p;

    [Header("Bezier_Shot")]
    [SerializeField] private float bezier_moveSpeed;
    [SerializeField] private float bezier_minRadius;
    [SerializeField] private float bezier_maxRadius;
    [SerializeField] private float bezier_maxMoveValue;
    bool stopThrow;
    bool startThrow;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            stopThrow = false;
            p.interactionButton.onClick.AddListener(UsingSupplyBaseButton);
            p.interactionButton.onClick.RemoveListener(p.FireModeChangeButton);
            p.interactionButton.GetComponent<Image>().color = Color.red;
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            stopThrow = true;
            startThrow = false;
            p.interactionButton.onClick.AddListener(p.FireModeChangeButton);
            p.interactionButton.onClick.RemoveListener(UsingSupplyBaseButton);
            p.interactionButton.GetComponent<Image>().color = Color.white;
        }
    }
    public void UsingSupplyBaseButton()
    {
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
        yield return StartCoroutine(ThrowItem(ItemType.O2, data.O2 - (100 - p.O2) < 0 ? data.O2 : 100 - p.O2));
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
