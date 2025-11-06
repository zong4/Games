using System;
using UnityEngine;

namespace Shaders
{
    public class DissolveController : MonoBehaviour
    {
        [Header("Target Material")]
        public Material dissolveMaterial;

        [Header("Solving Parameters")]
        public KeyCode triggerKey = KeyCode.Space; // 按下触发
        public float dissolveSpeed = 0.03f;         // Threshold 增长速度
        public float glowIntensity = 0.2f;
        public Color glowColor = Color.red;
        private float _threshold = 0f;
        private float _glowIntensity;
        private Color _glowColor;
        private bool _isDissolving = false;

        private void Start()
        {
            _threshold = 0f;
            _glowIntensity = glowIntensity;
            dissolveMaterial.SetFloat("_Threshold", _threshold);
            dissolveMaterial.SetFloat("_GlowIntensity", glowIntensity);
            dissolveMaterial.SetColor("_GlowColor", glowColor);
        }

        private void OnValidate()
        {
            _threshold = 0f;
            _glowIntensity = glowIntensity;
            dissolveMaterial.SetFloat("_Threshold", _threshold);
            dissolveMaterial.SetFloat("_GlowIntensity", glowIntensity);
            dissolveMaterial.SetColor("_GlowColor", glowColor);
        }

        void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                _isDissolving = true;
                _threshold = 0f;
                _glowIntensity = glowIntensity;
                dissolveMaterial.SetFloat("_Threshold", _threshold);
                dissolveMaterial.SetFloat("_GlowIntensity", glowIntensity);
                dissolveMaterial.SetColor("_GlowColor", glowColor);
            }

            if (!_isDissolving)
                return;

            // 随时间增加 Threshold
            _threshold += Time.deltaTime * dissolveSpeed;
            _threshold = Mathf.Clamp01(_threshold);

            // Glow强度随Threshold变化，可做循环或线性变化
            _glowIntensity -= Time.deltaTime * dissolveSpeed * glowIntensity;
            _glowIntensity = Mathf.Clamp01(_glowIntensity);
            
            _glowColor = Color.Lerp(glowColor, Color.black, _threshold);

            // 更新材质参数
            dissolveMaterial.SetFloat("_Threshold", _threshold);
            dissolveMaterial.SetFloat("_GlowIntensity", _glowIntensity);
            dissolveMaterial.SetColor("_GlowColor", _glowColor);

            // 可选：当Threshold达到1时停止
            if (_threshold >= 1f)
                _isDissolving = false;
        }
    }
}