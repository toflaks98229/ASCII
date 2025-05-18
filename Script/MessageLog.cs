using UnityEngine;
using System.Collections.Generic;
using System.Text; // For StringBuilder
using TMPro;       // For TextMeshProUGUI

/// <summary>
/// Represents a single message with text, color, and repetition count.
/// �ؽ�Ʈ, ����, �ݺ� Ƚ���� ���� ���� �޽����� ��Ÿ���ϴ�.
/// </summary>
public class Message
{
    public string Text { get; private set; }    // The message content �޽��� ����
    public Color Color { get; private set; }   // The display color ǥ�� ����
    public int Count { get; set; }             // How many times this message repeated consecutively �� �޽����� �������� �� �� �ݺ��Ǿ��°�

    /// <summary>
    /// Constructor for a new message.
    /// �� �޽��� ������.
    /// </summary>
    public Message(string text, Color color)
    {
        Text = text;
        Color = color;
        Count = 1; // Starts with a count of 1 Ƚ�� 1�� ����
    }

    /// <summary>
    /// Gets the full message text, including the count if greater than 1.
    /// Ƚ���� 1���� ũ�� Ƚ���� ������ ��ü �޽��� �ؽ�Ʈ�� �����ɴϴ�.
    /// </summary>
    public string FullText => Count > 1 ? $"{Text} (x{Count})" : Text;
}

/// <summary>
/// Manages a list of game messages and handles rendering them to a TextMeshProUGUI element.
/// ���� �޽��� ����� �����ϰ� TextMeshProUGUI ��ҿ� �������ϴ� ���� ó���մϴ�.
/// </summary>
public class MessageLog
{
    private readonly List<Message> messages;      // Internal list of messages ���� �޽��� ���
    private readonly int maxMessages;             // Maximum number of messages to keep ������ �ִ� �޽��� ��

    /// <summary>
    /// Constructor for the MessageLog.
    /// MessageLog ������.
    /// </summary>
    /// <param name="maxMessages">The maximum number of messages to store. ������ �ִ� �޽��� ��.</param>
    public MessageLog(int maxMessages = 100) // Default to 100 messages �⺻�� 100�� �޽���
    {
        messages = new List<Message>();
        this.maxMessages = maxMessages;
    }

    /// <summary>
    /// Adds a new message to the log.
    /// �α׿� �� �޽����� �߰��մϴ�.
    /// </summary>
    /// <param name="text">The message text. �޽��� �ؽ�Ʈ.</param>
    /// <param name="color">The message color. �޽��� ����.</param>
    /// <param name="stack">If true, increments the count of the last message if it's identical. true�̸� ������ �޽����� ������ ��� Ƚ���� ������ŵ�ϴ�.</param>
    public void AddMessage(string text, Color color, bool stack = true)
    {
        // Check if stacking is enabled and if the new message is the same as the last one
        // ����ŷ�� Ȱ��ȭ�Ǿ�����, �� �޽����� ������ �޽����� �������� Ȯ��
        if (stack && messages.Count > 0 && messages[messages.Count - 1].Text == text && messages[messages.Count - 1].Color == color)
        {
            messages[messages.Count - 1].Count++; // Increment count ī��Ʈ ����
        }
        else // Add as a new message �� �޽����� �߰�
        {
            // Remove the oldest message if the log is full �αװ� ���� á���� ���� ������ �޽��� ����
            if (messages.Count >= maxMessages)
            {
                messages.RemoveAt(0);
            }
            messages.Add(new Message(text, color)); // Add the new message �� �޽��� �߰�
        }
    }

    /// <summary>
    /// Renders the message log to the specified TextMeshProUGUI component.
    /// ������ TextMeshProUGUI ������Ʈ�� �޽��� �α׸� �������մϴ�.
    /// </summary>
    /// <param name="logTMP">The TextMeshProUGUI element to display the log on. �α׸� ǥ���� TMP ���.</param>
    /// <param name="maxLines">The maximum number of lines to display. ǥ���� �ִ� �� ��.</param>
    public void Render(TextMeshProUGUI logTMP, int maxLines)
    {
        if (logTMP == null) return; // Do nothing if the TMP element isn't assigned TMP ��Ұ� �Ҵ���� �ʾ����� �ƹ��͵� �� ��

        logTMP.text = ""; // Clear previous content ���� ���� �����
        StringBuilder logBuilder = new StringBuilder();
        int linesRendered = 0;

        // Iterate backwards through messages to show the newest first
        // �ֽ� �޽����� ���� �����ֱ� ���� �޽����� �������� �ݺ�
        for (int i = messages.Count - 1; i >= 0 && linesRendered < maxLines; i--)
        {
            Message message = messages[i];
            // Format the message with Rich Text color tags Rich Text ���� �±׷� �޽��� ���� ����
            string formattedMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(message.Color)}>{message.FullText}</color>";

            // Simple line break handling (more complex wrapping might be needed for long messages)
            // ������ �� �ٲ� ó�� (�� �޽����� ��� �� ������ �� �ٲ��� �ʿ��� �� ����)
            logBuilder.Insert(0, formattedMessage + "\n"); // Insert at the beginning �� �տ� ����
            linesRendered++;
        }

        // Set the text, removing the trailing newline ������ �� �ٲ� ���� �� �ؽ�Ʈ ����
        logTMP.text = logBuilder.ToString().TrimEnd('\n');
    }
}
