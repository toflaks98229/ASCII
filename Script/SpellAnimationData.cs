using UnityEngine;

/// <summary>
/// 파티클 이펙트의 생성 및 행동 방식에 대한 정의를 담는 구조체입니다.
/// ParticleManager가 이 데이터를 사용하여 파티클들을 절차적으로 생성하고 애니메이션합니다.
/// </summary>
[System.Serializable] // 인스펙터에서 직접 설정하거나, ScriptableObject로 만들어서 관리할 수도 있습니다.
public struct SpellAnimationData // 또는 ParticleEffectData로 클래스명 변경 고려
{
    /// <summary>
    /// 이펙트 내 개별 파티클의 기본 속성 및 생명주기 변화를 정의합니다.
    /// </summary>
    [System.Serializable]
    public struct ParticlePropertyData
    {
        [Header("기본 시각적 속성")]
        [Tooltip("파티클의 기본 문자. LifeCycleChars가 우선합니다.")]
        public char BaseChar;
        [Tooltip("파티클의 기본 색상. LifeCycleColors가 우선합니다.")]
        public Color BaseColor;

        [Header("생명주기: 문자 변화")]
        [Tooltip("시간에 따라 변할 문자 시퀀스")]
        public char[] LifeCycleChars;
        [Tooltip("각 문자가 지속될 시간 (LifeCycleChars와 개수 일치 필요)")]
        public float[] LifeCycleCharDurations;

        [Header("생명주기: 색상 변화")]
        [Tooltip("시간에 따라 변할 색상 시퀀스")]
        public Color[] LifeCycleColors;
        [Tooltip("각 색상이 지속될 시간 (LifeCycleColors와 개수 일치 필요)")]
        public float[] LifeCycleColorDurations;

        [Header("물리 및 수명")]
        [Tooltip("파티클 최소 수명 (초)")]
        public float LifetimeMin;
        [Tooltip("파티클 최대 수명 (초)")]
        public float LifetimeMax;

        [Tooltip("초기 속도 (최소 범위)")]
        public Vector2 InitialVelocityMin;
        [Tooltip("초기 속도 (최대 범위)")]
        public Vector2 InitialVelocityMax;
        [Tooltip("파티클 가속도")]
        public Vector2 Acceleration;
        // public float InitialRotationMin; // 필요시 회전 관련 속성 추가
        // public float InitialRotationMax;
        // public float AngularVelocityMin;
        // public float AngularVelocityMax;

        [Tooltip("외부 힘(예: 중력, 바람)의 영향을 받을지 여부")]
        public bool AffectedByExternalForces;

        // 생성자 (기본값 설정용)
        public ParticlePropertyData(char baseChar = '*', Color baseColor = default, float lifetimeMin = 1f, float lifetimeMax = 1f)
        {
            BaseChar = baseChar;
            BaseColor = (baseColor == default) ? Color.white : baseColor; // 기본 색상을 white로
            LifeCycleChars = null;
            LifeCycleCharDurations = null;
            LifeCycleColors = null;
            LifeCycleColorDurations = null;
            LifetimeMin = lifetimeMin;
            LifetimeMax = lifetimeMax;
            InitialVelocityMin = Vector2.zero;
            InitialVelocityMax = Vector2.zero;
            Acceleration = Vector2.zero;
            AffectedByExternalForces = false;
        }
    }

    /// <summary>
    /// 파티클 이미터(방출기)의 속성을 정의합니다.
    /// </summary>
    [System.Serializable]
    public struct EmitterPropertyData
    {
        public enum Shape { Point, Circle, Rectangle, Line, Cone }
        [Tooltip("이미터의 형태")]
        public Shape EmitterShape;
        [Tooltip("이미터의 크기 (형태에 따라 의미 변화: Point는 무시, Circle은 반지름, Rectangle은 너비/높이, Line은 길이)")]
        public Vector2 Size; // Line의 경우 x=길이, y=두께(0이면 한줄) / Cone의 경우 x=각도(도), y=반지름(길이)

        [Tooltip("이미터의 활성 지속 시간 (초). 0 이하면 무한 또는 BurstCount에 의존")]
        public float Duration;
        [Tooltip("초당 파티클 방출 수 (지속 방출 시)")]
        public int EmissionRate; // particles per second
        [Tooltip("한 번에 방출할 파티클 수 (단일 폭발 또는 주기적 폭발 시)")]
        public int BurstCount;
        [Tooltip("여러 번의 Burst가 있을 경우 각 Burst 간의 시간 간격 (초)")]
        public float BurstInterval;
        [Tooltip("총 Burst 횟수 (0이면 Duration에 따르거나 1회)")]
        public int TotalBursts;


