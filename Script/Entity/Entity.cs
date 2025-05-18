// Entity.cs
using UnityEngine;
using System.Collections.Generic; // Dictionary 사용을 위해 추가

/// <summary>
/// 엔티티의 기본적인 성향을 정의합니다. (DCSS의 Disposition 참고)
/// Defines the basic disposition of an entity. (Inspired by DCSS Disposition)
/// </summary>
public enum Disposition
{
    Hostile, Neutral, Peaceful, Friendly, FellowCreature
}

/// <summary>
/// 엔티티의 지능 수준을 정의합니다. (DCSS의 Monster Intelligence 참고)
/// Defines the intelligence level of an entity. (Inspired by DCSS Monster Intelligence)
/// </summary>
public enum IntelligenceLevel
{
    Brainless, Animal, Human
}

/// <summary>
/// 엔티티가 현재 어떤 행동 상태에 있는지를 나타냅니다.
/// Represents the current behavioral state of the entity.
/// </summary>
public enum AIState
{
    Idle, Wandering, Searching, Chasing, Attacking, Fleeing, UsingAbility, Interacting
}

/// <summary>
/// 다양한 스킬의 종류를 정의합니다.
/// Defines various types of skills.
/// </summary>
public enum SkillType
{
    // 전투 스킬 (Combat Skills)
    Fighting, ShortBlades, LongBlades, Axes, MacesAndFlails, Polearms, Staves,
    Slings, Bows, Crossbows, Throwing, UnarmedCombat,
    // 방어 스킬 (Defensive Skills)
    Armour, Dodging, Shields,
    // 마법 스킬 (Magic Skills)
    Spellcasting, Conjurations, Hexes, Charms, Summonings, Necromancy,
    Translocations, Transmutations, FireMagic, IceMagic, AirMagic, EarthMagic, PoisonMagic,
    Invocations, Evocations,
    // 기타 스킬 (Other Skills)
    Stealth, Traps
}

/// <summary>
/// Inspector에서 스킬 레벨을 쉽게 설정하고 MonsterData에서도 사용하기 위한 직렬화 가능한 구조체입니다.
/// Serializable struct for easily setting skill levels in the Inspector and for use in MonsterData.
/// </summary>
[System.Serializable]
public struct SkillValue
{
    public SkillType skill;
    public int level;

    public SkillValue(SkillType skill, int level)
    {
        this.skill = skill;
        this.level = level;
    }
}


public class Entity : MonoBehaviour
{
    [Header("기본 속성 (Entity Properties)")]
    [Tooltip("엔티티의 이름 (예: Player, Orc, Slime)")]
    public string entityName = "<Unnamed>";
    [Tooltip("맵에 표시될 엔티티의 문자")]
    public char displayChar = '?';
    [Tooltip("엔티티 문자의 색상")]
    public Color displayColor = Color.white;
    [Tooltip("엔티티 문자의 배경색 (Color.clear는 타일 배경색 사용)")]
    [ColorUsage(true, true)]
    public Color entityBackgroundColor = Color.clear;
    [Tooltip("이 엔티티가 이동을 막는지 여부")]
    public bool blocksMovement = true;
    [Tooltip("렌더링 순서 (낮을수록 먼저 그려짐). 액터(1)가 아이템/시체(0) 위에 그려지도록 합니다.")]
    public int renderOrder = 1;

    [Header("핵심 능력치 (Core Stats)")]
    [Tooltip("힘 (Strength): 근접 공격력, 소지 한계, 중장비 패널티 감소 등에 영향")]
    public int Strength = 10;
    [Tooltip("민첩성 (Dexterity): 명중률, 회피율, 일부 무기 공격력, 은신 등에 영향")]
    public int Dexterity = 10;
    [Tooltip("지능 (Intelligence): 주문 성공률, 주문 위력, 최대 MP 등에 영향")]
    public int Intelligence = 10;
    [Tooltip("건강 (Constitution): 최대 HP, HP 회복률, 독/질병 저항 등에 영향")]
    public int Constitution = 10;
    [Tooltip("지혜 (Wisdom): 마법 저항, 특정 주문(신성/정신 계열) 성공률/위력, MP 회복률 등에 영향")]
    public int Wisdom = 10;

