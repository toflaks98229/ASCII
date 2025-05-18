// Entity.cs
using UnityEngine;
using System.Collections.Generic; // Dictionary ����� ���� �߰�

/// <summary>
/// ��ƼƼ�� �⺻���� ������ �����մϴ�. (DCSS�� Disposition ����)
/// Defines the basic disposition of an entity. (Inspired by DCSS Disposition)
/// </summary>
public enum Disposition
{
    Hostile, Neutral, Peaceful, Friendly, FellowCreature
}

/// <summary>
/// ��ƼƼ�� ���� ������ �����մϴ�. (DCSS�� Monster Intelligence ����)
/// Defines the intelligence level of an entity. (Inspired by DCSS Monster Intelligence)
/// </summary>
public enum IntelligenceLevel
{
    Brainless, Animal, Human
}

/// <summary>
/// ��ƼƼ�� ���� � �ൿ ���¿� �ִ����� ��Ÿ���ϴ�.
/// Represents the current behavioral state of the entity.
/// </summary>
public enum AIState
{
    Idle, Wandering, Searching, Chasing, Attacking, Fleeing, UsingAbility, Interacting
}

/// <summary>
/// �پ��� ��ų�� ������ �����մϴ�.
/// Defines various types of skills.
/// </summary>
public enum SkillType
{
    // ���� ��ų (Combat Skills)
    Fighting, ShortBlades, LongBlades, Axes, MacesAndFlails, Polearms, Staves,
    Slings, Bows, Crossbows, Throwing, UnarmedCombat,
    // ��� ��ų (Defensive Skills)
    Armour, Dodging, Shields,
    // ���� ��ų (Magic Skills)
    Spellcasting, Conjurations, Hexes, Charms, Summonings, Necromancy,
    Translocations, Transmutations, FireMagic, IceMagic, AirMagic, EarthMagic, PoisonMagic,
    Invocations, Evocations,
    // ��Ÿ ��ų (Other Skills)
    Stealth, Traps
}

