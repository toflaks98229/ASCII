using UnityEngine;

/// <summary>
/// Represents the configurable properties of a single map tile type using a ScriptableObject.
/// ScriptableObject를 사용하여 단일 맵 타일 타입의 설정 가능한 속성을 나타냅니다.
/// </summary>
[CreateAssetMenu(fileName = "New TileType", menuName = "Roguelike/TileType")] // Allows creating TileType assets from the Unity Editor menu
public class TileType : ScriptableObject // Inherit from ScriptableObject instead of struct
{
    [Header("Tile Properties")]
    [Tooltip("Can this tile be walked on? 이동 가능한 타일인가?")]
    public bool walkable = true;

    [Tooltip("Does this tile block Field of View? 시야를 막는 타일인가?")]
    public bool transparent = true;

    [Header("Display (Light - In FOV)")]
    [Tooltip("Character to display when in FOV 시야 내에서 표시될 문자")]
    public char lightChar = '.';

    [Tooltip("Color when in FOV 시야 내 색상")]
    [ColorUsage(true, true)]
    public Color lightColor = Color.white;

    [Tooltip("Background color when in FOV 시야 내 배경색 (Optional)")]
    [ColorUsage(true, true)]
    public Color lightBackgroundColor = Color.black;

    [Header("Display (Dark - Explored, Not in FOV)")]
    [Tooltip("Character to display when explored but not in FOV 탐험되었지만 시야 밖에 있을 때 표시될 문자")]
    public char darkChar = '.'; // Default to lightChar if not specified? Consider adding logic if needed.


    [Tooltip("Color when explored but not in FOV 탐험되었지만 시야 밖일 때 색상")]
    [ColorUsage(true, true)]
    public Color darkColor = Color.grey;

    [Tooltip("Background color when explored but not in FOV 탐험되었지만 시야 밖일 때 배경색 (Optional)")]
    [ColorUsage(true, true)]
    public Color darkBackgroundColor = Color.black;


    // Note: The static readonly fields (Floor, Wall, etc.) are removed.
    // These will now be separate ScriptableObject assets created in the editor.
    // 참고: static readonly 필드(Floor, Wall 등)는 제거되었습니다.
    // 이제 에디터에서 생성된 별도의 ScriptableObject 에셋이 됩니다.
}
