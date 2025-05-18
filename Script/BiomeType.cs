// BiomeType.cs
using UnityEngine;

public enum BiomeType
{
    // Water & Coast
    DeepWater,      // 깊은 물 (바다)
    ShallowWater,   // 얕은 물 (바다 연안, 호수)
    Beach,          // 해변

    // --- Added Biomes based on Climate/Elevation ---
    PolarIce,       // 극지 얼음 (매우 춥고 높거나 극지방)
    Tundra,         // 툰드라 (춥고 나무 없음)
    Taiga,          // 타이가/북방수림 (춥고 침엽수림)
    MountainTundra, // 고산 툰드라 (높고 추움) - Tundra와 통합 가능
    AlpineForest,   // 아고산대 숲 (산악 지역의 숲) - Taiga/MixedForest와 통합 가능
    Mountainous,    // 산악 (바위 많음, 식생 적음) - 기존 Mountain과 유사

    ColdParklands,  // 한대림 성긴숲 (타이가와 초원 사이)
    Steppe,         // 스텝/온대 초원 (춥거나 온난 건조)
    Shrubland,      // 관목지 (건조 기후의 키 작은 나무)

    TemperateGrassland, // 온대 초원/목초지 (온화, 강수량 보통~적음)
    TemperateDeciduousForest, // 온대 낙엽수림 (온화, 강수량 충분)
    TemperateMixedForest,     // 온대 혼합림 (온화, 낙엽수+침엽수)
    TemperateConiferousForest,// 온대 침엽수림 (온화/서늘, 특정 지역)
    TemperateRainforest,    // 온대 우림 (온화/서늘, 강수량 매우 많음)

    Mediterranean,  // 지중해성 (덥고 건조한 여름, 온화하고 습한 겨울) - Shrubland/Woodland 형태

    SubtropicalDryForest,   // 아열대 건조림 (따뜻, 건기/우기 구분)
    SubtropicalMoistForest, // 아열대 다우림 (따뜻, 강수량 많음)
    SubtropicalGrassland,   // 아열대 초원/사바나 (따뜻, 강수량 적음/계절성)

    TropicalRainforest,   // 열대 우림 (덥고, 연중 강수량 많음) - TropicalMoistForest와 통합 가능
    TropicalDryForest,    // 열대 건조림 (덥고, 긴 건기)
    TropicalMoistForest,  // 열대 우림 (덥고, 연중 강수량 많음)
    TropicalGrassland,    // 열대 초원/사바나 (덥고, 건기/우기 명확)

    Desert,         // 사막 (매우 건조, 온도 다양) - HotDesert, ColdDesert 구분 가능
    XericShrubland, // 건조 관목지 (사막 주변) - Desert와 통합 가능

    Wetlands,       // 습지 (늪지, 저지대, 물 많음) - 기존 Swamp 대체 또는 포함
    FloodedGrassland,// 침수 초원/사바나 (계절적 침수)
    Riparian,       // 하안 지대 (강가)

    // Special (Keep existing or adjust)
    Plains,         // 평원 (기본 잔디 지역) - 다른 Biome으로 대체될 수 있음
    Forest,         // 숲 (일반적인 숲) - 더 구체적인 숲 타입으로 대체될 수 있음
    Hills,          // 언덕 - 지형 특징으로 남기거나 다른 Biome으로 대체 가능
    Mountain,       // 산 - Mountainous 또는 AlpineForest 등으로 대체 가능
    HighMountain,   // 높은 산 (설산) - PolarIce, MountainTundra 등으로 대체 가능
    Swamp,          // 늪지 - Wetlands로 대체

    // POI Markers (No change needed here)
    City,
    Town,
    DungeonEntrance,
    River // Keep River as a distinct type for path marking, overrides others visually
}