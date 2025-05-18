using UnityEngine;
using System.Collections.Generic; // If using dictionary later

/// <summary>
/// Singleton class to hold references to all TileType ScriptableObject assets.
/// 모든 TileType ScriptableObject 에셋 참조를 보유하는 싱글톤 클래스입니다.
/// Provides easy access to specific tile types throughout the game.
/// 게임 전체에서 특정 타일 타입에 쉽게 접근할 수 있도록 제공합니다.
/// </summary>
public class TileDatabase : MonoBehaviour
{
    // Singleton instance
    public static TileDatabase Instance { get; private set; }

    // --- Inspector Assigned TileTypes ---

    [Header("Dungeon - Core")] // 던전 - 핵심 타일
    [Tooltip("Assign the 'Floor' TileType ScriptableObject here.")]
    public TileType Floor;
    [Tooltip("Assign the 'Wall' TileType ScriptableObject here.")]
    public TileType Wall; // 기본 벽
    [Tooltip("Assign the 'StairsDown' TileType ScriptableObject here.")]
    public TileType StairsDown;
    [Tooltip("Assign the 'StairsUp' TileType ScriptableObject here.")]
    public TileType StairsUp;

    [Header("Dungeon - Wall Variations (Optional)")] // 던전 - 벽 변형 (선택 사항)
    [Tooltip("Assign the 'WallMossy' TileType ScriptableObject here.")]
    public TileType WallMossy;
    [Tooltip("Assign the 'WallBrick' TileType ScriptableObject here.")]
    public TileType WallBrick;
    [Tooltip("Assign the 'WallRough' TileType ScriptableObject here.")]
    public TileType WallRough;
    // public TileType WallCracked; // Add more variations if created
    // public TileType WallSmooth;
    // public TileType WallOrnate;

    [Header("Field - Natural Tiles")] // 필드 - 자연 타일
    [Tooltip("Assign the 'Grass' TileType ScriptableObject here.")]
    public TileType Grass; // 잔디
    [Tooltip("Assign the 'Water' TileType ScriptableObject here.")]
    public TileType Water; // 물
    [Tooltip("Assign the 'Tree' TileType ScriptableObject here.")]
    public TileType Tree; // 나무
    [Tooltip("Assign the 'Rock' TileType ScriptableObject here.")]
    public TileType Rock; // 바위
    // Add more natural tiles like Sand, Dirt, Flowers etc. if needed
    // 필요시 모래, 흙, 꽃 등 추가 자연 타일 추가

    [Header("Special / UI Tiles")] // 특수 / UI 타일
    [Tooltip("Assign the 'Shroud' TileType ScriptableObject here (for unseen areas).")]
    public TileType Shroud; // 안 보이는 영역
    [Tooltip("Assign the 'OutOfBounds' TileType ScriptableObject here (for areas outside map).")]
    public TileType OutOfBounds; // 맵 밖 영역

    // --- End Inspector Assigned ---


    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional
        }
        else
        {
            Debug.LogWarning("Duplicate TileDatabase instance found. Destroying self.");
            Destroy(gameObject);
            return; // Prevent further execution in the duplicate instance
        }

        // --- Sanity Checks (Optional but recommended) ---
        // 필수 타일들이 할당되었는지 확인 (선택 사항이지만 권장)
        if (Floor == null || Wall == null || StairsDown == null || StairsUp == null ||
            Grass == null || Water == null || Tree == null || Rock == null ||
            Shroud == null || OutOfBounds == null)
        {
            Debug.LogError("Essential TileType assets are not assigned in the TileDatabase Inspector! Please assign all required TileTypes.");
            // Optionally disable the GameManager or related components here
            // this.enabled = false; // Or GameManager.Instance.enabled = false; if preferred
        }
    }
}
