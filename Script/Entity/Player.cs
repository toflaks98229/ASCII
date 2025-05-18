using UnityEngine;
using Unity;

/// <summary>
/// Represents the player character. Inherits from Entity.
/// 플레이어 캐릭터를 나타냅니다. Entity 클래스를 상속받습니다.
/// Contains player-specific stats and potentially logic.
/// 플레이어 고유의 능력치 및 로직을 포함할 수 있습니다.
/// </summary>
public class Player : Entity // Inherits MaxHealth, CurrentHealth, Strength etc. from Entity
{
    // --- Player Specific Stats (Not already in Entity) ---
    // --- 플레이어 고유 능력치 (Entity에 없는 것들) ---
    [Header("Player Specific Stats")]
    public int Level = 1; // Level is specific to the player 레벨은 플레이어 고유
                          // TODO: Add other player-only stats like Experience, Mana, etc.
                          // TODO: 경험치, 마나 등 다른 플레이어 전용 능력치 추가


    /// <summary>
    /// 플레이어가 현재 목표로 하고 있는 엔티티입니다.
    /// The entity the player is currently targeting (e.g., last attacked).
    /// </summary>
    private Entity _currentTarget;

    /// <summary>
    /// Overrides Awake to set player-specific defaults for inherited stats.
    /// 상속된 능력치에 대한 플레이어별 기본값을 설정하기 위해 Awake를 재정의합니다.
    /// </summary>
    protected override void Awake()
    {
        // Call the base class Awake first (important!) 기본 클래스 Awake 먼저 호출 (중요!)
        base.Awake();

        // Set player-specific defaults 플레이어별 기본값 설정
        if (string.IsNullOrEmpty(entityName) || entityName == "<Unnamed>")
        {
            entityName = "Player";
        }
        displayChar = '@';
        displayColor = Color.green;
        blocksMovement = true; // Player usually blocks movement 플레이어는 보통 이동을 막음
        renderOrder = 1;       // Render player above items/corpses 아이템/시체 위에 플레이어 렌더링

        // Set initial values for inherited stats (can be overridden by Inspector)
        // 상속된 능력치 초기값 설정 (Inspector에서 재정의 가능)
        MaxHealth = 30; // Example: Player starts with more health 예시: 플레이어는 더 많은 체력으로 시작
        CurrentHealth = MaxHealth; // Start with full health 최대 체력으로 시작
        Strength = 6; // Example: Slightly stronger 예시: 약간 더 강함
        Dexterity = 6;
        Intelligence = 6;
    }

    /// <summary>
    /// Called on the frame when a script is enabled. Requests initial UI update.
    /// 스크립트가 활성화된 프레임에 호출됩니다. 초기 UI 업데이트를 요청합니다.
    /// </summary>
    void Start()
    {
        // Request initial UI update for player info
        // 플레이어 정보에 대한 초기 UI 업데이트 요청
        UIManager.Instance?.UpdatePlayerInfo(this);
    }

    /// <summary>
    /// 플레이어가 현재 목표로 하고 있는 엔티티를 반환합니다.
    /// Returns the entity the player is currently targeting.
    /// </summary>
    /// <returns>현재 목표 엔티티, 없으면 null</returns>
    public Entity GetCurrentTarget()
    {
        // 현재 타겟이 유효한지 (예: 죽지 않았는지) 확인하는 로직 추가 가능
        if (_currentTarget != null && _currentTarget.CurrentHealth <= 0)
        {
            _currentTarget = null; // 타겟이 죽었으면 null로 설정
        }
        return _currentTarget;
    }

    /// <summary>
    /// 플레이어의 현재 목표 엔티티를 설정합니다.
    /// 주로 공격 시 GameManager에서 호출됩니다.
    /// Sets the player's current target entity.
    /// Typically called by GameManager when the player attacks.
    /// </summary>
    /// <param name="target">새로운 목표 엔티티</param>
    public void SetCurrentTarget(Entity target)
    {
        _currentTarget = target;
        // Debug.Log($"Player's new target set to: {target?.entityName ?? "null"}");
    }

    /// <summary>
    /// Overrides the Die method for player-specific game over logic.
    /// 플레이어별 게임 오버 로직을 위해 Die 메서드를 재정의합니다.
    /// </summary>
    protected override void Die()
    {
        // Don't call base.Die() to prevent default corpse behavior
        // 기본 시체 동작을 방지하기 위해 base.Die() 호출 안 함

        Debug.Log($"{entityName} has died! GAME OVER.");
        // Change appearance to a corpse 시체 모양으로 변경
        displayChar = '%';
        displayColor = Color.red;
        // Player death might not remove blocking immediately depending on game rules
        // 게임 규칙에 따라 플레이어 사망이 즉시 이동 방해를 제거하지 않을 수 있음
        // blocksMovement = false;
        renderOrder = 0; // Render below actors 액터 아래에 렌더링

        // Inform GameManager about player death 플레이어 사망을 GameManager에 알림
        GameManager.Instance?.PlayerDied();
        // The GameManager will handle the actual Game Over state 게임 매니저가 실제 게임 오버 상태 처리
    }

    // Player.cs
    public override void SetGridPosition(int x, int y)
    {
        Debug.Log($"<color=orange>Player.SetGridPosition: {entityName} to ({x},{y}). Called from:\n{new System.Diagnostics.StackTrace()}</color>");
        gridX = x;
        gridY = y;
    }

    public override void Move(int dx, int dy)
    {
        int oldX = gridX;
        int oldY = gridY;
        gridX += dx;
        gridY += dy;
        actionPoints -= 1.0f;
        Debug.Log($"<color=orange>Player.Move: {entityName} from ({oldX},{oldY}) by ({dx},{dy}) to ({gridX},{gridY}). AP left: {actionPoints}. Called from:\n{new System.Diagnostics.StackTrace()}</color>");
    }

    // Add other player-specific methods here (e.g., leveling up)
    // 다른 플레이어 고유 메서드 추가 (예: 레벨업)
    // public void GainExperience(int amount) { ... }
}
