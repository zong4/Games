using UnityEngine;

public class BlinkTester : MonoBehaviour
{
    [Header("Reference")]
    public BlinkController blink;

    [Header("Blink Parameters (seconds)")]
    [Tooltip("闭眼时间（从睁到闭）")]
    public float closeDuration = 0.15f;

    [Tooltip("保持闭眼的时间")]
    public float holdDuration = 0.25f;

    [Tooltip("睁眼时间（从闭到睁）")]
    public float openDuration = 0.18f;

    [Tooltip("是否使用不受 TimeScale 影响的时间")]
    public bool useUnscaledTime = true;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 点击鼠标左键触发一次眨眼，使用 Inspector 里的参数
            blink.BlinkOnce(closeDuration, holdDuration, openDuration, useUnscaledTime);
        }
    }
}

