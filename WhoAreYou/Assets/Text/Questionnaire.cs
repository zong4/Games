using Camera;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Text
{
    [System.Serializable]
    public class InputGlitchSetting
    {
        [Tooltip("要触发输入干扰的题号（从 0 开始计数）")]
        public int questionIndex;

        [Tooltip("输入干扰出现的次数（多少次按键被篡改）")]
        public int glitchTimes = 5;
    }

    public class Questionnaire : MonoBehaviour
    {
        [System.Serializable]
        public class QuestionAnsweredEvent : UnityEvent<int, string> { }

        [Header("UI References")]
        public TextMeshPro questionText;
        public TextMeshPro answerText;

        private InputHandler _inputHandler;
        private int _currentQuestionIndex = 0;

        [Header("Question & Answer Data")]
        public List<string> questions;
        public List<string> allowedAnswers;

        [Header("Typing Settings")]
        public float typingSpeed = 0.04f;
        public float deleteSpeed = 0.03f;

        [Header("Text Colors")]
        public Color normalColor = Color.white;
        public Color errorColor = Color.red;

        [Header("Per-Question Events")]
        [Tooltip("为每个问题配置独立事件，长度与问题数量相同")]
        public List<QuestionAnsweredEvent> questionEvents = new List<QuestionAnsweredEvent>();

        // 🧩 新增：输入干扰配置（可在 Inspector 中编辑）
        [Header("Input Glitch Settings (可选)")]
        [Tooltip("在这里设置哪几题会启用输入干扰及其次数")]
        public List<InputGlitchSetting> glitchSettings = new List<InputGlitchSetting>();


        [Header("End Message")]
        [Tooltip("玩家完成问卷后显示的结束语")]
        private string endMessage1 = "So ";
        private string endMessage2 = ", Who are you?";



        private Coroutine typingCoroutine;
        private bool _isProcessingError = false;
        private bool _shouldSkipAdvance = false; // 控制是否跳题

        // 🧩 错误计数系统
        private int _wrongAnswerCount = 0;
        private const int MaxWrongAttempts = 5;

        public TurnAround turnAround;
        public CameraMove cameraMove;

        private void Start()
        {
            _inputHandler = answerText.GetComponent<InputHandler>();
            ShowQuestion();

            while (questionEvents.Count < questions.Count)
                questionEvents.Add(new QuestionAnsweredEvent());
        }

        private void Update()
        {

            if (_currentQuestionIndex >= questions.Count || _isProcessingError)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (CheckAnswer())
                {
                    string playerInput = GetCurrentPlayerInput();

                    _wrongAnswerCount = 0;
                    _shouldSkipAdvance = false;

                    if (_currentQuestionIndex < questionEvents.Count)
                        questionEvents[_currentQuestionIndex]?.Invoke(_currentQuestionIndex, playerInput);

                    if (_shouldSkipAdvance)
                        return;

                    _currentQuestionIndex++;

                    if (_currentQuestionIndex >= questions.Count)
                    {
                        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                        EndQuestionaire();
                        return;
                    }

                    ShowQuestion();
                    _inputHandler.CleanInput();
                }
                else
                {
                    StartCoroutine(HandleWrongAnswer());
                }
            }
        }

        // 🧩 新增统一输入获取方法
        private string GetCurrentPlayerInput(bool normalize = false)
        {
            string input = answerText.text ?? string.Empty;

            // 去掉 InputHandler 追加的游标 “_”
            if (input.EndsWith("_"))
                input = input.Substring(0, input.Length - 1);

            // 如果还是空，从 InputHandler 获取
            if (string.IsNullOrEmpty(input) && _inputHandler != null)
                input = _inputHandler.GetInput() ?? string.Empty;

            // ✅ 可选：是否转换为规范化（用于检测）
            if (normalize)
                return Normalize(input);
            else
                return input.Trim();
        }


        // 规范化：去首尾空白、统一大小写、去掉可能的零宽字符
        private string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            // 去掉常见零宽字符，防止意外粘贴
            s = s.Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
            return s.Trim().ToLowerInvariant();
        }



        private void ShowQuestion()
        {
            // 🧹 如果答案栏有内容，先逐字清空
            if (!string.IsNullOrEmpty(answerText.text))
            {
                StartCoroutine(ClearAnswerBeforeNextQuestion());
            }
            else
            {
                // 直接显示下一题
                StartCoroutine(ShowNextQuestionCoroutine());
            }
        }

        // 🧩 协程：逐字删除玩家答案后再显示下一题
        private IEnumerator ClearAnswerBeforeNextQuestion()
        {
            // 🔒 暂时禁用 InputHandler 防止它写回 "_"
            if (_inputHandler != null)
                _inputHandler.enabled = false;

            string currentText = answerText.text;

            // 去掉输入光标符号 _
            if (currentText.EndsWith("_"))
                currentText = currentText.Substring(0, currentText.Length - 1);

            // 逐字删除
            for (int i = currentText.Length; i > 0; i--)
            {
                answerText.text = currentText.Substring(0, i - 1);
                yield return new WaitForSeconds(deleteSpeed);
            }

            // ✅ 清除缓存与残留
            _inputHandler.CleanInput();
            answerText.text = string.Empty;
            answerText.ForceMeshUpdate();

            // ✅ 稍微延迟一下，避免出现空帧闪烁
            yield return new WaitForSeconds(0.1f);

            // 🟢 恢复输入
            if (_inputHandler != null)
                _inputHandler.enabled = true;

            // 然后显示下一题
            yield return StartCoroutine(ShowNextQuestionCoroutine());
        }


        // 🧩 协程：打出下一题内容 + 启用输入
        private IEnumerator ShowNextQuestionCoroutine()
        {
            // 停止上一个协程
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            // 禁用输入，防止玩家提前输入
            if (_inputHandler != null)
                _inputHandler.enabled = false;

            // 打字机输出题目
            typingCoroutine = StartCoroutine(TypeQuestion(questions[_currentQuestionIndex]));

            // 等待题目输出完成
            yield return typingCoroutine;

            // 检查是否要启用输入干扰
            foreach (var setting in glitchSettings)
            {
                if (setting.questionIndex == _currentQuestionIndex)
                {
                    _inputHandler.EnableInputGlitch(setting.glitchTimes);
                    Debug.Log($"[Questionnaire] 第 {_currentQuestionIndex + 1} 题启用输入干扰：{setting.glitchTimes} 次。");
                    break;
                }
            }

            // 恢复输入
            if (_inputHandler != null)
                _inputHandler.enabled = true;

            // 显示光标符号
            answerText.text = "_";
            answerText.ForceMeshUpdate();
        }



        private IEnumerator TypeQuestion(string fullText)
        {
            // 🔒 禁用玩家输入
            if (_inputHandler != null)
                _inputHandler.enabled = false;

            var textArea = questionText.GetComponent<RectTransform>();
            var width = textArea.rect.width;
            var height = textArea.rect.height;
            var length = fullText.Length;
            var charArea = (width * height) / Mathf.Max(1, length);
            var fontSize = Mathf.Clamp(Mathf.Sqrt(charArea) * 0.4f, 0f, 80f) * 35f;
            questionText.fontSize = fontSize;

            questionText.text = "";

            foreach (char c in fullText)
            {
                questionText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            // ✅ 打字完毕后恢复输入
            yield return new WaitForSeconds(0.1f); // 稍作延迟防止过早输入
            if (_inputHandler != null)
                _inputHandler.enabled = true;
        }


        private bool CheckAnswer()
        {
            // ✅ 这里要获取原始输入，不规范化
            string playerInput = GetCurrentPlayerInput(false);
            return CheckAnswer(playerInput);
        }


        private bool CheckAnswer(string externalInput)
        {
            if (_currentQuestionIndex >= allowedAnswers.Count)
                return true;

            // 允许答案集合（规范化后存成小写）
            var allowedSet = new HashSet<string>();
            foreach (var answer in allowedAnswers[_currentQuestionIndex].Split('/'))
            {
                var normalized = Normalize(answer);
                if (!string.IsNullOrEmpty(normalized))
                    allowedSet.Add(normalized);
            }

            // ✅ 玩家输入：检测时转小写，但保留原文
            string playerInputRaw = externalInput.Trim();
            string playerInputLower = Normalize(externalInput); // 转小写用于比较

            bool result = allowedSet.Count == 0 || allowedSet.Contains(playerInputLower);

            if (result)
            {
                // ✅ 保存原始输入（保持玩家原样，包括大小写）
                GlobalAnswerStorage.Instance?.AddAnswer(playerInputRaw);
            }

            return result;
        }



        private IEnumerator HandleWrongAnswer()
        {
            _isProcessingError = true;
            _inputHandler.enabled = false;

            string currentText = GetCurrentPlayerInput();

            if (string.IsNullOrEmpty(currentText))
                currentText = answerText.text;

            // 闪红
            for (int i = 0; i < 2; i++)
            {
                answerText.color = errorColor;
                yield return new WaitForSeconds(0.1f);
                answerText.color = normalColor;
                yield return new WaitForSeconds(0.1f);
            }

            // 退格删除
            for (int i = currentText.Length; i > 0; i--)
            {
                answerText.text = currentText.Substring(0, i - 1);
                yield return new WaitForSeconds(deleteSpeed);
            }

            _wrongAnswerCount++;

            if (_wrongAnswerCount >= MaxWrongAttempts)
            {
                _wrongAnswerCount = 0;
                StartCoroutine(AutoFillCorrectAnswer());
                yield break;
            }

            _inputHandler.CleanInput();
            answerText.color = normalColor;
            _inputHandler.enabled = true;
            _isProcessingError = false;
        }

        private IEnumerator AutoFillCorrectAnswer()
        {
            _isProcessingError = true;
            _inputHandler.enabled = false;

            string correctAnswer = "";
            if (_currentQuestionIndex < allowedAnswers.Count)
            {
                var parts = allowedAnswers[_currentQuestionIndex].Split('/');
                if (parts.Length > 0)
                    correctAnswer = parts[0].Trim();
            }

            answerText.text = "";
            answerText.color = Color.red;

            foreach (char c in correctAnswer)
            {
                answerText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            CheckAnswer(correctAnswer);

            yield return new WaitForSeconds(1f);
            answerText.color = normalColor;

            _currentQuestionIndex++;

            if (_currentQuestionIndex < questions.Count)
            {
                ShowQuestion();
                _inputHandler.CleanInput();
            }
            else
            {
                EndQuestionaire();
            }

            _inputHandler.enabled = true;
            _isProcessingError = false;
        }

        public void DisableInputForSeconds(float seconds)
        {
            if (_inputHandler != null)
            {
                _inputHandler.CleanInput();
                answerText.text = "_";
                answerText.ForceMeshUpdate();
            }

            StartCoroutine(DisableInputTemporarily(seconds));
        }

        private IEnumerator DisableInputTemporarily(float duration)
        {
            if (_inputHandler == null) yield break;

            _inputHandler.enabled = false;
            yield return new WaitForSeconds(duration);
            _inputHandler.enabled = true;
        }

        // ✅ 通用显示函数（增强版）
        private IEnumerator DisplayTextCoroutine(
    TMP_Text targetText,          // 输出目标（questionText 或 answerText）
    string newText,               // 要输出的文本
    Color textColor,              // 输出颜色
    bool isQuestionOutput,        // 是否为问题（true=questionText）
    bool autoAdvanceAfter = true, // 输出完是否跳题
    bool clearAnswerFirst = false // 是否在开始前清空答案框
)
        {
            _isProcessingError = true;
            _inputHandler.enabled = false;

            // 🧹 如果需要，先清空答案区域（带退格动画）
            if (clearAnswerFirst && answerText != null)
            {
                string currentAnswer = answerText.text;
                if (!string.IsNullOrEmpty(currentAnswer))
                {
                    for (int i = currentAnswer.Length; i > 0; i--)
                    {
                        answerText.text = currentAnswer.Substring(0, i - 1);
                        yield return new WaitForSeconds(deleteSpeed);
                    }
                }
                _inputHandler.CleanInput();
                answerText.text = string.Empty;
                answerText.ForceMeshUpdate();
            }

            // ❌ 如果是问题输出，不清空问题文字
            if (!isQuestionOutput)
            {
                // 🧹 清空当前输出目标（仅答案输出时才执行）
                string currentText = targetText.text;
                for (int i = currentText.Length; i > 0; i--)
                {
                    targetText.text = currentText.Substring(0, i - 1);
                    yield return new WaitForSeconds(deleteSpeed);
                }
            }

            // 🎨 输出新的文字
            Color originalColor = targetText.color;
            targetText.color = textColor;
            targetText.text = "";

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            // ✅ 问题文本 → 使用 TypeQuestion()（保持字体自适应）
            if (isQuestionOutput)
            {
                yield return StartCoroutine(TypeQuestion(newText));
            }
            else
            {
                // ✅ 答案文本 → 正常逐字输出
                foreach (char c in newText)
                {
                    targetText.text += c;
                    yield return new WaitForSeconds(typingSpeed);
                }
            }

            yield return new WaitForSeconds(1f);

            // 🎯 恢复颜色
            targetText.color = originalColor;

            // ✅ 跳题逻辑
            if (autoAdvanceAfter)
            {
                _currentQuestionIndex++;

                if (_currentQuestionIndex < questions.Count)
                {
                    ShowQuestion();
                    _inputHandler.CleanInput();
                }
                else
                {
                    EndQuestionaire();
                }
            }

            _inputHandler.enabled = true;
            _isProcessingError = false;
        }



        public void ReplaceAnswerWithRedText(string newAnswer)
        {
            _shouldSkipAdvance = true;
            StartCoroutine(DisplayTextCoroutine(answerText, newAnswer, errorColor, false, true));
        }

        public void ReplaceAnswerWithWhiteText(string newAnswer)
        {
            _shouldSkipAdvance = true;
            StartCoroutine(DisplayTextCoroutine(answerText, newAnswer, normalColor, false, true));
        }

        public void ShowRedQuestionText(string redText)
        {
            _shouldSkipAdvance = true;
            StartCoroutine(DisplayTextCoroutine(questionText, redText, errorColor, true, true, true));
        }

        public void ShowWhiteQuestionText(string whiteText)
        {
            _shouldSkipAdvance = true;
            StartCoroutine(DisplayTextCoroutine(questionText, whiteText, normalColor, true, true, true));
        }

        public void EndQuestionaire()
        {
            // questionText.text = endMessage;
            answerText.gameObject.SetActive(false);
            
            turnAround.GetComponent<RotateCamera>().enabled = false;
            if(turnAround.TriggerTurnAround())
            {
            }
        }

        public void OutputEndMessage()
        {
            string name = GlobalAnswerStorage.Instance.playerAnswers[0];
            if (name == "")
                name = "Unnamed";

            StartCoroutine(TypeQuestion(endMessage1 + name + endMessage2));
        }
    }
}
