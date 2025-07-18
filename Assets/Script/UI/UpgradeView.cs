using UnityEngine;
using UnityEngine.UI;

public class UpgradeView : MonoBehaviour
{
    [SerializeField] GameObject panels;
    [SerializeField] Text money;
    private bool isShow = false;

    public void OnOffPanel()
    {
        isShow = !isShow;

        panels.SetActive(isShow);
        money.text = $"보유자원 : ";
    }

    public void InitButton()
    {

    }
}
