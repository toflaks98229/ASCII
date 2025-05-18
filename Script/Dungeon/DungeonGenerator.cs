// DungeonGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Handles the procedural generation of map data for different levels (field and dungeon).
/// 다양한 레벨(필드 및 던전)에 대한 맵 데이터의 절차적 생성을 담당합니다.
/// </summary>
public class DungeonGenerator
{
    // --- Generation Settings (passed from GameManager or configured here) ---
    private int fieldMapWidth;
    private int fieldMapHeight;
    private float fieldNoiseScale;
    private float fieldWaterThreshold;
    private float fieldRockThreshold;
    private float fieldTreeDensity;

    private int dungeonMapWidth;
    private int dungeonMapHeight;
    private int maxRooms;
    private int minRoomSize;
    private int maxRoomSize;
    private int totalDungeonLevels;

    private float standardWallProbability;
    private float mossyWallProbability;
    private float brickWallProbability;
    private float roughWallProbability;

    private System.Random random; // Instance for random number generation

    public DungeonGenerator(
        int fieldMapWidth, int fieldMapHeight, float fieldNoiseScale, float fieldWaterThreshold, float fieldRockThreshold, float fieldTreeDensity,
        int dungeonMapWidth, int dungeonMapHeight, int maxRooms, int minRoomSize, int maxRoomSize, int totalDungeonLevels,
        float standardWallProb, float mossyWallProb, float brickWallProb, float roughWallProb)
    {
        this.fieldMapWidth = fieldMapWidth;
        this.fieldMapHeight = fieldMapHeight;
        this.fieldNoiseScale = fieldNoiseScale;
        this.fieldWaterThreshold = fieldWaterThreshold;
        this.fieldRockThreshold = fieldRockThreshold;
        this.fieldTreeDensity = fieldTreeDensity;

        this.dungeonMapWidth = dungeonMapWidth;
        this.dungeonMapHeight = dungeonMapHeight;
        this.maxRooms = maxRooms;
        this.minRoomSize = minRoomSize;
        this.maxRoomSize = maxRoomSize;
        this.totalDungeonLevels = totalDungeonLevels;

        this.standardWallProbability = standardWallProb;
        this.mossyWallProbability = mossyWallProb;
        this.brickWallProbability = brickWallProb;
        this.roughWallProbability = roughWallProb;

        this.random = new System.Random();
    }

    public MapData GenerateNewLevel(int levelNumber, int playerEntryX = -1, int playerEntryY = -1)
    {
        MapData newMap;
        Vector2Int playerStartPos;

        int width = (levelNumber == 0) ? fieldMapWidth : dungeonMapWidth;
        int height = (levelNumber == 0) ? fieldMapHeight : dungeonMapHeight;

        newMap = new MapData(width, height);

        if (levelNumber == 0)
        {
            Debug.Log($"Generating Field Map (Level {levelNumber}) [{width}x{height}]...");
            playerStartPos = GenerateFieldMap(newMap);
        }
        else
        {
            Debug.Log($"Generating Dungeon Level {levelNumber} / {totalDungeonLevels} [{width}x{height}]...");
            playerStartPos = GenerateDungeon(newMap, levelNumber, playerEntryX, playerEntryY);
        }

        newMap.PlayerSuggestedStartPosition = playerStartPos;

        // Post-generation validation for dungeon stairs (can be part of GenerateDungeon or here)
        // This validation is now more robustly handled within GenerateDungeon itself.
        // However, a final check here could be useful for debugging.
        if (levelNumber > 0) // Only for dungeons
        {
            if (levelNumber > 1 && newMap.UpStairsLocation == Vector2Int.one * -1)
            {
                // This case should ideally be handled by GenerateDungeon placing stairs at playerStartPos if no other valid spot.
                Debug.LogWarning($"Level {levelNumber} seems to be missing UpStairs after generation. Check GenerateDungeon logic.");
            }
            if (levelNumber < totalDungeonLevels && newMap.DownStairsLocation == Vector2Int.one * -1)
            {
                Debug.LogWarning($"Level {levelNumber} seems to be missing DownStairs after generation. Check GenerateDungeon logic.");
            }
        }
        return newMap;
    }