    [Header("전투 관련 파생 능력치 (Combat Derived Stats)")]
    [Tooltip("최대 체력 (스탯에 의해 주로 계산됨)")]
    public int MaxHealth = 10;
    [Tooltip("현재 체력")]
    public int CurrentHealth = 10;

    [Header("AI 및 행동 관련 속성 (AI & Behavior Properties)")]
    [Tooltip("엔티티의 현재 성향 (플레이어에 대한 태도 등)")]
    public Disposition currentDisposition = Disposition.Hostile;
    [Tooltip("엔티티의 지능 수준 (행동 패턴에 영향)")]
    public IntelligenceLevel intelligenceLevel = IntelligenceLevel.Animal;
    [Tooltip("엔티티가 속한 세력 ID (0: 중립/무소속)")]
    public int factionId = 0;
    [Tooltip("엔티티의 시야 반경 (타일 단위)")]
    public int sightRadius = 8;
    [Tooltip("이 엔티티가 마지막으로 인지한 플레이어 또는 주요 타겟의 위치")]
    public Vector2Int lastKnownTargetPosition = Vector2Int.one * -1;
    [Tooltip("현재 AI 행동 상태")]
    public AIState currentAiState = AIState.Idle;
    [Tooltip("행동 포인트. 0 미만이 되면 행동 불가.")]
    public float actionPoints = 0f;
    [Tooltip("턴당 회복되는 행동 포인트 (속도 개념)")]
    public float actionPointsPerTurn = 1.0f;

    [Header("스킬 레벨 (Skill Levels)")]
    // MonsterData로 초기화되므로 Inspector에서 직접 설정할 필요는 줄어듦.
    // 디버깅 또는 특정 엔티티의 기본값 설정용으로 남겨둘 수 있음.
    [SerializeField]
    private List<SkillValue> skillListForInspector = new List<SkillValue>();
    private Dictionary<SkillType, int> skills = new Dictionary<SkillType, int>();

    [Header("패널티 관련 (Penalty Factors)")]
    [Tooltip("현재 장비 등으로 인한 전반적인 행동 제약 수치 (0.0: 패널티 없음). 회피, 공속 등에 영향.")]
    [Range(0f, 1f)]
    public float encumbrancePenaltyFactor = 0f;
    [Tooltip("현재 갑옷으로 인한 주문 실패율 증가치 (절대값 또는 백분율).")]
    public float armorSpellFailurePenalty = 0f;

    /*[HideInInspector]*/ public int gridX;
    /*[HideInInspector]*/ public int gridY;
    protected Entity currentTargetEntity = null;

