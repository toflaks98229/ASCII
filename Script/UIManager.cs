using UnityEngine;
using TMPro; // Required for TextMeshPro ���ӽ����̽� ���
using System.Text; // Only needed if UIManager builds the map string ���� �� ���ڿ� ���� ��� �ʿ�
using System.Collections.Generic; // Only needed if UIManager manages messages directly ���� �޽��� ������ ��� �ʿ�

/// <summary>
/// Manages all User Interface elements in the game using a Singleton pattern.
/// �̱��� ������ ����Ͽ� ������ ��� ����� �������̽� ��Ҹ� �����մϴ�.
/// Updates the map display, player info panel, message log, and level display.
/// �� ���÷���, �÷��̾� ���� �г�, �޽��� �α�, �� ǥ�ø� ������Ʈ�մϴ�.
/// </summary>
public class UIManager : MonoBehaviour
{
    // Singleton instance �̱��� �ν��Ͻ�
    public static UIManager Instance { get; private set; }

    [Header("Map Display UI")]
    [Tooltip("The TextMeshPro component for displaying the map.")]
    [SerializeField] private TextMeshProUGUI mapDisplayTMP;
    [Tooltip("Font size for the map display.")]
    [SerializeField] private float mapFontSize = 14f;
    [Tooltip("Character spacing for the map display.")]
    [SerializeField] private float mapCharacterSpacing = -2f; // Adjust for tighter fit �����ϰ� ���߱� ���� ����

    [Header("Player Info Panel UI")]
    [Tooltip("Text component for player name and level.")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [Tooltip("Text component for player health (HP).")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("Text component for player stats (STR, DEX, INT).")]
    [SerializeField] private TextMeshProUGUI statsText;
    [Tooltip("Text component for the current dungeon level.")]
    [SerializeField] private TextMeshProUGUI levelText; // ���� ���� �� �ؽ�Ʈ

    [Header("Message Log UI")]
    [Tooltip("The TextMeshPro component for displaying messages.")]
    [SerializeField] private TextMeshProUGUI messageLogTMP;
    [Tooltip("Maximum number of messages to store in the log.")]
    [SerializeField] private int maxLogMessages = 100; // ������ �ִ� �޽��� ��
    [Tooltip("Maximum number of lines to display on screen.")]
    [SerializeField] private int displayLogLines = 5;  // ȭ�鿡 ǥ���� �� ��

    // Internal MessageLog instance ���� MessageLog �ν��Ͻ�
    private MessageLog messageLog;

    // Awake is called when the script instance is being loaded
    // ��ũ��Ʈ �ν��Ͻ��� �ε�� �� Awake�� ȣ��˴ϴ�
    void Awake()
    {
        // Singleton pattern implementation �̱��� ���� ����
        if (Instance == null) { Instance = this; /* DontDestroyOnLoad(gameObject); */ }
        else { Debug.LogWarning("Duplicate UIManager instance found. Destroying self."); Destroy(gameObject); return; }

        InitializeUI(); // Initialize UI components UI ������Ʈ �ʱ�ȭ
    }

    /// <summary>
    /// Initializes UI elements and the message log.
    /// UI ��� �� �޽��� �α׸� �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeUI()
    {
        // Setup Map Display �� ���÷��� ����
        if (mapDisplayTMP != null)
        {
            mapDisplayTMP.richText = true; mapDisplayTMP.fontSize = mapFontSize;
            mapDisplayTMP.characterSpacing = mapCharacterSpacing; mapDisplayTMP.text = "";
            mapDisplayTMP.alignment = TextAlignmentOptions.TopLeft;
        }
        else { Debug.LogError("UIManager: Map Display TMP reference is missing!"); }

        // Initialize Message Log �޽��� �α� �ʱ�ȭ
        messageLog = new MessageLog(maxLogMessages);
        if (messageLogTMP != null)
        {
            messageLogTMP.text = ""; messageLogTMP.alignment = TextAlignmentOptions.BottomLeft;
        }
        else { Debug.LogError("UIManager: Message Log TMP reference is missing!"); }

        // Initialize Player Info Panel �÷��̾� ���� �г� �ʱ�ȭ
        if (playerNameText != null) playerNameText.text = "Player";
        else { Debug.LogError("UIManager: Player Name Text reference is missing!"); }
        if (healthText != null) healthText.text = "HP: --/--";
        else { Debug.LogError("UIManager: Health Text reference is missing!"); }
        if (statsText != null) statsText.text = "STR: -- DEX: -- INT: --";
        else { Debug.LogError("UIManager: Stats Text reference is missing!"); }
        if (levelText != null) levelText.text = "Floor: --"; // �ʱ� �� �ؽ�Ʈ
        else { Debug.LogError("UIManager: Level Text reference is missing!"); }
    }

    /// <summary>
    /// Updates the map display TextMeshPro component with the provided map string.
    /// ������ �� ���ڿ��� �� ���÷��� TextMeshPro ������Ʈ�� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateMapDisplay(string mapString)
    {
        if (mapDisplayTMP != null) { mapDisplayTMP.text = mapString; }
        else { Debug.LogError("UIManager: Cannot update map display, TMP reference is missing!"); }
    }

    /// <summary>
    /// Updates the player information panel UI elements based on the Player data.
    /// Player �����͸� ������� �÷��̾� ���� �г� UI ��Ҹ� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdatePlayerInfo(Player player)
    {
        if (player == null)
        {
            if (playerNameText != null) playerNameText.text = "N/A";
            if (healthText != null) healthText.text = "HP: --/--";
            if (statsText != null) statsText.text = "STR: -- DEX: -- INT: --";
            return;
        }
        if (playerNameText != null) playerNameText.text = $"{player.entityName} (Lvl {player.Level})";
        if (healthText != null) healthText.text = $"HP: {player.CurrentHealth} / {player.MaxHealth}";
        if (statsText != null) statsText.text = $"STR: {player.Strength} DEX: {player.Dexterity} INT: {player.Intelligence}";
        // TODO: Update other player info UI elements (Mana, XP, etc.)
    }

    /// <summary>
    /// Updates the dungeon level display text.
    /// ���� �� ǥ�� �ؽ�Ʈ�� ������Ʈ�մϴ�.
    /// </summary>
    /// <param name="level">The current dungeon level number. ���� ���� �� ��ȣ.</param>
    public void UpdateLevelDisplay(int level)
    {
        if (levelText != null) { levelText.text = $"Floor: {level}"; }
        else { Debug.LogError("UIManager: Cannot update level display, Level Text reference is missing!"); }
    }


    /// <summary>
    /// Adds a message to the message log and updates the display.
    /// �޽��� �α׿� �޽����� �߰��ϰ� ǥ�ø� ������Ʈ�մϴ�.
    /// </summary>
    public void AddMessage(string text, Color color, bool stack = true)
    {
        if (messageLog != null)
        {
            messageLog.AddMessage(text, color, stack);
            UpdateMessageLogDisplay();
        }
        else { Debug.LogError("UIManager: Cannot add message, MessageLog is null!"); }
    }

    /// <summary>
    /// Renders the current messages from the MessageLog to the message log TMP element.
    /// MessageLog�� ���� �޽����� �޽��� �α� TMP ��ҿ� �������մϴ�.
    /// </summary>
    private void UpdateMessageLogDisplay()
    {
        if (messageLog != null && messageLogTMP != null)
        {
            messageLog.Render(messageLogTMP, displayLogLines);
        }
        else { Debug.LogError("UIManager: Cannot update message log display, MessageLog or TMP reference is missing!"); }
    }
}
