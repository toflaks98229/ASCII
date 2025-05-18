using UnityEngine;
using Unity;

/// <summary>
/// Represents the player character. Inherits from Entity.
/// �÷��̾� ĳ���͸� ��Ÿ���ϴ�. Entity Ŭ������ ��ӹ޽��ϴ�.
/// Contains player-specific stats and potentially logic.
/// �÷��̾� ������ �ɷ�ġ �� ������ ������ �� �ֽ��ϴ�.
/// </summary>
public class Player : Entity // Inherits MaxHealth, CurrentHealth, Strength etc. from Entity
{
    // --- Player Specific Stats (Not already in Entity) ---
    // --- �÷��̾� ���� �ɷ�ġ (Entity�� ���� �͵�) ---
    [Header("Player Specific Stats")]
    public int Level = 1; // Level is specific to the player ������ �÷��̾� ����
                          // TODO: Add other player-only stats like Experience, Mana, etc.
                          // TODO: ����ġ, ���� �� �ٸ� �÷��̾� ���� �ɷ�ġ �߰�


    /// <summary>
    /// �÷��̾ ���� ��ǥ�� �ϰ� �ִ� ��ƼƼ�Դϴ�.
    /// The entity the player is currently targeting (e.g., last attacked).
    /// </summary>
    private Entity _currentTarget;

    /// <summary>
    /// Overrides Awake to set player-specific defaults for inherited stats.
    /// ��ӵ� �ɷ�ġ�� ���� �÷��̾ �⺻���� �����ϱ� ���� Awake�� �������մϴ�.
    /// </summary>
    protected override void Awake()
    {
        // Call the base class Awake first (important!) �⺻ Ŭ���� Awake ���� ȣ�� (�߿�!)
        base.Awake();

        // Set player-specific defaults �÷��̾ �⺻�� ����
        if (string.IsNullOrEmpty(entityName) || entityName == "<Unnamed>")
        {
            entityName = "Player";
        }
        displayChar = '@';
        displayColor = Color.green;
        blocksMovement = true; // Player usually blocks movement �÷��̾�� ���� �̵��� ����
        renderOrder = 1;       // Render player above items/corpses ������/��ü ���� �÷��̾� ������

        // Set initial values for inherited stats (can be overridden by Inspector)
        // ��ӵ� �ɷ�ġ �ʱⰪ ���� (Inspector���� ������ ����)
        MaxHealth = 30; // Example: Player starts with more health ����: �÷��̾�� �� ���� ü������ ����
        CurrentHealth = MaxHealth; // Start with full health �ִ� ü������ ����
        Strength = 6; // Example: Slightly stronger ����: �ణ �� ����
        Dexterity = 6;
        Intelligence = 6;
    }

    /// <summary>
    /// Called on the frame when a script is enabled. Requests initial UI update.
    /// ��ũ��Ʈ�� Ȱ��ȭ�� �����ӿ� ȣ��˴ϴ�. �ʱ� UI ������Ʈ�� ��û�մϴ�.
    /// </summary>
    void Start()
    {
        // Request initial UI update for player info
        // �÷��̾� ������ ���� �ʱ� UI ������Ʈ ��û
        UIManager.Instance?.UpdatePlayerInfo(this);
    }

    /// <summary>
    /// �÷��̾ ���� ��ǥ�� �ϰ� �ִ� ��ƼƼ�� ��ȯ�մϴ�.
    /// Returns the entity the player is currently targeting.
    /// </summary>
    /// <returns>���� ��ǥ ��ƼƼ, ������ null</returns>
    public Entity GetCurrentTarget()
    {
        // ���� Ÿ���� ��ȿ���� (��: ���� �ʾҴ���) Ȯ���ϴ� ���� �߰� ����
        if (_currentTarget != null && _currentTarget.CurrentHealth <= 0)
        {
            _currentTarget = null; // Ÿ���� �׾����� null�� ����
        }
        return _currentTarget;
    }

    /// <summary>
    /// �÷��̾��� ���� ��ǥ ��ƼƼ�� �����մϴ�.
    /// �ַ� ���� �� GameManager���� ȣ��˴ϴ�.
    /// Sets the player's current target entity.
    /// Typically called by GameManager when the player attacks.
    /// </summary>
    /// <param name="target">���ο� ��ǥ ��ƼƼ</param>
    public void SetCurrentTarget(Entity target)
    {
        _currentTarget = target;
        // Debug.Log($"Player's new target set to: {target?.entityName ?? "null"}");
    }

    /// <summary>
    /// Overrides the Die method for player-specific game over logic.
    /// �÷��̾ ���� ���� ������ ���� Die �޼��带 �������մϴ�.
    /// </summary>
    protected override void Die()
    {
        // Don't call base.Die() to prevent default corpse behavior
        // �⺻ ��ü ������ �����ϱ� ���� base.Die() ȣ�� �� ��

        Debug.Log($"{entityName} has died! GAME OVER.");
        // Change appearance to a corpse ��ü ������� ����
        displayChar = '%';
        displayColor = Color.red;
        // Player death might not remove blocking immediately depending on game rules
        // ���� ��Ģ�� ���� �÷��̾� ����� ��� �̵� ���ظ� �������� ���� �� ����
        // blocksMovement = false;
        renderOrder = 0; // Render below actors ���� �Ʒ��� ������

        // Inform GameManager about player death �÷��̾� ����� GameManager�� �˸�
        GameManager.Instance?.PlayerDied();
        // The GameManager will handle the actual Game Over state ���� �Ŵ����� ���� ���� ���� ���� ó��
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
    // �ٸ� �÷��̾� ���� �޼��� �߰� (��: ������)
    // public void GainExperience(int amount) { ... }
}
