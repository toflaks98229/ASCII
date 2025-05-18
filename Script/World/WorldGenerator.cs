using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class WorldGenerator : MonoBehaviour
{
    // --- 인스펙터 변수들 ---
    [Header("World Dimensions")]
    [Tooltip("월드맵 너비 (타일 단위)")]
    [SerializeField] private int worldWidth = 256;
    [Tooltip("월드맵 높이 (타일 단위)")]
    [SerializeField] private int worldHeight = 256;

    [Header("Generation Parameters - General")]
    [Tooltip("난수 생성 시드. 0이면 무작위 시드를 사용합니다.")]
    [SerializeField] private int seed = 0;
    [Tooltip("상세 맵 간략화 비율 (예: 4는 4x4 블록이 1개의 간략화 타일이 됨)")]
    [SerializeField] private int simplificationFactor = 4;

    [Header("Generation Parameters - Elevation (Step 1)")]
    [Tooltip("고도 생성을 위한 펄린 노이즈 스케일.")]
    [SerializeField] private float elevationNoiseScale = 50f;
    [Tooltip("고도 노이즈 레이어(옥타브) 수.")]
    [SerializeField] private int elevationOctaves = 7;
    [Tooltip("고도 노이즈 지속성 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float elevationPersistence = 0.5f;
    [Tooltip("고도 노이즈 라쿠나리티 (> 1).")]
    [SerializeField] private float elevationLacunarity = 2.0f;
    [Tooltip("맵 가장자리 폴오프 사용 여부")]
    [SerializeField] private bool useEdgeFalloff = true;
    [Tooltip("폴오프 강도.")]
    [SerializeField] private float falloffPower = 3.0f;
    [Tooltip("폴오프 시작 지점 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float falloffStart = 0.8f;
    [Tooltip("고도 기준 깊은 물 임계값 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float deepWaterThreshold = 0.2f;
    [Tooltip("고도 기준 얕은 물 임계값 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float shallowWaterThreshold = 0.3f;
    [Tooltip("고도 기준 해변 임계값 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float beachThreshold = 0.35f;

    [Header("Generation Parameters - Puddle Removal (Step 1.5)")]
    [Tooltip("작은 고립된 물웅덩이 제거 활성화 여부?")]
    [SerializeField] private bool removeSmallPuddles = true;
    [Tooltip("내륙 수역이 유지되기 위한 최소 크기(타일 수).")]
    [SerializeField] private int minLakeSize = 10;

    [Header("Generation Parameters - Climate (Step 2)")]
    [Tooltip("기온 노이즈 스케일.")]
    [SerializeField] private float temperatureNoiseScale = 60f;
    [Tooltip("기본 기온 (0=추움, 1=더움).")]
    [SerializeField][Range(0f, 1f)] private float baseTemperature = 0.5f;
    [Tooltip("위도 기온 영향 계수 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float latitudeTempFactor = 0.9f;
    [Tooltip("고도 기온 영향 계수 (0-1).")]
    [SerializeField][Range(0f, 1f)] private float elevationTempFactor = 0.7f;
    [Tooltip("강우량 노이즈 스케일.")]
    [SerializeField] private float rainfallNoiseScale = 55f;
    [Tooltip("기본 강우량 (0=건조, 1=습함).")]
    [SerializeField][Range(0f, 1f)] private float baseRainfall = 0.5f;
    [Tooltip("위도 강우량 영향 지수.")]
    [SerializeField] private float latitudeRainfallExponent = 2.0f;
    [Tooltip("고도 강우량 영향 계수 (0-0.5).")]
    [SerializeField][Range(0f, 0.5f)] private float elevationRainfallFactor = 0.15f;

    // --- 강 생성 (유량 집적 방식) 파라미터 ---
    [Header("Generation Parameters - Rivers (Step 4 - Flow Accumulation)")]
    [Tooltip("강 형성을 위한 유량 집적 임계값 (최대 가능 유량 대비 비율). 높을수록 적고 큰 강.")]
    [Range(0.001f, 0.1f)] // 제어 용이성을 위해 범위 조정
    [SerializeField] private float riverFlowThreshold = 0.01f; // 유량 집적 임계값 복원
    [Tooltip("강 경로를 따라 고도를 낮출지 여부 (강바닥 침식)?")]
    [SerializeField] private bool carveRiverBed = true; // 유량 집적 방식용 침식 옵션 복원
    [Tooltip("강바닥 팔 경우 타일당 기본 고도 감소량.")]
    [SerializeField] private float baseRiverCarveDepth = 0.002f; // 유량 집적 방식용 침식 깊이 복원
    [Tooltip("유량에 따른 추가 침식 깊이 배율.")]
    [SerializeField] private float flowCarveMultiplier = 0.05f; // 유량 집적 방식용 침식 배율 복원
    [Tooltip("바다(맵 가장자리 물)나 큰 호수에 도달하지 못하는 내륙 강을 제거할지 여부?")]
    [SerializeField] private bool pruneInlandRivers = true; // 내륙 강 제거 옵션 복원

    // --- 강둑 침식 파라미터 ---
    [Header("Generation Parameters - River Banks (Step 4.5 - Optional)")]
    [Tooltip("생성된 강 주변의 둑(육지 타일)을 추가로 침식시킬지 여부")]
    [SerializeField] private bool enableBankErosion = true;
    [Tooltip("강둑 침식 반경 (강 타일로부터의 거리).")]
    [SerializeField] private int bankErosionRadius = 1;
    [Tooltip("강둑 기본 침식 깊이")]
    [SerializeField] private float baseBankErosionDepth = 0.005f;
    // [Tooltip("강 유량에 따른 추가 강둑 침식 깊이 배율")] // 유량 집적 방식에서는 flowAccumulationMap을 사용 가능
    [SerializeField] private float flowBankErosionMultiplier = 0.02f; // 유량 기반 강둑 침식 배율 복원

    [Header("Generation Parameters - POIs (Step 5)")]
    [Tooltip("맵에 배치하려고 시도할 목표 도시 수.")]
    [SerializeField] private int numberOfCities = 7;
    [Tooltip("맵에 배치하려고 시도할 목표 던전 입구 수.")]
    [SerializeField] private int numberOfDungeons = 12;
    [Tooltip("POI 당 배치 시도 횟수 배율.")]
    [SerializeField] private int poiPlacementAttemptsMultiplier = 100;
    [Tooltip("배치된 주요 지점(POI) 사이에 필요한 최소 거리.")]
    [SerializeField] private int minDistanceBetweenPOIs = 5;

    [Header("Generation Parameters - Roads (Step 6)")]
    [Tooltip("도시 사이에 길을 생성할지 여부?")]
    [SerializeField] private bool generateRoads = true;
    [Tooltip("A* 경로 탐색 플레이스홀더가 확인할 수 있는 최대 노드 수.")]
    [SerializeField] private int roadMaxSearchNodes = 50000;
    [Tooltip("도로 생성 시 가파른 경사 이동 비용 페널티 배율.")]
    [SerializeField] private float roadSlopePenaltyMultiplier = 4.0f;

    [Header("Visualization")]
    [Tooltip("상세 맵을 위한 WorldMapTextMeshVisualizer 컴포넌트를 할당하세요.")]
    [SerializeField] private WorldMapTextMeshVisualizer detailedMapVisualizer;
    [Tooltip("간략화된 맵을 위한 SimplifiedWorldMapVisualizer 컴포넌트를 할당하세요.")]
    [SerializeField] private SimplifiedWorldMapVisualizer simplifiedMapVisualizer;

    // --- 공개 속성 ---
    public WorldData GeneratedWorldData { get; private set; }
    public SimplifiedWorldData GeneratedSimplifiedWorldData { get; private set; }

    // --- 내부 변수 ---
    private System.Random worldRandom;
    private bool[,] isValidWaterEndpoint; // 유효 하구 맵 (내륙 강 제거용)

    // --- 유량 집적 관련 데이터 ---
    private Vector2Int[,] flowDirectionMap; // 흐름 방향 맵 (D4 또는 D8)
    private float[,] flowAccumulationMap; // 누적 유량 맵

    // --- Awake, GenerateWorld 등 기본 구조 ---
    void Awake()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        Debug.Log($"--- Starting World Generation (Flow Accumulation, Seed: {(seed == 0 ? "Random" : seed.ToString())}) ---");
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (seed == 0) { seed = UnityEngine.Random.Range(0, 1213321); }
        worldRandom = new System.Random(seed);
        UnityEngine.Random.InitState(seed);

        GeneratedWorldData = new WorldData(worldWidth, worldHeight);

        // --- 생성 단계 ---
        GenerateElevation(GeneratedWorldData);
        if (removeSmallPuddles) { RemoveSmallPuddles(GeneratedWorldData); }
        SmoothElevationMap(GeneratedWorldData, 1, 4);
        GenerateTemperature(GeneratedWorldData);
        GenerateRainfall(GeneratedWorldData);
        DetermineBiomes(GeneratedWorldData);

        // --- 강 생성 (유량 집적 방식) ---
        GenerateRivers_FlowAccumulation(GeneratedWorldData); // 유량 집적 방식 강 생성 함수 호출

        if (pruneInlandRivers) // 내륙 강 제거는 유량 집적 후, 강둑 침식 전에 수행
        {
            IdentifyValidWaterBodies(GeneratedWorldData);   // 유효 하구 식별
            ValidateAndPruneRivers(GeneratedWorldData);     // 유효 하구 미도달 강 제거
        }
        if (enableBankErosion) // 강둑 침식은 강 정의 및 제거 후 수행
        {
            ErodeRiverBanks(GeneratedWorldData);
        }
        // --- 강 생성 종료 ---

        PlacePOIs(GeneratedWorldData, numberOfCities, numberOfDungeons);
        if (generateRoads) { GenerateRoads(GeneratedWorldData); }
        GeneratedSimplifiedWorldData = GenerateSimplifiedWorld(GeneratedWorldData, simplificationFactor);

        stopwatch.Stop();
        Debug.Log($"--- World Generation Complete ({stopwatch.ElapsedMilliseconds} ms) ---");

        if (detailedMapVisualizer != null) { detailedMapVisualizer.Visualize(GeneratedWorldData); }
        if (simplifiedMapVisualizer != null) { simplifiedMapVisualizer.Visualize(GeneratedSimplifiedWorldData); }
    }

    // --- 고도, 기후, Biome, POI, 도로, 간략화 관련 함수들 ---
    /// <summary>
    /// 펄린 노이즈와 옥타브를 사용하여 월드의 고도 맵을 생성합니다.
    /// 선택적으로 가장자리 폴오프(falloff)를 적용하여 섬 형태를 만듭니다.
    /// </summary>
    /// <param name="world">고도를 생성하고 저장할 WorldData 객체</param>
    private void GenerateElevation(WorldData world)
    {
        Debug.Log("Step 1: Generating Elevation..."); // 단계 시작 로그
        float[,] noiseMap = new float[world.Width, world.Height]; // 각 타일의 원시 노이즈 값을 저장할 배열
        float maxNoiseHeight = float.MinValue; // 노이즈 값 정규화를 위한 최대값 추적
        float minNoiseHeight = float.MaxValue; // 노이즈 값 정규화를 위한 최소값 추적
        // 노이즈 오프셋: 매번 다른 지형을 생성하기 위해 랜덤 오프셋 사용
        float offsetX = UnityEngine.Random.Range(-10000f, 10000f); // Unity 랜덤 사용
        float offsetY = UnityEngine.Random.Range(-10000f, 10000f); // Unity 랜덤 사용

        // 모든 타일(x, y)에 대해 노이즈 계산
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                float amplitude = 1f; // 현재 옥타브의 진폭 (영향력)
                float frequency = 1f; // 현재 옥타브의 주파수 (세밀함)
                float noiseHeight = 0f; // 현재 타일의 누적 노이즈 값
                float normalizationFactor = 0f; // 노이즈 값을 0-1 범위로 정규화하기 위한 총 진폭 합

                // 여러 옥타브(레이어)의 노이즈를 중첩하여 디테일 추가
                for (int i = 0; i < elevationOctaves; i++)
                {
                    // 펄린 노이즈 샘플링 좌표 계산
                    // (맵 중심 기준 좌표, 스케일, 주파수, 오프셋 적용)
                    float sampleX = (x - world.Width / 2f) / elevationNoiseScale * frequency + offsetX;
                    float sampleY = (y - world.Height / 2f) / elevationNoiseScale * frequency + offsetY;

                    // Unity의 Mathf.PerlinNoise 함수를 사용하여 노이즈 값 얻기 (0~1 범위)
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    // 현재 옥타브의 진폭을 곱하여 누적 노이즈 값에 더함
                    noiseHeight += perlinValue * amplitude;
                    // 정규화 계수에 현재 진폭을 더함
                    normalizationFactor += amplitude;

                    // 다음 옥타브를 위한 진폭 및 주파수 업데이트
                    amplitude *= elevationPersistence; // 진폭 감소 (persistence 값 적용)
                    frequency *= elevationLacunarity; // 주파수 증가 (lacunarity 값 적용)
                }

                // 누적된 노이즈 값을 총 진폭 합으로 나누어 정규화 (대략 0~1 범위)
                noiseMap[x, y] = (normalizationFactor > 0) ? noiseHeight / normalizationFactor : 0f;

                // 전체 노이즈 값 중 최대/최소 값 업데이트 (최종 정규화용)
                if (noiseMap[x, y] > maxNoiseHeight) maxNoiseHeight = noiseMap[x, y];
                if (noiseMap[x, y] < minNoiseHeight) minNoiseHeight = noiseMap[x, y];
            }
        }

        // 계산된 노이즈 맵을 월드 타일에 적용
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                // 노이즈 값을 min/max를 사용하여 0~1 범위로 정확하게 정규화
                float normalizedHeight = (maxNoiseHeight > minNoiseHeight) ? Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]) : 0f;

                // 가장자리 폴오프(섬 형태 만들기) 적용 옵션이 켜져 있으면
                if (useEdgeFalloff)
                {
                    // 폴오프 계수를 계산하여 곱함 (가장자리에 가까울수록 값이 작아짐)
                    normalizedHeight *= CalculateEdgeFalloff(x, y, world.Width, world.Height, falloffPower, falloffStart);
                }

                // 최종 계산된 고도 값을 WorldTile 객체에 저장 (0~1 범위로 제한)
                world.WorldTiles[y, x].Elevation = Mathf.Clamp01(normalizedHeight);
                // 타일의 다른 속성들 초기화
                world.WorldTiles[y, x].Temperature = 0.5f; // 기본 기온
                world.WorldTiles[y, x].Rainfall = 0.5f; // 기본 강우량
                world.WorldTiles[y, x].IsRiver = false; // 강 아님
                world.WorldTiles[y, x].IsRoad = false; // 도로 아님
                world.WorldTiles[y, x].HasCity = false; // 도시 없음
                world.WorldTiles[y, x].HasTown = false; // 마을 없음 (현재 미사용)
                world.WorldTiles[y, x].HasDungeon = false; // 던전 없음
                // Biome은 이후 단계에서 결정됨
            }
        }
        Debug.Log("Step 1: Elevation Generated."); // 단계 완료 로그
    }

    /// <summary>
    /// 맵 가장자리로부터의 거리에 따라 고도를 낮추는 폴오프(falloff) 계수(0~1)를 계산합니다.
    /// 값이 1이면 영향이 없고, 0에 가까울수록 고도가 많이 낮아집니다.
    /// </summary>
    /// <param name="x">타일 X 좌표</param>
    /// <param name="y">타일 Y 좌표</param>
    /// <param name="width">맵 너비</param>
    /// <param name="height">맵 높이</param>
    /// <param name="power">폴오프 곡선의 강도 (높을수록 가장자리에서 급격히 떨어짐)</param>
    /// <param name="start">폴오프 효과 시작 지점 (0~1, 1에 가까울수록 가장자리에만 영향)</param>
    /// <returns>폴오프 계수 (0~1)</returns>
    private float CalculateEdgeFalloff(int x, int y, int width, int height, float power, float start)
    {
        // 좌표를 -1 ~ 1 범위로 정규화 (맵 중심이 0,0)
        float nx = x / (float)width * 2 - 1;
        float ny = y / (float)height * 2 - 1;

        // 중심으로부터 가장 먼 축까지의 거리 (정사각형 형태의 거리)
        float val = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny)); // 0 ~ 1 범위

        // 폴오프 계산: start 지점부터 가장자리(1)까지 값이 0에서 1로 증가하도록 조정
        // val 값이 (1-start) 보다 작으면 0 (폴오프 영향 없음)
        // val 값이 1일 때 falloffValue가 1이 되도록 start로 나눔
        float denominator = Mathf.Max(0.0001f, start); // 0으로 나누기 방지
        float falloffValue = Mathf.Max(0, val - (1f - start)) / denominator; // 0 ~ 1 범위

        // power를 적용하여 곡선 형태 조절 후, 1에서 빼서 최종 계수 계산
        // (가장자리에서 0, 안쪽으로 갈수록 1에 가까워짐)
        return 1f - Mathf.Pow(falloffValue, power);
    }

    #region Step 1.5: Remove Small Puddles (NEW) - 작은 웅덩이 제거

    /// <summary>
    /// 작은 고립된 수역(웅덩이/작은 호수)을 식별하고 제거(육지로 만듦)합니다.
    /// Flood Fill 알고리즘을 사용하여 연결된 물 타일 그룹을 찾습니다.
    /// 각 물 그룹이 바다(맵 가장자리)와 연결되지 않았고, 크기가 minLakeSize보다 작으면 해당 그룹의 타일 고도를 높여 육지로 만듭니다.
    /// </summary>
    /// <param name="world">수정할 WorldData 객체</param>
    private void RemoveSmallPuddles(WorldData world)
    {
        Debug.Log("Step 1.5: Removing small puddles..."); // 단계 시작 로그
        bool[,] visited = new bool[world.Height, world.Width]; // Flood Fill 중 방문 여부를 체크하기 위한 배열
        int puddlesRemovedCount = 0; // 제거된 웅덩이 수 카운트

        // 모든 타일 순회
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                // 아직 방문하지 않았고, 물 타일(얕은 물 기준)인 경우 Flood Fill 시작
                if (!visited[y, x] && world.WorldTiles[y, x].Elevation < shallowWaterThreshold)
                {
                    List<Vector2Int> currentBody = new List<Vector2Int>(); // 현재 탐색 중인 물 그룹에 속한 타일 목록
                    Queue<Vector2Int> queue = new Queue<Vector2Int>(); // Flood Fill을 위한 큐
                    bool isOceanConnected = false; // 현재 물 그룹이 바다(맵 가장자리)와 연결되었는지 여부

                    // Flood Fill 시작점 설정
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[y, x] = true; // 방문 처리

                    // 큐가 빌 때까지 반복 (연결된 모든 물 타일 탐색)
                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue(); // 큐에서 타일 하나 꺼냄
                        currentBody.Add(current); // 현재 물 그룹 목록에 추가

                        // 현재 타일이 맵 가장자리에 있는지 확인
                        if (current.x == 0 || current.x == world.Width - 1 || current.y == 0 || current.y == world.Height - 1)
                        {
                            isOceanConnected = true; // 바다와 연결됨 플래그 설정
                        }

                        // 상하좌우 인접 타일 확인
                        foreach (Vector2Int offset in GetNeighborOffsets())
                        {
                            int nx = current.x + offset.x;
                            int ny = current.y + offset.y;

                            // 인접 타일이 맵 범위 내에 있고, 아직 방문하지 않았으며, 물 타일인 경우
                            if (world.IsInBounds(nx, ny) && !visited[ny, nx] && world.WorldTiles[ny, nx].Elevation < shallowWaterThreshold)
                            {
                                visited[ny, nx] = true; // 방문 처리
                                queue.Enqueue(new Vector2Int(nx, ny)); // 큐에 추가하여 계속 탐색
                            }
                        }
                    } // Flood Fill 종료 (현재 물 그룹 탐색 완료)

                    // 탐색 결과, 바다와 연결되지 않았고 크기가 minLakeSize보다 작으면
                    if (!isOceanConnected && currentBody.Count < minLakeSize)
                    {
                        puddlesRemovedCount++; // 제거된 웅덩이 수 증가
                        // 웅덩이 제거: 해당 물 그룹에 속한 모든 타일의 고도를 물 수위(얕은 물 임계값) 바로 위로 올림
                        foreach (Vector2Int puddleTile in currentBody)
                        {
                            // 완벽하게 평평한 지역을 피하기 위해 작은 랜덤 값 추가
                            world.WorldTiles[puddleTile.y, puddleTile.x].Elevation = shallowWaterThreshold + (float)worldRandom.NextDouble() * 0.01f;
                            // Biome도 다시 결정해야 할 수 있으나, 여기서는 고도만 수정 (Biome 단계는 이후에 실행됨)
                        }
                    }
                }
            }
        }
        Debug.Log($"Step 1.5: Removed {puddlesRemovedCount} small puddles (smaller than {minLakeSize} tiles)."); // 단계 완료 로그
    }

    /// <summary>
    /// 고도 맵에 박스 블러(Box Blur) 스무딩을 적용하여 지형을 부드럽게 만듭니다.
    /// 각 타일의 고도를 주변 타일(커널 크기 내)의 평균 고도로 설정합니다.
    /// </summary>
    /// <param name="world">월드 데이터 객체</param>
    /// <param name="passes">스무딩 적용 횟수 (높을수록 더 부드러워짐)</param>
    /// <param name="kernelSize">스무딩 커널 크기 (홀수 권장, 예: 3이면 3x3 주변 평균)</param>
    private void SmoothElevationMap(WorldData world, int passes = 1, int kernelSize = 3)
    {
        // 유효하지 않은 파라미터 검사
        if (passes <= 0 || kernelSize < 3 || kernelSize % 2 == 0)
        {
            Debug.LogWarning("Invalid parameters for SmoothElevationMap. Skipping smoothing.");
            return;
        }

        Debug.Log($"Step 1.6: Smoothing Elevation Map ({passes} pass(es), {kernelSize}x{kernelSize} kernel)..."); // 단계 시작 로그

        int width = world.Width;
        int height = world.Height;
        // 커널 반경 계산 (예: kernelSize가 3이면 radius는 1)
        int radius = kernelSize / 2;

        // 임시 고도 맵 생성: 스무딩 계산 중 원본 값을 읽어야 하므로 필요
        float[,] tempElevationMap = new float[height, width];

        // 지정된 횟수(passes)만큼 스무딩 반복
        for (int p = 0; p < passes; p++)
        {
            // --- 정석적인 박스 블러 방식 (별도 임시 맵 사용) ---
            // 1. 현재 월드의 고도 데이터를 임시 맵(tempElevationMap)에 복사 (읽기용)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tempElevationMap[y, x] = world.WorldTiles[y, x].Elevation;
                }
            }

            // 2. 스무딩 결과를 저장할 새 임시 맵 생성 (쓰기용)
            float[,] smoothedElevation = new float[height, width];

            // 3. 모든 타일에 대해 스무딩 계산 (읽기는 tempElevationMap, 쓰기는 smoothedElevation)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sumElevation = 0f; // 주변 고도 합계
                    int neighborCount = 0; // 유효한 주변 타일 수

                    // 커널 범위 내의 이웃 타일 순회
                    for (int j = -radius; j <= radius; j++)
                    {
                        for (int i = -radius; i <= radius; i++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            // 맵 범위 내에 있는지 확인
                            if (world.IsInBounds(currentX, currentY))
                            {
                                // 읽기용 임시 맵에서 고도 값을 읽어와 합산
                                sumElevation += tempElevationMap[currentY, currentX];
                                neighborCount++; // 유효 이웃 수 증가
                            }
                        }
                    }

                    // 평균 계산 및 쓰기용 임시 맵에 저장
                    if (neighborCount > 0)
                    {
                        smoothedElevation[y, x] = sumElevation / neighborCount;
                    }
                    else // 이웃이 없는 경우 (이론상 발생 어려움)
                    {
                        smoothedElevation[y, x] = tempElevationMap[y, x]; // 원본 값 유지
                    }
                }
            }

            // 4. 계산 완료 후 실제 월드 데이터 업데이트
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 물 타일은 스무딩하지 않도록 조건 추가 가능 (선택 사항)
                    // if (world.WorldTiles[y, x].IsLand(beachThreshold))
                    // {
                    world.WorldTiles[y, x].Elevation = smoothedElevation[y, x];
                    // }
                }
            }
            // --- 정석적인 방식 종료 ---

        } // passes 루프 종료
        Debug.Log("Step 1.6: Elevation Map Smoothed."); // 단계 완료 로그
    }


    /// <summary>
    /// Flood Fill, 강둑 침식 등에 사용될 상하좌우 인접 타일 오프셋 배열을 반환합니다.
    /// </summary>
    /// <returns>Vector2Int 오프셋 배열 (상, 하, 좌, 우)</returns>
    private Vector2Int[] GetNeighborOffsets()
    {
        // 필요시 대각선 오프셋 (new Vector2Int(1, 1) 등) 추가 가능
        return new Vector2Int[] {
            new Vector2Int(0, 1),  // 위 (y+1)
            new Vector2Int(0, -1), // 아래 (y-1)
            new Vector2Int(1, 0),  // 오른쪽 (x+1)
            new Vector2Int(-1, 0)  // 왼쪽 (x-1)
        };
    }
    #endregion // 작은 웅덩이 제거 섹션 종료

    /// <summary>
    /// 월드의 기온 맵을 생성합니다.
    /// 기본 기온(baseTemperature)에 펄린 노이즈, 위도 효과(극지방 추움, 적도 더움), 고도 효과(높을수록 추움)를 적용합니다.
    /// </summary>
    /// <param name="world">기온을 생성하고 저장할 WorldData 객체</param>
    private void GenerateTemperature(WorldData world)
    {
        Debug.Log("Step 2a: Generating Temperature..."); // 단계 시작 로그
        // 기온 변화를 위한 별도의 노이즈 맵 생성 (GenerateNoiseMap 함수 사용)
        // 다른 노이즈와 겹치지 않도록 별도의 시드 오프셋 사용 (worldRandom.Next())
        float[,] noiseMap = GenerateNoiseMap(world.Width, world.Height, temperatureNoiseScale, 4, 0.5f, 2.0f, worldRandom.Next());

        // 각 타일에 대해 기온 계산
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                // 1. 위도 계산 (맵 중앙이 적도(1), 양 끝이 극지방(0))
                float latitude = 1f - Mathf.Abs(y - world.Height / 2f) / (world.Height / 2f); // 0 ~ 1 범위
                // 2. 위도 효과 계산 (latitudeTempFactor 적용, 적도에 가까울수록 1에 가까움)
                // 지수(1.5f)를 사용하여 극지방/적도 효과 강조
                float latitudeEffect = Mathf.Pow(latitude, 1.5f) * latitudeTempFactor; // 0 ~ latitudeTempFactor 범위

                // 3. 고도 값 가져오기 (0~1 범위)
                float elevation = world.WorldTiles[y, x].Elevation;

                // 4. 노이즈 값 가져오기 (0~1 범위)
                float noiseValue = noiseMap[x, y];

                // 5. 최종 기온 계산 시작 (기본 기온에서 출발)
                float temperature = baseTemperature;

                // 6. 위도 효과 적용:
                // 적도 근처(latitudeEffect 큼): 기본 기온보다 높은 쪽으로 이동 (최대 1까지)
                temperature += (latitudeEffect * (1f - baseTemperature));
                // 극지방 근처(latitudeEffect 작음): 기본 기온보다 낮은 쪽으로 이동 (최소 0까지)
                temperature -= ((latitudeTempFactor - latitudeEffect) * baseTemperature); // 위도 효과 최대치와의 차이만큼 감소

                // 7. 노이즈 적용 (기온에 무작위 변화 추가)
                // (noiseValue - 0.5f)는 -0.5 ~ 0.5 범위, * 0.2f 하여 -0.1 ~ +0.1 범위 변화
                temperature += (noiseValue - 0.5f) * 0.2f;

                // 8. 고도 효과 적용: 고도가 높을수록 기온 감소
                // elevation * elevationTempFactor 만큼 기온 감소
                temperature -= elevation * elevationTempFactor;

                // 9. 최종 기온 값을 0~1 범위로 제한하여 저장
                world.WorldTiles[y, x].Temperature = Mathf.Clamp01(temperature);
            }
        }
        Debug.Log("Step 2a: Temperature Generated."); // 단계 완료 로그
    }

    /// <summary>
    /// 월드의 강우량 맵을 생성합니다.
    /// 기본 강우량(baseRainfall)에 펄린 노이즈, 위도 효과(적도 습윤대), 고도 효과(높을수록 약간 습함)를 적용합니다.
    /// </summary>
    /// <param name="world">강우량을 생성하고 저장할 WorldData 객체</param>
    private void GenerateRainfall(WorldData world)
    {
        Debug.Log("Step 2b: Generating Rainfall..."); // 단계 시작 로그
        // 강우량 변화를 위한 별도의 노이즈 맵 생성
        float[,] noiseMap = GenerateNoiseMap(world.Width, world.Height, rainfallNoiseScale, 4, 0.5f, 2.0f, worldRandom.Next());

        // 각 타일에 대해 강우량 계산
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                // 1. 위도 계산 (적도=1, 극=0)
                float latitudeFactor = 1f - Mathf.Abs(y - world.Height / 2f) / (world.Height / 2f); // 0 ~ 1 범위
                // 2. 위도 효과 계산 (적도 부근에서 강우량 증가)
                // 지수(latitudeRainfallExponent)를 사용하여 적도 부근에 효과 집중
                float latitudeEffect = Mathf.Pow(latitudeFactor, latitudeRainfallExponent); // 0 ~ 1 범위

                // 3. 고도 값 가져오기 (0~1 범위)
                float elevation = world.WorldTiles[y, x].Elevation;
                // 4. 고도 효과 계산 (단순: 고도가 높을수록 약간 습해짐)
                float elevationEffect = elevation * elevationRainfallFactor; // 0 ~ elevationRainfallFactor 범위

                // 5. 노이즈 값 가져오기 (0~1 범위)
                float noiseValue = noiseMap[x, y];

                // 6. 최종 강우량 계산 시작 (기본 강우량에서 출발)
                float rainfall = baseRainfall;

                // 7. 위도 효과 적용: 적도(latitudeEffect 큼)는 습해짐
                // 기본 강우량보다 높은 쪽으로 이동 (최대 1까지)
                rainfall += latitudeEffect * (1f - baseRainfall);

                // 8. 노이즈 적용 (강우량에 무작위 변화 추가)
                // -0.15 ~ +0.15 범위 변화
                rainfall += (noiseValue - 0.5f) * 0.3f;

                // 9. 고도 효과 적용 (높을수록 약간 증가)
                rainfall += elevationEffect;

                // 10. 최종 강우량 값을 0~1 범위로 제한하여 저장
                world.WorldTiles[y, x].Rainfall = Mathf.Clamp01(rainfall);
            }
        }
        Debug.Log("Step 2b: Rainfall Generated."); // 단계 완료 로그
    }

    /// <summary>
    /// 지정된 파라미터로 펄린 노이즈 맵을 생성하고 0-1 범위로 정규화하여 반환하는 헬퍼 함수입니다.
    /// GenerateElevation, GenerateTemperature, GenerateRainfall 등에서 사용됩니다.
    /// </summary>
    /// <param name="mapWidth">맵 너비</param>
    /// <param name="mapHeight">맵 높이</param>
    /// <param name="scale">노이즈 스케일 (클수록 큰 특징)</param>
    /// <param name="octaves">옥타브 수 (많을수록 디테일 증가)</param>
    /// <param name="persistence">지속성 (다음 옥타브 진폭 감소율, 0~1)</param>
    /// <param name="lacunarity">라쿠나리티 (다음 옥타브 주파수 증가율, >1)</param>
    /// <param name="seedOffset">시드 오프셋 (다른 종류의 노이즈를 생성하기 위해 전역 시드에 더함)</param>
    /// <returns>정규화된 0-1 범위의 노이즈 맵 (float[,])</returns>
    private float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int octaves, float persistence, float lacunarity, int seedOffset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight]; // 노이즈 값 저장 배열
        float maxNoiseHeight = float.MinValue; // 최대 노이즈 값 (정규화용)
        float minNoiseHeight = float.MaxValue; // 최소 노이즈 값 (정규화용)

        // 시드 오프셋을 적용한 별도의 난수 생성기 사용 (오프셋 계산용)
        System.Random prng = new System.Random(this.seed + seedOffset);
        float offsetX = prng.Next(-100000, 100000); // 랜덤 오프셋 X
        float offsetY = prng.Next(-100000, 100000); // 랜덤 오프셋 Y

        if (scale <= 0) scale = 0.0001f; // 스케일 값이 0 이하인 경우 방지

        // 각 타일에 대해 옥타브 노이즈 계산
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseValue = 0f;
                float normalizationFactor = 0f;

                // 여러 옥타브 계산
                for (int i = 0; i < octaves; i++)
                {
                    // 샘플링 좌표 계산 (스케일, 주파수, 오프셋 적용)
                    // 여기서는 맵 중심 기준이 아닌 (0,0) 기준 좌표 사용
                    float sampleX = x / scale * frequency + offsetX;
                    float sampleY = y / scale * frequency + offsetY;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY); // 0~1 범위
                    noiseValue += perlinValue * amplitude;
                    normalizationFactor += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // 노이즈 값 정규화 (대략 0~1) 및 저장
                noiseMap[x, y] = (normalizationFactor > 0) ? noiseValue / normalizationFactor : 0.5f; // 분모 0 방지, 기본값 0.5

                // 최대/최소 값 업데이트
                if (noiseMap[x, y] > maxNoiseHeight) maxNoiseHeight = noiseMap[x, y];
                if (noiseMap[x, y] < minNoiseHeight) minNoiseHeight = noiseMap[x, y];
            }
        }

        // 최종적으로 전체 맵을 0~1 범위로 정확하게 정규화
        if (maxNoiseHeight > minNoiseHeight)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        else // 모든 노이즈 값이 동일한 경우 (예: 옥타브 1, 스케일 매우 큼)
        {
            // 모든 값을 0.5로 설정 (또는 minNoiseHeight 값 사용 가능)
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = 0.5f;
                }
            }
        }
        return noiseMap; // 최종 정규화된 노이즈 맵 반환
    }

    /// <summary>
    /// 각 타일의 고도, 기온, 강우량 값을 기반으로 BiomeType을 결정합니다.
    /// Whittaker Biome Diagram과 유사한 규칙 세트를 사용하여 분류합니다.
    /// 물, 해변, 산악 지형을 먼저 결정하고, 나머지는 기온과 강우량에 따라 결정합니다.
    /// </summary>
    /// <param name="world">Biome을 결정하고 저장할 WorldData 객체</param>
    private void DetermineBiomes(WorldData world)
    {
        Debug.Log("Step 3: Determining Biomes (Climate Based)..."); // 단계 시작 로그

        // Biome 결정을 위한 임계값 정의 (조정 가능)
        // 기온 (Temperature)
        float tCold = 0.15f;       // 매우 추움 (툰드라, 빙하 경계)
        float tCool = 0.35f;       // 추움 (타이가, 스텝 경계)
        float tTemperate = 0.65f;  // 온화함 (온대림, 초원 경계)
        float tWarm = 0.85f;       // 따뜻함 (아열대, 사막 경계)
        // 강우량 (Rainfall)
        float rDry = 0.15f;        // 매우 건조 (사막, 관목림 경계)
        float rArid = 0.30f;       // 건조 (초원, 건조림 경계)
        float rModerate = 0.55f;   // 보통 (혼합림, 습윤림 경계)
        float rWet = 0.75f;        // 습함 (온대우림, 열대우림 경계)
        // 고도 (Elevation)
        float eMountain = 0.7f;    // 산악 지대 시작 고도
        float eHighMountain = 0.85f; // 고산 지대 시작 고도

        // 모든 타일 순회하며 Biome 결정
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                // 현재 타일의 고도, 기온, 강우량 값 가져오기
                float e = world.WorldTiles[y, x].Elevation;
                float t = world.WorldTiles[y, x].Temperature;
                float r = world.WorldTiles[y, x].Rainfall;
                BiomeType biome; // 결정될 Biome

                // --- Biome 결정 로직 (우선순위: 물/해변 > 고산/산악 > 기온/강우량 > 습지) ---

                // 1. 수역 및 해변 결정 (고도 기준)
                if (e < deepWaterThreshold) biome = BiomeType.DeepWater;
                else if (e < shallowWaterThreshold) biome = BiomeType.ShallowWater;
                else if (e < beachThreshold) biome = BiomeType.Beach;

                // 2. 고산/산악 지대 결정 (고도 우선)
                else if (e > eHighMountain) // 매우 높은 고산 지대
                {
                    // 매우 높고 매우 추우면 극지방 빙하(PolarIce), 아니면 그냥 고산(HighMountain)
                    biome = (t < tCold * 1.5f) ? BiomeType.PolarIce : BiomeType.HighMountain;
                }
                else if (e > eMountain) // 일반 산악 지대
                {
                    // 산악 지대 내 기온에 따른 세분화
                    if (t < tCold) biome = BiomeType.MountainTundra; // 춥고 높음: 산악 툰드라
                    else if (t < tCool) biome = BiomeType.AlpineForest; // 서늘하고 높음: 고산림
                    else biome = BiomeType.Mountainous; // 온화하고 높음: 일반 산악
                }
                // 3. 일반 육지 Biome 결정 (기온과 강우량 기준)
                else if (t < tCool) // 추운 지역 (Cool/Cold)
                {
                    if (r > rModerate) biome = BiomeType.Taiga; // 습함: 타이가 (침엽수림)
                    else if (r > rArid) biome = BiomeType.ColdParklands; // 보통: 한대 소림지
                    else biome = BiomeType.Steppe; // 건조: 스텝 (한대 초원)
                    // Tundra는 더 추운 경우(tCold 미만) 또는 위도 기반으로 별도 처리 가능
                    if (t < tCold && r < rArid) biome = BiomeType.Tundra; // 매우 춥고 건조하면 툰드라
                }
                else if (t < tTemperate) // 온화한 지역 (Temperate)
                {
                    if (r > rWet) biome = BiomeType.TemperateRainforest; // 매우 습함: 온대 우림
                    else if (r > rModerate) biome = BiomeType.TemperateDeciduousForest; // 습함: 온대 낙엽수림
                    else if (r > rArid) biome = BiomeType.TemperateMixedForest; // 보통: 온대 혼합림
                    else if (r > rDry) biome = BiomeType.TemperateGrassland; // 건조: 온대 초원 (Plains, Hills 포함 가능)
                    else biome = BiomeType.Shrubland; // 매우 건조: 관목림
                }
                else if (t < tWarm) // 따뜻한 지역 (Warm/Subtropical)
                {
                    if (r > rWet) biome = BiomeType.SubtropicalMoistForest; // 매우 습함: 아열대 다습림
                    else if (r > rModerate) biome = BiomeType.SubtropicalDryForest; // 습함: 아열대 건조림
                    else if (r > rArid) biome = BiomeType.Mediterranean; // 보통: 지중해성 기후 지역
                    else if (r > rDry) biome = BiomeType.SubtropicalGrassland; // 건조: 아열대 초원 (사바나와 유사)
                    else biome = BiomeType.Desert; // 매우 건조: 사막
                }
                else // 더운 지역 (Hot/Tropical)
                {
                    if (r > rWet) biome = BiomeType.TropicalRainforest; // 매우 습함: 열대 우림
                    else if (r > rModerate) biome = BiomeType.TropicalMoistForest; // 습함: 열대 계절림
                    else if (r > rArid) biome = BiomeType.TropicalGrassland; // 보통: 열대 초원 (사바나)
                    else if (r > rDry) biome = BiomeType.TropicalDryForest; // 건조: 열대 건조림
                    else biome = BiomeType.Desert; // 매우 건조: 사막
                }

                // 4. 습지(Wetlands) 추가 조건:
                // 물/해변이 아니고, 고도가 해변보다 약간 높으며(낮은 육지), 강우량이 보통 이상인 경우 습지로 변경
                if (biome != BiomeType.ShallowWater && biome != BiomeType.DeepWater && biome != BiomeType.Beach &&
                    e < (beachThreshold + 0.1f) && // 예: 해변 고도 + 0.1 이내
                    r > rModerate)
                {
                    biome = BiomeType.Wetlands;
                }

                // 최종 결정된 Biome을 타일에 저장
                world.WorldTiles[y, x].Biome = biome;
            }
        }
        Debug.Log("Step 3: Biomes Determined."); // 단계 완료 로그
    }

    /// <summary>
    /// 월드에 주요 지점(POI - 도시, 던전 등)을 배치합니다.
    /// 지정된 수(cityCount, dungeonCount)만큼 랜덤 위치에 배치를 시도합니다.
    /// 배치 조건: 특정 Biome, 육지, 강 아님, 물가에서 떨어짐, 다른 POI와 최소 거리 유지 등.
    /// </summary>
    /// <param name="world">POI를 배치할 WorldData 객체</param>
    /// <param name="cityCount">배치할 도시 목표 수</param>
    /// <param name="dungeonCount">배치할 던전 목표 수</param>
    private void PlacePOIs(WorldData world, int cityCount, int dungeonCount)
    {
        Debug.Log($"Step 5: Placing {cityCount} Cities and {dungeonCount} Dungeons..."); // 단계 시작 로그
        List<Vector2Int> poiLocations = new List<Vector2Int>(); // 이미 배치된 모든 POI 위치 저장 리스트

        // --- 도시 배치 ---
        int citiesPlaced = 0; // 실제로 배치된 도시 수
        // 최대 시도 횟수 (목표 수 * 배율) 내에서 목표 수만큼 배치 시도
        for (int attempts = 0; citiesPlaced < cityCount && attempts < cityCount * poiPlacementAttemptsMultiplier; attempts++)
        {
            // 랜덤 좌표 선택 (worldRandom 사용)
            int randX = worldRandom.Next(0, world.Width);
            int randY = worldRandom.Next(0, world.Height);
            WorldTile tile = world.WorldTiles[randY, randX]; // 해당 타일 정보
            Vector2Int pos = new Vector2Int(randX, randY); // 현재 위치

            // --- 도시 배치 조건 확인 ---
            // 1. Biome 조건: 특정 Biome에만 배치 (예: 초원, 평야, 언덕, 지중해성)
            bool biomeCondition = (tile.Biome == BiomeType.TemperateGrassland || tile.Biome == BiomeType.Plains || tile.Biome == BiomeType.Hills || tile.Biome == BiomeType.Mediterranean);
            // 2. 지형 조건: 육지이고 강이 아니어야 함
            bool terrainCondition = tile.IsLand(beachThreshold) && !tile.IsRiver;
            // 3. 물가 조건: 물(바다, 호수, 강)에서 일정 거리(예: 반경 3) 이상 떨어져야 함
            bool waterDistanceCondition = !IsNearWater(world, randX, randY, 3);
            // 4. POI 거리 조건: 다른 기존 POI와 최소 거리(minDistanceBetweenPOIs) 유지
            bool poiDistanceCondition = !IsNearExistingPOI(pos, poiLocations, minDistanceBetweenPOIs);
            // 5. 중복 방지: 해당 타일에 이미 다른 POI가 없어야 함
            bool noExistingPoiCondition = !HasPOI(tile);

            // 모든 조건 만족 시 도시 배치
            if (biomeCondition && terrainCondition && waterDistanceCondition && poiDistanceCondition && noExistingPoiCondition)
            {
                world.WorldTiles[randY, randX].HasCity = true; // 도 시 플래그 설정
                world.WorldTiles[randY, randX].Biome = BiomeType.City; // Biome도 변경 (시각화용)
                poiLocations.Add(pos); // 배치된 POI 목록에 추가
                citiesPlaced++; // 배치 수 증가
            }
        }
        // 목표 수만큼 배치 못했을 경우 경고 로그
        if (citiesPlaced < cityCount) Debug.LogWarning($"Could only place {citiesPlaced}/{cityCount} cities. Try increasing placement attempts multiplier or adjusting placement conditions.");

        // --- 던전 배치 ---
        int dungeonsPlaced = 0; // 실제로 배치된 던전 수
        // 최대 시도 횟수 내에서 목표 수만큼 배치 시도
        for (int attempts = 0; dungeonsPlaced < dungeonCount && attempts < dungeonCount * poiPlacementAttemptsMultiplier; attempts++)
        {
            // 랜덤 좌표 선택
            int randX = worldRandom.Next(0, world.Width);
            int randY = worldRandom.Next(0, world.Height);
            WorldTile tile = world.WorldTiles[randY, randX]; // 해당 타일 정보
            Vector2Int pos = new Vector2Int(randX, randY); // 현재 위치

            // --- 던전 배치 조건 확인 ---
            // 1. Biome 조건: 다양한 지형에 배치 가능 (예: 산, 언덕, 숲, 사막, 습지, 툰드라 등)
            bool biomeCondition = (tile.Biome == BiomeType.Mountainous || tile.Biome == BiomeType.Hills || tile.Biome == BiomeType.AlpineForest || tile.Biome == BiomeType.Taiga || tile.Biome == BiomeType.TemperateDeciduousForest || tile.Biome == BiomeType.Desert || tile.Biome == BiomeType.Wetlands || tile.Biome == BiomeType.Tundra);
            // 2. 지형 조건: 육지이고 강이 아니어야 함
            bool terrainCondition = tile.IsLand(beachThreshold) && !tile.IsRiver;
            // 3. POI 거리 조건: 다른 기존 POI(도시 포함)와 최소 거리 유지
            bool poiDistanceCondition = !IsNearExistingPOI(pos, poiLocations, minDistanceBetweenPOIs);
            // 4. 중복 방지: 해당 타일에 이미 다른 POI가 없어야 함
            bool noExistingPoiCondition = !HasPOI(tile);
            // 5. 추가 조건 가능: 예) 물가 근처에는 배치 안 함 등

            // 모든 조건 만족 시 던전 배치
            if (biomeCondition && terrainCondition && poiDistanceCondition && noExistingPoiCondition)
            {
                world.WorldTiles[randY, randX].HasDungeon = true; // 던전 플래그 설정
                world.WorldTiles[randY, randX].Biome = BiomeType.DungeonEntrance; // Biome도 변경 (시각화용)
                poiLocations.Add(pos); // 배치된 POI 목록에 추가
                dungeonsPlaced++; // 배치 수 증가
            }
        }
        // 목표 수만큼 배치 못했을 경우 경고 로그
        if (dungeonsPlaced < dungeonCount) Debug.LogWarning($"Could only place {dungeonsPlaced}/{dungeonCount} dungeons. Try increasing placement attempts multiplier or adjusting placement conditions.");

        Debug.Log("Step 5: POI Placement Complete."); // 단계 완료 로그
    }

    /// <summary>
    /// 새로운 POI 위치(newPos)가 기존에 배치된 POI들(existingPois)과 최소 거리(minDistance) 이상 떨어져 있는지 확인합니다.
    /// 거리 계산 시 제곱근 계산을 피하기 위해 거리의 제곱을 비교합니다.
    /// </summary>
    /// <param name="newPos">새로운 POI 위치 후보</param>
    /// <param name="existingPois">기존에 배치된 POI 위치 리스트</param>
    /// <param name="minDistance">유지해야 할 최소 거리 (타일 단위)</param>
    /// <returns>최소 거리 내에 다른 POI가 있으면 true (가까움), 아니면 false (충분히 멂)</returns>
    private bool IsNearExistingPOI(Vector2Int newPos, List<Vector2Int> existingPois, int minDistance)
    {
        // 최소 거리의 제곱 미리 계산
        int minDistSq = minDistance * minDistance;
        // 기존 POI 목록 순회
        foreach (Vector2Int existingPos in existingPois)
        {
            // 두 점 사이의 x, y 차이 계산
            int dx = newPos.x - existingPos.x;
            int dy = newPos.y - existingPos.y;
            // 두 점 사이의 거리 제곱 계산 (dx*dx + dy*dy)
            if ((dx * dx + dy * dy) < minDistSq) // 제곱 거리가 최소 거리 제곱보다 작으면
            {
                return true; // 너무 가까움
            }
        }
        return false; // 모든 기존 POI와 충분히 멂
    }

    /// <summary>
    /// 해당 타일에 이미 POI(도시, 마을, 던전)가 배치되어 있는지 확인합니다.
    /// </summary>
    /// <param name="tile">확인할 WorldTile 객체</param>
    /// <returns>POI가 하나라도 있으면 true, 없으면 false</returns>
    private bool HasPOI(WorldTile tile)
    {
        return tile.HasCity || tile.HasTown || tile.HasDungeon;
    }

    /// <summary>
    /// 주어진 좌표(x, y) 주변(반경 radius)에 물 타일(육지가 아닌 타일)이 있는지 확인합니다.
    /// POI 배치 시 물가 근처를 피하는 등의 조건에 사용될 수 있습니다.
    /// </summary>
    /// <param name="world">WorldData 객체</param>
    /// <param name="x">중심 X 좌표</param>
    /// <param name="y">중심 Y 좌표</param>
    /// <param name="radius">확인할 반경 (기본값 1)</param>
    /// <returns>주변 반경 내에 물 타일이 하나라도 있으면 true, 없으면 false</returns>
    private bool IsNearWater(WorldData world, int x, int y, int radius = 1)
    {
        // 주변 반경 내 타일 순회 (중심 포함)
        for (int j = y - radius; j <= y + radius; j++)
        {
            for (int i = x - radius; i <= x + radius; i++)
            {
                // 자기 자신은 검사 제외
                if (i == x && j == y) continue;
                // 맵 범위 내이고 물 타일(육지가 아님)이면
                if (world.IsInBounds(i, j) && !world.WorldTiles[j, i].IsLand(beachThreshold))
                {
                    return true; // 물 타일 발견!
                }
            }
        }
        return false; // 주변 반경 내에 물 타일 없음
    }

    /// <summary>
    /// 도시들 사이에 도로를 생성합니다. (현재는 Placeholder: 인접한 도시 쌍을 단순 직선으로 연결)
    /// 실제 게임에서는 A* 등의 경로 탐색 알고리즘 사용 필요.
    /// </summary>
    /// <param name="world">도로를 생성할 WorldData 객체</param>
    private void GenerateRoads(WorldData world)
    {
        Debug.Log("Step 6: Generating Roads (Simple Placeholder - Straight Lines)..."); // 단계 시작 로그
        List<Vector2Int> cityLocations = new List<Vector2Int>(); // 맵에 배치된 도시 위치 목록

        // 맵 전체를 스캔하여 도시 위치 찾기
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (world.WorldTiles[y, x].HasCity)
                {
                    cityLocations.Add(new Vector2Int(x, y));
                }
            }
        }

        // 도시가 2개 미만이면 도로를 생성할 수 없음
        if (cityLocations.Count < 2)
        {
            Debug.Log("Not enough cities (< 2) to generate roads.");
            return;
        }

        // --- 도로 연결 방식 결정 (Placeholder: 순서대로 연결) ---
        // 실제로는 최소 신장 트리(Minimum Spanning Tree, MST)를 구하거나,
        // 근접한 도시 쌍을 찾아 연결하는 방식 등을 사용해야 더 자연스러움.
        // 여기서는 단순히 리스트 순서대로 i번째 도시와 i+1번째 도시를 연결 시도.
        for (int i = 0; i < cityLocations.Count - 1; i++)
        {
            Vector2Int start = cityLocations[i];
            Vector2Int end = cityLocations[i + 1]; // 임시로 다음 도시와 연결

            // --- 경로 탐색 (Placeholder: 단순 직선) ---
            // 실제로는 A* 알고리즘 사용 필요.
            // A* 구현 시 고려 사항:
            // - 이동 비용(Cost): 평지 < 언덕 < 산, 물/강은 이동 불가 또는 매우 높은 비용
            // - 경사 페널티(roadSlopePenaltyMultiplier): 급격한 고도 변화에 페널티 부여
            // - 최대 탐색 노드 수(roadMaxSearchNodes): 성능 제한
            List<Vector2Int> roadPath = FindRoadPath_SimpleLine(world, start, end); // 단순 직선 경로 찾기

            // 경로가 찾아졌으면 해당 경로를 도로로 표시
            if (roadPath != null && roadPath.Count > 0)
            {
                foreach (Vector2Int pos in roadPath)
                {
                    // 경로 상의 타일이 맵 범위 내이고, 육지이며, 강이 아닌 경우에만 도로로 표시
                    if (world.IsInBounds(pos.x, pos.y) && world.WorldTiles[pos.y, pos.x].IsLand(beachThreshold) && !world.WorldTiles[pos.y, pos.x].IsRiver)
                    {
                        world.WorldTiles[pos.y, pos.x].IsRoad = true;
                        // 도로 Biome을 따로 지정할 수도 있음 (선택 사항)
                    }
                }
            }
            else // 경로 찾기 실패 (거의 발생 안 함 - 직선 경로이므로)
            {
                Debug.LogWarning($"Could not find road path between {start} and {end} (Simple Line Placeholder).");
            }
        }
        Debug.Log("Step 6: Road Generation Attempt Complete."); // 단계 완료 로그
    }

    /// <summary>
    /// Bresenham의 직선 알고리즘을 사용하여 두 점(start, end) 사이의 직선 경로에 해당하는 타일 좌표 목록을 반환합니다. (단순 Placeholder)
    /// 지형 비용이나 장애물을 고려하지 않습니다.
    /// </summary>
    /// <param name="world">WorldData 객체 (IsLand 검사용)</param>
    /// <param name="start">시작점 좌표</param>
    /// <param name="end">끝점 좌표</param>
    /// <returns>직선 경로 상의 타일 좌표(Vector2Int) 리스트</returns>
    private List<Vector2Int> FindRoadPath_SimpleLine(WorldData world, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>(); // 경로 저장 리스트
        int x0 = start.x, y0 = start.y; // 시작점
        int x1 = end.x, y1 = end.y; // 끝점

        // Bresenham 알고리즘 변수 초기화
        int dx = Mathf.Abs(x1 - x0); // x 변화량 절댓값
        int dy = -Mathf.Abs(y1 - y0); // y 변화량 절댓값 (음수로 사용)
        int sx = x0 < x1 ? 1 : -1; // x 진행 방향 (+1 또는 -1)
        int sy = y0 < y1 ? 1 : -1; // y 진행 방향 (+1 또는 -1)
        int err = dx + dy; // 에러 누적 변수
        int e2; // 임시 에러 변수

        // 시작점부터 끝점까지 반복하며 경로 생성
        while (true)
        {
            // 현재 위치(x0, y0)가 맵 범위 내이고 '육지'이면 경로에 추가
            // (물 위에는 도로 생성 안 함 - 단순화)
            if (world.IsInBounds(x0, y0) && world.WorldTiles[y0, x0].IsLand(beachThreshold))
            {
                path.Add(new Vector2Int(x0, y0));
            }
            // 만약 경로가 이미 생성되었는데 현재 위치가 물이라면, 거기서 중단 (물 직전까지만 도로 생성)
            // (이 조건은 필요 없을 수도 있음. 위 IsLand 체크에서 걸러짐)
            // else if (path.Count > 0 && !world.WorldTiles[y0, x0].IsLand(beachThreshold))
            // {
            //     break;
            // }

            // 끝점에 도달했으면 루프 종료
            if (x0 == x1 && y0 == y1) break;

            // 다음 픽셀 위치 계산 (Bresenham 로직)
            e2 = 2 * err;
            if (e2 >= dy) // 에러가 y 변화량보다 크거나 같으면 (x축 이동 조건 만족)
            {
                // if (x0 == x1) break; // x가 끝점에 도달하면 더 이상 x 이동 불가 (필요시 추가)
                err += dy; // 에러 업데이트
                x0 += sx; // x 이동
            }
            if (e2 <= dx) // 에러가 x 변화량보다 작거나 같으면 (y축 이동 조건 만족)
            {
                // if (y0 == y1) break; // y가 끝점에 도달하면 더 이상 y 이동 불가 (필요시 추가)
                err += dx; // 에러 업데이트
                y0 += sy; // y 이동
            }
        }
        return path; // 생성된 경로 리스트 반환
    }

    /// <summary>
    /// 상세 월드 데이터(detailedData)를 기반으로 지정된 비율(factor)로 축소된 간략화된 월드 데이터(SimplifiedWorldData)를 생성합니다.
    /// 각 간략화된 타일은 해당하는 상세 타일 영역(factor x factor)의 주요 Biome, POI 존재 여부, 물 존재 여부 등의 정보를 요약하여 가집니다.
    /// 미니맵 등에 사용될 수 있습니다.
    /// </summary>
    /// <param name="detailedData">원본 상세 WorldData</param>
    /// <param name="factor">간략화 비율 (예: 4면 4x4 상세 타일이 1개의 간략화 타일이 됨)</param>
    /// <returns>생성된 SimplifiedWorldData 객체. 실패 시 null 반환.</returns>
    private SimplifiedWorldData GenerateSimplifiedWorld(WorldData detailedData, int factor)
    {
        // 유효하지 않은 입력 처리
        if (detailedData == null || factor <= 0)
        {
            Debug.LogError("Cannot generate simplified world: Detailed data is null or factor is invalid.");
            return null;
        }

        // 간략화된 맵 크기 계산 (올림 처리하여 원본 맵 전체 포함)
        int simplifiedWidth = Mathf.CeilToInt((float)detailedData.Width / factor);
        int simplifiedHeight = Mathf.CeilToInt((float)detailedData.Height / factor);
        // 간략화 데이터 객체 생성
        SimplifiedWorldData simplifiedData = new SimplifiedWorldData(simplifiedWidth, simplifiedHeight);
        Debug.Log($"Generating Simplified World ({simplifiedWidth}x{simplifiedHeight} from {detailedData.Width}x{detailedData.Height} with factor {factor})...");

        // 간략화된 맵의 각 타일(sx, sy)에 대해 처리
        for (int sy = 0; sy < simplifiedHeight; sy++)
        {
            for (int sx = 0; sx < simplifiedWidth; sx++)
            {
                // 현재 간략화 타일(sx, sy)에 해당하는 상세 맵 영역의 시작/끝 좌표 계산
                int startX = sx * factor;
                int startY = sy * factor;
                // 상세 맵 경계를 초과하지 않도록 Min 사용
                int endX = Mathf.Min(startX + factor, detailedData.Width);
                int endY = Mathf.Min(startY + factor, detailedData.Height);

                // 해당 영역의 정보를 집계하기 위한 변수 초기화
                BiomeType dominantBiome = BiomeType.Plains; // 영역 내 가장 많은 Biome (기본값)
                bool hasPoi = false; // 영역 내 POI(도시, 던전) 존재 여부
                bool blockIsMostlyWater = false; // 영역이 주로 물인지 여부
                bool blockContainsRiver = false; // 영역에 강 타일이 포함되는지 여부
                Dictionary<BiomeType, int> biomeCounts = new Dictionary<BiomeType, int>(); // Biome 종류별 타일 수 카운트
                int landCount = 0; // 영역 내 육지 타일 수
                int waterCount = 0; // 영역 내 물 타일 수
                int tileCount = 0; // 영역 내 총 유효 타일 수

                // 상세 맵 영역(startX, startY) ~ (endX, endY) 순회하며 정보 집계
                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        // (이론상 불필요하지만 안전을 위해) 상세 맵 범위 확인
                        if (y >= 0 && y < detailedData.Height && x >= 0 && x < detailedData.Width)
                        {
                            WorldTile detailTile = detailedData.WorldTiles[y, x]; // 상세 타일 정보 가져오기
                            tileCount++; // 유효 타일 수 증가

                            // 1. Biome 카운트
                            if (biomeCounts.ContainsKey(detailTile.Biome))
                            {
                                biomeCounts[detailTile.Biome]++;
                            }
                            else
                            {
                                biomeCounts.Add(detailTile.Biome, 1);
                            }

                            // 2. POI 존재 여부 확인 (하나라도 있으면 true)
                            if (detailTile.HasCity || detailTile.HasDungeon) { hasPoi = true; }
                            // 3. 강 존재 여부 확인 (하나라도 있으면 true)
                            if (detailTile.IsRiver) { blockContainsRiver = true; }
                            // 4. 육지/물 카운트
                            if (detailTile.IsLand(beachThreshold)) landCount++; else waterCount++;
                        }
                    }
                } // 상세 맵 영역 순회 종료

                // 집계된 정보로 간략화 타일의 주요 Biome 및 상태 결정
                int maxCount = 0; // 가장 많은 Biome의 타일 수
                if (tileCount > 0) // 영역 내 유효한 타일이 있는 경우
                {
                    // 5. 가장 많은 Biome 찾기 (단, POI, 강 관련 Biome은 제외하고 찾음)
                    foreach (var pair in biomeCounts)
                    {
                        // POI(City, DungeonEntrance, Town)나 River Biome은 dominantBiome 결정에서 제외
                        if (pair.Key == BiomeType.City || pair.Key == BiomeType.DungeonEntrance || pair.Key == BiomeType.Town || pair.Key == BiomeType.River) continue;

                        // 현재까지 가장 많은 Biome 업데이트
                        if (pair.Value > maxCount)
                        {
                            maxCount = pair.Value;
                            dominantBiome = pair.Key; // 가장 많은 Biome으로 설정
                        }
                    }
                    // 6. 영역이 주로 물인지 판단 (물 타일 수가 육지 타일 수보다 많으면)
                    blockIsMostlyWater = waterCount > landCount;
                }
                else // 영역 내 유효 타일이 없는 경우 (이론상 발생 어려움)
                {
                    dominantBiome = BiomeType.DeepWater; // 기본값으로 물 설정
                    blockIsMostlyWater = true;
                }

                // 7. 최종 Biome 및 상태 조정 (우선순위 적용)
                if (blockContainsRiver) // 영역에 강이 포함되면 무조건 강(River)으로 표시 (우선순위 높음)
                {
                    dominantBiome = BiomeType.River;
                    blockIsMostlyWater = true; // 강은 물로 간주
                }
                // 강이 없고, 주로 물이며, POI가 없고, 이미 심해(DeepWater)가 아닌 경우 -> 얕은 물(ShallowWater)로 표시
                // (주로 물이지만 DeepWater가 아닌 경우를 ShallowWater로 통일)
                else if (blockIsMostlyWater && !hasPoi && dominantBiome != BiomeType.DeepWater)
                {
                    dominantBiome = BiomeType.ShallowWater;
                }
                // 그 외의 경우 (주로 육지이거나, POI가 있거나, 심해인 경우): 위에서 결정된 dominantBiome 사용

                // 간략화된 타일 데이터(SimplifiedWorldTile) 생성 및 저장
                simplifiedData.SimplifiedTiles[sy, sx] = new SimplifiedWorldTile(dominantBiome, hasPoi, blockIsMostlyWater);
            }
        }
        Debug.Log("Simplified World Generation complete."); // 단계 완료 로그
        return simplifiedData; // 생성된 간략화 월드 데이터 반환
    }

    #region River Generation - Flow Accumulation Approach (Step 4)

    /// <summary>
    /// **복원됨:** 유량 집적(Flow Accumulation) 원리를 사용하여 강을 생성합니다.
    /// </summary>
    /// <param name="world">강을 생성할 WorldData 객체</param>
    private void GenerateRivers_FlowAccumulation(WorldData world)
    {
        Debug.Log("Step 4: Generating Rivers (Flow Accumulation)...");
        System.Diagnostics.Stopwatch riverWatch = System.Diagnostics.Stopwatch.StartNew();

        // 유량 관련 맵 초기화
        flowDirectionMap = new Vector2Int[world.Height, world.Width];
        flowAccumulationMap = new float[world.Height, world.Width];

        // 1. 흐름 방향 계산 (D4 로직 사용)
        CalculateFlowDirections(world); // 내부에서 CalculateFlowDirD4 호출
        Debug.Log(" - Flow Direction Calculated (D4).");

        // 2. 유량 집적 계산
        CalculateFlowAccumulation(world);
        Debug.Log(" - Flow Accumulation Calculated.");

        // 3. 강 정의 및 강바닥 침식
        DefineRiversAndCarve(world);

        riverWatch.Stop();
        Debug.Log($"Step 4: River Generation (Flow Accumulation) Complete ({riverWatch.ElapsedMilliseconds} ms).");
    }

    /// <summary>
    /// **복원됨:** 모든 육지 타일에 대해 흐름 방향을 계산하여 flowDirectionMap에 저장합니다.
    /// CalculateFlowDirD4 함수를 호출합니다.
    /// </summary>
    /// <param name="world">WorldData 객체</param>
    private void CalculateFlowDirections(WorldData world)
    {
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (world.WorldTiles[y, x].IsLand(beachThreshold))
                {
                    flowDirectionMap[y, x] = CalculateFlowDirD4(world, x, y); // D4 사용
                }
                else
                {
                    flowDirectionMap[y, x] = Vector2Int.zero;
                }
            }
        }
    }

    /// <summary>
    /// **복원됨:** 주어진 좌표(x, y)의 타일에 대해 D4(상하좌우) 흐름 방향을 계산합니다.
    /// </summary>
    /// <param name="world">WorldData 객체</param>
    /// <param name="x">타일 X 좌표</param>
    /// <param name="y">타일 Y 좌표</param>
    /// <returns>가장 낮은 인접 타일(상하좌우)로의 Vector2Int 오프셋. 없으면 Zero.</returns>
    private Vector2Int CalculateFlowDirD4(WorldData world, int x, int y)
    {
        float minElevation = world.WorldTiles[y, x].Elevation;
        Vector2Int flowDir = Vector2Int.zero;
        float lowestNeighborElevation = minElevation;

        Vector2Int[] neighborOffsets = GetNeighborOffsets(); // 상하좌우 오프셋 가져오기

        foreach (Vector2Int offset in neighborOffsets)
        {
            int nx = x + offset.x;
            int ny = y + offset.y;

            if (world.IsInBounds(nx, ny))
            {
                float neighborElevation = world.WorldTiles[ny, nx].Elevation;
                if (neighborElevation < lowestNeighborElevation)
                {
                    lowestNeighborElevation = neighborElevation;
                    flowDir = offset;
                }
            }
        }
        return flowDir;
    }

    /// <summary>
    /// **복원됨:** 각 타일의 누적 유량을 계산하여 flowAccumulationMap에 저장합니다.
    /// </summary>
    /// <param name="world">WorldData 객체</param>
    private void CalculateFlowAccumulation(WorldData world)
    {
        // 1. 유량 맵 초기화
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                flowAccumulationMap[y, x] = world.WorldTiles[y, x].IsLand(beachThreshold) ? 1f : 0f;
            }
        }

        // 2. 처리 순서 결정 (고도 내림차순)
        List<Vector2Int> processingOrder = GetProcessingOrder(world);

        // 3. 정렬된 순서대로 유량 전달
        foreach (Vector2Int pos in processingOrder)
        {
            int x = pos.x;
            int y = pos.y;

            if (flowAccumulationMap[y, x] > 0)
            {
                Vector2Int flowDir = flowDirectionMap[y, x]; // 계산된 D4 방향 사용
                if (flowDir != Vector2Int.zero)
                {
                    int targetX = x + flowDir.x;
                    int targetY = y + flowDir.y;

                    if (world.IsInBounds(targetX, targetY))
                    {
                        flowAccumulationMap[targetY, targetX] += flowAccumulationMap[y, x];
                    }
                }
            }
        }
    }

    /// <summary>
    /// **복원됨:** 유량 집적 계산을 위해 타일을 처리할 순서를 반환합니다.
    /// </summary>
    /// <param name="world">WorldData 객체</param>
    /// <returns>고도 내림차순으로 정렬된 육지 타일 좌표 리스트</returns>
    private List<Vector2Int> GetProcessingOrder(WorldData world)
    {
        List<KeyValuePair<Vector2Int, float>> sortedTiles = new List<KeyValuePair<Vector2Int, float>>(world.Width * world.Height);
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (world.WorldTiles[y, x].IsLand(beachThreshold))
                {
                    sortedTiles.Add(new KeyValuePair<Vector2Int, float>(new Vector2Int(x, y), world.WorldTiles[y, x].Elevation));
                }
            }
        }
        sortedTiles.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
        List<Vector2Int> order = new List<Vector2Int>(sortedTiles.Count);
        foreach (var pair in sortedTiles) { order.Add(pair.Key); }
        return order;
    }

    /// <summary>
    /// **복원됨:** 계산된 유량과 임계값을 사용하여 강 타일을 식별하고 강바닥을 침식시킵니다.
    /// </summary>
    /// <param name="world">WorldData 객체</param>
    private void DefineRiversAndCarve(WorldData world)
    {
        float maxAccumulation = 1f;
        if (flowAccumulationMap != null) // Null 체크 추가
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    if (flowAccumulationMap[y, x] > maxAccumulation)
                    {
                        maxAccumulation = flowAccumulationMap[y, x];
                    }
                }
            }
        }
        if (maxAccumulation <= 1f) maxAccumulation = 1f; // 최소값 보정

        float baseActualThreshold = riverFlowThreshold * maxAccumulation;
        int riverTileCount = 0;
        Debug.Log($"Max Flow Accumulation: {maxAccumulation}, Base River Threshold: {baseActualThreshold}");

        float mountainStartElevation = 0.6f;
        float highMountainElevation = 0.9f;
        float thresholdReductionFactor = 0.4f;

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                // flowAccumulationMap Null 체크 추가
                if (flowAccumulationMap == null) continue;

                float elevation = world.WorldTiles[y, x].Elevation;
                float thresholdMultiplier = 1.0f;
                if (elevation >= mountainStartElevation)
                {
                    float elevationFactor = Mathf.InverseLerp(mountainStartElevation, highMountainElevation, elevation);
                    thresholdMultiplier = Mathf.Lerp(1.0f, thresholdReductionFactor, elevationFactor);
                }
                float adjustedThreshold = baseActualThreshold * thresholdMultiplier;

                if (flowAccumulationMap[y, x] > adjustedThreshold && world.WorldTiles[y, x].IsLand(beachThreshold))
                {
                    world.WorldTiles[y, x].IsRiver = true;
                    riverTileCount++;

                    if (carveRiverBed)
                    {
                        float flowRatio = Mathf.Clamp01(flowAccumulationMap[y, x] / maxAccumulation);
                        float carveDepth = baseRiverCarveDepth + (flowRatio * flowCarveMultiplier);
                        // 침식 시 최소 고도를 shallowWaterThreshold보다 약간 높게 유지 (강둑 침식과 일관성)
                        world.WorldTiles[y, x].Elevation = Mathf.Max(shallowWaterThreshold + 0.01f, world.WorldTiles[y, x].Elevation - carveDepth);
                    }
                }
            }
        }
        Debug.Log($" - Identified {riverTileCount} river tiles using flow accumulation and elevation-adjusted threshold.");
    }

    /// <summary>
    /// **복원됨:** 유효한 수역(바다 또는 큰 호수)을 식별합니다. (내륙 강 제거용)
    /// </summary>
    private void IdentifyValidWaterBodies(WorldData world)
    {
        Debug.Log("Step 4.1: Identifying valid water bodies (for pruning)...");
        if (isValidWaterEndpoint == null) { isValidWaterEndpoint = new bool[world.Height, world.Width]; }
        else { Array.Clear(isValidWaterEndpoint, 0, isValidWaterEndpoint.Length); }

        bool[,] visited = new bool[world.Height, world.Width];
        int validEndpointCount = 0;

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (!visited[y, x] && world.WorldTiles[y, x].Elevation < shallowWaterThreshold)
                {
                    List<Vector2Int> currentBody = new List<Vector2Int>();
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    bool isOceanConnected = false;
                    bool isLargeEnough = false;

                    queue.Enqueue(new Vector2Int(x, y));
                    visited[y, x] = true;

                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue();
                        currentBody.Add(current);

                        if (current.x == 0 || current.x == world.Width - 1 || current.y == 0 || current.y == world.Height - 1)
                        {
                            isOceanConnected = true;
                        }

                        foreach (Vector2Int offset in GetNeighborOffsets())
                        {
                            int nx = current.x + offset.x;
                            int ny = current.y + offset.y;

                            if (world.IsInBounds(nx, ny) && !visited[ny, nx] && world.WorldTiles[ny, nx].Elevation < shallowWaterThreshold)
                            {
                                visited[ny, nx] = true;
                                queue.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                    isLargeEnough = currentBody.Count >= minLakeSize;

                    if (isOceanConnected || isLargeEnough)
                    {
                        foreach (Vector2Int waterTile in currentBody)
                        {
                            if (!isValidWaterEndpoint[waterTile.y, waterTile.x])
                            {
                                isValidWaterEndpoint[waterTile.y, waterTile.x] = true;
                                validEndpointCount++;
                            }
                        }
                    }
                }
            }
        }
        Debug.Log($"Step 4.1: Valid water bodies identified. Total valid endpoint tiles: {validEndpointCount}");
        if (validEndpointCount == 0 && pruneInlandRivers) // prune 옵션 켜져 있을 때만 경고
        {
            Debug.LogWarning(" Found 0 valid endpoints for pruning. All inland rivers might be removed if 'Prune Inland Rivers' is enabled.");
        }
    }

    /// <summary>
    /// **복원됨:** 유효한 수역에 도달하지 못하는 강 네트워크를 제거합니다.
    /// </summary>
    private void ValidateAndPruneRivers(WorldData world)
    {
        if (!pruneInlandRivers || isValidWaterEndpoint == null) return; // 옵션 꺼져있거나 맵 없으면 실행 안 함

        Debug.Log("Step 4.3: Validating and Pruning Rivers not reaching valid water..."); // 단계 번호 조정
        bool[,] visitedRiver = new bool[world.Height, world.Width];
        int riversPrunedCount = 0;
        int tilesPrunedCount = 0;

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (world.WorldTiles[y, x].IsRiver && !visitedRiver[y, x])
                {
                    List<Vector2Int> currentRiverNetworkTiles = new List<Vector2Int>();
                    HashSet<Vector2Int> bfsVisited = new HashSet<Vector2Int>();
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    bool isConnectedToValidWater = false;

                    Vector2Int startPos = new Vector2Int(x, y);
                    queue.Enqueue(startPos);
                    visitedRiver[y, x] = true;
                    bfsVisited.Add(startPos);

                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue();
                        if (world.WorldTiles[current.y, current.x].IsRiver)
                        {
                            currentRiverNetworkTiles.Add(current);
                        }

                        foreach (Vector2Int offset in GetNeighborOffsets()) // 상하좌우만 검사
                        {
                            int nx = current.x + offset.x;
                            int ny = current.y + offset.y;
                            Vector2Int neighborPos = new Vector2Int(nx, ny);

                            if (world.IsInBounds(nx, ny) && !bfsVisited.Contains(neighborPos))
                            {
                                // 유효 하구인지 확인
                                if (isValidWaterEndpoint[ny, nx])
                                {
                                    isConnectedToValidWater = true;
                                }

                                // 강 또는 물 타일이면 계속 탐색
                                bool isNeighborWater = world.WorldTiles[ny, nx].Elevation < shallowWaterThreshold;
                                bool isNeighborRiver = world.WorldTiles[ny, nx].IsRiver;

                                if (isNeighborRiver || isNeighborWater)
                                {
                                    bfsVisited.Add(neighborPos);
                                    queue.Enqueue(neighborPos);
                                    if (isNeighborRiver)
                                    {
                                        visitedRiver[ny, nx] = true;
                                    }
                                }
                            }
                        }
                    } // BFS 종료

                    if (!isConnectedToValidWater)
                    {
                        riversPrunedCount++;
                        tilesPrunedCount += currentRiverNetworkTiles.Count;
                        foreach (Vector2Int riverTile in currentRiverNetworkTiles)
                        {
                            world.WorldTiles[riverTile.y, riverTile.x].IsRiver = false;
                        }
                    }
                }
            }
        }
        Debug.Log($"Step 4.3: Pruned {riversPrunedCount} river networks not reaching valid water ({tilesPrunedCount} river tiles removed).");
    }


    /// <summary>
    /// 리스트의 요소 순서를 무작위로 섞습니다.
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = worldRandom.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    /// <summary>
    /// 주어진 좌표의 8방향 인접 타일 좌표 리스트를 반환합니다.
    /// </summary>
    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0 && j == 0) continue;
                neighbors.Add(new Vector2Int(pos.x + i, pos.y + j));
            }
        }
        return neighbors;
    }


    // IsNearExistingRiver 함수는 유량 집적 방식에서는 필요 없으므로 제거

    #endregion

    #region Optional Step 4.5: Erode River Banks

    /// <summary>
    /// 생성된 강 주변의 육지 타일 고도를 낮추어 강둑 침식을 시뮬레이션합니다.
    /// **수정됨:** 유량 기반 침식 로직 복원 (flowAccumulationMap 사용)
    /// </summary>
    private void ErodeRiverBanks(WorldData world)
    {
        if (!enableBankErosion) return;

        Debug.Log("Step 4.5: Eroding river banks (Optional)...");
        float[,] elevationChanges = new float[world.Height, world.Width];
        int erodedBankTiles = 0;

        // 최대 유량 값 찾기 (flowAccumulationMap 사용)
        float maxAccumulation = 1f;
        if (flowBankErosionMultiplier > 0 && flowAccumulationMap != null)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    if (flowAccumulationMap[y, x] > maxAccumulation)
                    {
                        maxAccumulation = flowAccumulationMap[y, x];
                    }
                }
            }
        }
        if (maxAccumulation <= 1f) maxAccumulation = 1f;

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (world.WorldTiles[y, x].IsLand(beachThreshold) && !world.WorldTiles[y, x].IsRiver)
                {
                    bool nearRiver = false;
                    float maxAdjacentFlowRatio = 0f; // 인접 강 최대 유량 비율

                    for (int j = -bankErosionRadius; j <= bankErosionRadius; j++)
                    {
                        for (int i = -bankErosionRadius; i <= bankErosionRadius; i++)
                        {
                            if (i == 0 && j == 0) continue;
                            int nx = x + i;
                            int ny = y + j;
                            if (world.IsInBounds(nx, ny) && world.WorldTiles[ny, nx].IsRiver)
                            {
                                nearRiver = true;
                                // 인접 강 유량 비율 계산 및 최대값 갱신
                                if (flowAccumulationMap != null)
                                {
                                    float neighborFlowRatio = Mathf.Clamp01(flowAccumulationMap[ny, nx] / maxAccumulation);
                                    if (neighborFlowRatio > maxAdjacentFlowRatio)
                                    {
                                        maxAdjacentFlowRatio = neighborFlowRatio;
                                    }
                                }
                                else { maxAdjacentFlowRatio = Mathf.Max(maxAdjacentFlowRatio, 0.5f); } // 유량 맵 없으면 기본값
                                // break; // 가장 가까운 강 하나만 찾으면 멈출 수도 있음
                            }
                        }
                        // if (nearRiver) break;
                    }

                    if (nearRiver)
                    {
                        // 유량 기반 침식 깊이 계산
                        float erosionDepth = baseBankErosionDepth + (maxAdjacentFlowRatio * flowBankErosionMultiplier);
                        elevationChanges[y, x] = -erosionDepth;
                        erodedBankTiles++;
                    }
                }
            }
        }

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (elevationChanges[y, x] < 0)
                {
                    world.WorldTiles[y, x].Elevation = Mathf.Max(shallowWaterThreshold + 0.01f, world.WorldTiles[y, x].Elevation + elevationChanges[y, x]);
                }
            }
        }
        Debug.Log($"Step 4.5: Eroded {erodedBankTiles} river bank tiles.");
    }

    #endregion

} // WorldGenerator 클래스 종료

