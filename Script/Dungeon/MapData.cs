// MapData.cs
using System.Collections.Generic;
using UnityEngine;
using System;

// RectInt struct (������ ����)
public readonly struct RectInt
{
    public readonly int xMin, yMin, xMax, yMax;
    public RectInt(int x, int y, int width, int height)
    { xMin = x; yMin = y; xMax = x + width; yMax = y + height; }
    public Vector2Int Center => new Vector2Int((xMin + xMax) / 2, (yMin + yMax) / 2);
    public bool Intersects(RectInt other)
    { return xMin <= other.xMax && xMax >= other.xMin && yMin <= other.yMax && yMax >= other.yMin; }
}

public class MapData
{
    #region Public Properties
    public readonly int Width;
    public readonly int Height;
    public TileType[,] Tiles { get; private set; }

    /// <summary>
    /// �÷��̾��� ���� �þ� ������ �����մϴ�. (True: ����, False: �� ����)
    /// Stores the player's current field of view. (True: visible, False: not visible)
    /// </summary>
    public bool[,] Visible { get; private set; }

    /// <summary>
    /// �÷��̾ Ž���� ���� ������ �����մϴ�. (True: Ž���, False: ��Ž��)
    /// Stores the areas explored by the player. (True: explored, False: unexplored)
    /// </summary>
    public bool[,] Explored { get; private set; }

    public List<Entity> Entities { get; private set; }
    public Vector2Int UpStairsLocation { get; private set; } = Vector2Int.one * -1;
    public Vector2Int DownStairsLocation { get; private set; } = Vector2Int.one * -1;
    public Vector2Int PlayerSuggestedStartPosition { get; internal set; } = Vector2Int.one * -1;
    #endregion

    // FOV ��꿡 ���Ǵ� 8���� ��ȯ ��� (������ ����)
    private static readonly int[,] fovMultipliers = { { 1, 0, 0, 1 }, { 0, 1, 1, 0 }, { 0, -1, 1, 0 }, { -1, 0, 0, 1 }, { -1, 0, 0, -1 }, { 0, -1, -1, 0 }, { 0, 1, -1, 0 }, { 1, 0, 0, -1 } };


    #region Constructor and Basic Map Operations
    public MapData(int width, int height)
    {
        if (width <= 0 || height <= 0)
        { Debug.LogError($"[MapData Constructor] Invalid dimensions: {width}x{height}. Using 1x1."); width = Mathf.Max(1, width); height = Mathf.Max(1, height); }
        Width = width; Height = height;
        Tiles = new TileType[height, width];
        Visible = new bool[height, width];   // �÷��̾� �þ߿�
        Explored = new bool[height, width];  // �÷��̾� Ž���
        Entities = new List<Entity>();
        UpStairsLocation = Vector2Int.one * -1;
        DownStairsLocation = Vector2Int.one * -1;
    }

    public bool IsInBounds(int x, int y)
    { return x >= 0 && x < this.Width && y >= 0 && y < this.Height; }

    public void Fill(TileType tile)
    {
        if (tile == null) { Debug.LogError("[MapData Fill] Null TileType provided!"); return; }
        for (int y = 0; y < Height; y++) { for (int x = 0; x < Width; x++) { Tiles[y, x] = tile; } }
    }

    public void SetTile(int x, int y, TileType newTile)
    {
        if (IsInBounds(x, y))
        {
            if (newTile == null)
            { Debug.LogWarning($"[MapData SetTile] Null TileType at ({x},{y}). Using default Wall."); Tiles[y, x] = TileDatabase.Instance?.Wall; if (Tiles[y, x] == null) Debug.LogError("Default Wall TileType is null!"); }
            else { Tiles[y, x] = newTile; }
        }
    }

    internal void ForceSetStairsLocation(bool isUpStairs, int x, int y)
    {
        if (isUpStairs) UpStairsLocation = (IsInBounds(x, y) ? new Vector2Int(x, y) : Vector2Int.one * -1);
        else DownStairsLocation = (IsInBounds(x, y) ? new Vector2Int(x, y) : Vector2Int.one * -1);
        if (!IsInBounds(x, y) && x != -1 && y != -1) Debug.LogWarning($"Attempted to force set stairs location out of bounds at ({x},{y})");
    }
    #endregion


    #region Field of View (FOV) - �÷��̾� �� ����

