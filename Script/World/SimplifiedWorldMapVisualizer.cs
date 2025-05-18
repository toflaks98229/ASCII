using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Text; // Required for StringBuilder
using System; // Required for Enum.IsDefined

/// <summary>
/// Visualizes the generated SimplifiedWorldData as colored ASCII characters.
/// 생성된 SimplifiedWorldData를 새로운 Biome 시스템을 반영하는 색상 있는 ASCII 문자로 시각화합니다.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class SimplifiedWorldMapVisualizer : MonoBehaviour
{
    private TextMeshProUGUI textMeshPro;

    [Header("Display Settings")]
    [Tooltip("텍스트 렌더링 시 문자 간 간격")]
    [SerializeField] private float characterSpacing = -1f; // Adjust for simplified map
    [Tooltip("텍스트 렌더링 시 줄 간 간격")]
    [SerializeField] private float lineSpacing = -1f;      // Adjust for simplified map

    // --- Simplified Biome Colors (Can be same as detailed or simplified versions) ---
    // --- 간략화된 Biome 색상 정의 ---
    [Header("Simplified Biome Colors")]
    [SerializeField] private Color deepWaterColor = new Color(0.0f, 0.0f, 0.4f);
    [SerializeField] private Color shallowWaterColor = new Color(0.3f, 0.5f, 0.9f);
    [SerializeField] private Color riverColor = new Color(0.4f, 0.6f, 1.0f); // Keep rivers distinct
    [SerializeField] private Color beachColor = new Color(0.8f, 0.75f, 0.5f);
    [SerializeField] private Color polarIceColor = Color.white;
    [SerializeField] private Color tundraColor = new Color(0.7f, 0.7f, 0.8f);
    [SerializeField] private Color taigaColor = new Color(0.3f, 0.5f, 0.4f);
    [SerializeField] private Color mountainColor = new Color(0.6f, 0.6f, 0.6f); // General mountain color
    [SerializeField] private Color highMountainColor = new Color(0.9f, 0.9f, 0.95f); // Snow peak color
    [SerializeField] private Color grasslandColor = new Color(0.5f, 0.8f, 0.4f); // General grassland/steppe/savanna
    [SerializeField] private Color shrublandColor = new Color(0.6f, 0.6f, 0.3f); // General shrubland/mediterranean
    [SerializeField] private Color temperateForestColor = new Color(0.2f, 0.7f, 0.2f); // General temperate forest
    [SerializeField] private Color tropicalForestColor = new Color(0.1f, 0.5f, 0.1f); // General tropical/subtropical forest
    [SerializeField] private Color desertColor = new Color(0.85f, 0.75f, 0.4f);
    [SerializeField] private Color wetlandsColor = new Color(0.3f, 0.45f, 0.3f);
    [SerializeField] private Color riparianColor = new Color(0.1f, 0.7f, 0.4f); // Can be same as grassland or distinct
    [SerializeField] private Color poiColor = Color.yellow; // Color for major POIs
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
        textMeshPro.text = "Generating simplified map...";
    }

    /// <summary>
    /// Generates and displays the simplified world map text.
    /// 간략화된 월드맵 텍스트를 생성하고 표시합니다.
    /// </summary>
    public void Visualize(SimplifiedWorldData simplifiedData)
    {
        if (simplifiedData == null || simplifiedData.Width == 0 || simplifiedData.Height == 0)
        {
            if (textMeshPro != null) textMeshPro.text = "Error: Invalid Simplified Data";
            Debug.LogError("Cannot visualize: SimplifiedWorldData is null or has zero dimensions.");
            return;
        }
        if (textMeshPro == null) { Debug.LogError("TextMeshProUGUI component not found!"); return; }

        StringBuilder mapBuilder = new StringBuilder(simplifiedData.Width * simplifiedData.Height + simplifiedData.Height);

        for (int y = 0; y < simplifiedData.Height; y++)
        {
            for (int x = 0; x < simplifiedData.Width; x++)
            {
                SimplifiedWorldTile tile = simplifiedData.SimplifiedTiles[y, x];
                char displayChar = '?';
                Color displayColor = defaultColor;

                // --- Determine Character and Color ---
                // Priority: POI > River > Biome
                if (tile.HasMajorPOI)
                {
                    displayChar = '*'; // Simple marker for POI on simplified map
                    displayColor = poiColor;
                }
                // River Biome is already prioritized during WorldGenerator's simplification
                else
                {
                    AssignSimplifiedBiomeCharacterAndColor(tile.DominantBiome, out displayChar, out displayColor);
                }

                mapBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(displayColor)}>{displayChar}</color>");
            }
            mapBuilder.Append('\n');
        }

        textMeshPro.text = mapBuilder.ToString().TrimEnd('\n');
        Debug.Log($"Simplified world map visualized on {gameObject.name}.");
    }

    /// <summary>
    /// Assigns a simplified ASCII character and color based on the dominant BiomeType.
    /// 주된 BiomeType에 따라 간략화된 ASCII 문자와 색상을 할당합니다.
    /// </summary>
    private void AssignSimplifiedBiomeCharacterAndColor(BiomeType biome, out char character, out Color color)
    {
        character = '?';
        color = defaultColor;

        // Assign based on BiomeType using simplified characters and grouped colors
        switch (biome)
        {
            // Water & Coast
            case BiomeType.DeepWater: character = ' '; color = deepWaterColor; break;
            case BiomeType.ShallowWater: character = '~'; color = shallowWaterColor; break;
            case BiomeType.River: character = '~'; color = riverColor; break; // Rivers are important
            case BiomeType.Beach: character = '.'; color = beachColor; break;

            // Polar & Tundra
            case BiomeType.PolarIce: character = '#'; color = polarIceColor; break;
            case BiomeType.Tundra:
            case BiomeType.MountainTundra: character = '-'; color = tundraColor; break; // Group Tundras

            // Forests (Grouped)
            case BiomeType.Taiga:
            case BiomeType.AlpineForest:
            case BiomeType.TemperateConiferousForest:
                character = '*'; color = taigaColor; break; // Coniferous group
            case BiomeType.TemperateDeciduousForest:
            case BiomeType.TemperateMixedForest:
            case BiomeType.TemperateRainforest:
                character = '&'; color = temperateForestColor; break; // Temperate broadleaf/mixed group
            case BiomeType.SubtropicalDryForest:
            case BiomeType.SubtropicalMoistForest:
            case BiomeType.TropicalDryForest:
            case BiomeType.TropicalMoistForest:
            case BiomeType.TropicalRainforest:
                character = '%'; color = tropicalForestColor; break; // Tropical/Subtropical group

            // Grasslands & Shrublands (Grouped)
            case BiomeType.ColdParklands:
            case BiomeType.Steppe:
            case BiomeType.TemperateGrassland:
            case BiomeType.SubtropicalGrassland:
            case BiomeType.TropicalGrassland:
            case BiomeType.FloodedGrassland:
                character = ','; color = grasslandColor; break; // Grassland group
            case BiomeType.Shrubland:
            case BiomeType.Mediterranean:
                character = ';'; color = shrublandColor; break; // Shrubland group

            // Deserts
            case BiomeType.Desert: character = ':'; color = desertColor; break;

            // Wetlands & Riparian
            case BiomeType.Wetlands: character = '='; color = wetlandsColor; break;
            case BiomeType.Riparian: character = '§'; color = riparianColor; break; // Keep distinct maybe? Or merge with grassland

            // Topography (Simplified)
            case BiomeType.Mountainous: character = '^'; color = mountainColor; break;
            case BiomeType.HighMountain: character = '▲'; color = highMountainColor; break;
            case BiomeType.Hills: character = 'n'; color = grasslandColor; break; // Hills as 'n' on grassland color

            // Fallbacks for older simple types if they somehow appear
            case BiomeType.Plains: character = ','; color = grasslandColor; break;
            case BiomeType.Forest: character = '%'; color = temperateForestColor; break;
            case BiomeType.Mountain: character = '^'; color = mountainColor; break;
            case BiomeType.Swamp: character = '='; color = wetlandsColor; break;

            // Default for unhandled (should not happen if BiomeType enum is updated)
            default:
                // Check if the biome exists in the enum before logging warning
                if (!Enum.IsDefined(typeof(BiomeType), biome))
                {
                    Debug.LogWarning($"Unhandled BiomeType '{biome}' encountered in Simplified Visualizer. Using default.");
                }
                // Assign defaults even if defined but missed in switch
                character = '?';
                color = defaultColor;
                break;
        }
    }
}