    private Vector2Int GenerateDungeon(MapData map, int currentLevel, int playerEntryX = -1, int playerEntryY = -1)
    {
        if (TileDatabase.Instance == null || TileDatabase.Instance.Wall == null || TileDatabase.Instance.Floor == null || TileDatabase.Instance.StairsUp == null || TileDatabase.Instance.StairsDown == null)
        { Debug.LogError($"[GenDungeon Lvl {currentLevel}] Core TileTypes missing! Aborting."); return map.GetFallbackStartPosition(currentLevel); }

        int mapWidth = map.Width;
        int mapHeight = map.Height;
        List<RectInt> rooms = new List<RectInt>();

        map.Fill(TileDatabase.Instance.Wall);
        Vector2Int playerStartPos = Vector2Int.zero; // Will be determined after room placement
        Vector2Int actualUpStairsFinalPos = Vector2Int.one * -1; // Track where up-stairs are actually placed

        for (int i = 0; i < maxRooms; i++)
        {
            int roomWidth = random.Next(minRoomSize, maxRoomSize + 1);
            int roomHeight = random.Next(minRoomSize, maxRoomSize + 1);
            int maxX = mapWidth - roomWidth - 1; int maxY = mapHeight - roomHeight - 1;
            if (maxX <= 1 || maxY <= 1) continue; // Ensure room + border fits
            int roomX = random.Next(1, maxX); int roomY = random.Next(1, maxY);
            RectInt newRoom = new RectInt(roomX, roomY, roomWidth, roomHeight);

            bool intersects = false;
            foreach (RectInt room in rooms)
            { if (newRoom.Intersects(new RectInt(room.xMin - 1, room.yMin - 1, (room.xMax - room.xMin) + 2, (room.yMax - room.yMin) + 2))) { intersects = true; break; } }
            if (intersects) continue;

            CarveRect(map, newRoom, TileDatabase.Instance.Floor);
            Vector2Int currentRoomCenter = newRoom.Center;

            if (rooms.Count > 0)
            {
                Vector2Int prevCenter = rooms[rooms.Count - 1].Center;
                if (random.Next(0, 2) == 0) { CarveHorizontalTunnel(map, prevCenter.x, currentRoomCenter.x, prevCenter.y); CarveVerticalTunnel(map, prevCenter.y, currentRoomCenter.y, currentRoomCenter.x); }
                else { CarveVerticalTunnel(map, prevCenter.y, currentRoomCenter.y, prevCenter.x); CarveHorizontalTunnel(map, prevCenter.x, currentRoomCenter.x, currentRoomCenter.y); }
            }
            rooms.Add(newRoom);
        }

        ApplyWallVariations(map);

        if (rooms.Count > 0)
        {
            RectInt firstRoom = rooms[0];
            playerStartPos = GetRandomWalkableTileInRoom(map, firstRoom);
            if (playerStartPos == Vector2Int.one * -1) { playerStartPos = map.GetFallbackStartPosition(currentLevel); }

            // --- Place Up Stairs ---
            if (currentLevel > 0) // Up stairs are relevant for levels > 0
            {
                Vector2Int upStairsPlacementAttemptPos = playerStartPos; // Default candidate position

                // If this is not level 1 and playerEntry coordinates are valid, use them
                if (currentLevel > 1 && playerEntryX != -1 && playerEntryY != -1 &&
                    map.IsInBounds(playerEntryX, playerEntryY) &&
                    map.Tiles[playerEntryY, playerEntryX]?.walkable == true)
                {
                    upStairsPlacementAttemptPos = new Vector2Int(playerEntryX, playerEntryY);
                }
                else // Otherwise (level 1 or invalid playerEntry), find a spot in the first room
                {
                    upStairsPlacementAttemptPos = GetRandomWalkableTileInRoom(map, firstRoom, playerStartPos); // Try to avoid playerStartPos
                    if (upStairsPlacementAttemptPos == Vector2Int.one * -1) // Fallback if no other spot
                    {
                        upStairsPlacementAttemptPos = playerStartPos;
                    }
                }

                if (TileDatabase.Instance.StairsUp != null)
                {
                    map.SetTile(upStairsPlacementAttemptPos.x, upStairsPlacementAttemptPos.y, TileDatabase.Instance.StairsUp);
                    map.ForceSetStairsLocation(true, upStairsPlacementAttemptPos.x, upStairsPlacementAttemptPos.y);
                    actualUpStairsFinalPos = upStairsPlacementAttemptPos; // Record the actual placement
                }
                else { Debug.LogError($"StairsUp TileType is null for Level {currentLevel}!"); }
            }

            // --- Place Down Stairs ---
            if (currentLevel < totalDungeonLevels) // Down stairs are relevant if not the last level
            {
                Vector2Int downStairsPos = Vector2Int.one * -1;
                List<Vector2Int> exclusionPoints = new List<Vector2Int>();
                exclusionPoints.Add(playerStartPos); // Always try to avoid player start position

                if (actualUpStairsFinalPos != Vector2Int.one * -1) // If up-stairs were placed, avoid that too
                {
                    exclusionPoints.Add(actualUpStairsFinalPos);
                }

                if (rooms.Count > 1) // If multiple rooms, try to place in a room other than the first
                {
                    for (int attempt = 0; attempt < rooms.Count * 2; attempt++) // Try a few times for randomness
                    {
                        int targetRoomIndex = random.Next(1, rooms.Count); // Pick a room (excluding the first one initially)
                        downStairsPos = GetRandomWalkableTileInRoom(map, rooms[targetRoomIndex], exclusionPoints.ToArray());
                        if (downStairsPos != Vector2Int.one * -1) break; // Found a spot
                    }
                    if (downStairsPos == Vector2Int.one * -1) // Fallback: If not found in other rooms, try the first room
                    {
                        downStairsPos = GetRandomWalkableTileInRoom(map, firstRoom, exclusionPoints.ToArray());
                    }
                }
                else // Only one room available
                {
                    downStairsPos = GetRandomWalkableTileInRoom(map, firstRoom, exclusionPoints.ToArray());
                }

                // Absolute fallback if still no suitable spot found (e.g., room is tiny and all spots excluded)
                if (downStairsPos == Vector2Int.one * -1)
                {
                    downStairsPos = playerStartPos; // This might overlap, but it's a last resort
                    Debug.LogWarning($"Down stairs for Level {currentLevel} fallback to playerStartPos. Possible overlap if up stairs/player start is the only walkable tile.");
                }

                if (TileDatabase.Instance.StairsDown != null)
                {
                    map.SetTile(downStairsPos.x, downStairsPos.y, TileDatabase.Instance.StairsDown);
                    map.ForceSetStairsLocation(false, downStairsPos.x, downStairsPos.y);
                }
                else { Debug.LogError($"StairsDown TileType is null for Level {currentLevel}!"); }
            }
        }
        else // No rooms generated
        {
            Debug.LogError($"[GenDungeon Lvl {currentLevel}] No rooms generated! Player will start at fallback.");
            playerStartPos = map.GetFallbackStartPosition(currentLevel);
            // Ensure no stairs are marked if no rooms were made
            map.ForceSetStairsLocation(true, -1, -1);
            map.ForceSetStairsLocation(false, -1, -1);
        }
        return playerStartPos;
    }