    /// <summary>
    /// �÷��̾��� �þ߸� ����ϰ� MapData�� Visible �� Explored �迭�� ������Ʈ�մϴ�.
    /// Calculates the player's FOV and updates the Visible and Explored arrays in MapData.
    /// </summary>
    /// <param name="playerX">�÷��̾��� X ��ǥ</param>
    /// <param name="playerY">�÷��̾��� Y ��ǥ</param>
    /// <param name="radius">�÷��̾��� �þ� �ݰ�</param>
    public void ComputePlayerFov(int playerX, int playerY, int radius)
    {
        // ���� ��� Ÿ���� �� ���̴� ���·� �ʱ�ȭ (�÷��̾� �þ� ����)
        for (int y = 0; y < Height; y++) { for (int x = 0; x < Width; x++) { Visible[y, x] = false; } }

        // ���� FOV ��� �Լ� ȣ��
        bool[,] playerVisibility = CalculateFov(new Vector2Int(playerX, playerY), radius);

        // ���� �þ߸� Visible �� Explored�� ����
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (playerVisibility[y, x])
                {
                    Visible[y, x] = true;
                    Explored[y, x] = true; // ���̴� Ÿ���� �׻� Ž��� ������ ����
                }
            }
        }
    }

    /// <summary>
    /// ������ ������ �ݰ��� �������� �þ�(FOV)�� ����Ͽ� bool �迭�� ��ȯ�մϴ�.
    /// �� �Լ��� MapData�� Visible�̳� Explored ���¸� ���� �������� �ʽ��ϴ�.
    /// Calculates the Field of View (FOV) from a given origin and radius, returning it as a bool array.
    /// This method does NOT directly modify MapData's Visible or Explored states.
    /// </summary>
    /// <param name="origin">�þ� ����� ���� (��: ��ƼƼ�� ��ġ)</param>
    /// <param name="radius">�þ� �ݰ�</param>
    /// <returns>���� �þ� ������ ���� bool[Height, Width] �迭. True�� ����, False�� �� ����.</returns>
    public bool[,] CalculateFov(Vector2Int origin, int radius)
    {
        bool[,] visibilityMap = new bool[Height, Width]; // �� FOV ����� ���� �ӽ� �þ� ��
        if (!IsInBounds(origin.x, origin.y)) return visibilityMap; // ������ �� ���̸� �� �� ��ȯ

        // ������ �׻� ����
        SetCellVisibleInMap(visibilityMap, origin.y, origin.x);

        // 8�������� ���� �߻��Ͽ� �þ� ���
        for (int octant = 0; octant < 8; ++octant)
        {
            try
            {
                CastLightForMap(visibilityMap, origin.y, origin.x, radius, 1, 1.0f, 0.0f,
                                fovMultipliers[octant, 0], fovMultipliers[octant, 1],
                                fovMultipliers[octant, 2], fovMultipliers[octant, 3]);
            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.LogError($"[CalculateFov] Index Error in Octant {octant} for origin {origin}! {ex.Message}\n{ex.StackTrace}");
            }
        }
        return visibilityMap;
    }

    /// <summary>
    /// Ư�� Ÿ���� �þ߸� ������ ���θ� Ȯ���մϴ�. (���� FOV ��꿡 ���)
    /// Checks if a specific tile blocks line of sight. (Used for generic FOV calculation)
    /// </summary>
    private bool DoesTileBlockLight(int y, int x)
    {
        if (!IsInBounds(x, y)) return true; // �� ���� ���� �þ߸� ���� ������ ����
        if (Tiles[y, x] == null) { Debug.LogWarning($"[DoesTileBlockLight] Null tile at ({x},{y}). Assuming it blocks light."); return true; }
        return !Tiles[y, x].transparent; // TileType�� transparent �Ӽ� ���
    }

    /// <summary>
    /// ������ �þ� ��(targetMap)���� Ư�� ���� ���̵��� �����մϴ�. (���� FOV ��꿡 ���)
    /// Sets a specific cell as visible in the provided visibility map (targetMap). (Used for generic FOV calculation)
    /// </summary>
    private void SetCellVisibleInMap(bool[,] targetMap, int y, int x)
    {
        if (IsInBounds(x, y)) { targetMap[y, x] = true; }
    }

    /// <summary>
    /// ��������� ���� �߻��Ͽ� ������ �þ� ��(targetMap)�� ������Ʈ�մϴ�. (���� FOV ��꿡 ���)
    /// Recursively casts light to update the provided visibility map (targetMap). (Used for generic FOV calculation)
    /// </summary>
    private void CastLightForMap(bool[,] targetMap, int pY, int pX, int r, int row, float startS, float endS, int xx, int xy, int yx, int yy)
    {
        if (startS < endS) return; // ���� ���Ⱑ �� ���⺸�� ������ ���� (�߸��� ����)
        float nextStartS = startS; // ���� ��� ȣ���� ���� ���� ����
        int rSq = r * r; // �þ� �ݰ��� ���� (�Ÿ� ����)

        for (int i = row; i <= r; ++i) // ���� ��(�������κ����� �Ÿ�)���� �ִ� �ݰ���� �ݺ�
        {
            bool blocked = false; // ���� ��ĵ������ ���� ���� �������� ����
            int dy_r = -i; // ���� ���� ��� Y ��ǥ (8���� ��ȯ ��)

            for (int dx_r = -i; dx_r <= 0; ++dx_r) // ���� ���� ��� X ��ǥ (8���� ��ȯ ��, ���� ���ݸ� ��ĵ)
            {
                // 8���� ��ȯ ����� ����Ͽ� ���� �ʿ����� ��� ��ǥ ���
                int cX_r = dx_r * xx + dy_r * xy; // ���� ���� ��� X
                int cY_r = dx_r * yx + dy_r * yy; // ���� ���� ��� Y

                // ���� �ʿ����� ���� ��ǥ ���
                int cX_a = pX + cX_r;
                int cY_a = pY + cY_r;

                float eps = 1e-6f; // �ε��Ҽ��� �񱳸� ���� ���� ��
                // ���� ���� ���� �� ������ �����ڸ��� ���� ���
                float lS = (dx_r - 0.5f) / (dy_r + 0.5f + eps);
                float rS = (dx_r + 0.5f) / (dy_r - 0.5f + eps);

                if (!IsInBounds(cX_a, cY_a) || startS < rS) continue; // �� ���̰ų�, ���� ��ĵ ������ �������� ������� �ǳʶٱ�
                else if (endS > lS) break; // ���� ��ĵ ������ ������ ������� �� ���� ��ĵ ����

                // ���� ���� �þ� �ݰ� ���� ������ ���̵��� ����
                if ((cX_r * cX_r + cY_r * cY_r) <= rSq)
                {
                    SetCellVisibleInMap(targetMap, cY_a, cX_a);
                }

                if (blocked) // ���� ���� ���̾��� ���
                {
                    if (DoesTileBlockLight(cY_a, cX_a)) // ���� ���� ���̸�
                    {
                        nextStartS = rS; // ���� ��ĵ ���� ���⸦ ���� ���� ������ �����ڸ��� ������Ʈ
                        continue;        // ���� ����
                    }
                    else // ���� ���� ���� �ƴϸ�
                    {
                        blocked = false;        // ���� ���� ����
                        startS = nextStartS;    // ���� ���⸦ ���� �� ������ ���� ����� ����
                    }
                }
                else // ���� ���� ���� �ƴϾ��� ���
                {
                    if (DoesTileBlockLight(cY_a, cX_a) && i < r) // ���� ���� ���̰� �ִ� �ݰ� ���̸�
                    {
                        blocked = true; // ���� ���·� ����
                        // ���� ���� ���� ���� ���� �׸��� ������ ���� ��������� �� �߻�
                        CastLightForMap(targetMap, pY, pX, r, i + 1, startS, lS, xx, xy, yx, yy);
                        nextStartS = rS; // ���� ��ĵ ���� ���⸦ ���� ���� ������ �����ڸ��� ������Ʈ
                    }
                }
            }
            if (blocked) break; // ���� ���� ������ �������� �� �̻� ������ �ʿ� ����
        }
    }
    #endregion

    #region Entity Management 
    public Entity GetEntityAt(int x, int y)
    {
        foreach (Entity entity in Entities) { if (entity.gridX == x && entity.gridY == y) return entity; }
        return null;
    }

    public Entity GetBlockingEntityAt(int x, int y)
    {
        foreach (Entity entity in Entities) { if (entity.gridX == x && entity.gridY == y && entity.blocksMovement) return entity; }
        return null;
    }

    public void RemoveEntity(Entity entity)
    { if (entity != null && Entities.Contains(entity)) Entities.Remove(entity); }

    public void AddEntity(Entity entity)
    {
        if (entity != null && !Entities.Contains(entity)) Entities.Add(entity);
    }
    #endregion

    #region Utility Methods
    public Vector2Int GetFallbackStartPosition(int level)
    {
        int mapCenterX = Width / 2; int mapCenterY = Height / 2;
        for (int radius = 0; radius < Mathf.Max(Width, Height) / 2; radius++)
        {
            int yMin = Mathf.Max(0, mapCenterY - radius); int yMax = Mathf.Min(Height - 1, mapCenterY + radius);
            int xMin = Mathf.Max(0, mapCenterX - radius); int xMax = Mathf.Min(Width - 1, mapCenterX + radius);
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    if (radius == 0 || y == yMin || y == yMax || x == xMin || x == xMax)
                    {
                        if (IsInBounds(x, y) && Tiles[y, x]?.walkable == true) return new Vector2Int(x, y);
                    }
                }
            }
        }
        Debug.LogError($"[GetFallbackStartPosition] Could not find ANY walkable tile on level {level} for map {Width}x{Height}!");
        if (IsInBounds(mapCenterX, mapCenterY)) return new Vector2Int(mapCenterX, mapCenterY);
        return Vector2Int.zero;
    }
    #endregion
}
