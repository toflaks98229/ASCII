using UnityEngine; // BiomeType enum이 정의된 네임스페이스를 포함해야 할 수 있습니다.

/// <summary>
/// Represents the data for a single tile on the world map grid.
/// 월드맵 그리드의 단일 타일에 대한 데이터를 나타냅니다.
/// </summary>
[System.Serializable] // 나중에 WorldData를 직렬화해야 할 경우 필요
public struct WorldTile
{
    public BiomeType Biome;     // 이 타일의 주된 Biome 타입
    public float Elevation;     // 고도 (e.g., 0.0 to 1.0) - 물, 평지, 산 구분용
    public float Temperature;   // 온도 (e.g., 0.0 to 1.0) - 사막, 설산 구분용
    public float Rainfall;      // 강수량 (e.g., 0.0 to 1.0) - 숲, 사막 구분용

    public bool IsRiver;       // 이 타일에 강이 흐르는지 여부
    public bool IsLake;        // 이 타일에 호수가 있는지 여부 (추가됨)
    public bool IsRoad;        // 이 타일에 길이 있는지 여부

    // Optional: Point of Interest (POI) information
    // 선택 사항: 주요 지점(POI) 정보
    public bool HasCity;       // 도시가 있는지 여부
    public bool HasTown;       // 마을이 있는지 여부
    public bool HasDungeon;    // 던전 입구가 있는지 여부

    // Constructor (optional, for default values)
    // 생성자 (선택 사항, 기본값 설정용)
    public WorldTile(BiomeType biome = BiomeType.Plains, float elevation = 0.5f)
    {
        Biome = biome;
        Elevation = elevation;
        Temperature = 0.5f; // 기본 평균 온도
        Rainfall = 0.5f;    // 기본 평균 강수량
        IsRiver = false;
        IsRoad = false;
        IsLake = false;     // IsLake 초기화 추가
        HasCity = false;
        HasTown = false;
        HasDungeon = false;
    }

    /// <summary>
    /// 이 타일이 육지인지 확인합니다.
    /// 참고: 이 구현은 WorldGenerator의 beachThreshold 값을 직접 참조할 수 없으므로,
    /// 해당 값과 일치하는 값을 사용하거나, 임계값을 파라미터로 받는 방식으로 수정할 수 있습니다.
    /// </summary>
    /// <returns>육지이면 true, 아니면 false</returns>
    public bool IsLand(float beachThresholdValue)
    {
        return Elevation >= beachThresholdValue && !IsRiver && !IsLake; // 강과 호수도 육지가 아님
    }

    /// <summary>
    /// **신규:** 이 타일이 생성된 물 지형(강 또는 호수)인지 확인합니다.
    /// </summary>
    /// <returns>강 또는 호수이면 true, 아니면 false</returns>
    public bool IsWaterBody()
    {
        return IsRiver || IsLake;
    }

    /// <summary>
    /// **신규:** 이 타일이 물 표면(바다, 강, 호수 포함)인지 확인합니다.
    /// 참고: 이 구현은 WorldGenerator의 beachThreshold 값을 직접 참조할 수 없으므로,
    /// 해당 값과 일치하는 값을 사용하거나, 임계값을 파라미터로 받는 방식으로 수정할 수 있습니다.
    /// </summary>
    /// <returns>물 표면이면 true, 아니면 false</returns>
    public bool IsWaterSurface()
    {
        const float beachThresholdValue = 0.35f; // WorldGenerator의 beachThreshold 값과 동일하게 설정
        // 고도가 임계값 미만이거나, 강 또는 호수이면 물 표면으로 간주
        return Elevation < beachThresholdValue || IsRiver || IsLake;
    }

    /// <summary>
    /// 이 타일에 POI가 있는지 확인합니다.
    /// </summary>
    /// <returns>POI가 있으면 true, 없으면 false</returns>
    public bool HasPOI()
    {
        return HasCity || HasTown || HasDungeon;
    }
}

// BiomeType enum 정의가 필요합니다.
/*
public enum BiomeType {
    // 수역
    DeepWater, ShallowWater, Beach, River, Lake, // Lake 추가
    // 육지 (예시)
    PolarIce, Tundra, MountainTundra, HighMountain, Mountainous, AlpineForest,
    Taiga, ColdParklands, Steppe,
    TemperateRainforest, TemperateDeciduousForest, TemperateMixedForest, TemperateGrassland, Shrubland, Plains, Hills,
    SubtropicalMoistForest, SubtropicalDryForest, Mediterranean, SubtropicalGrassland,
    TropicalRainforest, TropicalMoistForest, TropicalGrassland, TropicalDryForest, Desert,
    Wetlands, Riparian,
    // POI (시각화용)
    City, Town, DungeonEntrance
}
*/
