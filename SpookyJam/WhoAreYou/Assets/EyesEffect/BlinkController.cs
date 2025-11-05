using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class BlinkController : MonoBehaviour
{
    [Header("Blink Durations (seconds)")]
    public float closeDuration = 0.15f;   // ��������
    public float holdClosed = 0.25f;    // ����ͣ��
    public float openDuration = 0.18f;    // �ӱյ���
    public bool useUnscaledTime = false;  // �Ƿ�ʹ�ò��� timeScale Ӱ���ʱ��

    [Header("Eye Range")]
    public float maxY = 0.30f;  // ����ʱ����
    public float minY = 0.05f;  // ����ʱ����

    private Material _mat;
    private Coroutine _blinkCo;

    private void Start()
    {
        var img = GetComponent<Image>();
        _mat = Instantiate(img.material);
        img.material = _mat;
        SetY(maxY); // ��ʼΪ����
    }

    /// <summary>
    /// ����һ��գ�ۣ���Ĭ�ϲ�����
    /// </summary>
    public void BlinkOnce()
    {
        BlinkOnce(closeDuration, holdClosed, openDuration, useUnscaledTime, true);
    }

    /// <summary>
    /// ͨ���ӿڿ���һ�α��ۡ�ͣ��������
    /// </summary>
    public void BlinkOnce(float closeDur, float holdDur, float openDur, bool unscaled = false, bool interruptRunning = true)
    {
        if (_blinkCo != null && interruptRunning)
        {
            StopCoroutine(_blinkCo);
            _blinkCo = null;
        }

        if (_blinkCo == null)
            _blinkCo = StartCoroutine(BlinkRoutine(closeDur, holdDur, openDur, unscaled));
    }

    private IEnumerator BlinkRoutine(float closeDur, float holdDur, float openDur, bool unscaled)
    {
        // �׶�1������
        yield return LerpY(maxY, minY, closeDur, unscaled);

        // �׶�2��ͣ��
        yield return CustomWait(holdDur, unscaled);

        // �׶�3������
        yield return LerpY(minY, maxY, openDur, unscaled);

        _blinkCo = null;
    }
    
    /// <summary>
    /// 多次眨眼接口
    /// </summary>
    /// <param name="count">眨眼次数</param>
    /// <param name="interval">眨眼间隔时间（秒）</param>
    public void BlinkMultiple(List<float> interval)
    {
        if (_blinkCo != null)
        {
            StopCoroutine(_blinkCo);
            _blinkCo = null;
        }
        _blinkCo = StartCoroutine(BlinkMultipleRoutine(interval));
    }

    private IEnumerator BlinkMultipleRoutine(List<float> interval)
    {
        for (int i = 0; i < interval.Count; i++)
        {
            yield return BlinkRoutine(closeDuration, holdClosed, openDuration, useUnscaledTime);
            if (i < interval.Count - 1)
                yield return CustomWait(interval[i], useUnscaledTime);
        }
        _blinkCo = null;
    }

    /// <summary>
    /// ��ָ��ʱ����ƽ����ֵ _Param.y
    /// </summary>
    private IEnumerator LerpY(float from, float to, float duration, bool unscaled)
    {
        if (duration <= 0f) { SetY(to); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress); // ƽ������
            SetY(Mathf.Lerp(from, to, eased));
            yield return null;
        }
        SetY(to);
    }

    /// <summary>
    /// �ȴ�ָ����������ѡ��ʹ�ò��� TimeScale Ӱ���ʱ��
    /// </summary>
    private IEnumerator CustomWait(float seconds, bool unscaled)
    {
        if (seconds <= 0f) yield break;

        if (unscaled)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private void SetY(float y)
    {
        if (_mat == null) return;
        Vector4 param = _mat.GetVector("_Param");
        param.y = y;
        _mat.SetVector("_Param", param);
    }
}
