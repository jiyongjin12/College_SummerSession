using UnityEngine;
using UnityEngine.UI;

public class GameCanvas : MonoBehaviour
{
    private static GameCanvas _instance = null;
    public static GameCanvas Instance => _instance;

    Player p;
    public RectTransform target;
    public Button interactionButton;
    public Image rerode;
    public Image Escape;
    public Image EscapeBar;

    void Awake()
    {
        _instance = this;
    }
    void Start()
    {
        p = Player.Instance;
    }

    void Update()
    {

    }

    public void FillRerode(float fillAmount)
    {
        rerode.fillAmount = fillAmount;

        rerode.gameObject.SetActive(fillAmount < 1);
    }

    public void FillEscape(float fillAmount)
    {
        EscapeBar.fillAmount = fillAmount;

        Escape.gameObject.SetActive(fillAmount < 1);
    }
}
