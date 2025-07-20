using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUpgradeView : MonoBehaviour
{
    [SerializeField] GameObject panels;
    [SerializeField] GameObject button;
    [SerializeField] RectTransform btnParent;
    [SerializeField] TMP_Text money;
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
            string text = "Error";
            switch (i)
            {
                case 0: text = "HP"; break;
                case 1: text = "O2"; break;
                case 2: text = "Speed"; break;
                case 3: text = "Capacity"; break;
            }
            btn.Init(d, i, text);
        }
    }
}
