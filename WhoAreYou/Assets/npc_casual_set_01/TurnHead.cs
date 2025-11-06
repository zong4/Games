using System.Collections;
using System.Collections.Generic;
using Text;
using UnityEngine;

public class HeadRotateInterface : MonoBehaviour
{
    [Header("��ת�Ƕȣ�����=��ת������=��ת��")]
    public float rotateAngle = 13f;

    [Header("��תʱ�����룩")]
    public float duration = 1f;

    private bool isRotating = false;
    private Quaternion startRot;
    private Quaternion targetRot;
    private float elapsed = 0f;
    
    public Material noiseMaterial;
    public Questionnaire questionnaire;
    public BlinkController blinkController;
    public UnityEngine.Camera camera;

    /// <summary>
    /// �ⲿ�ӿڣ����ô˷������ɴ���תͷ
    /// </summary>
    public void TriggerRotate()
    {
        if (isRotating) return; // ��ֹ�ظ�����

        isRotating = true;
        elapsed = 0f;
        startRot = transform.localRotation;
        targetRot = startRot * Quaternion.Euler(0f, rotateAngle, 0f);
    }

    void Update()
    {
        if (!isRotating) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // ƽ����ֵ��ת
        transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
        
        float thresholdValue = Mathf.Lerp(0f, 1f, t);
        noiseMaterial.SetFloat("_Threshold", thresholdValue);

        if (t >= 1f)
        {
            questionnaire.OutputEndMessage();
            isRotating = false;
        }
    }
}

