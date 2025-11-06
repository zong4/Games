using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Text
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        public int maxStringLength = 20;

        [Header("Text Colors")]
        public Color normalColor = Color.white;
        public Color errorColor = Color.red;
        public float errorFlashTime = 0.15f;

        [Header("Typing Animation")]
        public float rotationAngle = 3f;
        public List<Transform> fingers;
        public AudioSource audioSource;
        public List<AudioClip> typingSounds;

        private TextMeshPro _textMeshPro;
        private string _currentInput = "";

        // 退格控制
        private const float InitialDelay = 0.5f;
        private const float RepeatRate = 0.05f;
        private float _timeUntilRepeat = 0f;
        private bool _isRepeating = false;
        private Coroutine _flashCoroutine;

        private char _lastChar;
        private Transform _fingerTransform;

        // ===== Glitch（输入干扰）系统 =====
        private bool _glitchEnabled = false;     // 是否启用干扰
        private int _glitchTimesLeft = 0;        // 剩余干扰次数
        private System.Random _random = new System.Random();

        private void Start()
        {
            _textMeshPro = GetComponent<TextMeshPro>();
        }

        private void Update()
        {
            HandleCharacterInput();
            HandleSpecialKeys();

            if (_textMeshPro)
                _textMeshPro.text = _currentInput + "_";
        }

        /// <summary>
        /// 处理普通输入与干扰输入
        /// </summary>
        private void HandleCharacterInput()
        {
            foreach (var c in Input.inputString)
            {
                // 指尖动画
                if (c != _lastChar)
                {
                    RestoreFinger(_fingerTransform);
                    _fingerTransform = MoveFinger();
                    _lastChar = c;
                }

                // 排除控制字符
                if (c == '\n' || c == '\r' || c == '\b' || c == '\t')
                    continue;

                // ✅ 允许输入字母、数字、空格
                if (char.IsLetterOrDigit(c) || c == ' ')
                {
                    if (_currentInput.Length < maxStringLength)
                    {
                        char finalChar = c;

                        // 🎭 若启用干扰模式
                        if (_glitchEnabled && _glitchTimesLeft > 0)
                        {
                            finalChar = GetNearbyChar(c); // 替换为“邻近”字母
                            _glitchTimesLeft--;

                            // 用完次数后关闭
                            if (_glitchTimesLeft <= 0)
                                _glitchEnabled = false;
                        }

                        _currentInput += finalChar;
                    }
                }
                else
                {
                    // ❌ 输入非法字符时闪红
                    if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                    _flashCoroutine = StartCoroutine(FlashErrorTwice());
                }
            }

            // 无按键时恢复手指
            if (!Input.anyKey)
            {
                RestoreFinger(_fingerTransform);
                _lastChar = '\0';
            }
        }

        /// <summary>
        /// 退格逻辑（支持长按）
        /// </summary>
        private void HandleSpecialKeys()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                RestoreFinger(_fingerTransform);
                _fingerTransform = MoveFinger();
                _lastChar = '\b';

                DeleteLastCharacter();
                _timeUntilRepeat = Time.time + InitialDelay;
                _isRepeating = true;
            }
            else if (Input.GetKey(KeyCode.Backspace))
            {
                _lastChar = '\b';
                if (_isRepeating && Time.time >= _timeUntilRepeat)
                {
                    DeleteLastCharacter();
                    _timeUntilRepeat = Time.time + RepeatRate;
                }
            }
            else if (Input.GetKeyUp(KeyCode.Backspace))
            {
                RestoreFinger(_fingerTransform);
                _lastChar = '\0';

                _isRepeating = false;
                _timeUntilRepeat = 0f;
            }
        }

        /// <summary> 删除最后一个字符 </summary>
        private void DeleteLastCharacter()
        {
            if (_currentInput.Length > 0)
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
        }

        public void CleanInput() => _currentInput = "";

        public string GetInput() => _currentInput;

        /// <summary> 非法字符时闪红两次 </summary>
        private IEnumerator FlashErrorTwice()
        {
            if (_textMeshPro == null) yield break;

            for (int i = 0; i < 2; i++)
            {
                _textMeshPro.color = errorColor;
                yield return new WaitForSeconds(errorFlashTime);
                _textMeshPro.color = normalColor;
                yield return new WaitForSeconds(errorFlashTime);
            }
        }

        // ==================== 手指动画与音效 ====================

        private Transform MoveFinger()
        {
            if (fingers == null || fingers.Count == 0) return null;

            var finger = fingers[Random.Range(0, fingers.Count)];
            finger.Rotate(Vector3.forward, rotationAngle);

            PlaySound();
            return finger;
        }

        private void RestoreFinger(Transform finger)
        {
            if (finger == null)
                return;

            finger.Rotate(Vector3.forward, -rotationAngle);
            _fingerTransform = null;
        }

        private void PlaySound()
        {
            if (audioSource == null || typingSounds.Count == 0) return;

            var clip = typingSounds[Random.Range(0, typingSounds.Count)];
            audioSource.clip = clip;
            audioSource.Play();
        }

        // ==================== 干扰系统核心 ====================

        /// <summary>
        /// 启用输入干扰模式：接下来 N 次输入会被替换为错误字符
        /// </summary>
        public void EnableInputGlitch(int times)
        {
            _glitchEnabled = true;
            _glitchTimesLeft = times;
            Debug.Log($"[InputHandler] Glitch mode enabled for {times} keystrokes!");
        }

        /// <summary>
        /// 将输入字符替换为“相邻”字母，例如 f→d 或 g
        /// </summary>
        private char GetNearbyChar(char c)
        {
            if (!char.IsLetter(c)) return c;

            int offset = _random.Next(0, 2) == 0 ? -1 : 1;
            char result = (char)(c + offset);

            if (!char.IsLetter(result))
                result = c;

            return result;
        }
    }
}
