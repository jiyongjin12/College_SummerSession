using UnityEngine;
using UnityEngine.UI;

public class GunUpgradeView : MonoBehaviour
{
    [SerializeField] GameObject panels;
    [SerializeField] GameObject button;
    [SerializeField] RectTransform btnParent;
    [SerializeField] Text money;
    private bool isShow = false;

    void Start()
    {
        InitButton();
    }

    public void OnOffPanel()
    {
        isShow = !isShow;

        panels.SetActive(isShow);
        money.text = $"보유자원 : ";
    }

    public void InitButton()
    {
        DataManager d = DataManager.instance;
        for (int i = 0; i < d.curPlayerData.playerLV.Count; i++)
        {
            var btn = Instantiate(button, btnParent).GetComponent<PlayerUpgradeButton>();
        }
    }
}
