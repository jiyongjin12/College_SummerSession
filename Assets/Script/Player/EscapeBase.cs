using UnityEngine;

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
        if (!holdButton.IsHeldDown || !isHold) return;
        curTime += Time.deltaTime;
        canvas.FillEscape(curTime / escpaeTime);

        if (curTime > escpaeTime) Debug.Log("OUT");
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
