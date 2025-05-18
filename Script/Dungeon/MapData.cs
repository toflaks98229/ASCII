// MapData.cs
using System.Collections.Generic;
using UnityEngine;
using System;

// RectInt struct (기존과 동일)
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
    /// 플레이어의 현재 시야 정보를 저장합니다. (True: 보임, False: 안 보임)
    /// Stores the player's current field of view. (True: visible, False: not visible)
    /// </summary>
    public bool[,] Visible { get; private set; }

    /// <summary>
    /// 플레이어가 탐험한 영역 정보를 저장합니다. (True: 탐험됨, False: 미탐험)
    /// Stores the areas explored by the player. (True: explored, False: unexplored)
    /// </summary>
    public bool[,] Explored { get; private set; }

    public List<Entity> Entities { get; private set; }
    public Vector2Int UpStairsLocation { get; private set; } = Vector2Int.one * -1;
    public Vector2Int DownStairsLocation { get; private set; } = Vector2Int.one * -1;
    public Vector2Int PlayerSuggestedStartPosition { get; internal set; } = Vector2Int.one * -1;
    #endregion

    // FOV 계산에 사용되는 8방향 변환 행렬 (기존과 동일)
    private static readonly int[,] fovMultipliers = { { 1, 0, 0, 1 }, { 0, 1, 1, 0 }, { 0, -1, 1, 0 }, { -1, 0, 0, 1 }, { -1, 0, 0, -1 }, { 0, -1, -1, 0 }, { 0, 1, -1, 0 }, { 1, 0, 0, -1 } };


    #region Constructor and Basic Map Operations
    public MapData(int width, int height)
    {
        if (width <= 0 || height <= 0)
        { Debug.LogError($"[MapData Constructor] Invalid dimensions: {width}x{height}. Using 1x1."); width = Mathf.Max(1, width); height = Mathf.Max(1, height); }
        Width = width; Height = height;
        Tiles = new TileType[height, width];
        Visible = new bool[height, width];   // 플레이어 시야용
        Explored = new bool[height, width];  // 플레이어 탐험용
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


    #region Field of View (FOV) - 플레이어 및 범용

    /// <summary>
    /// 플레이어의 시야를 계산하고 MapData의 Visible 및 Explored 배열을 업데이트합니다.
    /// Calculates the player's FOV and updates the Visible and Explored arrays in MapData.
    /// </summary>
    /// <param name="playerX">플레이어의 X 좌표</param>
    /// <param name="playerY">플레이어의 Y 좌표</param>
    /// <param name="radius">플레이어의 시야 반경</param>
    public void ComputePlayerFov(int playerX, int playerY, int radius)
    {
        // 먼저 모든 타일을 안 보이는 상태로 초기화 (플레이어 시야 기준)
        for (int y = 0; y < Height; y++) { for (int x = 0; x < Width; x++) { Visible[y, x] = false; } }

        // 범용 FOV 계산 함수 호출
        bool[,] playerVisibility = CalculateFov(new Vector2Int(playerX, playerY), radius);

        // 계산된 시야를 Visible 및 Explored에 적용
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (playerVisibility[y, x])
                {
                    Visible[y, x] = true;
                    Explored[y, x] = true; // 보이는 타일은 항상 탐험된 것으로 간주
                }
            }
        }
    }

    /// <summary>
    /// 지정된 원점과 반경을 기준으로 시야(FOV)를 계산하여 bool 배열로 반환합니다.
    /// 이 함수는 MapData의 Visible이나 Explored 상태를 직접 변경하지 않습니다.
    /// Calculates the Field of View (FOV) from a given origin and radius, returning it as a bool array.
    /// This method does NOT directly modify MapData's Visible or Explored states.
    /// </summary>
    /// <param name="origin">시야 계산의 원점 (예: 엔티티의 위치)</param>
    /// <param name="radius">시야 반경</param>
    /// <returns>계산된 시야 정보를 담은 bool[Height, Width] 배열. True는 보임, False는 안 보임.</returns>
    public bool[,] CalculateFov(Vector2Int origin, int radius)
    {
        bool[,] visibilityMap = new bool[Height, Width]; // 이 FOV 계산을 위한 임시 시야 맵
        if (!IsInBounds(origin.x, origin.y)) return visibilityMap; // 원점이 맵 밖이면 빈 맵 반환

        // 원점은 항상 보임
        SetCellVisibleInMap(visibilityMap, origin.y, origin.x);

        // 8방향으로 빛을 발사하여 시야 계산
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
    /// 특정 타일이 시야를 막는지 여부를 확인합니다. (범용 FOV 계산에 사용)
    /// Checks if a specific tile blocks line of sight. (Used for generic FOV calculation)
    /// </summary>
    private bool DoesTileBlockLight(int y, int x)
    {
        if (!IsInBounds(x, y)) return true; // 맵 범위 밖은 시야를 막는 것으로 간주
        if (Tiles[y, x] == null) { Debug.LogWarning($"[DoesTileBlockLight] Null tile at ({x},{y}). Assuming it blocks light."); return true; }
        return !Tiles[y, x].transparent; // TileType의 transparent 속성 사용
    }

    /// <summary>
    /// 제공된 시야 맵(targetMap)에서 특정 셀을 보이도록 설정합니다. (범용 FOV 계산에 사용)
    /// Sets a specific cell as visible in the provided visibility map (targetMap). (Used for generic FOV calculation)
    /// </summary>
    private void SetCellVisibleInMap(bool[,] targetMap, int y, int x)
    {
        if (IsInBounds(x, y)) { targetMap[y, x] = true; }
    }

    /// <summary>
    /// 재귀적으로 빛을 발사하여 제공된 시야 맵(targetMap)을 업데이트합니다. (범용 FOV 계산에 사용)
    /// Recursively casts light to update the provided visibility map (targetMap). (Used for generic FOV calculation)
    /// </summary>
    private void CastLightForMap(bool[,] targetMap, int pY, int pX, int r, int row, float startS, float endS, int xx, int xy, int yx, int yy)
    {
        if (startS < endS) return; // 시작 기울기가 끝 기울기보다 작으면 종료 (잘못된 범위)
        float nextStartS = startS; // 다음 재귀 호출을 위한 시작 기울기
        int rSq = r * r; // 시야 반경의 제곱 (거리 계산용)

        for (int i = row; i <= r; ++i) // 현재 행(원점으로부터의 거리)부터 최대 반경까지 반복
        {
            bool blocked = false; // 현재 스캔라인이 벽에 의해 막혔는지 여부
            int dy_r = -i; // 현재 행의 상대 Y 좌표 (8방향 변환 전)

            for (int dx_r = -i; dx_r <= 0; ++dx_r) // 현재 행의 상대 X 좌표 (8방향 변환 전, 왼쪽 절반만 스캔)
            {
                // 8방향 변환 행렬을 사용하여 실제 맵에서의 상대 좌표 계산
                int cX_r = dx_r * xx + dy_r * xy; // 원점 기준 상대 X
                int cY_r = dx_r * yx + dy_r * yy; // 원점 기준 상대 Y

                // 실제 맵에서의 절대 좌표 계산
                int cX_a = pX + cX_r;
                int cY_a = pY + cY_r;

                float eps = 1e-6f; // 부동소수점 비교를 위한 작은 값
                // 현재 셀의 왼쪽 및 오른쪽 가장자리의 기울기 계산
                float lS = (dx_r - 0.5f) / (dy_r + 0.5f + eps);
                float rS = (dx_r + 0.5f) / (dy_r - 0.5f + eps);

                if (!IsInBounds(cX_a, cY_a) || startS < rS) continue; // 맵 밖이거나, 현재 스캔 범위의 오른쪽을 벗어났으면 건너뛰기
                else if (endS > lS) break; // 현재 스캔 범위의 왼쪽을 벗어났으면 이 행의 스캔 종료

                // 현재 셀이 시야 반경 내에 있으면 보이도록 설정
                if ((cX_r * cX_r + cY_r * cY_r) <= rSq)
                {
                    SetCellVisibleInMap(targetMap, cY_a, cX_a);
                }

                if (blocked) // 이전 셀이 벽이었던 경우
                {
                    if (DoesTileBlockLight(cY_a, cX_a)) // 현재 셀도 벽이면
                    {
                        nextStartS = rS; // 다음 스캔 시작 기울기를 현재 셀의 오른쪽 가장자리로 업데이트
                        continue;        // 다음 셀로
                    }
                    else // 현재 셀이 벽이 아니면
                    {
                        blocked = false;        // 막힘 상태 해제
                        startS = nextStartS;    // 시작 기울기를 이전 벽 이후의 시작 기울기로 복원
                    }
                }
                else // 이전 셀이 벽이 아니었던 경우
                {
                    if (DoesTileBlockLight(cY_a, cX_a) && i < r) // 현재 셀이 벽이고 최대 반경 전이면
                    {
                        blocked = true; // 막힘 상태로 변경
                        // 현재 벽에 의해 새로 생긴 그림자 영역에 대해 재귀적으로 빛 발사
                        CastLightForMap(targetMap, pY, pX, r, i + 1, startS, lS, xx, xy, yx, yy);
                        nextStartS = rS; // 다음 스캔 시작 기울기를 현재 셀의 오른쪽 가장자리로 업데이트
                    }
                }
            }
            if (blocked) break; // 현재 행이 완전히 막혔으면 더 이상 진행할 필요 없음
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