        [Tooltip("파티클 방출 방향 (정규화된 벡터, Point 이미터의 경우 이 방향으로만 방출)")]
        public Vector2 EmissionDirection; // (0,0)이면 무작위 또는 Cone/Circle 내부
        [Tooltip("방출 각도 범위 (도, EmissionDirection을 중심으로 +- 값. 예: 30이면 EmissionDirection 기준 -15도 ~ +15도 범위)")]
        public float EmissionAngleSpread; // degrees

        [Tooltip("이미터의 시작 위치 (월드 좌표, 또는 부모 이펙트 기준 상대 좌표)")]
        public Vector2Int StartPosition; // 이펙트의 주 시작점
        [Tooltip("이미터의 끝 위치 (Line 이미터 등에 사용, StartPosition으로부터의 상대 위치 또는 월드 좌표)")]
        public Vector2Int EndPosition;   // Line 이미터의 끝점 또는 Cone의 방향벡터 끝점

        [Tooltip("이미터가 이펙트 시작 후 얼마나 있다가 활성화될지 (초)")]
        public float InitialDelay;


        // 생성자 (기본값 설정용)
        public EmitterPropertyData(Shape shape = Shape.Point, float duration = 1f)
        {
            EmitterShape = shape;
            Size = Vector2.one;
            Duration = duration;
            EmissionRate = 10;
            BurstCount = 0;
            BurstInterval = 0f;
            TotalBursts = 0;
            EmissionDirection = Vector2.up; // 기본 위쪽
            EmissionAngleSpread = 0f;
            StartPosition = Vector2Int.zero;
            EndPosition = Vector2Int.zero;
            InitialDelay = 0f;
        }
    }

    [Header("이펙트 전반 설정")]
    [Tooltip("이펙트의 전체 이름 또는 ID (디버깅 및 관리용)")]
    public string EffectName;
    [Tooltip("이펙트의 총 지속 시간 (초). 0이면 모든 이미터가 끝날 때까지. 모든 이미터가 무한이면 이펙트도 무한 지속될 수 있음.")]
    public float MaxEffectDuration;


    [Header("이미터 및 파티클 속성 목록")]
    // 하나의 이펙트가 여러 개의 이미터와 각각 다른 파티클 속성을 가질 수 있도록 리스트로 관리
    // 여기서는 간단히 하나의 이미터와 하나의 파티클 속성 세트만 사용하도록 구성.
    // 더 복잡한 시스템에서는 List<EmitterPropertyData>와 List<ParticlePropertyData>를 사용할 수 있습니다.
    [Tooltip("이 이펙트에 사용될 이미터 설정")]
    public EmitterPropertyData Emitter;
    [Tooltip("이 이미터에서 생성될 파티클의 속성")]
    public ParticlePropertyData ParticleProps;


    // --- 기존 SpellAnimationData의 레거시 또는 단순 효과 필드 (점진적으로 위 구조로 통합하거나 분리) ---
    // 이 부분은 새로운 파티클 시스템과 어떻게 통합할지, 또는 별도의 간단한 효과 처리기로 남길지 결정 필요.
    // 예를 들어, ImpactFlash는 매우 짧은 수명의 단일 파티클 방출로 Emitter/ParticleProps로 표현 가능.
    [Header("레거시/단순 효과 (통합 예정)")]
    public char LegacyImpactFlashChar;
    public Color LegacyImpactFlashColor;
    public float LegacyImpactFlashDuration;
    // ... 기타 기존 필드들 ...


    /// <summary>
    /// 기본값으로 SpellAnimationData를 생성합니다.
    /// </summary>
    public static SpellAnimationData CreateDefault(string name = "DefaultEffect")
    {
        return new SpellAnimationData
        {
            EffectName = name,
            MaxEffectDuration = 2f, // 예시: 기본 이펙트 최대 2초 지속
            Emitter = new EmitterPropertyData(EmitterPropertyData.Shape.Circle, 0.5f) // 0.5초간 원형 방출
            {
                Size = new Vector2(3, 0), // 반지름 3
                EmissionRate = 20,
                StartPosition = Vector2Int.zero // 이펙트 발생 위치가 될 것
            },
            ParticleProps = new ParticlePropertyData('+', Color.yellow, 0.5f, 1.0f)
            {
                InitialVelocityMin = new Vector2(-1, -1) * 5f, // 초당 5타일 속도
                InitialVelocityMax = new Vector2(1, 1) * 5f,
                Acceleration = new Vector2(0, 9.8f) // 간단한 중력 효과 (Y가 아래로 갈수록 증가하는 좌표계 기준)
            },

            // 레거시 필드 초기화 (필요시)
            LegacyImpactFlashChar = 'X',
            LegacyImpactFlashColor = Color.red,
            LegacyImpactFlashDuration = 0.1f
        };
    }
}