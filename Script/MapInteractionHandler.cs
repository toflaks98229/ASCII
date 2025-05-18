// MapInteractionHandler.cs
using UnityEngine;
using UnityEngine.EventSystems; // Event System 네임스페이스 추가
using TMPro;

/// <summary>
/// 맵 TextMeshProUGUI 위에서의 마우스 상호작용을 처리합니다.
/// 마우스 위치의 타일 정보를 표시하고 가상 커서를 관리합니다.
/// Handles mouse interaction over the map TextMeshProUGUI component.
/// Displays tile information at the mouse position and manages the virtual cursor.
/// </summary>
public class MapInteractionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler // 인터페이스 구현
{
    [Header("참조 (References)")]
    [Tooltip("맵이 표시되는 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI mapTextComponent;
    [Tooltip("DungeonVisualizer 컴포넌트 참조")]
    [SerializeField] private DungeonVisualizer dungeonVisualizer;
    [Tooltip("정보를 표시할 TextMeshProUGUI (선택 사항)")]
    [SerializeField] private TextMeshProUGUI infoTextComponent;

    [Header("가상 커서 설정 (Virtual Cursor Settings)")]
    [Tooltip("가상 커서로 사용할 문자")]
    [SerializeField] private char cursorCharacter = '▒';
    [Tooltip("가상 커서의 색상")]
    [SerializeField] private Color cursorColor = Color.yellow;
    [Tooltip("커서 점멸 간격 (초)")]
    [SerializeField] private float blinkInterval = 0.5f;
    [Tooltip("마우스 이동 감도 (오프셋 기반 이동 방식 사용 시)")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [Tooltip("마우스 비활성 시 커서 숨김 딜레이 (초)")]
    [SerializeField] private float mouseHideDelay = 3.0f;

    // --- 커서 위치 관리 변수 ---
    private Vector2Int currentWorldCursorPosition; // 계산된 현재 커서의 최종 월드 좌표 (정수)
    private Vector2Int lastKnownWorldCursorPosition = Vector2Int.one * -1; // 이전 프레임의 커서 월드 좌표 (변경 감지용)

    // --- 방법 2 (오프셋 기반) 사용 시 필요한 변수들 (현재는 주석 처리된 함수 내에서만 사용) ---
    private Vector2 cursorOffsetFromPlayer_Method2;
    private Vector2Int currentGridOffset_Method2;
    // ---

    private bool isCursorVisible = true;
    private float blinkTimer = 0f;
    private float lastMouseMoveTime = 0f;
    private bool shouldHideCursorDueToInactivity = false;
    private bool isMouseOverMap = false;

    private RectTransform mapRectTransform;
    private Camera mainCamera;

    void Start()
    {
        if (mapTextComponent == null || dungeonVisualizer == null)
        {
            Debug.LogError("MapInteractionHandler: 필수 컴포넌트가 할당되지 않았습니다!");
            this.enabled = false; return;
        }
        mapRectTransform = mapTextComponent.GetComponent<RectTransform>();
        if (!mapTextComponent.raycastTarget) { Debug.LogWarning("MapInteractionHandler: mapTextComponent의 Raycast Target이 비활성화되어 있습니다."); }

        Canvas canvas = mapTextComponent.canvas;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) { mainCamera = null; }
        else
        {
            mainCamera = canvas.worldCamera;
            if (mainCamera == null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera) { mainCamera = Camera.main; }
                else { Debug.LogError("MapInteractionHandler: World Space Canvas에 카메라가 설정되지 않았습니다!"); this.enabled = false; return; }
            }
        }

        InitializeCursorPosition();
        if (infoTextComponent != null) { infoTextComponent.text = ""; }
        blinkTimer = blinkInterval;
        lastMouseMoveTime = Time.time;
    }

    void OnEnable()
    {
        InitializeCursorPosition();
        lastMouseMoveTime = Time.time;
        shouldHideCursorDueToInactivity = false;
        isMouseOverMap = false;
        if (dungeonVisualizer != null) { dungeonVisualizer.RemoveVirtualCursorEffect(); }
        if (infoTextComponent != null) { infoTextComponent.text = ""; }
    }

    void OnDisable()
    {
        if (dungeonVisualizer != null) { dungeonVisualizer.RemoveVirtualCursorEffect(); }
        Cursor.lockState = CursorLockMode.None;
        isMouseOverMap = false;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void InitializeCursorPosition()
    {
        // 방법 2 변수 초기화
        cursorOffsetFromPlayer_Method2 = Vector2.zero;
        currentGridOffset_Method2 = Vector2Int.zero;

        // 현재 커서 위치 초기화 (플레이어 위치 또는 중앙)
        if (GameManager.Instance?.PlayerInstance != null)
        {
            currentWorldCursorPosition = new Vector2Int(GameManager.Instance.PlayerInstance.gridX, GameManager.Instance.PlayerInstance.gridY);
        }
        else if (dungeonVisualizer != null)
        {
            currentWorldCursorPosition = new Vector2Int(
                dungeonVisualizer.LastRenderedStartX + dungeonVisualizer.DisplayWidth / 2,
                dungeonVisualizer.LastRenderedStartY + dungeonVisualizer.DisplayHeight / 2);
            ClampWorldPosition(ref currentWorldCursorPosition);
        }
        else { currentWorldCursorPosition = Vector2Int.zero; }

        lastKnownWorldCursorPosition = Vector2Int.one * -1; // 초기에는 이전 위치 없음
        isCursorVisible = true;
        blinkTimer = blinkInterval;
        lastMouseMoveTime = Time.time;
        shouldHideCursorDueToInactivity = false;
    }

    void Update()
    {
        if (!isMouseOverMap) return; // 마우스가 맵 위에 없으면 종료

        if (GameManager.Instance?.CurrentMapData == null || dungeonVisualizer == null)
        {
            if (infoTextComponent != null) infoTextComponent.text = "정보 없음";
            return;
        }

        // --- 커서 위치 업데이트 ---
        // 방법 1: 마우스 위치 직접 감지 (현재 활성화된 방식)
        UpdateCursorPosition_DirectMouse();

        // 방법 2: 마우스 움직임 누적 (비활성화된 방식)
        // UpdateCursorPosition_OffsetBased(); // 사용하려면 이 줄의 주석 해제하고 위 줄 주석 처리

        // --- 마우스 비활성 감지 및 숨김 처리 ---
        HandleCursorHiding();

        // --- 커서 점멸 처리 ---
        HandleCursorBlinking();

        // --- 시각 효과 및 정보 업데이트 ---
        UpdateCursorVisual();
        UpdateInfoText();
    }

    // ========================================================================
    // 커서 위치 업데이트 방법 1: 마우스 위치 직접 감지
    // ========================================================================
    /// <summary>
    /// 마우스 현재 위치를 기반으로 가상 커서의 월드 좌표를 직접 업데이트합니다.
    /// </summary>
    private void UpdateCursorPosition_DirectMouse()
    {
        Vector2Int targetWorldPos = GetWorldPositionFromMouse(Input.mousePosition);

        if (targetWorldPos != Vector2Int.one * -1) // 마우스가 유효한 맵 셀 위에 있을 때
        {
            lastMouseMoveTime = Time.time; // 마우스가 맵 위에 있으면 항상 움직인 것으로 간주 (숨김 방지)
            shouldHideCursorDueToInactivity = false;

            if (targetWorldPos != lastKnownWorldCursorPosition) // 이전 위치와 다를 때만 업데이트 및 점멸 리셋
            {
                currentWorldCursorPosition = targetWorldPos;
                ClampWorldPosition(ref currentWorldCursorPosition); // 경계 확인
                ResetBlink();
                // Debug.Log($"Cursor Pos (Direct): {currentWorldCursorPosition}");
                lastKnownWorldCursorPosition = currentWorldCursorPosition;
            }
        }
        else // 마우스가 맵 영역 밖이거나 유효하지 않은 위치일 때
        {
            // 커서 위치를 변경하지 않거나, 숨김 처리 (현재는 변경 안 함)
            // lastKnownWorldCursorPosition = Vector2Int.one * -1; // 맵 밖으로 나가면 이전 위치 리셋
        }
    }

    // ========================================================================
    // 커서 위치 업데이트 방법 2: 마우스 움직임 누적 (현재 비활성화)
    // ========================================================================
    /// <summary>
    /// 마우스 이동량(GetAxis)을 누적하여 플레이어 기준 오프셋을 계산하고,
    /// 이를 바탕으로 가상 커서의 월드 좌표를 업데이트합니다. (현재 사용 안 함)
    /// </summary>
    private void UpdateCursorPosition_OffsetBased()
    {
        /* // 사용하려면 이 주석 블록 해제
        Vector2Int playerPos = Vector2Int.zero;
        bool playerExists = GameManager.Instance.PlayerInstance != null;
        if(playerExists) {
            playerPos = new Vector2Int(GameManager.Instance.PlayerInstance.gridX, GameManager.Instance.PlayerInstance.gridY);
        } else {
            playerPos = currentWorldCursorPosition;
            cursorOffsetFromPlayer_Method2 = Vector2.zero;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            lastMouseMoveTime = Time.time;
            shouldHideCursorDueToInactivity = false;
            cursorOffsetFromPlayer_Method2.x += mouseX * mouseSensitivity;
            cursorOffsetFromPlayer_Method2.y -= mouseY * mouseSensitivity;
             Debug.Log($"Mouse Moved: dX={mouseX:F3}, dY={mouseY:F3} => New Offset(M2): {cursorOffsetFromPlayer_Method2}");
        }

        Vector2Int newGridOffset = new Vector2Int(
            Mathf.FloorToInt(cursorOffsetFromPlayer_Method2.x),
            Mathf.FloorToInt(cursorOffsetFromPlayer_Method2.y)
        );

        if (newGridOffset != currentGridOffset_Method2)
        {
             Debug.Log($"Grid Offset Changed(M2): {currentGridOffset_Method2} -> {newGridOffset}");
            currentGridOffset_Method2 = newGridOffset;
            ResetBlink();
        }

        Vector2Int previousWorldCursorPos = currentWorldCursorPosition;
        currentWorldCursorPosition = playerPos + currentGridOffset_Method2;
        ClampWorldPosition(ref currentWorldCursorPosition);
        if (previousWorldCursorPos != currentWorldCursorPosition) Debug.Log($"World Cursor Pos (Offset Based): {previousWorldCursorPos} -> {currentWorldCursorPosition}");
        */
    }


    // --- 이하 공통 헬퍼 함수들 ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOverMap = true;
        Debug.Log($"<color=green>OnPointerEnter: Mouse entered map area.</color> Screen Pos: {eventData.position}, Camera: {eventData.enterEventCamera?.name}");

        // 마우스 진입 시 커서 위치 즉시 업데이트 (방법 1 기준)
        Vector2Int targetWorldPos = GetWorldPositionFromMouse(eventData.position);
        if (targetWorldPos != Vector2Int.one * -1)
        {
            Debug.Log($"OnPointerEnter: Calculated World Pos: {targetWorldPos}");
            currentWorldCursorPosition = targetWorldPos;
            ClampWorldPosition(ref currentWorldCursorPosition);
            lastKnownWorldCursorPosition = currentWorldCursorPosition; // 이전 위치도 업데이트

            // 오프셋 기반 방식 사용 시 오프셋도 계산
            // if (GameManager.Instance?.PlayerInstance != null) {
            //      Vector2Int playerPos = new Vector2Int(GameManager.Instance.PlayerInstance.gridX, GameManager.Instance.PlayerInstance.gridY);
            //      cursorOffsetFromPlayer_Method2 = targetWorldPos - playerPos;
            //      currentGridOffset_Method2 = new Vector2Int(Mathf.FloorToInt(cursorOffsetFromPlayer_Method2.x), Mathf.FloorToInt(cursorOffsetFromPlayer_Method2.y));
            //      Debug.Log($"OnPointerEnter: Calculated Offset(M2): {cursorOffsetFromPlayer_Method2}, Grid Offset(M2): {currentGridOffset_Method2}");
            // }

            ResetBlink();
            UpdateCursorVisual();
            UpdateInfoText();
        }
        else
        {
            Debug.LogWarning("OnPointerEnter: Could not calculate valid world position from mouse.");
        }
        lastMouseMoveTime = Time.time;
        shouldHideCursorDueToInactivity = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOverMap = false;
        Debug.Log("<color=red>OnPointerExit: Mouse exited map area.</color>");
        if (dungeonVisualizer != null)
        {
            dungeonVisualizer.RemoveVirtualCursorEffect();
            //if (GameManager.Instance != null) GameManager.Instance.RenderCurrentMap();
        }
        if (infoTextComponent != null) { infoTextComponent.text = ""; }
        lastKnownWorldCursorPosition = Vector2Int.one * -1; // 맵 나가면 이전 위치 리셋
    }

    private Vector2Int GetWorldPositionFromMouse(Vector2 screenPosition)
    {
        Vector2 localMousePosition;
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapTextComponent.rectTransform, screenPosition, mainCamera, out localMousePosition);

        // Debug.Log($"GetWorldPos: Screen={screenPosition}, isInside={isInside}, Local={localMousePosition}");

        if (isInside)
        {
            RectTransform rectTransform = mapTextComponent.rectTransform;
            float rectWidth = rectTransform.rect.width;
            float rectHeight = rectTransform.rect.height;
            Vector4 margins = mapTextComponent.margin;
            float effectiveWidth = rectWidth - margins.x - margins.z;
            float effectiveHeight = rectHeight - margins.y - margins.w;

            // Debug.Log($"GetWorldPos: Rect={rectTransform.rect}, Pivot={rectTransform.pivot}, Margins={margins}, Effective Size=({effectiveWidth}, {effectiveHeight})");

            if (effectiveWidth <= 0 || effectiveHeight <= 0) { return Vector2Int.one * -1; }

            float textOriginX_local = -rectTransform.pivot.x * rectWidth + margins.x;
            float textOriginY_local = (1 - rectTransform.pivot.y) * rectHeight - margins.y;

            float mouseX_relativeToTextOrigin = localMousePosition.x - textOriginX_local;
            float mouseY_relativeToTextOrigin = localMousePosition.y - textOriginY_local;

            float normalizedX = mouseX_relativeToTextOrigin / effectiveWidth;
            float normalizedY = -mouseY_relativeToTextOrigin / effectiveHeight; // 위쪽 0, 아래쪽 1

            // Debug.Log($"GetWorldPos: TextOriginLocal=({textOriginX_local:F2}, {textOriginY_local:F2}), MouseRelativeToOrigin=({mouseX_relativeToTextOrigin:F2}, {mouseY_relativeToTextOrigin:F2}), Normalized=({normalizedX:F3}, {normalizedY:F3})");

            if (normalizedX < 0f || normalizedX >= 1f || normalizedY < 0f || normalizedY >= 1f) { return Vector2Int.one * -1; }

            int viewX = Mathf.FloorToInt(normalizedX * dungeonVisualizer.DisplayWidth);
            int viewY = Mathf.FloorToInt(normalizedY * dungeonVisualizer.DisplayHeight);

            // Debug.Log($"GetWorldPos: View Coords=({viewX}, {viewY})");

            if (viewX >= 0 && viewX < dungeonVisualizer.DisplayWidth && viewY >= 0 && viewY < dungeonVisualizer.DisplayHeight)
            {
                int worldX = dungeonVisualizer.LastRenderedStartX + viewX;
                int worldY = dungeonVisualizer.LastRenderedStartY + viewY;

                // Debug.Log($"GetWorldPos: Calculated World Coords=({worldX}, {worldY}) (StartX={dungeonVisualizer.LastRenderedStartX}, StartY={dungeonVisualizer.LastRenderedStartY})");

                if (GameManager.Instance?.CurrentMapData != null && GameManager.Instance.CurrentMapData.IsInBounds(worldX, worldY))
                {
                    return new Vector2Int(worldX, worldY);
                }
                else
                {
                    // Debug.LogWarning($"GetWorldPos: Calculated World Coords ({worldX},{worldY}) are out of map bounds.");
                }
            }
            else
            {
                // Debug.LogWarning($"GetWorldPos: Calculated View Coords ({viewX},{viewY}) are out of display bounds ({dungeonVisualizer.DisplayWidth},{dungeonVisualizer.DisplayHeight}).");
            }
        }
        return Vector2Int.one * -1;
    }

