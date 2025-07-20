using UnityEngine;
using UnityEngine.UI;

public class PlayerUpgradeButton : MonoBehaviour
{
    public Button button;
    public Text upgradeTarget;
    public Text curUpgradeLV;

    DataManager d;
    string target;
    int curLV;
    int index;

    public void Init(DataManager _d, int _index, string _target)
    {
        d = _d;
        index = _index;
        curLV = d.curPlayerData.playerLV[index];
        target = _target;

        upgradeTarget.text = $"{target}\nLV : {curLV + 1}";
        if (curLV >= d.upgradeData[index].data.Count - 1)
        {
            curUpgradeLV.text = $"{d.upgradeData[index].data[d.curPlayerData.playerLV[index]]}";
            button.gameObject.SetActive(false);
        }
        else curUpgradeLV.text = $"{d.upgradeData[index].data[curLV]} => {d.upgradeData[index].data[curLV + 1]}";

        button.onClick.AddListener(Upgrade);
    }

    public void Upgrade()
    {
        d.curPlayerData.playerLV[index]++;
        curLV = d.curPlayerData.playerLV[index];

        upgradeTarget.text = $"{target}\nLV : {curLV + 1}";
        if (curLV >= d.upgradeData[index].data.Count - 1)
        {
            curUpgradeLV.text = $"{d.upgradeData[index].data[d.curPlayerData.playerLV[index]]}";
            button.gameObject.SetActive(false);
        }
        else curUpgradeLV.text = $"{d.upgradeData[index].data[curLV]} => {d.upgradeData[index].data[curLV + 1]}";
    }
}
