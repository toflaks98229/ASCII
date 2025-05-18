// Goblin.cs
using UnityEngine;

/// <summary>
/// 고블린 몬스터를 나타내는 클래스입니다. Entity를 상속받습니다.
/// Represents a Goblin monster, inheriting from Entity.
/// </summary>
public class Goblin : Entity
{
    /// <summary>
    /// 고블린 특유의 기본값을 설정하기 위해 Awake를 재정의합니다.
    /// Overrides Awake to set Goblin-specific defaults.
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake를 먼저 호출합니다.

        // --- 고블린 기본 설정 ---
        entityName = "고블린";         // 이름
        displayChar = 'g';             // 표시 문자
        displayColor = Color.green;    // 표시 색상 (연두색)
        entityBackgroundColor = Color.clear; // 배경색 없음 (타일 배경색 사용)

        // 핵심 능력치 (기본값보다 약간 낮게 설정)
        Strength = 8;
        Dexterity = 12; // 약간 민첩하게
        Intelligence = 6;
        Constitution = 8;
        Wisdom = 6;

        // 파생 능력치 (Constitution 기반으로 MaxHealth 설정 예시)
        MaxHealth = 5 + (Constitution / 2); // 예: 건강 2당 체력 1 증가 + 기본 5
        CurrentHealth = MaxHealth;

        // AI 및 행동 관련
        currentDisposition = Disposition.Hostile;       // 기본적으로 적대적
        intelligenceLevel = IntelligenceLevel.Animal; // 동물 수준의 지능
        factionId = 1; // 예: 몬스터 세력 (플레이어와 다른 factionId)
        sightRadius = 7; // 시야 반경
        actionPointsPerTurn = 1.0f; // 턴당 행동 포인트

        // 스킬 레벨 (기본적으로 낮거나 없음)
        // 필요하다면 SetSkillLevel을 사용하여 초기 스킬 설정
        // 예: SetSkillLevel(SkillType.ShortBlades, 2);

        // 패널티 (기본적으로 없음)
        encumbrancePenaltyFactor = 0f;
        armorSpellFailurePenalty = 0f;

        // 기타
        blocksMovement = true;
        renderOrder = 1; // 일반 액터
    }

    /// <summary>
    /// 고블린의 턴 행동을 정의합니다.
    /// Defines the Goblin's actions for its turn.
    /// </summary>
    public override void PerformAction()
    {
        if (actionPoints < 1.0f) return; // 행동 포인트 없으면 행동 불가

        // Debug.Log($"{entityName} (AP:{actionPoints:F1}) is performing an action at ({gridX},{gridY}). State: {currentAiState}");

        // 현재는 Entity의 기본 PerformHostileAction을 사용하도록 합니다.
        // 필요에 따라 고블린만의 특별한 행동 로직을 여기에 추가할 수 있습니다.
        // 예: 플레이어가 약하면 더 공격적으로, 강하면 도망가려 시도 등
        base.PerformAction(); // Entity의 기본 행동 로직 호출 (Hostile 성향에 따라 작동)

        // 만약 base.PerformAction()이 actionPoints를 소모하지 않는다면, 여기서 소모해야 합니다.
        // 하지만 현재 Entity.cs의 Move(), TryAttackOrMoveTowards() 등에서 actionPoints를 소모하므로
        // base.PerformAction()을 호출하면 그 안에서 AP가 소모됩니다.
        // 만약 base.PerformAction()이 아무것도 안하고 턴을 넘기는 경우가 있다면,
        // 여기서 actionPoints -= 1.0f; 와 같이 AP를 명시적으로 소모해야 합니다.
    }

    // 필요하다면 고블린만의 특별한 Die() 로직, TakeDamage() 로직 등을 재정의할 수 있습니다.
    // protected override void Die()
    // {
    //     base.Die();
    //     Debug.Log("고블린은 작은 주머니를 떨어뜨렸습니다!"); // 예시: 아이템 드랍
    // }
}
