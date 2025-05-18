// MonsterData.cs
using UnityEngine;
using System.Collections.Generic; // 스킬 리스트 등을 위해

[CreateAssetMenu(fileName = "New MonsterData", menuName = "Roguelike/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterName = "몬스터";
    public char displayChar = 'm';
    public Color displayColor = Color.white;
    [ColorUsage(true, true)]
    public Color backgroundColor = Color.clear;

    [Header("핵심 능력치")]
    public int strength = 10;
    public int dexterity = 10;
    public int Intelligence = 10;
    public int constitution = 10;
    public int wisdom = 10;
    // public int charisma = 10; // 필요시

    [Header("파생 능력치 (기본값, 실제로는 스탯 기반 계산될 수 있음)")]
    public int maxHealth = 10;
    // public int maxMana = 10;

    [Header("AI 및 행동")]
    public Disposition initialDisposition = Disposition.Hostile;
    public IntelligenceLevel intelligenceLevel = IntelligenceLevel.Animal;
    public int factionId = 1; // 0: 플레이어 세력, 1: 일반 몬스터 세력 등
    public int sightRadius = 8;
    public float actionPointsPerTurn = 1.0f;
    // TODO: 특정 AI 스크립트나 행동 패턴 ID 등을 추가하여 다양한 AI 로직 연결 가능

    [Header("스킬 (초기 레벨)")]
    public List<SkillValue> initialSkills = new List<SkillValue>(); // Entity.cs의 SkillValue 구조체 재활용

    [Header("패널티 (기본값)")]
    public float encumbrancePenaltyFactor = 0f;
    public float armorSpellFailurePenalty = 0f;

    [Header("기타")]
    public bool blocksMovement = true;
    public int renderOrder = 1;
    // TODO: 드랍 테이블 ID, 특수 능력 목록, 저항/취약점 등 추가 가능
}
