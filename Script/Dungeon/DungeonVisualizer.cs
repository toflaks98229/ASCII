using UnityEngine;
using System.Text;
using TMPro;
using System.Collections.Generic; // List 사용
// using System.Collections; // EffectSequencer로 이동했으므로 IEnumerator 불필요해질 수 있음
// using System; // Action 불필요

public class DungeonVisualizer : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("화면에 표시할 맵의 너비 (문자 개수)")]
    [SerializeField] private int displayWidth = 60;
    [Tooltip("화면에 표시할 맵의 높이 (줄 개수)")]
    [SerializeField] private int displayHeight = 30;

    [Header("Component References")]
    [Tooltip("맵이 표시되는 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI mapTextComponent;


    // activeEffects 리스트와 TemporaryVisualEffect 구조체 제거됨
    // private List<TemporaryVisualEffect> activeEffects = new List<TemporaryVisualEffect>();
    private TemporaryVisualEffect? virtualCursorEffect = null; // 가상 커서는 유지

    public int LastRenderedStartX { get; private set; }
    public int LastRenderedStartY { get; private set; }
    public int DisplayWidth => displayWidth;
    public int DisplayHeight => displayHeight;

    private RectTransform mapRectTransform;

    // TemporaryVisualEffect 구조체는 가상 커서용으로만 남겨두거나,
    // 가상 커서도 Particle 시스템으로 통합할지 고민 필요. 여기서는 유지.
    private struct TemporaryVisualEffect
    {
        public Vector2Int position;
        public char character;
        public Color color;
        public float expiryTime;
        public bool isVirtualCursor;

        public TemporaryVisualEffect(Vector2Int pos, char ch, Color col, float duration, bool isCursor = false)
        {
            position = pos;
            character = ch;
            color = col;
            expiryTime = Time.time + duration;
            isVirtualCursor = isCursor;
        }
    }


    void Awake()
    {
        if (mapTextComponent == null)
        {
            Debug.LogError("DungeonVisualizer: mapTextComponent가 할당되지 않았습니다!");
            this.enabled = false;
            return;

        }
        mapRectTransform = mapTextComponent.GetComponent<RectTransform>();
    }

    void Start()
    {
        // GameManager 등을 통해 ParticleManager와 EffectSequencer 인스턴스 가져오기
        // if (particleManager == null) particleManager = ParticleManager.Instance;
        // if (effectSequencer == null) effectSequencer = EffectSequencer.Instance;
        // if (particleManager == null) Debug.LogError("DungeonVisualizer: ParticleManager 인스턴스를 찾을 수 없습니다!");
        // if (effectSequencer == null) Debug.LogError("DungeonVisualizer: EffectSequencer 인스턴스를 찾을 수 없습니다!");

        AdjustRectTransformSize();
    }

    public void AdjustRectTransformSize()
    {
        if (mapTextComponent == null || mapRectTransform == null) return;
        string widthSampleText = new string('M', displayWidth);
        StringBuilder heightSampleBuilder = new StringBuilder();
        for (int i = 0; i < displayHeight; i++) { heightSampleBuilder.Append("M\n"); }
        string heightSampleText = heightSampleBuilder.ToString().TrimEnd('\n');
        Vector2 widthPreferred = mapTextComponent.GetPreferredValues(widthSampleText, float.PositiveInfinity, float.PositiveInfinity);
        Vector2 heightPreferred = mapTextComponent.GetPreferredValues(heightSampleText, float.PositiveInfinity, float.PositiveInfinity);
        float targetWidth = widthPreferred.x;
        float targetHeight = heightPreferred.y;

        RectTransform parentRectTransform = mapRectTransform.parent as RectTransform;
        if (parentRectTransform != null)
        {
            float parentWidth = parentRectTransform.rect.width;
            float parentHeight = parentRectTransform.rect.height;
            Vector2 newSizeDelta = new Vector2(targetWidth - parentWidth, targetHeight - parentHeight);
            mapRectTransform.sizeDelta = newSizeDelta;
        }
    }

    public void RenderMap(MapData mapData, Player player)
    {
        if (mapTextComponent == null || mapData == null || player == null || ParticleManager.Instance == null)
        {
            // Debug.LogWarning("RenderMap: 필수 컴포넌트가 준비되지 않았습니다.");
            if (mapTextComponent != null) mapTextComponent.text = "맵 로딩 중...";
            return;
        }
        mapTextComponent.text = BuildMapString(mapData, player, ParticleManager.Instance.GetActiveParticles());
    }

    private string BuildMapString(MapData mapData, Player player, List<Particle> activeParticles)
    {
        StringBuilder mapStringBuilder = new StringBuilder(displayWidth * displayHeight + displayHeight);
        int currentMapW = mapData.Width;
        int currentMapH = mapData.Height;

        int sx = Mathf.Clamp(player.gridX - displayWidth / 2, 0, Mathf.Max(0, currentMapW - displayWidth));
        int sy = Mathf.Clamp(player.gridY - displayHeight / 2, 0, Mathf.Max(0, currentMapH - displayHeight));

        if (currentMapW < displayWidth) sx = 0;
        if (currentMapH < displayHeight) sy = 0;

        LastRenderedStartX = sx;
        LastRenderedStartY = sy;

        // 가상 커서 만료 처리 (기존 로직 유지)
        if (virtualCursorEffect.HasValue && Time.time >= virtualCursorEffect.Value.expiryTime)
        {
            virtualCursorEffect = null;
        }

        // 화면에 표시될 문자/색상 정보를 저장할 2차원 배열 (또는 유사 구조)
        // 기본적으로 타일/엔티티 정보로 채우고, 파티클로 덮어쓰기
        char[,] displayChars = new char[displayHeight, displayWidth];
        Color[,] displayColors = new Color[displayHeight, displayWidth];
        // Color[,] backgroundColors = new Color[displayHeight, displayWidth]; // 필요시 배경색도

        // 1. 기본 맵 타일 및 엔티티 렌더링 준비
        for (int y_view = 0; y_view < displayHeight; y_view++)
        {
            for (int x_view = 0; x_view < displayWidth; x_view++)
            {
                int worldX = LastRenderedStartX + x_view;
                int worldY = LastRenderedStartY + y_view;
                char tileChar = ' '; Color tileColor = Color.black;

                if (mapData.IsInBounds(worldX, worldY))
                {
                    TileType tile = mapData.Tiles[worldY, worldX];
                    bool isVisible = mapData.Visible[worldY, worldX];
                    bool isExplored = mapData.Explored[worldY, worldX];

                    if (tile == null) tile = TileDatabase.Instance?.OutOfBounds ?? TileDatabase.Instance?.Shroud;
                    if (tile == null) { tileChar = '?'; tileColor = Color.magenta; }
                    else
                    {
                        Entity entityOnTile = mapData.GetEntityAt(worldX, worldY);
                        if (isVisible)
                        {
                            if (entityOnTile != null) { tileChar = entityOnTile.displayChar; tileColor = entityOnTile.displayColor; }
                            else { tileChar = tile.lightChar; tileColor = tile.lightColor; }
                        }
                        else if (isExplored) { tileChar = tile.darkChar; tileColor = tile.darkColor; }
                        else
                        {
                            TileType shroudTile = TileDatabase.Instance?.Shroud;
                            if (shroudTile != null) { tileChar = shroudTile.lightChar; tileColor = shroudTile.lightColor; }
                        }
                    }
                }
                else
                {
                    TileType oobTile = TileDatabase.Instance?.OutOfBounds;
                    if (oobTile != null) { tileChar = oobTile.lightChar; tileColor = oobTile.lightColor; }
                }
                displayChars[y_view, x_view] = tileChar;
                displayColors[y_view, x_view] = tileColor;
            }
        }

        // 2. 활성 파티클 렌더링 준비 (기존 맵 위에 덮어쓰기)
        if (activeParticles != null)
        {
            foreach (Particle particle in activeParticles)
            {
                if (!particle.IsActive) continue;

                int worldX = Mathf.RoundToInt(particle.Position.x);
                int worldY = Mathf.RoundToInt(particle.Position.y);

                // 파티클이 현재 뷰포트 내에 있는지 확인
                if (worldX >= LastRenderedStartX && worldX < LastRenderedStartX + displayWidth &&
                    worldY >= LastRenderedStartY && worldY < LastRenderedStartY + displayHeight)
                {
                    int viewX = worldX - LastRenderedStartX;
                    int viewY = worldY - LastRenderedStartY;

                    // TODO: 파티클 렌더링 순서 (renderOrder) 또는 알파 블렌딩(ASCII에서는 어려움) 고려
                    // 현재는 파티클이 최상위에 그려짐
                    displayChars[viewY, viewX] = particle.CurrentChar;
                    displayColors[viewY, viewX] = particle.CurrentColor;
                }
            }
        }

        // 3. 가상 커서 렌더링 준비 (파티클 위에도 덮어쓸 수 있음)
        if (virtualCursorEffect.HasValue)
        {
            Vector2Int cursorPos = virtualCursorEffect.Value.position;
            if (cursorPos.x >= LastRenderedStartX && cursorPos.x < LastRenderedStartX + displayWidth &&
                cursorPos.y >= LastRenderedStartY && cursorPos.y < LastRenderedStartY + displayHeight)
            {
                int viewX = cursorPos.x - LastRenderedStartX;
                int viewY = cursorPos.y - LastRenderedStartY;
                displayChars[viewY, viewX] = virtualCursorEffect.Value.character;
                displayColors[viewY, viewX] = virtualCursorEffect.Value.color;
            }
        }

        // 4. 최종 문자열 빌드
        for (int y_view = 0; y_view < displayHeight; y_view++)
        {
            for (int x_view = 0; x_view < displayWidth; x_view++)
            {
                mapStringBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(displayColors[y_view, x_view])}>{displayChars[y_view, x_view]}</color>");
            }
            mapStringBuilder.Append('\n');
        }

        return mapStringBuilder.ToString().TrimEnd('\n');
    }


    // PlaySpellProjectileAnimation 코루틴은 EffectSequencer로 이동함
    // public IEnumerator PlaySpellProjectileAnimation(SpellAnimationData animData, Action onAnimationComplete) { ... }


    // GetLinePoints는 EffectSequencer에서 필요하다면 그쪽으로 이동하거나, 공용 유틸리티 클래스로 분리 가능
    // private List<Vector2Int> GetLinePoints(Vector2Int start, Vector2Int end) { ... }

    // AddTemporaryEffect는 ParticleManager와 EffectSequencer를 사용하도록 변경되므로 제거
    // public void AddTemporaryEffect(Vector2Int position, char character, Color color, float duration) { ... }


    // 가상 커서 관련 메서드는 유지
    public void UpdateVirtualCursorEffect(Vector2Int position, char character, Color color, float duration)
    {
        virtualCursorEffect = null; // 이전 효과 제거
        virtualCursorEffect = new TemporaryVisualEffect(position, character, color, duration, true);
    }

    public void RemoveVirtualCursorEffect()
    {
        virtualCursorEffect = null;
    }
}