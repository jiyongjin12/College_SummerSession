using UnityEngine;
using UnityEngine.UI;

public class GunUpgradeView : MonoBehaviour
{
    [SerializeField] GameObject gunPanels;
    [SerializeField] GameObject gunbutton;
    [SerializeField] GameObject upgradePanels;
    [SerializeField] GameObject upgradeButton;
    [SerializeField] RectTransform gunBtnParent;
    [SerializeField] RectTransform upgradeBtnParent;
    [SerializeField] Text money;
    private bool isShow = false;

    void Start()
    {
        InitGunButton();
    }

    public void OnOffPanel()
    {
        isShow = !isShow;

        gunPanels.SetActive(isShow);
        money.text = $"보유자원 : ";
    }

    public void InitGunButton()
    {
        DataManager d = DataManager.instance;
        for (int i = 0; i < d.curPlayerData.playerLV.Count; i++)
        {
            var btn = Instantiate(gunbutton, gunBtnParent).GetComponent<Button>();
        }
    }
}
