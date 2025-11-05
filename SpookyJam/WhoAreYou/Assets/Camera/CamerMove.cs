using System.Collections;
using System.Collections.Generic;
using Text;
using UnityEngine;


public class CameraMove : MonoBehaviour
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
    private HeadRotateInterface headScript;
    
    public Material noiseMaterial;
    public BlinkController blinkController;
    
    private void Start()
    {
        noiseMaterial.SetFloat("_Threshold", 0f);
    }

    // 调用此函数即可触发镜头动画
    public void TriggerCameraMove()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("未指定 targetTransform！");
            return;
        }

        startPos = transform.position;
        startRot = transform.rotation;
        elapsedTime = 0f;
        isMoving = true;
    }

    void Update()
    {
        GameObject headObj = GameObject.Find("Characters/YouTomorrow/Root/Hips/Spine/Spine1/Neck/Head");
        if (headObj != null)
        {
            headScript = headObj.GetComponent<HeadRotateInterface>();
        }
        else
        {
            Debug.LogError("找不到 Head，请检查路径是否正确！");
        }
        if (isMoving)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration);
            float easedT = easeCurve.Evaluate(t);

            // 平滑插值位置和旋转
            transform.position = Vector3.Lerp(startPos, targetTransform.position, easedT);
            transform.rotation = Quaternion.Slerp(startRot, targetTransform.rotation, easedT);

            // 逐步降低 Threshold，从 1 -> 0
            // float thresholdValue = Mathf.Lerp(0f, 1f, easedT);
            // noiseMaterial.SetFloat("_Threshold", thresholdValue);

            if (t >= 1f)
                isMoving = false;
        }

    }
   public IEnumerator CameraThenHead()
    {
        yield return new WaitForSeconds(2f);
        blinkController.BlinkOnce();
        GetComponent<UnityEngine.Camera>().targetDisplay = 0;

        // 1. 先触发相机动画
        TriggerCameraMove();

        // 2. 等待相机动画播放完（假设3秒）
        yield return new WaitForSeconds(10f);

        // 3. 再触发头部旋转
        headScript.TriggerRotate();

        yield return new WaitForSeconds(3.5f);  
        blinkController.BlinkOnce();
        GetComponent<UnityEngine.Camera>().targetDisplay = 1;
    }
}
