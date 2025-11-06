using UnityEngine;

public class RealisticCameraBreathingV2 : MonoBehaviour
{
    [Header("呼吸参数")]
    public float verticalAmplitude = 0.001f;   // 上下起伏幅度
    public float depthAmplitude = 0.01f;       // 前后移动幅度（呼吸深度）
    public float rotationAmplitude = 0.3f;     // 头部旋转幅度（仰俯角）
    public float frequency = 0.15f;             // 呼吸频率
    public float smoothSpeed = 5f;             // 平滑插值速度

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;

    void Start()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;
    }

    void Update()
    {
        // 正弦波控制呼吸节奏（一个完整周期 = 一次吸气 + 呼气）
        float breathing = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f);

        // 吸气（breathing > 0）：头上抬 + 向后
        // 呼气（breathing < 0）：头下落 + 向前
        Vector3 targetPos = startLocalPos;
        targetPos.y += breathing * verticalAmplitude;       // 单独控制上下起伏
        targetPos.z -= breathing * depthAmplitude;          // 单独控制前后位移

        Quaternion targetRot = startLocalRot;
        targetRot *= Quaternion.Euler(-breathing * rotationAmplitude, 0f, 0f);  // 仰俯旋转

        // 平滑插值
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smoothSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}
