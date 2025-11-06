using System.Collections.Generic;
using UnityEngine;

public class GlobalAnswerStorage : MonoBehaviour
{
    public static GlobalAnswerStorage Instance;  // 单例引用

    public List<string> playerAnswers = new List<string>();  // 存储所有玩家输入答案

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景也保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddAnswer(string answer)
    {
        playerAnswers.Add(answer);
        Debug.Log($"[GlobalAnswerStorage] Recorded answer: {answer}");
    }
}
