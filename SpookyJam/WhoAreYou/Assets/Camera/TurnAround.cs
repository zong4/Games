using Camera;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Light;


public class TurnAround : MonoBehaviour
{
    [Header("目标位置和角度")]
    public Transform targetTransform;   // 目标位置和角度（在场景中拖一个空物体进去）

    [Header("动画参数")]
    public float moveDuration = 2f;     // 移动/旋转的时间（秒）
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 缓动曲线

    private Vector3 startPos;
    private Quaternion startRot;
    private bool isMoving = false;
    private float elapsedTime = 0f;

    public CameraMove cameraMove;
    public LightFlicker lightController;
    public AudioSource audioSource;

    // 调用此函数即可触发镜头动画
    public bool TriggerTurnAround()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("未指定 targetTransform！");
            return false;
        }

        startPos = transform.position;
        startRot = transform.rotation;
        elapsedTime = 0f;
        isMoving = true;
        return true;
    }

    void Update()
    {
        if (isMoving)
        {
            audioSource.Play();
            lightController.maxIntensity = 0.5f;
            lightController.burstMaxIntensity = 1.0f;
            
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration);
            float easedT = easeCurve.Evaluate(t);

            // 平滑插值位置和旋转
            transform.position = Vector3.Lerp(startPos, targetTransform.position, easedT);
            transform.rotation = Quaternion.Slerp(startRot, targetTransform.rotation, easedT);

            if (t >= 1f)
            {
                isMoving = false;
                StartCoroutine(cameraMove.CameraThenHead());
            }
        }
    }
}
