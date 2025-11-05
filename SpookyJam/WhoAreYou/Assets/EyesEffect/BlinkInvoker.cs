using UnityEngine;

public class BlinkInvoker : MonoBehaviour
{
    public BlinkController blink;

    [Header("Editor Parameters")]
    public float closeDuration = 0.15f;
    public float holdDuration = 0.25f;
    public float openDuration = 0.18f;
    public bool useUnscaledTime = true;

    private void Start()
    {
        blink = FindObjectOfType<BlinkController>();
    }

    // 这个函数签名与问卷事件一致
    public void OnQuestionAnswered()
    {
        blink.BlinkOnce(closeDuration, holdDuration, openDuration, useUnscaledTime);
    }
}
