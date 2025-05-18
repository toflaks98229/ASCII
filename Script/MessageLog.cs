using UnityEngine;
using System.Collections.Generic;
using System.Text; // For StringBuilder
using TMPro;       // For TextMeshProUGUI

/// <summary>
/// Represents a single message with text, color, and repetition count.
/// 텍스트, 색상, 반복 횟수를 가진 단일 메시지를 나타냅니다.
/// </summary>
public class Message
{
    public string Text { get; private set; }    // The message content 메시지 내용
    public Color Color { get; private set; }   // The display color 표시 색상
    public int Count { get; set; }             // How many times this message repeated consecutively 이 메시지가 연속으로 몇 번 반복되었는가

    /// <summary>
    /// Constructor for a new message.
    /// 새 메시지 생성자.
    /// </summary>
    public Message(string text, Color color)
    {
        Text = text;
        Color = color;
        Count = 1; // Starts with a count of 1 횟수 1로 시작
    }

    /// <summary>
    /// Gets the full message text, including the count if greater than 1.
    /// 횟수가 1보다 크면 횟수를 포함한 전체 메시지 텍스트를 가져옵니다.
    /// </summary>
    public string FullText => Count > 1 ? $"{Text} (x{Count})" : Text;
}

/// <summary>
/// Manages a list of game messages and handles rendering them to a TextMeshProUGUI element.
/// 게임 메시지 목록을 관리하고 TextMeshProUGUI 요소에 렌더링하는 것을 처리합니다.
/// </summary>
public class MessageLog
{
    private readonly List<Message> messages;      // Internal list of messages 내부 메시지 목록
    private readonly int maxMessages;             // Maximum number of messages to keep 보관할 최대 메시지 수

    /// <summary>
    /// Constructor for the MessageLog.
    /// MessageLog 생성자.
    /// </summary>
    /// <param name="maxMessages">The maximum number of messages to store. 저장할 최대 메시지 수.</param>
    public MessageLog(int maxMessages = 100) // Default to 100 messages 기본값 100개 메시지
    {
        messages = new List<Message>();
        this.maxMessages = maxMessages;
    }

    /// <summary>
    /// Adds a new message to the log.
    /// 로그에 새 메시지를 추가합니다.
    /// </summary>
    /// <param name="text">The message text. 메시지 텍스트.</param>
    /// <param name="color">The message color. 메시지 색상.</param>
    /// <param name="stack">If true, increments the count of the last message if it's identical. true이면 마지막 메시지가 동일할 경우 횟수를 증가시킵니다.</param>
    public void AddMessage(string text, Color color, bool stack = true)
    {
        // Check if stacking is enabled and if the new message is the same as the last one
        // 스태킹이 활성화되었는지, 새 메시지가 마지막 메시지와 동일한지 확인
        if (stack && messages.Count > 0 && messages[messages.Count - 1].Text == text && messages[messages.Count - 1].Color == color)
        {
            messages[messages.Count - 1].Count++; // Increment count 카운트 증가
        }
        else // Add as a new message 새 메시지로 추가
        {
            // Remove the oldest message if the log is full 로그가 가득 찼으면 가장 오래된 메시지 제거
            if (messages.Count >= maxMessages)
            {
                messages.RemoveAt(0);
            }
            messages.Add(new Message(text, color)); // Add the new message 새 메시지 추가
        }
    }

    /// <summary>
    /// Renders the message log to the specified TextMeshProUGUI component.
    /// 지정된 TextMeshProUGUI 컴포넌트에 메시지 로그를 렌더링합니다.
    /// </summary>
    /// <param name="logTMP">The TextMeshProUGUI element to display the log on. 로그를 표시할 TMP 요소.</param>
    /// <param name="maxLines">The maximum number of lines to display. 표시할 최대 줄 수.</param>
    public void Render(TextMeshProUGUI logTMP, int maxLines)
    {
        if (logTMP == null) return; // Do nothing if the TMP element isn't assigned TMP 요소가 할당되지 않았으면 아무것도 안 함

        logTMP.text = ""; // Clear previous content 이전 내용 지우기
        StringBuilder logBuilder = new StringBuilder();
        int linesRendered = 0;

        // Iterate backwards through messages to show the newest first
        // 최신 메시지를 먼저 보여주기 위해 메시지를 역순으로 반복
        for (int i = messages.Count - 1; i >= 0 && linesRendered < maxLines; i--)
        {
            Message message = messages[i];
            // Format the message with Rich Text color tags Rich Text 색상 태그로 메시지 서식 지정
            string formattedMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(message.Color)}>{message.FullText}</color>";

            // Simple line break handling (more complex wrapping might be needed for long messages)
            // 간단한 줄 바꿈 처리 (긴 메시지의 경우 더 복잡한 줄 바꿈이 필요할 수 있음)
            logBuilder.Insert(0, formattedMessage + "\n"); // Insert at the beginning 맨 앞에 삽입
            linesRendered++;
        }

        // Set the text, removing the trailing newline 마지막 줄 바꿈 제거 후 텍스트 설정
        logTMP.text = logBuilder.ToString().TrimEnd('\n');
    }
}
