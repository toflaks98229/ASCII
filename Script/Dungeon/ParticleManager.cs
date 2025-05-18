using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For ToList()

/// <summary>
/// 파티클 객체의 생성, 업데이트, 풀링 및 활성 파티클 목록 제공을 담당합니다.
/// MonoBehaviour로 구현되어 게임 루프 내에서 업데이트됩니다.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Tooltip("파티클 풀의 초기 크기")]
    [SerializeField] private int initialPoolSize = 100;
    [Tooltip("파티클 풀의 최대 크기. 0이면 무제한 (권장하지 않음)")]
    [SerializeField] private int maxPoolSize = 500;

    private List<Particle> particlePool;
    private List<Particle> activeParticles;

    // 이펙트 인스턴스 관리를 위한 리스트 (선택적 확장)
    // private List<ActiveEffectInstance> activeEffectInstances;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환 시 유지하려면
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
        activeParticles = new List<Particle>();
        // activeEffectInstances = new List<ActiveEffectInstance>();
    }

    private void InitializePool()
    {
        particlePool = new List<Particle>(initialPoolSize);
        for (int i = 0; i < initialPoolSize; i++)
        {
            particlePool.Add(new Particle());
        }
    }

    /// <summary>
    /// 파티클 풀에서 비활성 파티클을 가져오거나, 풀이 비었으면 새로 생성합니다.
    /// </summary>
    private Particle GetPooledParticle()
    {
        foreach (Particle p in particlePool)
        {
            if (!p.IsActive)
            {
                return p;
            }
        }

        // 풀에 가용 파티클이 없고, 최대 크기에 도달하지 않았다면 새로 생성
        if (maxPoolSize == 0 || particlePool.Count < maxPoolSize)
        {
            Particle p = new Particle();
            particlePool.Add(p);
            return p;
        }

        Debug.LogWarning("Particle pool exhausted and max size reached. Cannot spawn more particles.");
        return null; // 풀이 가득 찼으면 null 반환
    }


    /// <summary>
    /// 지정된 속성으로 파티클을 생성하고 활성화합니다.
    /// 실제 파티클 생성 로직은 이미터 타입, 파티클 속성 등을 고려하여 더 복잡해집니다.
    /// 이 메서드는 EffectSequencer 등에서 호출됩니다.
    /// </summary>
    public Particle SpawnParticle(
        Vector2 startPosition,
        SpellAnimationData.ParticlePropertyData particleProps,
        SpellAnimationData.EmitterPropertyData emitterProps)
    {
        Particle particle = GetPooledParticle();
        if (particle != null)
        {
            // 초기 속도 랜덤화
            float initialSpeedX = Random.Range(particleProps.InitialVelocityMin.x, particleProps.InitialVelocityMax.x);
            float initialSpeedY = Random.Range(particleProps.InitialVelocityMin.y, particleProps.InitialVelocityMax.y);
            Vector2 initialVelocity = new Vector2(initialSpeedX, initialSpeedY);

            // 이미터 방향 및 각도 적용 (간단화된 예시)
            if (emitterProps.EmitterShape == SpellAnimationData.EmitterPropertyData.Shape.Point || emitterProps.EmissionAngleSpread == 0)
            {
                initialVelocity = emitterProps.EmissionDirection.normalized * initialVelocity.magnitude;
            }
            else if (emitterProps.EmissionAngleSpread > 0)
            {
                float angle = Random.Range(-emitterProps.EmissionAngleSpread / 2f, emitterProps.EmissionAngleSpread / 2f);
                // EmissionDirection을 기준으로 회전. Vector2.up이 (0,1)이라고 가정.
                // 실제로는 EmissionDirection의 각도를 구하고 거기에 angle을 더해야 함.
                // 여기서는 간단히 Vector2.up을 기준으로 회전시킨 후 EmissionDirection 방향으로 조정하는 방식을 사용하거나,
                // 또는 initialVelocity 자체를 랜덤 방향으로 설정 (원형 방출 등)
                if (emitterProps.EmissionDirection == Vector2.zero) // 무작위 방향
                {
                    initialVelocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * initialVelocity.magnitude;
                }
                else // 지정된 방향 기준으로 분산
                {
                    float baseAngle = Mathf.Atan2(emitterProps.EmissionDirection.y, emitterProps.EmissionDirection.x) * Mathf.Rad2Deg;
                    float finalAngle = baseAngle + angle;
                    initialVelocity = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad)) * initialVelocity.magnitude;
                }
            }


            float lifetime = Random.Range(particleProps.LifetimeMin, particleProps.LifetimeMax);

            particle.Initialize(
                startPosition,
                initialVelocity,
                particleProps.Acceleration,
                lifetime,
                particleProps,
                emitterProps
            );

            if (!activeParticles.Contains(particle)) // 중복 추가 방지 (이론상 GetPooledParticle에서 처리)
            {
                activeParticles.Add(particle);
            }
            return particle;
        }
        return null;
    }

    void Update()
    {
        // 활성 파티클 업데이트
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            Particle particle = activeParticles[i];
            particle.Update(Time.deltaTime);
            if (!particle.IsActive)
            {
                activeParticles.RemoveAt(i);
                // 파티클 풀로 돌려보내는 로직은 GetPooledParticle에서 이미 처리됨 (IsActive 플래그 사용)
            }
        }

        // 활성 이펙트 인스턴스 업데이트 (선택적 확장)
        // foreach (ActiveEffectInstance effectInstance in activeEffectInstances)
        // {
        //     effectInstance.Update(Time.deltaTime);
        // }
        // activeEffectInstances.RemoveAll(inst => !inst.IsAlive);
    }

    /// <summary>
    /// 현재 활성화된 모든 파티클의 목록을 반환합니다.
    /// DungeonVisualizer에서 렌더링 시 사용합니다.
    /// </summary>
    public List<Particle> GetActiveParticles()
    {
        // 매번 새 리스트를 반환하여 외부에서의 변경 방지 (방어적 복사)
        // 성능이 중요하다면 readonly 컬렉션을 노출하거나 다른 방식 고려
        return activeParticles.ToList();
    }

    // --- 선택적 확장: 이펙트 인스턴스 관리 ---
    // public void PlayEffect(SpellAnimationData effectData, Vector2Int worldPosition)
    // {
    //     activeEffectInstances.Add(new ActiveEffectInstance(effectData, worldPosition, this));
    // }
}