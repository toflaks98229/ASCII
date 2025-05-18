// MonsterData.cs
using UnityEngine;
using System.Collections.Generic; // ��ų ����Ʈ ���� ����

[CreateAssetMenu(fileName = "New MonsterData", menuName = "Roguelike/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("�⺻ ����")]
    public string monsterName = "����";
    public char displayChar = 'm';
    public Color displayColor = Color.white;
    [ColorUsage(true, true)]
    public Color backgroundColor = Color.clear;

    [Header("�ٽ� �ɷ�ġ")]
    public int strength = 10;
    public int dexterity = 10;
    public int Intelligence = 10;
    public int constitution = 10;
    public int wisdom = 10;
    // public int charisma = 10; // �ʿ��

    [Header("�Ļ� �ɷ�ġ (�⺻��, �����δ� ���� ��� ���� �� ����)")]
    public int maxHealth = 10;
    // public int maxMana = 10;

    [Header("AI �� �ൿ")]
    public Disposition initialDisposition = Disposition.Hostile;
    public IntelligenceLevel intelligenceLevel = IntelligenceLevel.Animal;
    public int factionId = 1; // 0: �÷��̾� ����, 1: �Ϲ� ���� ���� ��
    public int sightRadius = 8;
    public float actionPointsPerTurn = 1.0f;
    // TODO: Ư�� AI ��ũ��Ʈ�� �ൿ ���� ID ���� �߰��Ͽ� �پ��� AI ���� ���� ����

    [Header("��ų (�ʱ� ����)")]
    public List<SkillValue> initialSkills = new List<SkillValue>(); // Entity.cs�� SkillValue ����ü ��Ȱ��

    [Header("�г�Ƽ (�⺻��)")]
    public float encumbrancePenaltyFactor = 0f;
    public float armorSpellFailurePenalty = 0f;

    [Header("��Ÿ")]
    public bool blocksMovement = true;
    public int renderOrder = 1;
    // TODO: ��� ���̺� ID, Ư�� �ɷ� ���, ����/����� �� �߰� ����
}
