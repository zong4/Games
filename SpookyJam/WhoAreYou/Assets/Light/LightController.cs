using UnityEngine;

namespace Light
{
    [RequireComponent(typeof(UnityEngine.Light))]
    public class LightFlicker : MonoBehaviour
    {
        public AudioSource audioSource;
        
        [Header("Normal Flicker Settings")]
        public float minIntensity = 0.6f;    // 普通最小亮度
        public float maxIntensity = 1.2f;    // 普通最大亮度
        public float flickerSpeed = 2.0f;    // 普通闪烁间隔速度

        [Header("Burst Flicker (High Frequency)")]
        public float burstMinIntensity = 0.0f;  // 高频时最低亮度
        public float burstMaxIntensity = 1.5f;  // 高频时最高亮度
        public float burstDuration = 0.5f;      // 高频闪烁持续时间
        public float burstSpeed = 0.05f;        // 高频闪烁基础速度
        public float burstSpeedRandomFactor = 0.3f; // 高频闪烁间隔随机范围（越大越乱）
        public float burstInterval = 5f;        // 高频闪烁触发间隔

        [Header("Audio Settings")]
        public float burstVolume = 0.5f;         // 高频时音量
        public float fadeOutTime = 1.0f;         // 淡出原音量时间（秒）

        private UnityEngine.Light _light;
        private float timer;
        private bool isBursting = false;
        private float burstTimer;
        private float nextBurstTime;
        private float _audioOriginalVolume;
        private float fadeTimer = 0f;
        private bool fadingBack = false;

        private void Awake()
        {
            _light = GetComponent<UnityEngine.Light>();
            nextBurstTime = Time.time + burstInterval * Random.Range(0.5f, 1.5f);
            if (audioSource != null)
                _audioOriginalVolume = audioSource.volume;
        }

        private void Update()
        {
            // 检查是否进入高频闪烁
            if (!isBursting && Time.time >= nextBurstTime)
            {
                isBursting = true;
                burstTimer = burstDuration;
                nextBurstTime = Time.time + burstInterval * Random.Range(0.8f, 1.5f);

                if (audioSource != null)
                {
                    audioSource.volume = burstVolume;
                    fadingBack = false;
                }
            }

            if (!isBursting)
            {
                // 普通闪烁
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    _light.intensity = Random.Range(minIntensity, maxIntensity);
                    timer = flickerSpeed * Random.Range(0.5f, 1.5f);
                }

                // 音量缓慢回原
                if (fadingBack && audioSource != null)
                {
                    fadeTimer += Time.deltaTime / fadeOutTime;
                    audioSource.volume = Mathf.Lerp(burstVolume, _audioOriginalVolume, fadeTimer);
                    if (fadeTimer >= 1f) fadingBack = false;
                }
            }
            else
            {
                // 高频闪烁中
                burstTimer -= Time.deltaTime;
                if (burstTimer > 0f)
                {
                    _light.intensity = Random.Range(burstMinIntensity, burstMaxIntensity);
                    // 随机下次闪烁时间
                    timer = burstSpeed * Random.Range(1f - burstSpeedRandomFactor, 1f + burstSpeedRandomFactor);
                }
                else
                {
                    isBursting = false;
                    fadeTimer = 0f;
                    fadingBack = true;
                }
            }
        }
    }
}