    /// <summary>
    /// MonsterData ScriptableObject의 값으로 엔티티를 초기화합니다.
    /// 이 메서드는 엔티티가 스폰될 때 GameManager에 의해 호출되어야 합니다.
    /// </summary>
    /// <param name="data">엔티티를 설정할 MonsterData</param>
    public virtual void InitializeFromData(MonsterData data)
    {
        if (data == null)
        {
            Debug.LogError($"InitializeFromData: MonsterData is null for {this.gameObject.name}! Using default/inspector values if any.");
            // MonsterData가 없으면 Awake에서 설정된 값이나 Inspector 값을 유지
            if (skills.Count == 0 && skillListForInspector.Count > 0) InitializeSkillsFromInspector();
            MaxHealth = CalculateMaxHealthFromStats(); // 스탯 기반 체력 계산
            CurrentHealth = MaxHealth;
            actionPoints = actionPointsPerTurn;
            return;
        }

        entityName = data.monsterName;
        displayChar = data.displayChar;
        displayColor = data.displayColor;
        entityBackgroundColor = data.backgroundColor;

        Strength = data.strength;
        Dexterity = data.dexterity;
        Intelligence = data.Intelligence;
        Constitution = data.constitution;
        Wisdom = data.wisdom;

        // MaxHealth는 MonsterData의 값을 기본으로 하되, 스탯에 따라 재계산할 수도 있음
        // 여기서는 MonsterData의 maxHealth를 사용하고, 필요시 CalculateMaxHealthFromStats로 보정
        MaxHealth = data.maxHealth > 0 ? data.maxHealth : CalculateMaxHealthFromStats();
        CurrentHealth = MaxHealth;

        currentDisposition = data.initialDisposition;
        intelligenceLevel = data.intelligenceLevel;
        factionId = data.factionId;
        sightRadius = data.sightRadius;
        actionPointsPerTurn = data.actionPointsPerTurn;

        // 스킬 초기화 (MonsterData의 initialSkills 사용)
        skills.Clear();
        if (data.initialSkills != null)
        {
            foreach (var skillValue in data.initialSkills)
            {
                SetSkillLevel(skillValue.skill, skillValue.level); // 내부 딕셔너리 업데이트
            }
        }
        // Inspector용 리스트도 동기화 (선택적, 디버깅용)
        SyncSkillsToInspectorList();


        encumbrancePenaltyFactor = data.encumbrancePenaltyFactor;
        armorSpellFailurePenalty = data.armorSpellFailurePenalty;

        blocksMovement = data.blocksMovement;
        renderOrder = data.renderOrder;

        actionPoints = actionPointsPerTurn; // 행동 포인트 초기화
        currentAiState = AIState.Idle;      // AI 상태 초기화
        lastKnownTargetPosition = Vector2Int.one * -1; // 마지막 타겟 위치 초기화
        currentTargetEntity = null; // 현재 타겟 초기화

        Debug.Log($"Entity '{entityName}' initialized from MonsterData '{data.name}'. HP: {CurrentHealth}/{MaxHealth}");
    }

    /// <summary>
    /// 현재 스탯을 기반으로 최대 체력을 계산합니다. (게임 디자인에 맞게 수정 필요)
    /// </summary>
    protected virtual int CalculateMaxHealthFromStats()
    {
        // 예시: 기본 5 + 건강 1당 1 HP, 힘 5당 1 HP
        return 5 + Constitution + (Strength / 5) + (GetSkillLevel(SkillType.Fighting) / 2);
    }

    protected virtual void Awake()
    {
        // InitializeFromData가 호출될 것이므로, Awake에서는 최소한의 초기화만 수행하거나
        // InitializeFromData가 호출되지 않았을 경우를 대비한 기본값 설정을 유지합니다.
        if (skills.Count == 0 && skillListForInspector.Count > 0)
        {
            InitializeSkillsFromInspector(); // Inspector 값으로 스킬 초기화 (폴백)
        }

        if (MaxHealth <= 0) // MaxHealth가 아직 설정되지 않았다면 (예: Inspector에서만 설정된 경우)
        {
            MaxHealth = CalculateMaxHealthFromStats();
        }
        CurrentHealth = MaxHealth; // 항상 MaxHealth로 시작

        actionPoints = actionPointsPerTurn; // 행동 포인트 초기화
    }

    private void InitializeSkillsFromInspector()
    {
        skills.Clear();
        foreach (var skillValue in skillListForInspector)
        {
            if (!skills.ContainsKey(skillValue.skill))
            {
                skills.Add(skillValue.skill, skillValue.level);
            }
            else
            {
                Debug.LogWarning($"Duplicate skill type '{skillValue.skill}' in inspector list for {entityName}. Using first entry.");
            }
        }
    }

