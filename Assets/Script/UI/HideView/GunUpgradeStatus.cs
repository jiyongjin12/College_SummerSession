using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunUpgradeStatus : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Image background;
    [SerializeField] Image nextStatus;
    [SerializeField] Image curStatus;

    DataManager d;
    int curLV;
    int gunIndex;
    int statusIndex;

    public void Init(int _gunIndex, int _statusIndex)
    {
        d = DataManager.instance;
        statusIndex = _statusIndex;
        gunIndex = _gunIndex;
        curLV = d.curPlayerData.gunLV[gunIndex];

        if (curLV >= d.gunUpgradeData[gunIndex].LV.Count - 1)
        {
            curStatus.fillAmount =
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] < d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] ?
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] :
            d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex];

        }
        else
        {
            curStatus.fillAmount =
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] < d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] ?
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] :
            d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex];

            nextStatus.fillAmount =
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] < d.gunUpgradeData[gunIndex].LV[curLV + 1].status[statusIndex] ?
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[curLV + 1].status[statusIndex] :
            d.gunUpgradeData[gunIndex].LV[curLV + 1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex];
        }
    }

    public void NewData(int index)
    {
        gunIndex = index;
        curLV = d.curPlayerData.gunLV[gunIndex];

        if (curLV >= d.gunUpgradeData[gunIndex].LV.Count - 1)
        {
            curStatus.fillAmount =
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] < d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] ?
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] :
            d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex];

        }
        else
        {
            curStatus.fillAmount =
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] < d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] ?
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] :
            d.gunUpgradeData[gunIndex].LV[curLV].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex];

            nextStatus.fillAmount =
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] < d.gunUpgradeData[gunIndex].LV[curLV + 1].status[statusIndex] ?
            d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[curLV + 1].status[statusIndex] :
            d.gunUpgradeData[gunIndex].LV[curLV + 1].status[statusIndex] / d.gunUpgradeData[gunIndex].LV[^1].status[statusIndex];
        }
    }
}
