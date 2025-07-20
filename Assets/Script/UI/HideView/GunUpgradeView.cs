using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunUpgradeView : MonoBehaviour
{
    [SerializeField] GameObject gunPanels;
    [SerializeField] GameObject gunbutton;
    [SerializeField] GameObject status;
    [SerializeField] RectTransform gunBtnParent;
    [SerializeField] RectTransform statusParent;
    [SerializeField] Button upgradeButton;
    [SerializeField] TMP_Text money;

    List<GunUpgradeStatus> statuses = new();
    private bool isShow = false;

    void Start()
    {
        InitButton();
    }

    public void OnOffPanel()
    {
        isShow = !isShow;

        gunPanels.SetActive(isShow);
        money.text = "보유자원 : ";
    }

    public void InitButton()
    {
        DataManager d = DataManager.instance;
        for (int i = 0; i < 10; i++)
        {
            var btn = Instantiate(status, statusParent).GetComponent<GunUpgradeStatus>();
            statuses.Add(btn);
        }
        for (int i = 0; i < d.gunUpgradeData.Count; i++)
        {
            var btn = Instantiate(gunbutton, gunBtnParent).GetComponent<GunUpgradeButton>();
            btn.Init(statuses, i, upgradeButton);
            if (i == 0) btn.Selection();
        }
    }
}