    // Inspector 디버깅용으로 skills 딕셔너리 내용을 skillListForInspector에 반영
    private void SyncSkillsToInspectorList()
    {
        skillListForInspector.Clear();
        foreach (var skillPair in skills)
        {
            skillListForInspector.Add(new SkillValue(skillPair.Key, skillPair.Value));
        }
    }

    public int GetSkillLevel(SkillType skill)
    {
        skills.TryGetValue(skill, out int level);
        return level;
    }

    public void SetSkillLevel(SkillType skill, int level)
    {
        skills[skill] = Mathf.Max(0, level);
        // SyncSkillsToInspectorList(); // 매번 호출하면 성능에 영향 줄 수 있음, 필요시 호출
    }

    public virtual void SetGridPosition(int x, int y) { gridX = x; gridY = y; }
    public virtual void Move(int dx, int dy) { gridX += dx; gridY += dy; actionPoints -= 1.0f; }

    public virtual void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        Debug.Log($"{entityName} takes {amount} damage. Health: {CurrentHealth}/{MaxHealth}");
        if (CurrentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        Debug.Log($"{entityName} dies.");
        displayChar = '%'; displayColor = Color.grey; entityBackgroundColor = Color.clear;
        blocksMovement = false; renderOrder = 0;
        string originalName = entityName; entityName = $"Remains of {originalName}";
        currentDisposition = Disposition.Neutral; currentAiState = AIState.Idle;
        if (this is Player) GameManager.Instance?.PlayerDied();
        else { UIManager.Instance?.AddMessage($"The {originalName} dies!", Color.red); GameManager.Instance?.CurrentMapData?.RemoveEntity(this); }
    }

    public virtual void ReplenishActionPoints() { actionPoints += actionPointsPerTurn; actionPoints = Mathf.Min(actionPoints, actionPointsPerTurn * 2); }

    public virtual void PerformAction()
    {
        if (actionPoints < 1.0f) return;
        // Debug.Log($"{entityName} (AP:{actionPoints:F1}, Disp:{currentDisposition}, Intel:{intelligenceLevel}, State:{currentAiState}) performs an action at ({gridX},{gridY}).");
        switch (currentDisposition)
        {
            case Disposition.Hostile: PerformHostileAction(); break;
            case Disposition.Neutral: PerformNeutralAction(); break;
            case Disposition.Peaceful: PerformPeacefulAction(); break;
            case Disposition.Friendly: PerformFriendlyAction(); break;
            case Disposition.FellowCreature: PerformFellowCreatureAction(); break;
            default: currentAiState = AIState.Idle; if (actionPoints >= 1.0f) actionPoints -= 1.0f; break;
        }
    }