    private void ApplyWallVariations(MapData map)
    {
        if (TileDatabase.Instance == null || TileDatabase.Instance.Wall == null) return;

        bool variationsPossible = TileDatabase.Instance.WallMossy != null ||
                                  TileDatabase.Instance.WallBrick != null ||
                                  TileDatabase.Instance.WallRough != null;

        if (!variationsPossible && (mossyWallProbability > 0 || brickWallProbability > 0 || roughWallProbability > 0))
        {
            Debug.LogWarning("[DungeonGenerator] Wall variation probabilities set, but TileTypes missing. Variations disabled.");
        }
        if (!variationsPossible) return;


        float totalProb = standardWallProbability + mossyWallProbability + brickWallProbability + roughWallProbability;
        float normalizedStandard = standardWallProbability;
        float normalizedMossy = mossyWallProbability;
        float normalizedBrick = brickWallProbability;
        float normalizedRough = roughWallProbability;

        if (Mathf.Abs(totalProb - 1.0f) > 0.01f && totalProb > 0)
        {
            normalizedStandard /= totalProb;
            normalizedMossy /= totalProb;
            normalizedBrick /= totalProb;
            normalizedRough /= totalProb;
        }
        else if (totalProb <= 0)
        {
            normalizedStandard = 1.0f;
            normalizedMossy = 0;
            normalizedBrick = 0;
            normalizedRough = 0;
        }

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (map.Tiles[y, x] == TileDatabase.Instance.Wall)
                {
                    float roll = (float)random.NextDouble();
                    if (roll < normalizedStandard) { /* Keep standard wall */ }
                    else if (roll < normalizedStandard + normalizedMossy && TileDatabase.Instance.WallMossy != null) { map.Tiles[y, x] = TileDatabase.Instance.WallMossy; }
                    else if (roll < normalizedStandard + normalizedMossy + normalizedBrick && TileDatabase.Instance.WallBrick != null) { map.Tiles[y, x] = TileDatabase.Instance.WallBrick; }
                    else if (TileDatabase.Instance.WallRough != null) { map.Tiles[y, x] = TileDatabase.Instance.WallRough; } // Assumes this is the remainder
                }
            }
        }
    }

    private void CarveRect(MapData map, RectInt rect, TileType tileToSet)
    {
        if (tileToSet == null) { Debug.LogError("[CarveRect] Null TileType!"); return; }
        // Ensure carving happens within map boundaries, excluding the outermost border if desired for walls
        int startX = Mathf.Max(1, rect.xMin);
        int endX = Mathf.Min(map.Width - 1, rect.xMax);
        int startY = Mathf.Max(1, rect.yMin);
        int endY = Mathf.Min(map.Height - 1, rect.yMax);

        // The original code used rect.xMin+1, etc., which carves the *interior* of the rect.
        // If the rect itself is defined from border to border, this is correct.
        // Let's stick to the original intention of carving the room's floor tiles.
        startX = Mathf.Clamp(rect.xMin, 1, map.Width - 2); // Clamp to ensure at least 1 tile border
        endX = Mathf.Clamp(rect.xMax - 1, startX, map.Width - 2); // xMax is exclusive in RectInt, so xMax-1 is last tile
        startY = Mathf.Clamp(rect.yMin, 1, map.Height - 2);
        endY = Mathf.Clamp(rect.yMax - 1, startY, map.Height - 2);


        for (int y = startY; y <= endY; y++) // Iterate up to and including endY/endX
        {
            for (int x = startX; x <= endX; x++)
            {
                if (map.IsInBounds(x, y)) map.Tiles[y, x] = tileToSet;
            }
        }
    }

    private void CarveHorizontalTunnel(MapData map, int x1, int x2, int y)
    {
        if (TileDatabase.Instance?.Floor == null) return;
        if (!map.IsInBounds(0, y)) return;
        int startX = Mathf.Min(x1, x2);
        int endX = Mathf.Max(x1, x2);
        for (int x = startX; x <= endX; x++)
        {
            if (map.IsInBounds(x, y)) map.Tiles[y, x] = TileDatabase.Instance.Floor;
        }
    }

    private void CarveVerticalTunnel(MapData map, int y1, int y2, int x)
    {
        if (TileDatabase.Instance?.Floor == null) return;
        if (!map.IsInBounds(x, 0)) return;
        int startY = Mathf.Min(y1, y2);
        int endY = Mathf.Max(y1, y2);
        for (int y = startY; y <= endY; y++)
        {
            if (map.IsInBounds(x, y)) map.Tiles[y, x] = TileDatabase.Instance.Floor;
        }
    }

    private Vector2Int GenerateFieldMap(MapData map)
    {
        if (TileDatabase.Instance == null || TileDatabase.Instance.Grass == null || TileDatabase.Instance.Water == null || TileDatabase.Instance.Rock == null || TileDatabase.Instance.Tree == null)
        { Debug.LogError("[GenerateFieldMap] Core natural TileTypes missing! Aborting."); map.Fill(TileDatabase.Instance?.Floor ?? null); return map.GetFallbackStartPosition(0); }

        float offsetX = random.Next(-10000, 10000);
        float offsetY = random.Next(-10000, 10000);

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                float sampleX = (x / (float)map.Width) * fieldNoiseScale + offsetX;
                float sampleY = (y / (float)map.Height) * fieldNoiseScale + offsetY;
                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                if (noiseValue < fieldWaterThreshold) { map.Tiles[y, x] = TileDatabase.Instance.Water; }
                else if (noiseValue > fieldRockThreshold) { map.Tiles[y, x] = TileDatabase.Instance.Rock; }
                else { map.Tiles[y, x] = TileDatabase.Instance.Grass; }
            }
        }

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (map.Tiles[y, x] == TileDatabase.Instance.Grass && (float)random.NextDouble() < fieldTreeDensity)
                {
                    map.Tiles[y, x] = TileDatabase.Instance.Tree;
                }
            }
        }
        map.ForceSetStairsLocation(true, -1, -1);
        map.ForceSetStairsLocation(false, -1, -1);

        Vector2Int playerStartPos = FindRandomWalkableTile(map, TileDatabase.Instance.Grass);
        if (playerStartPos == Vector2Int.one * -1)
        {
            Debug.LogWarning("[GenerateFieldMap] Could not find Grass tile for start! Using fallback.");
            playerStartPos = map.GetFallbackStartPosition(0);
        }
        return playerStartPos;
    }

    private Vector2Int FindRandomWalkableTile(MapData map, TileType targetTile, int maxAttempts = 100)
    {
        if (targetTile == null || !targetTile.walkable) return Vector2Int.one * -1;
        List<Vector2Int> possibleLocations = new List<Vector2Int>();
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (map.Tiles[y, x] == targetTile) { possibleLocations.Add(new Vector2Int(x, y)); }
            }
        }
        if (possibleLocations.Count > 0) { return possibleLocations[random.Next(possibleLocations.Count)]; }

        for (int i = 0; i < maxAttempts; i++)
        {
            int randX = random.Next(0, map.Width);
            int randY = random.Next(0, map.Height);
            if (map.Tiles[randY, randX] == targetTile) return new Vector2Int(randX, randY);
        }
        Debug.LogWarning($"Could not find any tile of type {targetTile.name} after {maxAttempts} attempts.");
        return Vector2Int.one * -1;
    }

    /// <summary>
    /// Gets a random walkable tile within the specified room, avoiding specified exclusion points.
    /// 지정된 방 내에서 지정된 제외 지점을 피하여 무작위로 걸을 수 있는 타일을 가져옵니다.
    /// </summary>
    /// <param name="map">The map data.</param>
    /// <param name="room">The room to search within.</param>
    /// <param name="excludePositions">An array of positions to exclude. Can be null or empty.</param>
    /// <returns>A walkable tile position, or Vector2Int.one * -1 if none found.</returns>
    private Vector2Int GetRandomWalkableTileInRoom(MapData map, RectInt room, params Vector2Int[] excludePositions)
    {
        List<Vector2Int> walkableTiles = new List<Vector2Int>();
        // Room interior: xMin to xMax-1, yMin to yMax-1
        int startX = Mathf.Max(1, room.xMin);
        int endX = Mathf.Min(map.Width - 2, room.xMax - 1); // xMax is exclusive
        int startY = Mathf.Max(1, room.yMin);
        int endY = Mathf.Min(map.Height - 2, room.yMax - 1); // yMax is exclusive

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                bool isExcluded = false;
                if (excludePositions != null)
                {
                    foreach (Vector2Int excludePos in excludePositions)
                    {
                        if (currentPos == excludePos)
                        {
                            isExcluded = true;
                            break;
                        }
                    }
                }

                if (!isExcluded && map.IsInBounds(x, y) && map.Tiles[y, x]?.walkable == true)
                {
                    walkableTiles.Add(currentPos);
                }
            }
        }
        if (walkableTiles.Count > 0) { return walkableTiles[random.Next(walkableTiles.Count)]; }
        return Vector2Int.one * -1;
    }

    // GetRoomsForMap was a problematic placeholder and has been removed.
    // The logic for needing rooms (like stair placement) should be self-contained within GenerateDungeon
    // or rooms should be a property of MapData if they need to be accessed externally post-generation.
}
