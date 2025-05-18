// Particle.cs
using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 추가
using static SpellAnimationData;

/// <summary>
/// 개별 파티클의 상태와 속성을 나타내는 클래스입니다.
/// ParticleManager에 의해 관리됩니다.
/// </summary>
public class Particle
{
    // --- 기본 속성 ---
    public Vector2 Position;         // 현재 위치 (세부 좌표, 정수형 타일 좌표는 ParticleManager가 계산)
    public Vector2 Velocity;         // 현재 속도
    public Vector2 Acceleration;     // 가속도
    public float Lifetime;           // 남은 생명 시간 (초)
    public float Age;                // 현재 나이 (초)

    // --- 시각적 속성 ---
    public char CurrentChar;         // 현재 표시 문자
    public Color CurrentColor;       // 현재 전경색
    // public Color CurrentBackgroundColor; // 필요시 배경색 추가

    // --- 생명주기 변화 관련 속성 ---
    // 이 파티클이 생성될 때 참조한 원본 SpellAnimationData.ParticlePropertyData
    // 또는 개별적으로 설정된 값들을 가질 수 있습니다.
    // 여기서는 간단하게 유지하고, ParticleManager가 SpellAnimationData를 참조하여 처리하도록 합니다.
    public SpellAnimationData.ParticlePropertyData OriginalProperties { get; private set; }
    public SpellAnimationData.EmitterPropertyData EmitterProperties { get; private set; } // 어떤 이미터 설정으로 생성되었는지 참조

    // --- 상태 ---
    public bool IsActive;            // 파티클 활성화 여부 (풀링 시스템에서 사용)

    // --- 생명주기 애니메이션 인덱스 및 타이머 ---
    private int currentCharIndex;
    private float charChangeTimer;
    private int currentColorIndex;
    private float colorChangeTimer;

    /// <summary>
    /// 파티클을 초기화하고 활성화합니다.
    /// </summary>
    public void Initialize(
        Vector2 startPosition,
        Vector2 initialVelocity,
        Vector2 acceleration,
        float lifetime,
        SpellAnimationData.ParticlePropertyData properties,
        SpellAnimationData.EmitterPropertyData emitterProps)
    {
        Position = startPosition;
        Velocity = initialVelocity;
        Acceleration = acceleration;
        Lifetime = lifetime;
        Age = 0f;

        OriginalProperties = properties;
        EmitterProperties = emitterProps;

        IsActive = true;

        // 초기 문자 및 색상 설정
        currentCharIndex = 0;
        charChangeTimer = 0f;
        if (OriginalProperties.LifeCycleChars != null && OriginalProperties.LifeCycleChars.Length > 0)
        {
            CurrentChar = OriginalProperties.LifeCycleChars[0];
            if (OriginalProperties.LifeCycleCharDurations != null && OriginalProperties.LifeCycleCharDurations.Length > 0)
            {
                charChangeTimer = OriginalProperties.LifeCycleCharDurations[0];
            }
        }
        else
        {
            CurrentChar = OriginalProperties.BaseChar; // 기본 문자 사용
        }

        currentColorIndex = 0;
        colorChangeTimer = 0f;
        if (OriginalProperties.LifeCycleColors != null && OriginalProperties.LifeCycleColors.Length > 0)
        {
            CurrentColor = OriginalProperties.LifeCycleColors[0];
            if (OriginalProperties.LifeCycleColorDurations != null && OriginalProperties.LifeCycleColorDurations.Length > 0)
            {
                colorChangeTimer = OriginalProperties.LifeCycleColorDurations[0];
            }
        }
        else
        {
            CurrentColor = OriginalProperties.BaseColor; // 기본 색상 사용
        }
    }

    /// <summary>
    /// 매 프레임 파티클의 상태를 업데이트합니다.
    /// ParticleManager에 의해 호출됩니다.
    /// </summary>
    /// <param name="deltaTime">프레임 간 시간 간격</param>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        Age += deltaTime;
        Lifetime -= deltaTime;

        if (Lifetime <= 0)
        {
            IsActive = false;
            return;
        }

        // 물리 업데이트
        Velocity += Acceleration * deltaTime;
        Position += Velocity * deltaTime;

        // 생명주기에 따른 문자 변화
        if (OriginalProperties.LifeCycleChars != null && OriginalProperties.LifeCycleChars.Length > 0 &&
            OriginalProperties.LifeCycleCharDurations != null && OriginalProperties.LifeCycleCharDurations.Length > currentCharIndex)
        {
            charChangeTimer -= deltaTime;
            if (charChangeTimer <= 0)
            {
                currentCharIndex++;
                if (currentCharIndex < OriginalProperties.LifeCycleChars.Length)
                {
                    CurrentChar = OriginalProperties.LifeCycleChars[currentCharIndex];
                    // 다음 문자 변경까지의 시간 설정 (배열 범위 확인)
                    if (currentCharIndex < OriginalProperties.LifeCycleCharDurations.Length)
                    {
                        charChangeTimer = OriginalProperties.LifeCycleCharDurations[currentCharIndex];
                    }
                    else // 문자 지속시간 배열이 문자 배열보다 짧으면 마지막 지속시간을 사용하거나, 더 이상 변경 안 함
                    {
                        charChangeTimer = float.MaxValue; // 더 이상 변경 안 함
                    }
                }
                else // 모든 문자 시퀀스 재생 완료
                {
                    // 마지막 문자를 유지하거나, 다른 로직 (예: 소멸 문자)
                }
            }
        }

        // 생명주기에 따른 색상 변화
        if (OriginalProperties.LifeCycleColors != null && OriginalProperties.LifeCycleColors.Length > 0 &&
            OriginalProperties.LifeCycleColorDurations != null && OriginalProperties.LifeCycleColorDurations.Length > currentColorIndex)
        {
            colorChangeTimer -= deltaTime;
            if (colorChangeTimer <= 0)
            {
                currentColorIndex++;
                if (currentColorIndex < OriginalProperties.LifeCycleColors.Length)
                {
                    CurrentColor = OriginalProperties.LifeCycleColors[currentColorIndex];
                    if (currentColorIndex < OriginalProperties.LifeCycleColorDurations.Length)
                    {
                        colorChangeTimer = OriginalProperties.LifeCycleColorDurations[currentColorIndex];
                    }
                    else
                    {
                        colorChangeTimer = float.MaxValue;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 파티클을 비활성화하고 풀로 돌아갈 준비를 합니다.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Age = 0f;
        // 다른 속성들 초기화 (필요시)
    }
}