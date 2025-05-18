using UnityEngine;
using System.Collections;
using System.Collections.Generic; // List 사용
using System; // Action 사용

/// <summary>
/// SpellAnimationData를 기반으로 복잡한 시각적 이펙트 시퀀스를 재생합니다.
/// ParticleManager를 사용하여 파티클을 생성하고 관리합니다.
/// </summary>
public class EffectSequencer : MonoBehaviour
{
    public static EffectSequencer Instance { get; private set; }

    private ParticleManager particleManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        particleManager = ParticleManager.Instance;
        if (particleManager == null)
        {
            Debug.LogError("EffectSequencer: ParticleManager 인스턴스를 찾을 수 없습니다!");
            this.enabled = false;
        }
    }

    /// <summary>
    /// 지정된 SpellAnimationData를 사용하여 이펙트 시퀀스를 재생합니다.
    /// </summary>
    public void PlayEffect(SpellAnimationData effectData, Vector2Int originWorldPos, Action onComplete = null)
    {
        StartCoroutine(EffectCoroutine(effectData, originWorldPos, onComplete));
    }

    private IEnumerator EffectCoroutine(SpellAnimationData effectData, Vector2Int effectOriginWorldPos, Action onComplete)
    {
        float elapsedTime = 0f;
        float emitterElapsedDuration = 0f;
        int burstsDone = 0;
        float timeSinceLastBurst = 0f;
        float timeSinceLastEmission = 0f;

        if (effectData.Emitter.InitialDelay > 0)
        {
            yield return new WaitForSeconds(effectData.Emitter.InitialDelay);
            elapsedTime += effectData.Emitter.InitialDelay;
        }

        bool isEmitterFinished = false;

        while (!isEmitterFinished && (effectData.MaxEffectDuration == 0 || elapsedTime < effectData.MaxEffectDuration))
        {
            bool canEmitterBeActive = effectData.Emitter.Duration == 0 || emitterElapsedDuration < effectData.Emitter.Duration;
            bool emitterActiveThisFrame = false;

            if (canEmitterBeActive)
            {
                // Burst 처리
                if (effectData.Emitter.BurstCount > 0 &&
                    (effectData.Emitter.TotalBursts == 0 || burstsDone < effectData.Emitter.TotalBursts))
                {
                    if (burstsDone == 0 || (effectData.Emitter.BurstInterval > 0 && timeSinceLastBurst >= effectData.Emitter.BurstInterval))
                    {
                        for (int i = 0; i < effectData.Emitter.BurstCount; i++)
                        {
                            SpawnParticleFromEmitter(effectData.ParticleProps, effectData.Emitter, effectOriginWorldPos);
                        }
                        burstsDone++;
                        timeSinceLastBurst = 0f;
                        emitterActiveThisFrame = true;
                    }
                }

                // 지속 방출 (EmissionRate) 처리
                if (effectData.Emitter.EmissionRate > 0)
                {
                    timeSinceLastEmission += Time.deltaTime;
                    float emissionInterval = 1.0f / effectData.Emitter.EmissionRate;
                    while (timeSinceLastEmission >= emissionInterval)
                    {
                        SpawnParticleFromEmitter(effectData.ParticleProps, effectData.Emitter, effectOriginWorldPos);
                        timeSinceLastEmission -= emissionInterval;
                        emitterActiveThisFrame = true; // 파티클 방출 시 이미터 활성으로 간주
                    }
                }

                if (emitterActiveThisFrame) // 이번 프레임에 버스트 또는 지속 방출이 있었다면
                {
                    emitterElapsedDuration += Time.deltaTime; // 실제 활동 시간에만 경과 시간 추가
                }
                timeSinceLastBurst += Time.deltaTime; // 버스트 간격 타이머는 항상 증가
            }


            // 이미터 종료 조건 확인
            bool allBurstsDone = (effectData.Emitter.TotalBursts > 0 && burstsDone >= effectData.Emitter.TotalBursts);
            bool durationReached = (effectData.Emitter.Duration > 0 && emitterElapsedDuration >= effectData.Emitter.Duration);

            if (allBurstsDone && effectData.Emitter.EmissionRate == 0) // 버스트만 있고 다 쐈으면 종료
            {
                isEmitterFinished = true;
            }
            else if (durationReached && effectData.Emitter.BurstCount == 0) // 지속 방출만 있고 시간 다 됐으면 종료
            {
                isEmitterFinished = true;
            }
            else if (allBurstsDone && durationReached) // 둘 다 있고 둘 다 끝났으면 종료
            {
                isEmitterFinished = true;
            }


            elapsedTime += Time.deltaTime;
            if (effectData.MaxEffectDuration > 0 && elapsedTime >= effectData.MaxEffectDuration)
            {
                isEmitterFinished = true; // 이펙트 최대 시간 도달 시 강제 종료
            }

            yield return null;
        }

        // 레거시 ImpactFlash 처리 (새 시스템으로 통합 고려)
        if (effectData.LegacyImpactFlashChar != default(char) && effectData.LegacyImpactFlashDuration > 0)
        {
            SpellAnimationData.ParticlePropertyData flashProps = new SpellAnimationData.ParticlePropertyData(
                effectData.LegacyImpactFlashChar,
                effectData.LegacyImpactFlashColor,
                effectData.LegacyImpactFlashDuration,
                effectData.LegacyImpactFlashDuration
            );
            // ImpactFlash는 특정 지점에 단일 파티클로 표현
            Vector2Int impactPosition = effectOriginWorldPos; // 기본적으로 이펙트 원점
            // 만약 SpellAnimationData에 EndPosition 같은 필드가 있다면 그 위치를 사용
            // if (effectData.EndPosition != null) impactPosition = effectData.EndPosition;

            SpellAnimationData.EmitterPropertyData flashEmitter = new SpellAnimationData.EmitterPropertyData(
                SpellAnimationData.EmitterPropertyData.Shape.Point,
                0 // 지속시간 0 (즉시 폭발)
            )
            {
                StartPosition = impactPosition,
                BurstCount = 1,
                TotalBursts = 1
            };
            particleManager.SpawnParticle(impactPosition, flashProps, flashEmitter);
            // yield return new WaitForSeconds(effectData.LegacyImpactFlashDuration); // 파티클 수명에 맡김
        }

        // 모든 파티클이 사라질 때까지 기다리는 로직 (선택적, 매우 중요)
        // 현재는 onComplete가 즉시 호출될 수 있음.
        // 좀 더 정교하게 하려면, 이 이펙트에서 생성된 파티클들이 모두 사라질 때까지 기다려야 함.
        // 이를 위해서는 ParticleManager에서 특정 이펙트 ID로 생성된 파티클을 추적하거나,
        // EffectSequencer가 자신이 생성한 파티클들을 추적하고 모두 IsActive = false가 될 때까지 기다려야 함.
        // 간단한 예시: 약간의 추가 시간 대기 (모든 파티클이 사라질 충분한 시간이라고 가정)
        // float maxParticleLifetime = effectData.ParticleProps.LifetimeMax; // 실제로는 더 복잡
        // yield return new WaitForSeconds(maxParticleLifetime);


        onComplete?.Invoke();
    }

    private void SpawnParticleFromEmitter(SpellAnimationData.ParticlePropertyData particleProps, SpellAnimationData.EmitterPropertyData emitterProps, Vector2Int effectOriginWorldPos)
    {
        // 이미터의 StartPosition은 이펙트 원점(effectOriginWorldPos)에 대한 상대 위치일 수도,
        // 또는 이미 월드 좌표일 수도 있습니다. 여기서는 emitterProps.StartPosition이
        // 이펙트의 주 시작점(월드 좌표)을 기준으로 한 상대 오프셋이거나,
        // 아니면 effectOriginWorldPos를 무시하고 사용될 절대 좌표라고 가정합니다.
        // 가장 간단한 경우는 emitterProps.StartPosition이 (0,0)이고, effectOriginWorldPos를 사용하는 것입니다.
        // 여기서는 emitterProps.StartPosition을 기준으로 하고, effectOriginWorldPos는 보조적으로 사용합니다.

        Vector2 baseSpawnPos = emitterProps.StartPosition; // 이미터 정의에 있는 시작 위치
                                                           // 만약 emitterProps.StartPosition이 (0,0)이고 항상 effectOriginWorldPos를 사용하고 싶다면:
                                                           // baseSpawnPos = effectOriginWorldPos;

        Vector2 finalSpawnPos = baseSpawnPos; // 최종 파티클 생성 위치

        switch (emitterProps.EmitterShape)
        {
            case SpellAnimationData.EmitterPropertyData.Shape.Point:
                // finalSpawnPos는 이미 baseSpawnPos로 설정됨
                break;
            case SpellAnimationData.EmitterPropertyData.Shape.Circle:
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = UnityEngine.Random.Range(0f, emitterProps.Size.x); // Size.x를 반지름으로 사용
                finalSpawnPos = baseSpawnPos + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                break;
            case SpellAnimationData.EmitterPropertyData.Shape.Rectangle:
                float halfWidth = emitterProps.Size.x / 2f;
                float halfHeight = emitterProps.Size.y / 2f;
                finalSpawnPos = baseSpawnPos + new Vector2(UnityEngine.Random.Range(-halfWidth, halfWidth), UnityEngine.Random.Range(-halfHeight, halfHeight));
                break;
            case SpellAnimationData.EmitterPropertyData.Shape.Line:
                float t = UnityEngine.Random.value;
                // Line은 StartPosition에서 EndPosition으로 이어지는 선. EndPosition도 EmitterPropertyData에 정의되어야 함.
                // 여기서는 StartPosition이 선의 중심이고 Size.x가 길이라고 가정하여 좌우로 랜덤하게 생성.
                // 또는 StartPosition과 EndPosition을 사용:
                // finalSpawnPos = Vector2.Lerp(emitterProps.StartPosition, emitterProps.EndPosition, t);
                finalSpawnPos = baseSpawnPos + new Vector2(UnityEngine.Random.Range(-emitterProps.Size.x / 2f, emitterProps.Size.x / 2f), 0); // 가로선 예시
                break;
            case SpellAnimationData.EmitterPropertyData.Shape.Cone:
                // 콘 형태는 초기 속도 방향을 EmissionDirection과 EmissionAngleSpread로 조절하고,
                // 생성 위치는 Point와 동일하게 baseSpawnPos (콘의 꼭지점)로 설정하는 것이 일반적입니다.
                finalSpawnPos = baseSpawnPos;
                break;
        }
        particleManager.SpawnParticle(finalSpawnPos, particleProps, emitterProps);
    }
}