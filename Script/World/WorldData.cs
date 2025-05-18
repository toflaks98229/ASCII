using UnityEngine;

/// <summary>
/// Holds the generated world map data (grid of WorldTiles).
/// 생성된 월드맵 데이터(WorldTile 그리드)를 보유합니다.
/// </summary>
public class WorldData
{
    public readonly int Width;
    public readonly int Height;
    public WorldTile[,] WorldTiles { get; private set; } // [y, x] format

    public WorldData(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"Invalid WorldData dimensions: {width}x{height}. Using 1x1.");
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }
        Width = width;
        Height = height;
        WorldTiles = new WorldTile[height, width];
        // Initialize with a default tile (e.g., Plains)
        // 기본 타일(예: Plains)로 초기화
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                WorldTiles[y, x] = new WorldTile(BiomeType.Plains); // Initialize here
            }
        }
    }

    /// <summary>
    /// Checks if the given world coordinates are within bounds.
    /// 주어진 월드 좌표가 경계 내에 있는지 확인합니다.
    /// </summary>
    public bool IsInBounds(int worldX, int worldY)
    {
        return worldX >= 0 && worldX < Width && worldY >= 0 && worldY < Height;
    }

    /// <summary>
    /// Gets the WorldTile data at the specified coordinates. Returns default if out of bounds.
    /// 지정된 좌표의 WorldTile 데이터를 가져옵니다. 경계를 벗어나면 기본값을 반환합니다.
    /// </summary>
    public WorldTile GetTile(int worldX, int worldY)
    {
        if (IsInBounds(worldX, worldY))
        {
            return WorldTiles[worldY, worldX];
        }
        // Return a default or error tile if out of bounds
        Debug.LogWarning($"Accessing WorldTile out of bounds at ({worldX},{worldY}). Returning default.");
        return new WorldTile(BiomeType.DeepWater, 0); // Example default (Ocean)
    }

    /// <summary>
    /// Gets the BiomeType at the specified coordinates.
    /// 지정된 좌표의 BiomeType을 가져옵니다.
    /// </summary>
    public BiomeType GetBiome(int worldX, int worldY)
    {
        if (IsInBounds(worldX, worldY))
        {
            return WorldTiles[worldY, worldX].Biome;
        }
        return BiomeType.DeepWater; // Default if out of bounds
    }

    /// <summary>
    /// Checks if a river exists at the specified coordinates.
    /// 지정된 좌표에 강이 있는지 확인합니다.
    /// </summary>
    public bool HasRiver(int worldX, int worldY)
    {
        if (IsInBounds(worldX, worldY))
        {
            return WorldTiles[worldY, worldX].IsRiver;
        }
        return false;
    }

    /// <summary>
    /// Checks if a city exists at the specified coordinates.
    /// 지정된 좌표에 도시가 있는지 확인합니다.
    /// </summary>
    public bool HasCity(int worldX, int worldY)
    {
        if (IsInBounds(worldX, worldY))
        {
            return WorldTiles[worldY, worldX].HasCity;
        }
        return false;
    }

    /// <summary>
    /// Checks if a dungeon entrance exists at the specified coordinates.
    /// 지정된 좌표에 던전 입구가 있는지 확인합니다.
    /// </summary>
    public bool HasDungeonEntrance(int worldX, int worldY)
    {
        if (IsInBounds(worldX, worldY))
        {
            return WorldTiles[worldY, worldX].HasDungeon;
        }
        return false;
    }

    /// <summary>
    /// Example method check used in MapData.GenerateLocalMap
    /// MapData.GenerateLocalMap에서 사용되는 예시 확인 메서드
    /// </summary>
    public bool ShouldHaveDungeonEntrance(int worldX, int worldY)
    {
        // Implement logic based on world data (e.g., place near mountains, specific biomes)
        // 월드 데이터 기반 로직 구현 (예: 산 근처, 특정 Biome 근처에 배치)
        if (IsInBounds(worldX, worldY))
        {
            // Simple example: Place dungeon if the tile itself is marked or if it's a mountain biome
            // 간단 예시: 타일 자체가 표시되어 있거나 산 Biome인 경우 던전 배치
            return WorldTiles[worldY, worldX].HasDungeon || WorldTiles[worldY, worldX].Biome == BiomeType.Mountain;
        }
        return false;
    }

    // Add more methods as needed to access world information
    // 필요에 따라 월드 정보에 접근하는 메서드 추가
}
