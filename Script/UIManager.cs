using UnityEngine;
using TMPro; // Required for TextMeshPro 네임스페이스 사용
using System.Text; // Only needed if UIManager builds the map string 직접 맵 문자열 만들 경우 필요
using System.Collections.Generic; // Only needed if UIManager manages messages directly 직접 메시지 관리할 경우 필요

/// <summary>
/// Manages all User Interface elements in the game using a Singleton pattern.
/// 싱글톤 패턴을 사용하여 게임의 모든 사용자 인터페이스 요소를 관리합니다.
/// Updates the map display, player info panel, message log, and level display.
/// 맵 디스플레이, 플레이어 정보 패널, 메시지 로그, 층 표시를 업데이트합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    // Singleton instance 싱글톤 인스턴스
    public static UIManager Instance { get; private set; }

    [Header("Map Display UI")]
    [Tooltip("The TextMeshPro component for displaying the map.")]
    [SerializeField] private TextMeshProUGUI mapDisplayTMP;
    [Tooltip("Font size for the map display.")]
    [SerializeField] private float mapFontSize = 14f;
    [Tooltip("Character spacing for the map display.")]
    [SerializeField] private float mapCharacterSpacing = -2f; // Adjust for tighter fit 촘촘하게 맞추기 위해 조정

    [Header("Player Info Panel UI")]
    [Tooltip("Text component for player name and level.")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [Tooltip("Text component for player health (HP).")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("Text component for player stats (STR, DEX, INT).")]
    [SerializeField] private TextMeshProUGUI statsText;
    [Tooltip("Text component for the current dungeon level.")]
    [SerializeField] private TextMeshProUGUI levelText; // 현재 던전 층 텍스트

    [Header("Message Log UI")]
    [Tooltip("The TextMeshPro component for displaying messages.")]
    [SerializeField] private TextMeshProUGUI messageLogTMP;
    [Tooltip("Maximum number of messages to store in the log.")]
    [SerializeField] private int maxLogMessages = 100; // 보관할 최대 메시지 수
    [Tooltip("Maximum number of lines to display on screen.")]
    [SerializeField] private int displayLogLines = 5;  // 화면에 표시할 줄 수

    // Internal MessageLog instance 내부 MessageLog 인스턴스
    private MessageLog messageLog;

    // Awake is called when the script instance is being loaded
    // 스크립트 인스턴스가 로드될 때 Awake가 호출됩니다
    void Awake()
    {
        // Singleton pattern implementation 싱글톤 패턴 구현
        if (Instance == null) { Instance = this; /* DontDestroyOnLoad(gameObject); */ }
        else { Debug.LogWarning("Duplicate UIManager instance found. Destroying self."); Destroy(gameObject); return; }

        InitializeUI(); // Initialize UI components UI 컴포넌트 초기화
    }

    /// <summary>
    /// Initializes UI elements and the message log.
    /// UI 요소 및 메시지 로그를 초기화합니다.
    /// </summary>
    private void InitializeUI()
    {
        // Setup Map Display 맵 디스플레이 설정
        if (mapDisplayTMP != null)
        {
            mapDisplayTMP.richText = true; mapDisplayTMP.fontSize = mapFontSize;
            mapDisplayTMP.characterSpacing = mapCharacterSpacing; mapDisplayTMP.text = "";
            mapDisplayTMP.alignment = TextAlignmentOptions.TopLeft;
        }
        else { Debug.LogError("UIManager: Map Display TMP reference is missing!"); }

        // Initialize Message Log 메시지 로그 초기화
        messageLog = new MessageLog(maxLogMessages);
        if (messageLogTMP != null)
        {
            messageLogTMP.text = ""; messageLogTMP.alignment = TextAlignmentOptions.BottomLeft;
        }
        else { Debug.LogError("UIManager: Message Log TMP reference is missing!"); }

        // Initialize Player Info Panel 플레이어 정보 패널 초기화
        if (playerNameText != null) playerNameText.text = "Player";
        else { Debug.LogError("UIManager: Player Name Text reference is missing!"); }
        if (healthText != null) healthText.text = "HP: --/--";
        else { Debug.LogError("UIManager: Health Text reference is missing!"); }
        if (statsText != null) statsText.text = "STR: -- DEX: -- INT: --";
        else { Debug.LogError("UIManager: Stats Text reference is missing!"); }
        if (levelText != null) levelText.text = "Floor: --"; // 초기 층 텍스트
        else { Debug.LogError("UIManager: Level Text reference is missing!"); }
    }

    /// <summary>
    /// Updates the map display TextMeshPro component with the provided map string.
    /// 제공된 맵 문자열로 맵 디스플레이 TextMeshPro 컴포넌트를 업데이트합니다.
    /// </summary>
    public void UpdateMapDisplay(string mapString)
    {
        if (mapDisplayTMP != null) { mapDisplayTMP.text = mapString; }
        else { Debug.LogError("UIManager: Cannot update map display, TMP reference is missing!"); }
    }

    /// <summary>
    /// Updates the player information panel UI elements based on the Player data.
    /// Player 데이터를 기반으로 플레이어 정보 패널 UI 요소를 업데이트합니다.
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
    /// 던전 층 표시 텍스트를 업데이트합니다.
    /// </summary>
    /// <param name="level">The current dungeon level number. 현재 던전 층 번호.</param>
    public void UpdateLevelDisplay(int level)
    {
        if (levelText != null) { levelText.text = $"Floor: {level}"; }
        else { Debug.LogError("UIManager: Cannot update level display, Level Text reference is missing!"); }
    }


    /// <summary>
    /// Adds a message to the message log and updates the display.
    /// 메시지 로그에 메시지를 추가하고 표시를 업데이트합니다.
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
    /// MessageLog의 현재 메시지를 메시지 로그 TMP 요소에 렌더링합니다.
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
