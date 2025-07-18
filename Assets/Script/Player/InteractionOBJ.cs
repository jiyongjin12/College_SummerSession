using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class InteractionOBJ : MonoBehaviour
{
    [SerializeField] protected UnityAction add;
    [SerializeField] protected Color color;
    protected GameCanvas canvas;
    protected Player p;

    protected virtual void Start()
    {
        canvas = GameCanvas.Instance;
        p = Player.Instance;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TriggerEvent();
            canvas.interactionButton.onClick.AddListener(add);
            canvas.interactionButton.onClick.RemoveListener(p.FireModeChangeButton);
            canvas.interactionButton.GetComponent<Image>().color = color;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TriggerEvent(true);
            canvas.interactionButton.onClick.AddListener(p.FireModeChangeButton);
            canvas.interactionButton.onClick.RemoveListener(add);
            canvas.interactionButton.GetComponent<Image>().color = Color.white;
        }
    }

    protected virtual void TriggerEvent(bool isOut = false) { }
}