    protected virtual void PerformHostileAction()
    {
        Player player = GameManager.Instance?.PlayerInstance;
        if (player == null) { WanderOrIdle(); return; }
        bool canSeePlayer = IsEntityInMySight(player);
        if (canSeePlayer) { currentTargetEntity = player; lastKnownTargetPosition = new Vector2Int(player.gridX, player.gridY); currentAiState = AIState.Chasing; }
        else { if (currentTargetEntity == player && lastKnownTargetPosition != (Vector2Int.one * -1)) { currentAiState = AIState.Searching; } else { currentAiState = AIState.Wandering; currentTargetEntity = null; } }

        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Brainless:
                if (currentTargetEntity != null) TryAttackOrMoveTowards(currentTargetEntity, true);
                else if (lastKnownTargetPosition != (Vector2Int.one * -1)) TryMoveTowards(lastKnownTargetPosition, true);
                else Wander();
                break;
            case IntelligenceLevel.Animal:
                if (CurrentHealth < MaxHealth * 0.3f && canSeePlayer) { currentAiState = AIState.Fleeing; TryFleeFrom(player); break; }
                if (currentTargetEntity != null) TryAttackOrMoveTowards(currentTargetEntity);
                else if (lastKnownTargetPosition != (Vector2Int.one * -1)) TryMoveTowards(lastKnownTargetPosition);
                else Wander();
                break;
            case IntelligenceLevel.Human:
                if (CurrentHealth < MaxHealth * 0.5f && canSeePlayer) { currentAiState = AIState.Fleeing; TryFleeFrom(player); break; }
                if (currentTargetEntity != null) TryAttackOrMoveTowards(currentTargetEntity);
                else if (lastKnownTargetPosition != (Vector2Int.one * -1)) TryMoveTowards(lastKnownTargetPosition);
                else Wander();
                break;
        }
    }

    protected virtual void PerformNeutralAction() { WanderOrIdle(); }
    protected virtual void PerformPeacefulAction()
    {
        Entity hostileTarget = FindNearestHostileMonster();
        if (hostileTarget != null && IsEntityInMySight(hostileTarget)) { currentAiState = AIState.Chasing; TryAttackOrMoveTowards(hostileTarget); }
        else { WanderOrIdle(); }
    }
    protected virtual void PerformFriendlyAction()
    {
        Player player = GameManager.Instance?.PlayerInstance;
        if (player == null) { WanderOrIdle(); return; }
        Entity playerTarget = player.GetCurrentTarget();
        Entity nearestEnemy = FindNearestHostileMonster(true);
        if (playerTarget != null && IsEntityInMySight(playerTarget)) { currentAiState = AIState.Attacking; TryAttackOrMoveTowards(playerTarget); }
        else if (nearestEnemy != null && IsEntityInMySight(nearestEnemy)) { currentAiState = AIState.Chasing; TryAttackOrMoveTowards(nearestEnemy); }
        else { currentAiState = AIState.Chasing; TryMoveTowards(new Vector2Int(player.gridX, player.gridY)); }
    }
    protected virtual void PerformFellowCreatureAction()
    {
        Entity attackerOfPlayer = FindAttackerOfPlayer();
        if (attackerOfPlayer != null && IsEntityInMySight(attackerOfPlayer)) { currentAiState = AIState.Attacking; TryAttackOrMoveTowards(attackerOfPlayer); }
        else { WanderOrIdle(); }
    }

    protected bool IsEntityInMySight(Entity targetEntity)
    {
        if (targetEntity == null || GameManager.Instance?.CurrentMapData == null) return false;
        MapData currentMap = GameManager.Instance.CurrentMapData;
        bool[,] myFovMap = currentMap.CalculateFov(new Vector2Int(this.gridX, this.gridY), this.sightRadius);
        if (currentMap.IsInBounds(targetEntity.gridX, targetEntity.gridY)) { return myFovMap[targetEntity.gridY, targetEntity.gridX]; }
        return false;
    }

    protected virtual void TryAttackOrMoveTowards(Entity target, bool ignoreObstacles = false)
    {
        if (target == null || GameManager.Instance?.CurrentMapData == null) { if (actionPoints >= 1.0f) actionPoints -= 1.0f; return; }
        int dx = target.gridX - gridX; int dy = target.gridY - gridY;
        if (Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1 && (dx != 0 || dy != 0)) { GameManager.Instance?.ProcessEntityAttack(this, target); actionPoints -= 1.0f; return; }
        TryMoveTowards(new Vector2Int(target.gridX, target.gridY), ignoreObstacles);
    }

    protected virtual void TryMoveTowards(Vector2Int targetPosition, bool ignoreObstacles = false)
    {
        if (GameManager.Instance?.CurrentMapData == null) { if (actionPoints >= 1.0f) actionPoints -= 1.0f; return; }
        int dx = targetPosition.x - gridX; int dy = targetPosition.y - gridY;
        int moveX = 0; int moveY = 0;
        if (dx > 0) moveX = 1; else if (dx < 0) moveX = -1;
        if (dy > 0) moveY = 1; else if (dy < 0) moveY = -1;
        if (moveX == 0 && moveY == 0) { if (actionPoints >= 1.0f) actionPoints -= 1.0f; return; }

        MapData map = GameManager.Instance.CurrentMapData;
        Vector2Int nextPos; bool moved = false;
        if (moveX != 0 && moveY != 0) { nextPos = new Vector2Int(gridX + moveX, gridY + moveY); if (map.IsInBounds(nextPos.x, nextPos.y) && (ignoreObstacles || map.Tiles[nextPos.y, nextPos.x].walkable) && (ignoreObstacles || map.GetBlockingEntityAt(nextPos.x, nextPos.y) == null)) { Move(moveX, moveY); moved = true; } }
        if (!moved && moveX != 0) { nextPos = new Vector2Int(gridX + moveX, gridY); if (map.IsInBounds(nextPos.x, nextPos.y) && (ignoreObstacles || map.Tiles[nextPos.y, nextPos.x].walkable) && (ignoreObstacles || map.GetBlockingEntityAt(nextPos.x, nextPos.y) == null)) { Move(moveX, 0); moved = true; } }
        if (!moved && moveY != 0) { nextPos = new Vector2Int(gridX, gridY + moveY); if (map.IsInBounds(nextPos.x, nextPos.y) && (ignoreObstacles || map.Tiles[nextPos.y, nextPos.x].walkable) && (ignoreObstacles || map.GetBlockingEntityAt(nextPos.x, nextPos.y) == null)) { Move(0, moveY); moved = true; } }

        if (!moved) { if (actionPoints >= 1.0f) actionPoints -= 1.0f; }
    }

    protected virtual void TryFleeFrom(Entity target) { if (target == null) { if (actionPoints >= 1.0f) actionPoints -= 1.0f; return; } int dx = gridX - target.gridX; int dy = gridY - target.gridY; TryMoveTowards(new Vector2Int(gridX + System.Math.Sign(dx), gridY + System.Math.Sign(dy))); }
    protected virtual void WanderOrIdle() { if (UnityEngine.Random.value < 0.7f) Wander(); else { if (actionPoints >= 1.0f) actionPoints -= 1.0f; currentAiState = AIState.Idle; } }
    protected virtual void Wander() { if (GameManager.Instance?.CurrentMapData == null) { if (actionPoints >= 1.0f) actionPoints -= 1.0f; return; } currentAiState = AIState.Wandering; int randomDx = UnityEngine.Random.Range(-1, 2); int randomDy = UnityEngine.Random.Range(-1, 2); TryMoveTowards(new Vector2Int(gridX + randomDx, gridY + randomDy)); }
    protected Entity FindNearestHostileMonster(bool targetPlayerHostileOnly = false)
    {
        Entity nearest = null; float minDistance = float.MaxValue; MapData map = GameManager.Instance?.CurrentMapData; Player player = GameManager.Instance?.PlayerInstance; if (map == null) return null;
        foreach (Entity entity in map.Entities) { if (entity == this || entity == player) continue; bool isHostileToThis = (entity.factionId != 0 && this.factionId != 0 && entity.factionId != this.factionId) || entity.currentDisposition == Disposition.Hostile; bool isHostileToPlayer = player != null && ((entity.factionId != 0 && player.factionId != 0 && entity.factionId != player.factionId) || entity.currentDisposition == Disposition.Hostile); if (targetPlayerHostileOnly ? isHostileToPlayer : isHostileToThis) { if (IsEntityInMySight(entity)) { float distance = Vector2Int.Distance(new Vector2Int(gridX, gridY), new Vector2Int(entity.gridX, entity.gridY)); if (distance < minDistance) { minDistance = distance; nearest = entity; } } } }
        return nearest;
    }
    protected Entity FindAttackerOfPlayer() { return FindNearestHostileMonster(true); }
    public virtual void ReactToEntity(Entity otherEntity) { /* 반응 로직 */ }
}