    private void ClampWorldPosition(ref Vector2Int worldPosition)
    {
        if (GameManager.Instance?.CurrentMapData != null)
        {
            MapData map = GameManager.Instance.CurrentMapData;
            worldPosition.x = Mathf.Clamp(worldPosition.x, 0, map.Width - 1);
            worldPosition.y = Mathf.Clamp(worldPosition.y, 0, map.Height - 1);
        }
    }

    private void HandleCursorHiding() // 마우스 숨김 처리 함수
    {
        if (mouseHideDelay > 0f && Time.time - lastMouseMoveTime > mouseHideDelay)
        {
            shouldHideCursorDueToInactivity = true;
        }
        // 마우스가 움직이면 shouldHideCursorDueToInactivity는 Update 또는 OnPointerEnter에서 false로 설정됨
    }


    private void HandleCursorBlinking()
    {
        if (shouldHideCursorDueToInactivity)
        {
            isCursorVisible = false;
            return;
        }
        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            isCursorVisible = !isCursorVisible;
            blinkTimer = blinkInterval;
        }
    }

    private void ResetBlink()
    {
        isCursorVisible = true;
        blinkTimer = blinkInterval;
        shouldHideCursorDueToInactivity = false; // 숨김 상태 해제
        lastMouseMoveTime = Time.time; // 리셋 시점도 마지막 이동으로 간주
    }

    private void UpdateCursorVisual()
    {
        if (dungeonVisualizer != null)
        {
            if (isCursorVisible)
            {
                dungeonVisualizer.UpdateVirtualCursorEffect(currentWorldCursorPosition, cursorCharacter, cursorColor, blinkInterval * 1.1f);
            }
            else
            {
                dungeonVisualizer.RemoveVirtualCursorEffect();
            }
            //if (GameManager.Instance != null) GameManager.Instance.RenderCurrentMap();
        }
    }

    private void UpdateInfoText()
    {
        if (infoTextComponent == null) return;
        if (shouldHideCursorDueToInactivity || !isMouseOverMap)
        {
            infoTextComponent.text = "";
            return;
        }

        int worldX = currentWorldCursorPosition.x;
        int worldY = currentWorldCursorPosition.y;

        if (GameManager.Instance.CurrentMapData.IsInBounds(worldX, worldY))
        {
            MapData currentMap = GameManager.Instance.CurrentMapData;
            TileType tile = currentMap.Tiles[worldY, worldX];
            Entity entity = currentMap.GetEntityAt(worldX, worldY);
            bool isVisible = currentMap.Visible[worldY, worldX];
            bool isExplored = currentMap.Explored[worldY, worldX];
            char displayChar = ' ';
            string tileName = "알 수 없음";
            string entityName = "없음";

            if (isVisible)
            {
                if (entity != null) { displayChar = entity.displayChar; entityName = entity.entityName; tileName = tile != null ? tile.name : "바닥"; }
                else if (tile != null) { displayChar = tile.lightChar; tileName = tile.name; }
            }
            else if (isExplored && tile != null) { displayChar = tile.darkChar; tileName = tile.name + " (탐험됨)"; }
            else
            {
                TileType shroudTile = TileDatabase.Instance?.Shroud;
                if (shroudTile != null) displayChar = shroudTile.lightChar;
                else displayChar = ' ';
                tileName = "알 수 없음 (가려짐)";
            }
            infoTextComponent.text = $"위치: ({worldX}, {worldY})\n문자: {displayChar}\n타일: {tileName}\n엔티티: {entityName}\n보임: {isVisible}, 탐험: {isExplored}";
        }
        else { infoTextComponent.text = "맵 범위 밖"; }
    }
}