// --- 필요한 데이터 클래스 (WorldData, SimplifiedWorldData, WorldTile, SimplifiedWorldTile, BiomeType 등) ---
// 이 클래스들은 별도의 파일에 정의되어 있거나 이 파일 하단에 포함되어 있어야 합니다.
// 예시:
/*
public class WorldData {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public WorldTile[,] WorldTiles { get; private set; }

    public WorldData(int width, int height) {
        Width = width;
        Height = height;
        WorldTiles = new WorldTile[height, width];
        // 타일 객체 초기화 필요
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                WorldTiles[y, x] = new WorldTile();
            }
        }
    }

    public bool IsInBounds(int x, int y) {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
}

public class WorldTile {
    public float Elevation;    // 0-1
    public float Temperature;  // 0-1
    public float Rainfall;     // 0-1
    public BiomeType Biome;
    public bool IsRiver;
    public bool IsRoad;
    public bool HasCity;
    public bool HasTown;
    public bool HasDungeon;

    public bool IsLand(beachThreshold) {
        // 예시: 해변 임계값 이상이면 육지로 간주 (실제 임계값 변수 사용 필요)
        return Elevation >= 0.35f; // beachThreshold 값 사용 권장
    }
    // HasPOI() 메소드 등 추가 가능
}

public class SimplifiedWorldData {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public SimplifiedWorldTile[,] SimplifiedTiles { get; private set; }

    public SimplifiedWorldData(int width, int height) {
        Width = width;
        Height = height;
        SimplifiedTiles = new SimplifiedWorldTile[height, width];
    }
}

public struct SimplifiedWorldTile {
    public BiomeType DominantBiome;
    public bool HasPoi;
    public bool IsMostlyWater;

    public SimplifiedWorldTile(BiomeType biome, bool poi, bool water) {
        DominantBiome = biome;
        HasPoi = poi;
        IsMostlyWater = water;
    }
}

public enum BiomeType {
    // 수역
    DeepWater, ShallowWater, Beach, River, // River 추가
    // 육지 (예시, 필요에 따라 추가/수정)
    PolarIce, Tundra, MountainTundra, HighMountain, Mountainous, AlpineForest,
    Taiga, ColdParklands, Steppe,
    TemperateRainforest, TemperateDeciduousForest, TemperateMixedForest, TemperateGrassland, Shrubland, Plains, Hills, // Plains, Hills 추가
    SubtropicalMoistForest, SubtropicalDryForest, Mediterranean, SubtropicalGrassland,
    TropicalRainforest, TropicalMoistForest, TropicalGrassland, TropicalDryForest, Desert,
    Wetlands, Riparian, // Riparian 추가
    // POI (시각화용)
    City, Town, DungeonEntrance
}

// WorldMapTextMeshVisualizer, SimplifiedWorldMapVisualizer 클래스는 별도 정의 필요
public class WorldMapTextMeshVisualizer : MonoBehaviour { public void Visualize(WorldData data) {} }
public class SimplifiedWorldMapVisualizer : MonoBehaviour { public void Visualize(SimplifiedWorldData data) {} }

*/

