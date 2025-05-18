// GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;       // For Action
using System.Collections; // For IEnumerator

/// <summary>
/// 게임의 주요 상태를 정의합니다.
/// Defines the possible states of the game's turn cycle.
/// </summary>
public enum GameState
{
    PlayerTurn,     // 플레이어 턴: 플레이어 입력 대기 상태
    EnemyTurn,      // 적 턴: 적 행동 진행 상태
    Paused,         // 일시정지: 게임이 일시 중지된 상태
    GameOver,       // 게임 오버: 플레이어 사망 상태
    GameWon,        // 게임 승리: 플레이어가 던전을 탈출한 상태 (또는 다른 승리 조건)
    SpellAnimation  // 마법 애니메이션: 마법 효과가 연출되는 상태
}

/// <summary>
/// 게임의 메인 컨트롤러 클래스입니다.
/// 게임 상태, 턴, 여러 던전 층, 플레이어를 관리하고 UI와 같은 다른 시스템을 조정합니다.
/// 싱글톤 패턴으로 구현되었습니다.
/// The main controller class for the game. Manages game state, turns, multiple dungeon levels,
/// player, and coordinates other systems like UI. Implemented as a Singleton.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton Instance // 싱글톤 인스턴스
    /// <summary>
    /// GameManager의 싱글톤 인스턴스입니다.
    /// Singleton instance of the GameManager.
    /// </summary>
    public static GameManager Instance { get; private set; }
    #endregion

    [Header("전투 설정 (Combat Settings)")]
    [Tooltip("기본 명중률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float baseHitChance = 0.8f; // 예: 80% 기본 명중률



    #region Inspector Variables // 인스펙터에서 설정 가능한 변수들

    // =========================================================================
    // 맵 생성 관련 설정 (Map Generation Settings)
    // =========================================================================
    [Header("맵 생성 설정 (Map Generation Settings)")]

    // -------------------------------------------------------------------------
    // 필드 맵 설정 (Field Map Settings) - 레벨 0 (지상)
    // -------------------------------------------------------------------------
    [Header("필드 맵 설정 (Level 0 - Surface)")]
    [Tooltip("필드 맵(레벨 0)의 너비 (타일 단위)")]
    [SerializeField] private int fieldMapWidth = 100;

    [Tooltip("필드 맵(레벨 0)의 높이 (타일 단위)")]
    [SerializeField] private int fieldMapHeight = 60;

    [Tooltip("필드 맵 생성 시 사용될 펄린 노이즈 스케일 값 (클수록 지형 변화가 부드러워짐)")]
    [SerializeField] private float fieldNoiseScale = 15f;

    [Tooltip("필드 맵에서 물(Water) 타일이 생성될 노이즈 임계값 (이 값보다 낮으면 물)")]
    [Range(0f, 1f)]
    [SerializeField] private float fieldWaterThreshold = 0.3f;

    [Tooltip("필드 맵에서 바위(Rock) 타일이 생성될 노이즈 임계값 (이 값보다 높으면 바위)")]
    [Range(0f, 1f)]
    [SerializeField] private float fieldRockThreshold = 0.7f;

    [Tooltip("필드 맵의 풀(Grass) 타일 위에 나무(Tree)가 생성될 대략적인 밀도 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float fieldTreeDensity = 0.1f;

    // -------------------------------------------------------------------------
    // 던전 맵 설정 (Dungeon Map Settings) - 레벨 1 이상
    // -------------------------------------------------------------------------
    [Header("던전 맵 설정 (Level 1+)")]
    [Tooltip("던전 맵(레벨 1 이상)의 너비 (타일 단위)")]
    [SerializeField] private int dungeonMapWidth = 80;

    [Tooltip("던전 맵(레벨 1 이상)의 높이 (타일 단위)")]
    [SerializeField] private int dungeonMapHeight = 50;

    [Tooltip("생성될 총 던전 레벨의 수 (필드 맵 제외)")]
    [SerializeField] private int totalDungeonLevels = 10;

    [Tooltip("각 던전 레벨마다 생성 시도할 최대 방의 개수")]
    [SerializeField] private int maxRooms = 15;

    [Tooltip("방의 최소 크기 (너비/높이, 타일 단위)")]
    [SerializeField] private int minRoomSize = 6;

    [Tooltip("방의 최대 크기 (너비/높이, 타일 단위)")]
    [SerializeField] private int maxRoomSize = 12;

    // -------------------------------------------------------------------------
    // 던전 벽 변형 설정 (Dungeon Wall Variation Settings) - 선택 사항
    // -------------------------------------------------------------------------
    [Header("던전 벽 변형 확률 (선택 사항)")]
    [Tooltip("표준 벽(Standard Wall)이 생성될 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float standardWallProbability = 0.7f;

    [Tooltip("이끼 낀 벽(Mossy Wall)이 생성될 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float mossyWallProbability = 0.1f;

    [Tooltip("벽돌 벽(Brick Wall)이 생성될 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float brickWallProbability = 0.1f;

    [Tooltip("거친 벽(Rough Wall)이 생성될 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float roughWallProbability = 0.1f;


    // =========================================================================
    // 시야 (FOV) 설정
    // =========================================================================
    [Header("시야(FOV) 설정")]
    [Tooltip("플레이어의 시야 반경 (타일 단위)")]
    [SerializeField] private int fovRadius = 8;


    // =========================================================================
    // 마법 (Spell) 설정
    // =========================================================================
    [Header("마법 설정 (Spell Settings)")]
    [Tooltip("디폴트 마법(스페이스바)의 사정거리 (타일 단위)")]
    [SerializeField] private int defaultSpellRange = 5;

    [Tooltip("디폴트 마법(스페이스바)의 기본 데미지")]
    [SerializeField] private int defaultSpellDamage = 3;


    // =========================================================================
    // 입력 (Input) 설정
    // =========================================================================
    [Header("입력 설정 (Input Settings)")]
    [Tooltip("키 반복 입력 시작 전 딜레이 시간 (초)")]
    [SerializeField] private float keyRepeatDelay = 0.3f;

    [Tooltip("키 반복 입력 간격 (초)")]
    [SerializeField] private float keyRepeatInterval = 0.05f;

    // =========================================================================
    // 프리팹 (Prefabs)
    // =========================================================================
    [Header("프리팹 (Prefabs)")]
    [Tooltip("플레이어 캐릭터 프리팹을 할당해주세요.")]
    [SerializeField] private GameObject playerPrefab;
    // TODO: 몬스터, 아이템 프리팹 추가 예정
    [Tooltip("던전 레벨당 최대 스폰할 몬스터 수 (테스트용)")]
    [SerializeField] private int maxMonstersPerLevel = 5;

    [Tooltip("모든 몬스터가 공통으로 사용할 'GenericMonster' 프리팹을 할당해주세요. 이 프리팹에는 Entity 또는 Monster 스크립트가 있어야 합니다.")]
    [SerializeField] private GameObject genericMonsterPrefab; // 특정 몬스터 프리팹 대신 범용 프리팹
    [Tooltip("현재 레벨에 스폰될 수 있는 몬스터 데이터(ScriptableObject) 목록입니다.")]
    [SerializeField] private List<MonsterData> availableMonstersForLevel = new List<MonsterData>(); // MonsterData 목록

    // =========================================================================
    // 컴포넌트 참조 (Component References)
    // =========================================================================
    [Header("컴포넌트 참조 (Component References)")]
    [Tooltip("씬에 있는 DungeonVisualizer 컴포넌트를 할당해주세요. 자동으로 찾기도 합니다.")]
    [SerializeField] private DungeonVisualizer dungeonVisualizer;
    #endregion

    #region Game State & Level Management Variables // 게임 상태 및 레벨 관리 변수
    /// <summary>
    /// 생성된 모든 맵 레벨 데이터를 저장하는 딕셔너리입니다. (키: 레벨 번호, 값: MapData)
    /// Dictionary storing all generated map level data. (Key: level number, Value: MapData)
    /// </summary>
    private Dictionary<int, MapData> mapLevels;
    /// <summary>
    /// 현재 플레이어가 위치한 레벨 번호입니다. (0: 필드, 1 이상: 던전)
    /// The current level number the player is on. (0: Field, 1+: Dungeon)
    /// </summary>
    private int currentLevel;
    /// <summary>
    /// 현재 활성화된 맵의 데이터입니다.
    /// Data for the currently active map.
    /// </summary>
    public MapData CurrentMapData => mapLevels.TryGetValue(currentLevel, out MapData map) ? map : null;
    /// <summary>
    /// 플레이어 객체 참조입니다.
    /// Reference to the player object.
    /// </summary>
    private Player player;
    /// <summary>
    /// DungeonVisualizer 등 외부에서 플레이어 객체에 안전하게 접근하기 위한 프로퍼티입니다.
    /// Property for safe external access to the player object (e.g., by DungeonVisualizer).
    /// </summary>
    public Player PlayerInstance => player;

    /// <summary>
    /// 현재 게임의 상태입니다. (예: 플레이어 턴, 적 턴 등)
    /// The current state of the game (e.g., PlayerTurn, EnemyTurn).
    /// </summary>
    private GameState currentGameState = GameState.PlayerTurn;
    /// <summary>
    /// 맵 생성 로직을 담당하는 DungeonGenerator 인스턴스입니다.
    /// Instance of DungeonGenerator responsible for map creation logic.
    /// </summary>
    private DungeonGenerator dungeonGenerator;

    /// <summary>
    /// 현재 재생 중인 마법 애니메이션의 데이터입니다. 없으면 null입니다.
    /// Data for the currently playing spell animation. Null if none.
    /// </summary>
    private SpellAnimationData? currentSpellAnimationData = null;
    /// <summary>
    /// 마법 애니메이션이 완료된 후 실행될 액션(콜백 함수)입니다.
    /// Action (callback function) to be executed after a spell animation completes.
    /// </summary>
    private Action onSpellAnimationCompleteAction = null;
    #endregion

    #region Input Handling Variables // 입력 처리 관련 변수
    /// <summary>
    /// 마지막 입력으로부터 경과된 시간 (키 반복 입력 처리에 사용)
    /// Time elapsed since the last input (used for key repeat handling).
    /// </summary>
    private float timeSinceLastInput = 0f;
    /// <summary>
    /// 현재 키가 눌린 지속 시간 (키 반복 입력 처리에 사용)
    /// Duration the current key has been pressed (used for key repeat handling).
    /// </summary>
    private float timeKeyPressed = 0f;
    /// <summary>
    /// 플레이어가 마지막으로 이동했거나 이동하려고 시도한 방향입니다. 디폴트 마법 발사 방향에 사용됩니다.
    /// The last direction the player moved or attempted to move. Used for default spell firing direction.
    /// </summary>
    private Vector2Int lastMoveDirection = new Vector2Int(0, -1); // 기본 방향: 위 (Y가 감소하는 방향이 위쪽인 맵 기준)
    #endregion

    #region Unity Lifecycle Methods // Unity 생명주기 메서드
    void Awake()
    {
       Application.targetFrameRate = 30; // FPS 설정 (옵션)

        // 싱글톤 인스턴스 설정
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // DungeonGenerator 인스턴스 생성 및 초기화
        dungeonGenerator = new DungeonGenerator(
            fieldMapWidth, fieldMapHeight, fieldNoiseScale, fieldWaterThreshold, fieldRockThreshold, fieldTreeDensity,
            dungeonMapWidth, dungeonMapHeight, maxRooms, minRoomSize, maxRoomSize, totalDungeonLevels,
            standardWallProbability, mossyWallProbability, brickWallProbability, roughWallProbability
        );

        // DungeonVisualizer 참조 설정 (Inspector에서 할당되지 않았다면 찾기)
        if (dungeonVisualizer == null) dungeonVisualizer = FindObjectOfType<DungeonVisualizer>();
        if (dungeonVisualizer == null) Debug.LogError("DungeonVisualizer component not found in the scene!");

        // 맵 레벨 데이터 딕셔너리 초기화
        mapLevels = new Dictionary<int, MapData>();
        // 게임 초기화 시작
        InitializeGame();
    }

    void Update()
    {
        // 필수 객체가 없거나 특정 게임 상태일 경우 Update 로직 건너뛰기
        if (player == null || CurrentMapData == null || currentGameState == GameState.Paused || currentGameState == GameState.GameOver || currentGameState == GameState.GameWon) return;

        // 현재 게임 상태에 따라 다른 로직 처리
        switch (currentGameState)
        {
            case GameState.PlayerTurn:
                HandlePlayerInput(); // 플레이어 입력 처리
                break;
            case GameState.EnemyTurn:
                ProcessEnemyTurns(); // 적 턴 처리
                break;
            case GameState.SpellAnimation:
 
                break;
        }
        RenderCurrentMap();
    }
    #endregion

    #region Initialization // 게임 초기화 관련 메서드
    /// <summary>
    /// 게임 시작 시 필요한 모든 요소를 초기화합니다.
    /// Initializes all necessary elements when the game starts.
    /// </summary>
    private void InitializeGame()
    {
        Debug.Log("Initializing Game...");
        mapLevels.Clear(); // 이전 맵 데이터 초기화
        currentLevel = 0;  // 시작 레벨 (0: 필드)

        // 첫 번째 레벨 생성
        MapData firstLevelData = dungeonGenerator.GenerateNewLevel(currentLevel);

        if (firstLevelData == null)
        {
            Debug.LogError("FATAL: Failed to generate starting level (Level 0 Field Map)! Disabling GameManager.");
            this.enabled = false; // 게임 매니저 비활성화
            return;
        }
        mapLevels[currentLevel] = firstLevelData; // 생성된 맵 저장

        // 플레이어 시작 위치 결정
        Vector2Int playerStartPosition = firstLevelData.PlayerSuggestedStartPosition;
        if (playerStartPosition == Vector2Int.one * -1) // 생성기가 위치를 못 정한 경우 대비
        {
            playerStartPosition = firstLevelData.GetFallbackStartPosition(currentLevel);
        }
        SpawnPlayer(playerStartPosition.x, playerStartPosition.y); // 플레이어 스폰

        if (player == null)
        {
            Debug.LogError("FATAL: Failed to spawn player! Disabling GameManager.");
            this.enabled = false; // 게임 매니저 비활성화
            return;
        }

        // 몬스터 스폰 (테스트용)
        SpawnMonstersForLevel(firstLevelData);

        CurrentMapData?.ComputePlayerFov(player.gridX, player.gridY, fovRadius);
        RenderCurrentMap();
        UIManager.Instance?.UpdatePlayerInfo(player);
        UIManager.Instance?.UpdateLevelDisplay(currentLevel);
        UIManager.Instance?.AddMessage("엘리아스에 오신 것을 환영합니다!", Color.green, false);
        currentGameState = GameState.PlayerTurn;

        // 초기 시야 계산 및 맵 렌더링
        CurrentMapData?.ComputePlayerFov(player.gridX, player.gridY, fovRadius);
        RenderCurrentMap();

        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerInfo(player);
            UIManager.Instance.UpdateLevelDisplay(currentLevel);
            UIManager.Instance.AddMessage("엘리아스에 오신 것을 환영합니다!", Color.green, false); // 시작 메시지
        }
        else
        {
            Debug.LogWarning("UIManager instance not found during initialization. UI updates will be skipped.");
        }

        Debug.Log($"Game Initialized on Level {currentLevel} (Field). Player at ({player.gridX},{player.gridY})");
        currentGameState = GameState.PlayerTurn; // 게임 상태를 플레이어 턴으로 설정
    }

    /// <summary>
    /// 현재 레벨에 몬스터를 스폰합니다. MonsterData를 사용합니다.
    /// Spawns monsters for the current level using MonsterData.
    /// </summary>
    /// <param name="mapData">몬스터를 스폰할 맵 데이터</param>
    private void SpawnMonstersForLevel(MapData mapData)
    {
        if (mapData == null || genericMonsterPrefab == null || availableMonstersForLevel == null || availableMonstersForLevel.Count == 0)
        {
            Debug.LogWarning("Cannot spawn monsters: MapData, GenericMonsterPrefab, or AvailableMonstersForLevel list is not set up correctly.");
            return;
        }

        int monstersSpawned = 0;
        for (int i = 0; i < 500; i++) // 최대 스폰 시도 횟수
        {
            if (monstersSpawned >= maxMonstersPerLevel) break;

            int randX = UnityEngine.Random.Range(0, mapData.Width);
            int randY = UnityEngine.Random.Range(0, mapData.Height);

            if (mapData.Tiles[randY, randX].walkable &&
                mapData.GetEntityAt(randX, randY) == null &&
                (player == null || (player.gridX != randX || player.gridY != randY)))
            {
                // 스폰할 몬스터 데이터 무작위 선택
                MonsterData selectedMonsterData = availableMonstersForLevel[UnityEngine.Random.Range(0, availableMonstersForLevel.Count)];
                if (selectedMonsterData == null) continue; // 혹시 모를 null 데이터 방지

                GameObject monsterObject = Instantiate(genericMonsterPrefab, Vector3.zero, Quaternion.identity);
                Entity monsterEntity = monsterObject.GetComponent<Entity>(); // 일반 Entity로 받거나, Monster 타입으로 캐스팅

                if (monsterEntity != null)
                {
                    monsterEntity.InitializeFromData(selectedMonsterData); // MonsterData로 초기화
                    monsterEntity.SetGridPosition(randX, randY);
                    mapData.AddEntity(monsterEntity);
                    monsterObject.name = $"{selectedMonsterData.monsterName}_{monstersSpawned}"; // 이름 설정
                    monstersSpawned++;
                    Debug.Log($"Spawned {selectedMonsterData.monsterName} at ({randX},{randY})");
                }
                else
                {
                    Debug.LogError("GenericMonsterPrefab is missing the Entity (or Monster) component!");
                    Destroy(monsterObject);
                }
            }
        }
        if (monstersSpawned < maxMonstersPerLevel)
        {
            Debug.LogWarning($"Spawned {monstersSpawned}/{maxMonstersPerLevel} monsters due to placement attempts limit or lack of space.");
        }
    }



    /// <summary>
    /// 지정된 위치에 플레이어를 스폰하거나 이동시킵니다.
    /// Spawns or moves the player to the specified position.
    /// </summary>
    /// <param name="startX">스폰할 X 좌표</param>
    /// <param name="startY">스폰할 Y 좌표</param>
    private void SpawnPlayer(int startX, int startY)
    {
        if (playerPrefab == null) { Debug.LogError("Player Prefab is missing in GameManager Inspector!"); return; }
        MapData mapToSpawnOn = CurrentMapData;
        if (mapToSpawnOn == null) { Debug.LogError($"Cannot spawn player, CurrentMapData for level {currentLevel} is null!"); return; }

        // 스폰 위치 유효성 검사 및 폴백 처리
        if (!mapToSpawnOn.IsInBounds(startX, startY) || mapToSpawnOn.Tiles[startY, startX] == null || !mapToSpawnOn.Tiles[startY, startX].walkable)
        {
            Debug.LogWarning($"Invalid player spawn position ({startX},{startY}) on level {currentLevel}. Attempting fallback.");
            Vector2Int fallbackPos = mapToSpawnOn.GetFallbackStartPosition(currentLevel);
            startX = fallbackPos.x; startY = fallbackPos.y;
            // 폴백 위치도 유효하지 않은 경우 심각한 오류
            if (!mapToSpawnOn.IsInBounds(startX, startY) || mapToSpawnOn.Tiles[startY, startX] == null || !mapToSpawnOn.Tiles[startY, startX].walkable)
            {
                Debug.LogError($"FATAL: Fallback player spawn position ({startX},{startY}) is also invalid on level {currentLevel}! Disabling GameManager.");
                this.enabled = false; return;
            }
        }

        if (player != null) // 플레이어가 이미 존재하면 (예: 레벨 이동 시)
        {
            Debug.Log($"Moving existing player to ({startX},{startY}) on level {currentLevel}");
            player.SetGridPosition(startX, startY); // 위치 업데이트
            // 현재 맵의 엔티티 리스트에 플레이어가 없으면 추가 (중복 방지)
            if (!mapToSpawnOn.Entities.Contains(player))
            {
                mapToSpawnOn.AddEntity(player);
            }
        }
        else // 플레이어가 존재하지 않으면 (게임 시작 시)
        {
            Debug.Log($"Spawning new player at ({startX},{startY}) on level {currentLevel}");
            GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity); // 프리팹으로부터 플레이어 생성
            player = playerObject.GetComponent<Player>(); // Player 컴포넌트 가져오기
            if (player != null)
            {
                player.SetGridPosition(startX, startY); // 위치 설정
                mapToSpawnOn.AddEntity(player);       // 현재 맵에 엔티티로 추가
                playerObject.name = "Player";         // 오브젝트 이름 설정
            }
            else
            {
                Debug.LogError("Player Prefab is missing the Player component!");
                Destroy(playerObject); // 잘못 생성된 오브젝트 제거
            }
        }
    }
    #endregion

    #region Level Management // 레벨 변경 및 관리 관련 메서드
    /// <summary>
    /// 현재 레벨에서 지정된 방향(위/아래)으로 레벨을 변경합니다.
    /// Changes the current level in the specified direction (up/down).
    /// </summary>
    /// <param name="direction">레벨 변경 방향 (+1: 아래로, -1: 위로)</param>
    private void ChangeLevel(int direction)
    {
        int targetLevel = currentLevel + direction; // 목표 레벨 계산

        // 레벨 경계 확인
        if (targetLevel < 0) { UIManager.Instance?.AddMessage("더 이상 위로 올라갈 수 없습니다.", Color.yellow); return; }
        if (targetLevel > totalDungeonLevels) { UIManager.Instance?.AddMessage("더 이상 아래로 내려갈 수 없습니다.", Color.yellow); return; } // 필드(0) + 던전 수

        MapData targetMapData;
        // 플레이어의 현재 위치 (다음 레벨 생성 시 이전 레벨에서의 계단 위치로 사용될 수 있음)
        Vector2Int playerCurrentPos = (player != null) ? new Vector2Int(player.gridX, player.gridY) : Vector2Int.zero;

        // 목표 레벨의 맵 데이터가 이미 생성되었는지 확인, 없으면 새로 생성
        if (!mapLevels.TryGetValue(targetLevel, out targetMapData))
        {
            Debug.Log($"Generating new map for level {targetLevel} (player was at {playerCurrentPos} on level {currentLevel})");
            targetMapData = dungeonGenerator.GenerateNewLevel(targetLevel, playerCurrentPos.x, playerCurrentPos.y);
            if (targetMapData == null) { UIManager.Instance?.AddMessage("길이 막혀있습니다!", Color.red); return; }
            mapLevels[targetLevel] = targetMapData; // 생성된 맵 저장

            // ★★★ 새로운 레벨이 생성되었을 때만 몬스터 스폰 ★★★
            // TODO: targetLevel에 따라 availableMonstersForLevel 목록을 다르게 설정하는 로직 추가 가능
            SpawnMonstersForLevel(targetMapData);
        }
        else
        {
            Debug.Log($"Loading existing map for level {targetLevel}");
        }

        // 목표 레벨에서의 플레이어 도착 위치 결정
        Vector2Int destination = Vector2Int.one * -1; // 초기값: 유효하지 않은 위치
        if (currentLevel == 0 && direction > 0) // 현재 필드(0)에서 던전(1)으로 이동
        {
            destination = targetMapData.UpStairsLocation; // 던전 1층의 위로 가는 계단 위치가 진입점
            if (destination == Vector2Int.one * -1)
            { // 위 계단이 없으면
                Debug.LogWarning($"Level {targetLevel} (entry from field) is missing UpStairs. Using PlayerSuggestedStartPosition.");
                destination = targetMapData.PlayerSuggestedStartPosition; // 생성기가 제안한 시작 위치 사용
            }
        }
        else if (currentLevel > 0 && targetLevel == 0) // 현재 던전에서 필드(0)로 이동
        {
            destination = targetMapData.PlayerSuggestedStartPosition; // 필드의 제안된 시작 위치 사용
            if (destination == Vector2Int.one * -1)
            {
                Debug.LogWarning($"Level {targetLevel} (field map) is missing PlayerSuggestedStartPosition. Using fallback.");
                destination = targetMapData.GetFallbackStartPosition(targetLevel); // 그래도 없으면 일반 폴백
            }
        }
        else // 일반적인 던전 간 이동 (예: 1층 <-> 2층)
        {
            destination = (direction > 0) ? targetMapData.UpStairsLocation : targetMapData.DownStairsLocation;
            if (destination == Vector2Int.one * -1) // 해당 방향 계단이 없으면
            {
                Debug.LogWarning($"Level {targetLevel} is missing {(direction > 0 ? "Up" : "Down")}Stairs. Using PlayerSuggestedStartPosition.");
                destination = targetMapData.PlayerSuggestedStartPosition; // 생성기가 제안한 시작 위치 사용
            }
        }

        // 모든 경우에도 도착 위치를 못 찾았다면, 최후의 폴백 사용
        if (destination == Vector2Int.one * -1)
        {
            Debug.LogError($"Critical: Could not determine a valid destination for level {targetLevel} from level {currentLevel}. Using absolute fallback position.");
            destination = targetMapData.GetFallbackStartPosition(targetLevel);
        }

        // UI 메시지 표시
        string levelName = (targetLevel == 0) ? "지상" : $"{targetLevel}층";
        UIManager.Instance?.AddMessage($"당신은 {(direction > 0 ? "아래로 내려가" : "위로 올라가")} {levelName}에 도착했습니다.", Color.cyan);

        // 이전 맵에서 플레이어 엔티티 제거
        MapData oldMapData = CurrentMapData;
        if (oldMapData != null && player != null) oldMapData.RemoveEntity(player);

        currentLevel = targetLevel; // 현재 레벨 업데이트

        SpawnPlayer(destination.x, destination.y); // 새 레벨의 목적지에 플레이어 스폰 (또는 이동)

        // 새 레벨의 시야 계산 및 맵 렌더링
        if (CurrentMapData != null && player != null)
        {
            CurrentMapData.ComputePlayerFov(player.gridX, player.gridY, fovRadius);
            //RenderCurrentMap();
        }
        else
        {
            Debug.LogError("Failed to compute FOV or render map after level change due to null CurrentMapData or player.");
        }
        UIManager.Instance?.UpdateLevelDisplay(currentLevel); // UI 레벨 표시 업데이트
    }
    #endregion

    #region Input Handling & Player Actions // 입력 처리 및 플레이어 행동 관련 메서드

    /// <summary>
    /// 매 프레임 플레이어의 입력을 받아 처리합니다.
    /// Handles player input every frame.
    /// </summary>
    private void HandlePlayerInput()
    {
        bool actionTaken = false; // 이번 프레임에 플레이어가 행동을 취했는지 여부

        // 1. 스페이스바: 디폴트 마법 (현재는 비활성화된 함수 호출)
        if (!actionTaken && Input.GetKeyDown(KeyCode.Space))
        {
            actionTaken = TryHandleDefaultMagicAttack();
        }

        // 2. 숫자키 1: 특정 마법
        if (!actionTaken && Input.GetKeyDown(KeyCode.Alpha1)) // GetKeyDown은 키를 누르는 순간 한 번만 true
        {
            actionTaken = HandleSpecificMagicAttack();
        }
        // 3. 계단 이용
        if (!actionTaken) actionTaken = HandleStairInput();
        // 4. 기타 행동 (아이템 줍기 등)
        if (!actionTaken) actionTaken = HandleOtherActionInput();
        // 5. 대기 (제자리에서 턴 넘기기)
        if (!actionTaken) actionTaken = HandleWaitInput();
        // 6. 이동
        if (!actionTaken) actionTaken = HandleMovementInput();

        // 플레이어가 행동을 취했고, 현재 게임 상태가 플레이어 턴이라면 (마법 애니메이션 등으로 상태가 바뀌지 않았다면)
        if (actionTaken && currentGameState == GameState.PlayerTurn)
        {
            EndPlayerTurn(); // 플레이어 턴 종료
        }
    }

    /// <summary>
    /// 디폴트 마법(스페이스바 가정) 발사를 시도합니다.
    /// </summary>
    private bool TryHandleDefaultMagicAttack()
    {
        if (player == null || CurrentMapData == null) return false;

        Vector2Int spellDirectionInt = lastMoveDirection;
        if (spellDirectionInt == Vector2Int.zero)
        {
            spellDirectionInt = new Vector2Int(0, -1); // 기본 위쪽
        }
        Vector2 spellDirection = (Vector2)spellDirectionInt; // 계산을 위해 Vector2로 변환

        Vector2Int startPos = new Vector2Int(player.gridX, player.gridY);
        Vector2Int targetPos = startPos;
        Entity hitEntity = null;
        bool hitWall = false;

        for (int i = 1; i <= defaultSpellRange; i++)
        {
            // 정수 단위로 다음 위치 계산
            Vector2Int nextGridPos = new Vector2Int(
                startPos.x + Mathf.RoundToInt(spellDirection.x * i),
                startPos.y + Mathf.RoundToInt(spellDirection.y * i)
            );
            // 대각선 이동 시 한 번에 한 칸씩 정확히 이동하도록 수정
            if (i > 0 && (Mathf.Abs(spellDirection.x) > 0.5f || Mathf.Abs(spellDirection.y) > 0.5f))
            { // 대각선 또는 직선 체크
                nextGridPos = new Vector2Int(
                   startPos.x + (int)Mathf.Sign(spellDirection.x) * Mathf.Min(i, Mathf.Abs(Mathf.RoundToInt(spellDirection.x * i))),
                   startPos.y + (int)Mathf.Sign(spellDirection.y) * Mathf.Min(i, Mathf.Abs(Mathf.RoundToInt(spellDirection.y * i)))
               );
                // 좀 더 정확한 직선/대각선 타일 스캔 로직이 필요할 수 있음 (Bresenham 등)
                // 여기서는 간단하게 처리
                if (Mathf.Abs(nextGridPos.x - startPos.x) > i || Mathf.Abs(nextGridPos.y - startPos.y) > i)
                { // 한 번에 너무 많이 건너뛰는 것 방지
                    nextGridPos = new Vector2Int(startPos.x + (int)Mathf.Sign(spellDirection.x) * i, startPos.y + (int)Mathf.Sign(spellDirection.y) * i);
                }
                // 한 번에 한 칸씩만 이동하도록 조정 (가장 간단한 방식)
                nextGridPos = new Vector2Int(startPos.x + (int)Mathf.Sign(spellDirection.x), startPos.y + (int)Mathf.Sign(spellDirection.y));
                if (i > 1)
                { // 두 번째 스텝부터는 이전 targetPos에서 한칸 더
                    nextGridPos = new Vector2Int(targetPos.x + (int)Mathf.Sign(spellDirection.x), targetPos.y + (int)Mathf.Sign(spellDirection.y));
                }

            }


            if (!CurrentMapData.IsInBounds(nextGridPos.x, nextGridPos.y))
            {
                // targetPos는 이미 이전 단계의 유효한 위치임
                hitWall = true;
                break;
            }
            targetPos = nextGridPos; // 유효하면 targetPos 업데이트
            Entity entityOnTile = CurrentMapData.GetBlockingEntityAt(targetPos.x, targetPos.y);
            if (entityOnTile != null && entityOnTile != player)
            {
                hitEntity = entityOnTile;
                break;
            }
            TileType tile = CurrentMapData.Tiles[targetPos.y, targetPos.x];
            if (tile != null && !tile.walkable)
            {
                hitWall = true;
                break;
            }
        }

        // 1. 발사체 이펙트 정의
        // SpellAnimationData의 Emitter.EndPosition은 현재 구조에 없으므로,
        // Line 이미터의 경우 Duration과 Particle Lifetime/Velocity로 도달 거리를 조절해야 함.
        // 또는 EmitterPropertyData에 EndPosition을 추가하고 EffectSequencer가 활용하도록 수정.
        // 여기서는 Line 이미터 대신 Point 이미터에서 지정된 방향으로 발사하는 형태로 수정.
        float distanceToTarget = Vector2.Distance(startPos, targetPos);
        if (distanceToTarget < 1f && (hitEntity != null || hitWall)) distanceToTarget = 1f; // 최소 거리 보장 (제자리에서 벽/적에게 쏠 때)
        else if (distanceToTarget < 1f) distanceToTarget = defaultSpellRange; // 허공에 쏠때는 최대 사거리까지 날아가는 것처럼 보이게


        SpellAnimationData projectileEffectData = new SpellAnimationData
        {
            EffectName = "DefaultMagicProjectile",
            MaxEffectDuration = 0.1f * distanceToTarget + 0.1f, // 파티클 수명 고려하여 최대 지속시간 설정
            Emitter = new SpellAnimationData.EmitterPropertyData
            {
                EmitterShape = SpellAnimationData.EmitterPropertyData.Shape.Point, // 점에서 발사
                StartPosition = startPos,
                Duration = 0.05f * distanceToTarget, // 거리에 따라 짧게 여러개 방출 또는 단일 버스트
                EmissionRate = 0, // 지속 방출 없음 (Burst로 제어)
                BurstCount = Mathf.Max(1, Mathf.RoundToInt(distanceToTarget / 2f)), // 거리에 따라 파티클 수 조절
                BurstInterval = 0.03f, // 짧은 간격으로 여러개 발사하여 선처럼 보이게
                TotalBursts = Mathf.Max(1, Mathf.RoundToInt(distanceToTarget / 1.5f)), // 여러번 끊어서 발사
                EmissionDirection = spellDirection.normalized, // 정규화된 방향
                InitialDelay = 0f
            },
            ParticleProps = new SpellAnimationData.ParticlePropertyData
            {
                BaseChar = '@',
                BaseColor = Color.magenta,
                LifetimeMin = 0.08f * distanceToTarget, // 목표 지점 도달 시간과 비슷하게
                LifetimeMax = 0.1f * distanceToTarget,
                InitialVelocityMin = spellDirection.normalized * 10f, // 초당 타일 속도
                InitialVelocityMax = spellDirection.normalized * 12f,
                Acceleration = Vector2.zero
            }
        };

        // 2. 피격 시 이펙트 정의
        SpellAnimationData impactEffectData = default;
        bool createImpactEffect = (hitEntity != null || hitWall);

        if (createImpactEffect)
        {
            impactEffectData = new SpellAnimationData
            {
                EffectName = "DefaultMagicImpact",
                MaxEffectDuration = 0.3f,
                Emitter = new SpellAnimationData.EmitterPropertyData
                {
                    EmitterShape = SpellAnimationData.EmitterPropertyData.Shape.Circle, // 충격 지점 중심 원형
                    StartPosition = targetPos, // 피격 지점
                    Size = new Vector2(1f, 0), // 반지름 1
                    Duration = 0.1f,
                    BurstCount = 5,
                    TotalBursts = 1,
                    EmissionDirection = Vector2.zero, // 모든 방향으로 퍼짐
                    EmissionAngleSpread = 360f
                },
                ParticleProps = new SpellAnimationData.ParticlePropertyData
                {
                    BaseChar = 'X',
                    BaseColor = hitEntity != null ? Color.yellow : Color.cyan,
                    LifetimeMin = 1.2f,
                    LifetimeMax = 2.3f,
                    InitialVelocityMin = new Vector2(1f, 1f), // 최소 속도 (방향은 이미터에서)
                    InitialVelocityMax = new Vector2(3f, 3f), // 최대 속도
                    Acceleration = new Vector2(0, 9.8f) // 중력 (선택적)
                }
            };
        }

        // 실제 마법 효과 적용 콜백
        Action spellActualEffect = () => {
            if (hitEntity != null && hitEntity.CurrentHealth > 0)
            {
                hitEntity.TakeDamage(defaultSpellDamage);
                UIManager.Instance?.AddMessage($"The {hitEntity.entityName} is zapped for {defaultSpellDamage} damage!", new Color(0.8f, 0.8f, 1f));
            }
            else if (hitWall)
            {
                UIManager.Instance?.AddMessage("The magic fizzles against the obstacle.", Color.grey);
            }
            else
            {
                UIManager.Instance?.AddMessage("The magic dissipates into nothing.", Color.grey);
            }
            //RenderCurrentMap();
        };

        // 애니메이션 시작
        // 발사체 애니메이션 후, 피격 애니메이션과 실제 효과를 순차적으로 실행
        StartSpellAnimation(projectileEffectData, startPos, () => {
            if (createImpactEffect)
            {
                // 피격 애니메이션 재생 후 실제 효과 적용
                StartSpellAnimation(impactEffectData, targetPos, spellActualEffect);
            }
            else
            {
                // 피격 애니메이션 없으면 바로 실제 효과 적용
                spellActualEffect();
            }
        });
        return true;
    }

    int specificSpellDamage  = 4;

    /// <summary>
    /// 특정 키(예: 숫자키 1)에 할당된 마법을 발사합니다.
    /// </summary>
    private bool HandleSpecificMagicAttack()
    {
        Entity target = FindTargetForSpell();
        if (target != null)
        {
            Vector2Int playerPos = new Vector2Int(player.gridX, player.gridY);
            Vector2Int targetActualPos = new Vector2Int(target.gridX, target.gridY);

            SpellAnimationData spellData = new SpellAnimationData
            {
                EffectName = "TargetedBurstMagic",
                MaxEffectDuration = 0.6f,
                Emitter = new SpellAnimationData.EmitterPropertyData
                {
                    EmitterShape = SpellAnimationData.EmitterPropertyData.Shape.Circle,
                    StartPosition = targetActualPos,
                    Size = new Vector2(2.0f, 0), // 반지름
                    Duration = 0.3f,
                    EmissionRate = 2,
                    BurstCount = 15,
                    TotalBursts = 1,
                    InitialDelay = 0.1f,
                    EmissionDirection = Vector2.zero, // 모든 방향
                    EmissionAngleSpread = 360f
                },
                ParticleProps = new SpellAnimationData.ParticlePropertyData
                {
                    BaseChar = '*',
                    BaseColor = Color.cyan,
                    LifeCycleChars = new char[] { '*', '+', '.', ' ' },
                    LifeCycleCharDurations = new float[] { 0.3f, 0.3f, 0.3f, 0.3f },
                    LifetimeMin = 2.3f,
                    LifetimeMax = 2.45f,
                    InitialVelocityMin = new Vector2(2f, 2f), // 속도 범위
                    InitialVelocityMax = new Vector2(4f, 4f),
                    Acceleration = Vector2.zero
                }
            };

            Action spellActualEffect = () => {
                if (target != null && target.CurrentHealth > 0)
                {
                    target.TakeDamage(specificSpellDamage);
                    UIManager.Instance?.AddMessage($"적 {target.entityName}에게 마법으로 {specificSpellDamage}의 피해를 입혔습니다!", Color.magenta);
                }
                //RenderCurrentMap();
            };

            // 이펙트의 원점은 타겟이지만, StartSpellAnimation의 originPos는 플레이어 위치로 전달하여
            // 게임 상태 변경의 주체를 명확히 할 수 있습니다.
            // EffectSequencer는 SpellAnimationData.Emitter.StartPosition을 실제 이펙트 발생 위치로 사용합니다.
            StartSpellAnimation(spellData, playerPos, spellActualEffect);
            return true;
        }
        else
        {
            UIManager.Instance?.AddMessage("마법을 사용할 대상이 없습니다.", Color.yellow);
            return false;
        }
    }

    /// <summary>
    /// 마법 애니메이션을 시작하고, 완료 시 특정 액션을 실행합니다.
    /// 이제 EffectSequencer를 통해 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="data">재생할 이펙트의 데이터</param>
    /// <param name="originPos">이펙트 발생 월드 좌표</param>
    /// <param name="onActualEffect">애니메이션 시퀀스 완료 후 실행될 실제 효과(데미지 등) 적용 액션</param>
    public void StartSpellAnimation(SpellAnimationData data, Vector2Int originPos, Action onActualEffect)
    {
        if (EffectSequencer.Instance == null)
        {
            Debug.LogError("EffectSequencer is not initialized! Cannot play spell animation.");
            onActualEffect?.Invoke(); // 애니메이션 없이 효과 즉시 적용
            EndPlayerTurn(); // 즉시 턴 종료
            return;
        }

        if (currentGameState == GameState.SpellAnimation)
        {
            Debug.LogWarning("Cannot start new spell animation while one is already in progress. New request ignored, previous onComplete will be called.");
            // 현재 진행 중인 애니메이션이 있으므로 새 요청은 무시하고,
            // 이전 애니메이션의 onCompleteAction이 결국 ApplySpellEffectsAndEndAnimation을 호출할 것임.
            // 또는, 여기서 onActualEffect를 즉시 호출하고 return하여 새 마법 시전 자체를 취소할 수도 있음.
            // 여기서는 새 마법 시전이 취소되고, 이전 마법의 흐름을 방해하지 않도록 함.
            // (플레이어는 행동 포인트를 소모하지 않아야 함 - 이 부분은 HandlePlayerInput에서 관리)
            return;
        }

        Debug.Log($"GameManager: Requesting spell animation '{data.EffectName}' at {originPos}. Changing state to SpellAnimation.");
        currentGameState = GameState.SpellAnimation;

        // EffectSequencer에게 이펙트 재생을 요청하고, 모든 시퀀스가 완료되면 ApplySpellEffectsAndEndAnimation을 호출하도록 콜백 설정
        EffectSequencer.Instance.PlayEffect(data, originPos, () => {
            ApplySpellEffectsAndEndAnimation(onActualEffect);
        });
    }

    /// <summary>
    /// 마법 애니메이션이 완료된 후 실제 효과를 적용하고 게임 상태를 다음 턴으로 넘깁니다.
    /// </summary>
    private void ApplySpellEffectsAndEndAnimation(Action actualSpellEffectAction)
    {
        Debug.Log("GameManager: Spell animation finished. Applying actual spell effects.");
        actualSpellEffectAction?.Invoke(); // 실제 마법 효과 적용 (데미지 처리 등)

        // 애니메이션이 끝났으므로, 다음 턴으로 진행할 준비
        if (currentGameState == GameState.SpellAnimation) // 상태 확인
        {
            EndPlayerTurn(); // 플레이어 턴 종료 로직 호출 (적 턴으로 전환 등)
        }
        else
        {
            Debug.LogWarning("GameManager: GameState was not SpellAnimation when ApplySpellEffectsAndEndAnimation was called. This might indicate an issue.");
            // 필요하다면 여기서 강제로 턴을 넘기거나 상태를 재설정할 수 있음
        }
    }



    /// <summary>
    /// 마법 애니메이션이 완료된 후 실제 효과를 적용하고 게임 상태를 다음 턴으로 넘깁니다.
    /// Applies actual spell effects after the animation is complete and transitions to the next turn.
    /// </summary>
    private void ApplySpellEffectsAndEndAnimation()
    {
        Debug.Log("GameManager: ApplySpellEffectsAndEndAnimation called.");
        onSpellAnimationCompleteAction?.Invoke(); // 저장된 마법 효과 적용 액션 실행
        // 참조 초기화
        onSpellAnimationCompleteAction = null;
        currentSpellAnimationData = null;

        // 애니메이션이 끝났으므로, 다음 턴으로 진행할 준비
        if (currentGameState == GameState.SpellAnimation) // 상태 확인 (다른 곳에서 변경되지 않았는지)
        {
            EndPlayerTurn(); // 플레이어 턴 종료 로직 호출 (적 턴으로 전환 등)
        }
    }

    /// <summary>
    /// 플레이어 시야 내의 가장 가까운 적을 찾습니다. (간단한 타겟팅 예시)
    /// Finds the closest enemy within the player's field of view. (Simple targeting example)
    /// </summary>
    /// <returns>가장 가까운 적 Entity, 없으면 null</returns>
    private Entity FindTargetForSpell()
    {
        Entity closestTarget = null;
        float minDistance = float.MaxValue;
        if (CurrentMapData == null || player == null) return null; // 필수 데이터 없으면 null 반환

        foreach (Entity entity in CurrentMapData.Entities)
        {
            // 자신이 아니고, 살아있고, 맵 범위 내에 있으며, 시야에 보이는 엔티티만 고려
            if (entity != player && entity.CurrentHealth > 0 &&
                CurrentMapData.IsInBounds(entity.gridX, entity.gridY) &&
                CurrentMapData.Visible[entity.gridY, entity.gridX])
            {
                float distance = Vector2Int.Distance(new Vector2Int(player.gridX, player.gridY), new Vector2Int(entity.gridX, entity.gridY));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = entity;
                }
            }
        }
        return closestTarget;
    }

    /// <summary>
    /// 플레이어의 이동 입력을 처리합니다.
    /// Handles player movement input.
    /// </summary>
    /// <returns>행동을 취했으면 true, 아니면 false.</returns>
    private bool HandleMovementInput()
    {
        Vector2Int moveDirectionInput = Vector2Int.zero; // 입력된 이동 방향 초기화
        // 키 입력 감지 (방향키 및 숫자패드)
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Keypad8)) { moveDirectionInput.y = -1; } // Y축 감소가 위쪽인 경우
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Keypad2)) { moveDirectionInput.y = 1; } // Y축 증가가 아래쪽인 경우
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Keypad4)) { moveDirectionInput.x = -1; }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Keypad6)) { moveDirectionInput.x = 1; }
        else if (Input.GetKey(KeyCode.Keypad7)) { moveDirectionInput.Set(-1, -1); } // 좌상단
        else if (Input.GetKey(KeyCode.Keypad9)) { moveDirectionInput.Set(1, -1); }  // 우상단
        else if (Input.GetKey(KeyCode.Keypad1)) { moveDirectionInput.Set(-1, 1); }  // 좌하단
        else if (Input.GetKey(KeyCode.Keypad3)) { moveDirectionInput.Set(1, 1); }  // 우하단

        bool keyPressed = moveDirectionInput != Vector2Int.zero; // 이동 키가 눌렸는지 여부
        bool actionTaken = false;

   

        if (keyPressed)
        {
            Debug.Log($"Move Direction Input: {moveDirectionInput}, Key Pressed: {keyPressed}");
            lastMoveDirection = moveDirectionInput; // 입력이 있으면 항상 마지막 방향 업데이트 (디폴트 마법용)

            // 키를 처음 눌렀는지, 아니면 누르고 있는지 확인 (키 반복 처리용)
            bool firstPress = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                             Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Keypad6) ||
                             Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Keypad3);
            if (firstPress) // 처음 누른 경우
            {
                actionTaken = ProcessPlayerAction(moveDirectionInput); // 즉시 행동 처리
                timeKeyPressed = 0f; timeSinceLastInput = 0f; // 반복 입력 타이머 초기화
            }
            else // 키를 누르고 있는 경우 (반복)
            {
                timeKeyPressed += Time.deltaTime;
                timeSinceLastInput += Time.deltaTime;
                // 반복 딜레이와 간격을 만족하면 행동 처리
                if (timeKeyPressed >= keyRepeatDelay && timeSinceLastInput >= keyRepeatInterval)
                {
                    actionTaken = ProcessPlayerAction(moveDirectionInput);
                    timeSinceLastInput = 0f; // 간격 타이머 초기화
                }
            }
        }
        else // 아무 이동 키도 눌리지 않은 경우
        {
            timeKeyPressed = 0f; // 키 누름 지속 시간 초기화
        }
        return actionTaken;
    }

    /// <summary>
    /// 플레이어의 대기 입력을 처리합니다. (제자리에서 턴 넘기기)
    /// Handles player wait input (pass turn in place).
    /// </summary>
    /// <returns>행동을 취했으면 true, 아니면 false.</returns>
    private bool HandleWaitInput()
    {
        if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Period)) // 숫자패드 5 또는 마침표
        {
            UIManager.Instance?.AddMessage("당신은 잠시 숨을 고릅니다.", Color.grey);
            return true; // 행동을 취했음
        }
        return false;
    }

    /// <summary>
    /// 플레이어의 기타 행동 입력을 처리합니다. (예: 아이템 줍기)
    /// Handles other player action inputs (e.g., picking up items).
    /// </summary>
    /// <returns>행동을 취했으면 true, 아니면 false.</returns>
    private bool HandleOtherActionInput()
    {
        // 예시: 'G' 키로 아이템 줍기 (현재는 구현 안됨)
        // if (Input.GetKeyDown(KeyCode.G)) { /* 아이템 줍기 로직 */ return true; }
        return false;
    }

    /// <summary>
    /// 플레이어의 실제 행동(이동 또는 공격)을 처리합니다.
    /// Processes the player's actual action (movement or attack).
    /// </summary>
    /// <param name="direction">행동 방향</param>
    /// <returns>행동이 성공적으로 처리되었으면 true, 아니면 false.</returns>
    private bool ProcessPlayerAction(Vector2Int direction)
    {
        if (player == null || CurrentMapData == null) return false; // 필수 객체 확인
        int destX = player.gridX + direction.x; // 목표 X 좌표
        int destY = player.gridY + direction.y; // 목표 Y 좌표

        // 목표 좌표가 맵 범위 밖인지 확인
        if (!CurrentMapData.IsInBounds(destX, destY))
        { UIManager.Instance?.AddMessage("갈 수 없는 방향입니다.", Color.yellow); return false; }

        // 목표 위치에 다른 엔티티(몬스터 등)가 있는지 확인 (공격 대상)
        Entity targetEntity = CurrentMapData.GetBlockingEntityAt(destX, destY);
        if (targetEntity != null) // 공격 대상이 있으면
        {
            // lastMoveDirection = direction; // 공격 방향도 마지막 방향으로 기록할 수 있음 (선택적)
            int damage = player.Strength; // 플레이어의 힘만큼 데미지 (예시)
            string message = $"당신은 {targetEntity.entityName}을(를) 공격하여 {damage}의 피해를 입혔습니다!";
            targetEntity.TakeDamage(damage); // 엔티티에게 데미지 적용
            if (targetEntity.CurrentHealth <= 0) { message = $"당신은 {targetEntity.entityName}을(를) 처치했습니다!"; } // 사망 메시지
            UIManager.Instance?.AddMessage(message, (targetEntity.CurrentHealth <= 0 ? Color.green : Color.white)); // UI 메시지 표시
            return true; // 행동 성공
        }

        // 목표 위치의 타일이 이동 가능한지 확인
        TileType destTile = CurrentMapData.Tiles[destY, destX];
        if (destTile == null || !destTile.walkable) // 이동 불가능한 타일(벽 등)이면
        {
            UIManager.Instance?.AddMessage("벽에 부딪혔습니다.", Color.yellow);
            return false; // 행동 실패
        }

        // 이동 성공
        player.Move(direction.x, direction.y); // 플레이어 이동
        // lastMoveDirection은 HandleMovementInput에서 이미 업데이트됨 (또는 여기서 업데이트해도 됨)
        CurrentMapData.ComputePlayerFov(player.gridX, player.gridY, fovRadius); // 시야 재계산
        //RenderCurrentMap(); // 맵 다시 그리기
        return true; // 행동 성공
    }

    /// <summary>
    /// 엔티티 간의 공격을 처리합니다. (명중률, 데미지 계산 등)
    /// Processes an attack between two entities.
    /// </summary>
    /// <param name="attacker">공격하는 엔티티</param>
    /// <param name="target">공격받는 엔티티</param>
    public void ProcessEntityAttack(Entity attacker, Entity target)
    {
        if (attacker == null || target == null) return;

        // TODO: 더 정교한 명중률 계산 (예: 공격자 민첩 vs 방어자 민첩)
        bool hit = UnityEngine.Random.value < baseHitChance; // 기본 명중률 기반

        if (hit)
        {
            // TODO: 더 정교한 데미지 계산 (예: 공격자 힘 - 방어자 방어력, 무기 데미지 등)
            int damage = attacker.Strength; // 현재는 공격자의 힘만큼 데미지

            string message = $"{attacker.entityName}이(가) {target.entityName}을(를) 공격하여 <color=red>{damage}</color>의 피해를 입혔습니다!";
            if (target.CurrentHealth - damage <= 0) // 이번 공격으로 죽는지 확인
            {
                message += $" {target.entityName}은(는) 치명상을 입었습니다!";
            }
            UIManager.Instance?.AddMessage(message, Color.white);
            target.TakeDamage(damage); // 타겟에게 데미지 적용
        }
        else
        {
            UIManager.Instance?.AddMessage($"{attacker.entityName}의 공격이 {target.entityName}에게 빗나갔습니다!", Color.grey);
        }

        // 공격 후에는 항상 맵을 다시 그려서 변경사항(체력 등)을 반영할 수 있음
        // 또는 턴 종료 시에만 그릴 수도 있음
        //RenderCurrentMap();
    }


    /// <summary>
    /// 플레이어의 계단 이용 입력을 처리합니다.
    /// Handles player input for using stairs.
    /// </summary>
    /// <returns>행동을 취했으면 true, 아니면 false.</returns>
    private bool HandleStairInput()
    {
        // 아래로 내려가는 키: '>' 또는 Shift + '.'
        bool useStairsDown = Input.GetKeyDown(KeyCode.Greater) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Period));
        // 위로 올라가는 키: '<' 또는 Shift + ','
        bool useStairsUp = Input.GetKeyDown(KeyCode.Less) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Comma));

        if (!useStairsDown && !useStairsUp) return false; // 계단 관련 키 입력 없음

        // 필수 타일 데이터베이스 및 계단 타일 존재 여부 확인
        if (TileDatabase.Instance == null || TileDatabase.Instance.StairsDown == null || TileDatabase.Instance.StairsUp == null)
        { Debug.LogError("TileDatabase instance or Stairs TileTypes are missing!"); return false; }

        // 플레이어가 서 있는 타일 정보 가져오기
        TileType playerTile = null;
        if (CurrentMapData != null && player != null && CurrentMapData.IsInBounds(player.gridX, player.gridY))
        { playerTile = CurrentMapData.Tiles[player.gridY, player.gridX]; }

        if (playerTile == null)
        { // 플레이어가 유효하지 않은 타일 위에 있는 경우
            Debug.LogWarning("Player is on a null tile. Cannot use stairs.");
            return false;
        }

        if (currentLevel == 0) // 현재 필드 맵(지상)인 경우
        {
            if (useStairsDown && playerTile.walkable) // 아래로 내려가려고 하고, 현재 타일이 걸을 수 있다면
            {
                // TODO: 필드에서 던전 입구 타일인지 등을 추가로 확인할 수 있음
                // 예: if (playerTile == TileDatabase.Instance.DungeonEntrance)
                ChangeLevel(1); return true; // 던전 1층으로 이동
            }
            else if (useStairsDown) { UIManager.Instance?.AddMessage("여기서는 던전으로 들어갈 수 없습니다.", Color.yellow); return false; }

            if (useStairsUp) { UIManager.Instance?.AddMessage("여기서는 더 위로 올라갈 수 없습니다.", Color.yellow); return false; }
        }
        else // 현재 던전 내부인 경우
        {
            if (useStairsDown) // 아래로 내려가려는 경우
            {
                if (playerTile == TileDatabase.Instance.StairsDown) { ChangeLevel(1); return true; } // 아래층으로
                else { UIManager.Instance?.AddMessage("여기에 아래로 내려가는 계단이 없습니다.", Color.yellow); return false; }
            }
            else if (useStairsUp) // 위로 올라가려는 경우
            {
                if (playerTile == TileDatabase.Instance.StairsUp) { ChangeLevel(-1); return true; } // 위층으로 (0층이면 필드로)
                else { UIManager.Instance?.AddMessage("여기에 위로 올라가는 계단이 없습니다.", Color.yellow); return false; }
            }
        }
        return false;
    }
    #endregion


    #region Rendering // 맵 렌더링 관련 메서드
    /// <summary>
    /// 현재 맵을 화면에 렌더링합니다. (DungeonVisualizer를 통해)
    /// Renders the current map to the screen (via DungeonVisualizer).
    /// </summary>
    public void RenderCurrentMap()
    {
        if (dungeonVisualizer != null && CurrentMapData != null && player != null)
        {
            dungeonVisualizer.RenderMap(CurrentMapData, player);
        }
        else
        {
            // Debug.LogWarning("//RenderCurrentMap skipped: visualizer, map data, or player is null.");
        }
    }
    #endregion

    #region Turn Management & Enemy AI // 턴 관리 및 적 AI 관련 메서드

    /// <summary>
    /// 플레이어의 턴을 종료하고 다음 턴(주로 적 턴)으로 전환합니다.
    /// Ends the player's turn and transitions to the next turn (usually enemy turn).
    /// </summary>
    private void EndPlayerTurn()
    {
        timeKeyPressed = 0f;    // 키 누름 지속 시간 초기화
        timeSinceLastInput = 0f; // 마지막 입력 시간 초기화

        // 현재 게임 상태가 마법 애니메이션 중이 아니라면 (즉, 일반 행동 후) 적 턴으로 변경
        // 마법 애니메이션 중이었다면, ApplySpellEffectsAndEndAnimation에서 이미 EnemyTurn으로 변경됨
        if (currentGameState != GameState.SpellAnimation)
        {
            Debug.Log("Player turn ended normally. Switching to EnemyTurn.");
            currentGameState = GameState.EnemyTurn;
        }
        else
        {
            // 이 경우는 ApplySpellEffectsAndEndAnimation에서 EnemyTurn으로 바꿔주므로,
            // 여기서 또 EnemyTurn으로 바꾸면 중복 로깅이 될 수 있음.
            // 하지만 방어적으로 한 번 더 상태를 확인하고 설정하는 것이 안전할 수 있음.
            // 현재 로직에서는 ApplySpellEffectsAndEndAnimation -> EndPlayerTurn 순서로 호출될 때
            // currentGameState가 SpellAnimation 상태에서 EnemyTurn으로 변경됨.
            Debug.Log("Spell animation finished. State already set or will be set to EnemyTurn by ApplySpellEffectsAndEndAnimation.");
            currentGameState = GameState.EnemyTurn; // 중복될 수 있으나 명시적으로. 또는 위의 if 조건문으로 충분.
        }
    }

    /// <summary>
    /// 모든 적들의 턴을 처리합니다.
    /// Processes the turns for all enemies.
    /// </summary>
    private void ProcessEnemyTurns()
    {
        Debug.Log("Processing Enemy Turns...");
        if (CurrentMapData == null) { EndEnemyTurn(); return; } // 맵 데이터 없으면 즉시 종료

        List<Entity> entitiesCopy = new List<Entity>(CurrentMapData.Entities); // 반복 중 컬렉션 변경 방지를 위해 복사본 사용
        bool enemyActedThisTurn = false; // 이번 턴에 행동한 적이 있는지 여부
        foreach (Entity entity in entitiesCopy)
        {
            if (entity == null || entity is Player) continue; // 플레이어는 제외

            entity.ReplenishActionPoints(); // 1. 행동 포인트 회복
            while (entity.actionPoints >= 1.0f && entity.CurrentHealth > 0) // 2. 행동 가능하고 살아있으면
            {
                if (currentGameState != GameState.EnemyTurn) break; // 턴 중간에 상태 변경 시 중단

                entity.PerformAction(); // 3. ★★★ AI 행동 실행 ★★★
                                        // 이 메서드 내부에서 이동, 공격 등의 결정이 이루어집니다.

                // PerformAction 후에도 엔티티가 살아있는지 다시 확인 (자폭 등으로 죽을 수 있음)
                if (entity.CurrentHealth <= 0) break;
            }
        }
        if (enemyActedThisTurn) Debug.Log("Some enemies acted during their turn.");
        else Debug.Log("No enemies acted during their turn.");

        EndEnemyTurn(); // 모든 적의 행동이 끝나면 적 턴 종료
    }


    /// <summary>
    /// 적 턴을 종료하고 플레이어 턴으로 전환합니다.
    /// Ends the enemy turn and transitions to the player turn.
    /// </summary>
    private void EndEnemyTurn()
    {
        Debug.Log("Enemy turn ended. Switching to PlayerTurn.");
        // 플레이어 시야 재계산 및 맵 업데이트
        if (player != null && CurrentMapData != null)
        { CurrentMapData.ComputePlayerFov(player.gridX, player.gridY, fovRadius); }
        //RenderCurrentMap();
        UIManager.Instance?.UpdatePlayerInfo(player); // 플레이어 UI 정보 업데이트 (체력 등)
        currentGameState = GameState.PlayerTurn; // 게임 상태를 플레이어 턴으로 변경
    }
    #endregion

    #region Game Over / Win // 게임 오버 및 승리 관련 메서드
    /// <summary>
    /// 플레이어 사망 시 호출됩니다.
    /// Called when the player dies.
    /// </summary>
    public void PlayerDied()
    {
        currentGameState = GameState.GameOver; // 게임 상태를 게임 오버로 변경
        UIManager.Instance?.AddMessage("당신은 사망했습니다!", Color.red, false); // UI 메시지 표시
        Debug.Log("--- GAME OVER ---");
        // TODO: 게임 오버 화면 표시 등의 로직 추가
    }
    #endregion
}
