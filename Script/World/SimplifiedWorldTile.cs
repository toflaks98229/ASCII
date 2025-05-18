using UnityEngine;

/// <summary>
/// Represents the simplified data for a larger region on the world map.
/// 월드맵의 더 큰 지역에 대한 간략화된 데이터를 나타냅니다.
/// </summary>
[System.Serializable]
public struct SimplifiedWorldTile
{
    public BiomeType DominantBiome; // 해당 지역의 주된 Biome
    public bool HasMajorPOI;        // 해당 지역 내 주요 POI(도시, 던전) 존재 여부
    public bool IsWaterBody;        // 해당 지역이 주로 물인지 여부 (강 포함 가능)

    // Constructor
    public SimplifiedWorldTile(BiomeType biome = BiomeType.Plains, bool hasPoi = false, bool isWater = false)
    {
        DominantBiome = biome;
        HasMajorPOI = hasPoi;
        IsWaterBody = isWater;
    }
}

/// <summary>
/// Holds the grid data for the simplified world map.
/// 간략화된 월드맵의 그리드 데이터를 보유합니다.
/// </summary>
public class SimplifiedWorldData
{
    public readonly int Width;
    public readonly int Height;
    public SimplifiedWorldTile[,] SimplifiedTiles { get; private set; } // [y, x] format

    public SimplifiedWorldData(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"Invalid SimplifiedWorldData dimensions: {width}x{height}. Using 1x1.");
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }
        Width = width;
        Height = height;
        SimplifiedTiles = new SimplifiedWorldTile[height, width];
        // Initialize with a default? Or let generator fill it.
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
