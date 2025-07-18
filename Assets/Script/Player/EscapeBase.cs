using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeBase : InteractionOBJ
{
    [SerializeField] float escpaeTime;
    [SerializeField] bool isHold;
    HoldButton holdButton;
    float curTime;

    protected override void Start()
    {
        base.Start();
        holdButton = canvas.interactionButton.GetComponent<HoldButton>();
        add += Click;
    }

    void Update()
    {
        if (!holdButton.IsHeldDown || !isHold)
        {
            curTime = 0;
            canvas.FillEscape(1);
            p.isCantMove = false;
            return;
        }
        p.isCantMove = true;
        curTime += Time.deltaTime;
        canvas.FillEscape(curTime / escpaeTime);

        if (curTime > escpaeTime) SceneManager.LoadScene(2);
    }

    void Click()
    {
        curTime = 0;
    }

    protected override void TriggerEvent(bool isOut = false)
    {
        isHold = !isOut;
    }
}
