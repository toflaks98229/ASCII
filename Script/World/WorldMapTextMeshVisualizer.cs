using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Text; // Required for StringBuilder
using System; // Required for Enum.IsDefined

/// <summary>
/// Visualizes the generated WorldData as colored ASCII characters,
/// reflecting diverse biomes, lakes, and drawing connected river lines.
/// 생성된 WorldData를 다양한 생물 군계, 호수, 연결된 강 선을 반영하는
/// 색상 있는 ASCII 문자로 시각화합니다.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class WorldMapTextMeshVisualizer : MonoBehaviour
{
    private TextMeshProUGUI textMeshPro;

    [Header("Display Settings")]
    [Tooltip("텍스트 렌더링 시 문자 간 간격")]
    [SerializeField] private float characterSpacing = -1f;
    [Tooltip("텍스트 렌더링 시 줄 간 간격")]
    [SerializeField] private float lineSpacing = -1f;

    // --- Biome Colors (Inspector에서 수정 가능) ---
    [Header("Biome Colors")]
    [SerializeField] private Color deepWaterColor = new Color(0.0f, 0.0f, 0.4f);
    [SerializeField] private Color shallowWaterColor = new Color(0.3f, 0.5f, 0.9f);
    [SerializeField] private Color riverColor = new Color(0.4f, 0.6f, 1.0f); // 강 색상
    [SerializeField] private Color lakeColor = new Color(0.2f, 0.4f, 0.8f); // <<< 호수 색상 추가
    [SerializeField] private Color beachColor = new Color(0.8f, 0.75f, 0.5f);
    [SerializeField] private Color polarIceColor = Color.white;
    [SerializeField] private Color tundraColor = new Color(0.7f, 0.7f, 0.8f);
    [SerializeField] private Color taigaColor = new Color(0.3f, 0.5f, 0.4f);
    [SerializeField] private Color mountainTundraColor = new Color(0.6f, 0.6f, 0.7f);
    [SerializeField] private Color alpineForestColor = new Color(0.4f, 0.55f, 0.45f);
    [SerializeField] private Color mountainousColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color highMountainColor = new Color(0.9f, 0.9f, 0.95f);
    [SerializeField] private Color coldParklandsColor = new Color(0.6f, 0.7f, 0.5f);
    [SerializeField] private Color steppeColor = new Color(0.7f, 0.65f, 0.4f);
    [SerializeField] private Color shrublandColor = new Color(0.6f, 0.6f, 0.3f);
    [SerializeField] private Color temperateGrasslandColor = new Color(0.5f, 0.8f, 0.4f);
    [SerializeField] private Color temperateDeciduousForestColor = new Color(0.2f, 0.7f, 0.2f);
    [SerializeField] private Color temperateMixedForestColor = new Color(0.3f, 0.65f, 0.3f);
    [SerializeField] private Color temperateConiferousForestColor = new Color(0.1f, 0.6f, 0.3f);
    [SerializeField] private Color temperateRainforestColor = new Color(0.1f, 0.5f, 0.2f);
    [SerializeField] private Color mediterraneanColor = new Color(0.5f, 0.5f, 0.2f);
    [SerializeField] private Color subtropicalDryForestColor = new Color(0.4f, 0.6f, 0.2f);
    [SerializeField] private Color subtropicalMoistForestColor = new Color(0.3f, 0.7f, 0.3f);
    [SerializeField] private Color subtropicalGrasslandColor = new Color(0.8f, 0.7f, 0.3f);
    [SerializeField] private Color tropicalDryForestColor = new Color(0.6f, 0.7f, 0.2f);
    [SerializeField] private Color tropicalMoistForestColor = new Color(0.2f, 0.6f, 0.2f);
    [SerializeField] private Color tropicalRainforestColor = new Color(0.1f, 0.5f, 0.1f);
    [SerializeField] private Color tropicalGrasslandColor = new Color(0.9f, 0.8f, 0.2f);
    [SerializeField] private Color desertColor = new Color(0.85f, 0.75f, 0.4f);
    [SerializeField] private Color wetlandsColor = new Color(0.3f, 0.45f, 0.3f);
    [SerializeField] private Color floodedGrasslandColor = new Color(0.6f, 0.8f, 0.6f);
    [SerializeField] private Color riparianColor = new Color(0.1f, 0.7f, 0.4f);
    [SerializeField] private Color cityColor = Color.yellow;
    [SerializeField] private Color dungeonColor = Color.red;
    [SerializeField] private Color roadColor = Color.grey;
    [SerializeField] private Color defaultColor = Color.magenta;

    void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        if (textMeshPro == null)
        {
            Debug.LogError("TextMeshProUGUI component not found!", this);
            this.enabled = false;
            return;
        }
        textMeshPro.richText = true;
        textMeshPro.characterSpacing = characterSpacing;
        textMeshPro.lineSpacing = lineSpacing;
        textMeshPro.alignment = TextAlignmentOptions.TopLeft;
        textMeshPro.text = "Generating detailed map...";
    }

    /// <summary>
    /// Generates and displays the detailed world map text using ASCII characters.
    /// ASCII 문자를 사용하여 상세 월드맵 텍스트를 생성하고 표시합니다.
    /// </summary>
    public void Visualize(WorldData worldData)
    {
        if (worldData == null || worldData.Width == 0 || worldData.Height == 0)
        {
            if (textMeshPro != null) textMeshPro.text = "Error: Invalid World Data";
            Debug.LogError("Cannot visualize: WorldData is null or has zero dimensions.");
            return;
        }
        if (textMeshPro == null) { Debug.LogError("TextMeshProUGUI component not found!"); return; }

        StringBuilder mapBuilder = new StringBuilder(worldData.Width * worldData.Height + worldData.Height);

        for (int y = 0; y < worldData.Height; y++)
        {
            for (int x = 0; x < worldData.Width; x++)
            {
                WorldTile tile = worldData.WorldTiles[y, x];
                char displayChar = '?';
                Color displayColor = defaultColor;

                // --- Determine Character and Color based on Priority ---
                // 우선순위: POI > 강 > 호수 > 도로 > Biome
                if (tile.HasCity)
                {
                    displayChar = 'C';
                    displayColor = cityColor;
                }
                else if (tile.HasDungeon)
                {
                    displayChar = 'D';
                    displayColor = dungeonColor;
                }
                else if (tile.IsRiver) // 강을 먼저 체크
                {
                    // 강 선 문자 결정 (호수와 연결 고려)
                    displayChar = GetRiverCharacter(worldData, x, y);
                    displayColor = riverColor;
                }
                else if (tile.IsLake) // 그 다음 호수 체크
                {
                    displayChar = '≈'; // 호수 문자 (예: ≈ 또는 ~)
                    displayColor = lakeColor; // 호수 색상 사용
                }
                else if (tile.IsRoad)
                {
                    displayChar = '#';
                    displayColor = roadColor;
                }
                else // 위 조건에 해당하지 않으면 Biome 기반으로 결정
                {
                    AssignBiomeCharacterAndColor(tile, out displayChar, out displayColor);
                }

                mapBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(displayColor)}>{displayChar}</color>");
            }
            mapBuilder.Append('\n');
        }

        textMeshPro.text = mapBuilder.ToString().TrimEnd('\n');
        Debug.Log($"Detailed world map visualized on {gameObject.name}.");
    }


    /// <summary>
    /// Assigns the appropriate ASCII character and color based on the BiomeType and Elevation.
    /// BiomeType과 Elevation을 기반으로 적절한 ASCII 문자와 색상을 할당합니다.
    /// </summary>
    private void AssignBiomeCharacterAndColor(WorldTile tile, out char character, out Color color)
    {
        character = '?';
        color = defaultColor;
        float e = tile.Elevation;

        switch (tile.Biome)
        {
            // --- Water & Coast ---
            case BiomeType.DeepWater: character = '≈'; color = deepWaterColor; break;
            case BiomeType.ShallowWater: character = '~'; color = shallowWaterColor; break;
            case BiomeType.Beach: character = '.'; color = beachColor; break;
            // case BiomeType.Lake: character = '≈'; color = lakeColor; break; // BiomeType에 Lake가 있다면 여기서 처리 가능

            // --- Cold & Ice ---
            case BiomeType.PolarIce: character = '#'; color = polarIceColor; break;
            case BiomeType.Tundra: character = '-'; color = tundraColor; break;
            case BiomeType.MountainTundra: character = '='; color = mountainTundraColor; break;

            // --- Forests ---
            case BiomeType.Taiga: character = '*'; color = taigaColor; break;
            case BiomeType.AlpineForest: character = '%'; color = alpineForestColor; break;
            case BiomeType.TemperateDeciduousForest: character = '&'; color = temperateDeciduousForestColor; break;
            case BiomeType.TemperateMixedForest: character = '%'; color = temperateMixedForestColor; break;
            case BiomeType.TemperateConiferousForest: character = '*'; color = temperateConiferousForestColor; break;
            case BiomeType.TemperateRainforest: character = '#'; color = temperateRainforestColor; break;
            case BiomeType.SubtropicalDryForest: character = ';'; color = subtropicalDryForestColor; break;
            case BiomeType.SubtropicalMoistForest: character = '$'; color = subtropicalMoistForestColor; break;
            case BiomeType.TropicalRainforest: character = '@'; color = tropicalRainforestColor; break;
            case BiomeType.TropicalDryForest: character = ';'; color = tropicalDryForestColor; break;
            case BiomeType.TropicalMoistForest: character = '$'; color = tropicalMoistForestColor; break;
            case BiomeType.Forest: character = '%'; color = temperateMixedForestColor; break; // 일반 숲

            // --- Grasslands & Shrublands ---
            case BiomeType.ColdParklands: character = ','; color = coldParklandsColor; break;
            case BiomeType.Steppe: character = ','; color = steppeColor; break;
            case BiomeType.Shrubland: character = ';'; color = shrublandColor; break;
            case BiomeType.TemperateGrassland: character = '"'; color = temperateGrasslandColor; break;
            case BiomeType.Mediterranean: character = ';'; color = mediterraneanColor; break;
            case BiomeType.SubtropicalGrassland: character = '"'; color = subtropicalGrasslandColor; break;
            case BiomeType.TropicalGrassland: character = '"'; color = tropicalGrasslandColor; break;
            case BiomeType.Plains: character = '.'; color = temperateGrasslandColor; break; // 평원

            // --- Desert & Dry ---
            case BiomeType.Desert: character = ':'; color = desertColor; break;

            // --- Special Landforms ---
            case BiomeType.Wetlands: character = '='; color = wetlandsColor; break;
            case BiomeType.Swamp: character = '='; color = wetlandsColor; break; // 늪지
            case BiomeType.Riparian: character = '|'; color = riparianColor; break;
            case BiomeType.Mountainous: character = '^'; color = mountainousColor; break;
            case BiomeType.HighMountain: character = 'M'; color = highMountainColor; break;
            case BiomeType.Hills: character = 'n'; color = temperateGrasslandColor; break;
            case BiomeType.Mountain: character = '^'; color = mountainousColor; break; // 일반 산

            // --- POI Biomes (Visualize 함수에서 우선 처리되므로 여기서는 생략 가능) ---
            // case BiomeType.City: ...
            // case BiomeType.DungeonEntrance: ...

            default:
                if (!Enum.IsDefined(typeof(BiomeType), tile.Biome))
                {
                    // Debug.LogWarning($"Unhandled BiomeType '{tile.Biome}' encountered.");
                }
                character = '?';
                color = defaultColor;
                break;
        }
    }

    /// <summary>
    /// Determines the correct box-drawing character for a river tile based on its neighbors.
    /// **수정됨:** 이제 호수 타일도 연결된 것으로 간주합니다.
    /// </summary>
    private char GetRiverCharacter(WorldData worldData, int x, int y)
    {
        int riverCont = 0;

        // Check orthogonal neighbors (Up, Down, Left, Right)
        // IsWaterBodyTile 함수를 사용하여 강 또는 호수인지 확인
        bool up = IsWaterBodyTile(worldData, x, y - 1);
        bool down = IsWaterBodyTile(worldData, x, y + 1);
        bool left = IsWaterBodyTile(worldData, x - 1, y);
        bool right = IsWaterBodyTile(worldData, x + 1, y);

        riverCont += up ? 1 : 0;
        riverCont += down ? 1 : 0;
        riverCont += left ? 1 : 0;
        riverCont += right ? 1 : 0;

        riverCont += IsWaterBodyTile(worldData, x - 1, y - 1) ? 1 : 0;
        riverCont += IsWaterBodyTile(worldData, x + 1, y - 1) ? 1 : 0;
        riverCont += IsWaterBodyTile(worldData, x - 1, y + 1) ? 1 : 0;
        riverCont += IsWaterBodyTile(worldData, x + 1, y + 1) ? 1 : 0;

        if(riverCont >= 5) return '~'; 

        // --- Determine character based on connections ---
        if (up && down && left && right) return '┼';
        if (up && down && left) return '┤';
        if (up && down && right) return '├';
        if (up && left && right) return '┴';
        if (down && left && right) return '┬';

        if (up && down) return '│';
        if (left && right) return '─';

        if (up && left) return '┘';
        if (up && right) return '└';
        if (down && left) return '┐';
        if (down && right) return '┌';

        // Handle endpoints
        if (up) return '│';
        if (down) return '│';
        if (left) return '─';
        if (right) return '─';

        // Default if isolated
        return '+'; // 또는 강 기본 문자 '~' 등
    }

    /// <summary>
    /// **수정됨:** Helper function to check if a tile at given coordinates is a river OR a lake tile.
    /// 주어진 좌표의 타일이 강 또는 호수 타일인지 확인하는 헬퍼 함수.
    /// </summary>
    private bool IsWaterBodyTile(WorldData worldData, int x, int y)
    {
        // Check bounds first
        if (x < 0 || x >= worldData.Width || y < 0 || y >= worldData.Height)
        {
            return false;
        }
        // Check IsRiver OR IsLake flag using the helper function
        // WorldTile 구조체에 IsWaterBody() 함수가 정의되어 있어야 함
        return worldData.WorldTiles[y, x].IsWaterBody();

        // 만약 WorldTile에 IsWaterBody() 함수가 없다면 아래처럼 직접 체크:
        // return worldData.WorldTiles[y, x].IsRiver || worldData.WorldTiles[y, x].IsLake;
    }
}