/// <summary>
/// Inspector���� ��ų ������ ���� �����ϰ� MonsterData������ ����ϱ� ���� ����ȭ ������ ����ü�Դϴ�.
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
    [Header("�⺻ �Ӽ� (Entity Properties)")]
    [Tooltip("��ƼƼ�� �̸� (��: Player, Orc, Slime)")]
    public string entityName = "<Unnamed>";
    [Tooltip("�ʿ� ǥ�õ� ��ƼƼ�� ����")]
    public char displayChar = '?';
    [Tooltip("��ƼƼ ������ ����")]
    public Color displayColor = Color.white;
    [Tooltip("��ƼƼ ������ ���� (Color.clear�� Ÿ�� ���� ���)")]
    [ColorUsage(true, true)]
    public Color entityBackgroundColor = Color.clear;
    [Tooltip("�� ��ƼƼ�� �̵��� ������ ����")]
    public bool blocksMovement = true;
    [Tooltip("������ ���� (�������� ���� �׷���). ����(1)�� ������/��ü(0) ���� �׷������� �մϴ�.")]
    public int renderOrder = 1;

    [Header("�ٽ� �ɷ�ġ (Core Stats)")]
    [Tooltip("�� (Strength): ���� ���ݷ�, ���� �Ѱ�, ����� �г�Ƽ ���� � ����")]
    public int Strength = 10;
    [Tooltip("��ø�� (Dexterity): ���߷�, ȸ����, �Ϻ� ���� ���ݷ�, ���� � ����")]
    public int Dexterity = 10;
    [Tooltip("���� (Intelligence): �ֹ� ������, �ֹ� ����, �ִ� MP � ����")]
    public int Intelligence = 10;
    [Tooltip("�ǰ� (Constitution): �ִ� HP, HP ȸ����, ��/���� ���� � ����")]
    public int Constitution = 10;
    [Tooltip("���� (Wisdom): ���� ����, Ư�� �ֹ�(�ż�/���� �迭) ������/����, MP ȸ���� � ����")]
    public int Wisdom = 10;

    [Header("���� ���� �Ļ� �ɷ�ġ (Combat Derived Stats)")]
    [Tooltip("�ִ� ü�� (���ȿ� ���� �ַ� ����)")]
    public int MaxHealth = 10;
    [Tooltip("���� ü��")]
    public int CurrentHealth = 10;

    [Header("AI �� �ൿ ���� �Ӽ� (AI & Behavior Properties)")]
    [Tooltip("��ƼƼ�� ���� ���� (�÷��̾ ���� �µ� ��)")]
    public Disposition currentDisposition = Disposition.Hostile;
    [Tooltip("��ƼƼ�� ���� ���� (�ൿ ���Ͽ� ����)")]
    public IntelligenceLevel intelligenceLevel = IntelligenceLevel.Animal;
    [Tooltip("��ƼƼ�� ���� ���� ID (0: �߸�/���Ҽ�)")]
    public int factionId = 0;
    [Tooltip("��ƼƼ�� �þ� �ݰ� (Ÿ�� ����)")]
    public int sightRadius = 8;
    [Tooltip("�� ��ƼƼ�� ���������� ������ �÷��̾� �Ǵ� �ֿ� Ÿ���� ��ġ")]
    public Vector2Int lastKnownTargetPosition = Vector2Int.one * -1;
    [Tooltip("���� AI �ൿ ����")]
    public AIState currentAiState = AIState.Idle;
    [Tooltip("�ൿ ����Ʈ. 0 �̸��� �Ǹ� �ൿ �Ұ�.")]
    public float actionPoints = 0f;
    [Tooltip("�ϴ� ȸ���Ǵ� �ൿ ����Ʈ (�ӵ� ����)")]
    public float actionPointsPerTurn = 1.0f;

    [Header("��ų ���� (Skill Levels)")]
    // MonsterData�� �ʱ�ȭ�ǹǷ� Inspector���� ���� ������ �ʿ�� �پ��.
    // ����� �Ǵ� Ư�� ��ƼƼ�� �⺻�� ���������� ���ܵ� �� ����.
    [SerializeField]
    private List<SkillValue> skillListForInspector = new List<SkillValue>();
    private Dictionary<SkillType, int> skills = new Dictionary<SkillType, int>();

    [Header("�г�Ƽ ���� (Penalty Factors)")]
    [Tooltip("���� ��� ������ ���� �������� �ൿ ���� ��ġ (0.0: �г�Ƽ ����). ȸ��, ���� � ����.")]
    [Range(0f, 1f)]
    public float encumbrancePenaltyFactor = 0f;
    [Tooltip("���� �������� ���� �ֹ� ������ ����ġ (���밪 �Ǵ� �����).")]
    public float armorSpellFailurePenalty = 0f;

    /*[HideInInspector]*/ public int gridX;
    /*[HideInInspector]*/ public int gridY;
    protected Entity currentTargetEntity = null;

    /// <summary>
    /// MonsterData ScriptableObject�� ������ ��ƼƼ�� �ʱ�ȭ�մϴ�.
    /// �� �޼���� ��ƼƼ�� ������ �� GameManager�� ���� ȣ��Ǿ�� �մϴ�.
    /// </summary>
    /// <param name="data">��ƼƼ�� ������ MonsterData</param>
    public virtual void InitializeFromData(MonsterData data)
    {
        if (data == null)
        {
            Debug.LogError($"InitializeFromData: MonsterData is null for {this.gameObject.name}! Using default/inspector values if any.");
            // MonsterData�� ������ Awake���� ������ ���̳� Inspector ���� ����
            if (skills.Count == 0 && skillListForInspector.Count > 0) InitializeSkillsFromInspector();
            MaxHealth = CalculateMaxHealthFromStats(); // ���� ��� ü�� ���
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

        // MaxHealth�� MonsterData�� ���� �⺻���� �ϵ�, ���ȿ� ���� ������ ���� ����
        // ���⼭�� MonsterData�� maxHealth�� ����ϰ�, �ʿ�� CalculateMaxHealthFromStats�� ����
        MaxHealth = data.maxHealth > 0 ? data.maxHealth : CalculateMaxHealthFromStats();
        CurrentHealth = MaxHealth;

        currentDisposition = data.initialDisposition;
        intelligenceLevel = data.intelligenceLevel;
        factionId = data.factionId;
        sightRadius = data.sightRadius;
        actionPointsPerTurn = data.actionPointsPerTurn;

        // ��ų �ʱ�ȭ (MonsterData�� initialSkills ���)
        skills.Clear();
        if (data.initialSkills != null)
        {
            foreach (var skillValue in data.initialSkills)
            {
                SetSkillLevel(skillValue.skill, skillValue.level); // ���� ��ųʸ� ������Ʈ
            }
        }
        // Inspector�� ����Ʈ�� ����ȭ (������, ������)
        SyncSkillsToInspectorList();


        encumbrancePenaltyFactor = data.encumbrancePenaltyFactor;
        armorSpellFailurePenalty = data.armorSpellFailurePenalty;

        blocksMovement = data.blocksMovement;
        renderOrder = data.renderOrder;

        actionPoints = actionPointsPerTurn; // �ൿ ����Ʈ �ʱ�ȭ
        currentAiState = AIState.Idle;      // AI ���� �ʱ�ȭ
        lastKnownTargetPosition = Vector2Int.one * -1; // ������ Ÿ�� ��ġ �ʱ�ȭ
        currentTargetEntity = null; // ���� Ÿ�� �ʱ�ȭ

        Debug.Log($"Entity '{entityName}' initialized from MonsterData '{data.name}'. HP: {CurrentHealth}/{MaxHealth}");
    }

    /// <summary>
    /// ���� ������ ������� �ִ� ü���� ����մϴ�. (���� �����ο� �°� ���� �ʿ�)
    /// </summary>
    protected virtual int CalculateMaxHealthFromStats()
    {
        // ����: �⺻ 5 + �ǰ� 1�� 1 HP, �� 5�� 1 HP
        return 5 + Constitution + (Strength / 5) + (GetSkillLevel(SkillType.Fighting) / 2);
    }

    protected virtual void Awake()
    {
        // InitializeFromData�� ȣ��� ���̹Ƿ�, Awake������ �ּ����� �ʱ�ȭ�� �����ϰų�
        // InitializeFromData�� ȣ����� �ʾ��� ��츦 ����� �⺻�� ������ �����մϴ�.
        if (skills.Count == 0 && skillListForInspector.Count > 0)
        {
            InitializeSkillsFromInspector(); // Inspector ������ ��ų �ʱ�ȭ (����)
        }

        if (MaxHealth <= 0) // MaxHealth�� ���� �������� �ʾҴٸ� (��: Inspector������ ������ ���)
        {
            MaxHealth = CalculateMaxHealthFromStats();
        }
        CurrentHealth = MaxHealth; // �׻� MaxHealth�� ����

        actionPoints = actionPointsPerTurn; // �ൿ ����Ʈ �ʱ�ȭ
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

    // Inspector ���������� skills ��ųʸ� ������ skillListForInspector�� �ݿ�
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
        // SyncSkillsToInspectorList(); // �Ź� ȣ���ϸ� ���ɿ� ���� �� �� ����, �ʿ�� ȣ��
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
    public virtual void ReactToEntity(Entity otherEntity) { /* ���� ���� */ }
}
