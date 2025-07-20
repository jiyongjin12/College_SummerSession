using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunUpgradeButton : MonoBehaviour
{
    public Button gunButton;
    public Button upgradeButton;
    public int index;
    [SerializeField] List<GunUpgradeStatus> statuses = new();
    DataManager d;

    public void Init(List<GunUpgradeStatus> _statuses, int _index, Button _upgradeButton)
    {
        d = DataManager.instance;
        statuses = _statuses;
        index = _index;
        upgradeButton = _upgradeButton;

        gunButton.onClick.AddListener(Selection);
        upgradeButton.onClick.AddListener(Upgrade);
    }

    public void Selection()
    {
        d.curPlayerData.gunID = index;
        for (int i = 0; i < statuses.Count; i++)
        {
            if (i < d.gunUpgradeData[index].LV[0].status.Count) { statuses[i].gameObject.SetActive(true); }
            else { statuses[i].gameObject.SetActive(false); continue; }

            statuses[i].Init(index, i);
        }
    }

    public void Upgrade()
    {
        if (d.curPlayerData.gunLV[d.curPlayerData.gunID] >= d.gunUpgradeData[index].LV.Count - 1) return;
        if (index != d.curPlayerData.gunID) return;

        d.curPlayerData.gunLV[d.curPlayerData.gunID]++;

        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].gameObject.activeSelf) statuses[i].NewData(index);
        }
    }
}
