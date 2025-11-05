using UnityEngine;
using System.Collections;

namespace Camera
{
    public class CameraWake : MonoBehaviour
    {
        [Header("Wake Animation Settings")]
        public float duration = 3f;           // 总持续时间
        public float shakeAmplitude = 2f;     // 左右摇晃幅度（度）
        public float tiltAmplitude = 3f;      // 前后轻晃幅度（度）
        public float frequency = 3f;          // 摇晃频率
        public float downAngle = 8f;          // 低头角度（度）
        public AnimationCurve recoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private float timer = 0f;
        private Quaternion initialRotation;
        private bool playing = false;

        void Awake()
        {
            initialRotation = transform.localRotation;
            // StartWakeEffect(); // 进入场景立即播放
        }

       public void StartWakeEffect()
        {
            timer = 0f;
            playing = true;
            Debug.Log("Starting Wake Effect");
        }

        void Update()
        {
            if (!playing) return;

            timer += Time.deltaTime;
            float t = timer / duration;

            if (t >= 1f)
            {
                // 动画结束，恢复原状
                playing = false;
                transform.localRotation = initialRotation;
                var rotateCam = GetComponent<RotateCamera>();
                rotateCam.enabled = true;
                rotateCam.OnEnable();
                return;
            }

            // 使用衰减曲线（随时间渐弱）
            float recovery = recoveryCurve.Evaluate(t);

            // 左右摇晃 (Yaw)
            float yaw = Mathf.Sin(timer * frequency * Mathf.PI * 2f) * shakeAmplitude * (1 - recovery);

            // 前后轻晃 (Pitch)
            float pitch = Mathf.Sin(timer * frequency * 1.3f) * tiltAmplitude * (1 - recovery);

            // 模拟“抬头醒来”过程：开始时略低，逐渐抬起
            float wakeUpPitch = Mathf.Lerp(-downAngle, 0f, recovery); 

            transform.localRotation = initialRotation * Quaternion.Euler(pitch - wakeUpPitch, yaw, 0f);
        }
    }
}
