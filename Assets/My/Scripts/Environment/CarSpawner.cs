using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 도로에 자동차를 주기적으로 스폰해 스폰 포인트의 forward 방향으로 travelDistance 만큼
// 직선 주행시킨 뒤 소멸. 주행 중 DOTween 셰이크로 엔진 덜덜거림을 표현한다.
// 씬에 빈 GameObject 를 두고 붙인 뒤, 도로 양 끝에 스폰 포인트(빈 GameObject)를 만들어
// 파란 축(Z/forward)을 주행 방향으로 돌려서 spawnPoints 에 연결한다.
public class CarSpawner : MonoBehaviour
{
    [Header("스폰 대상")]
    [Tooltip("스폰할 자동차 프리팹들. 매 스폰마다 랜덤 선택")]
    [SerializeField] private GameObject[] carPrefabs;

    [Tooltip("스폰 포인트. 각 포인트의 forward(파란 축) 방향으로 주행. 여러 개 가능")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("주행")]
    [Tooltip("스폰 지점에서 전진할 거리 (예: z -200 → 200 이면 400)")]
    [SerializeField] private float travelDistance = 400f;

    [Tooltip("주행 속도 (units/sec). 이동 시간 = travelDistance / speed")]
    [SerializeField] private float speed = 12f;

    [Header("스폰 주기")]
    [Tooltip("다음 스폰까지 랜덤 대기 시간 (min, max) 초")]
    [SerializeField] private Vector2 spawnInterval = new Vector2(4f, 10f);

    [Tooltip("동시에 존재할 수 있는 자동차 최대 수")]
    [SerializeField] private int maxAlive = 6;

    [Tooltip("Start 시 자동으로 스폰 루프 시작 (DayPhaseManager 없을 때 폴백)")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("이 시간대에만 스폰. 저녁·새벽엔 도로가 조용하도록 비워둠")]
    [SerializeField] private DayPhase[] activePhases = { DayPhase.Morning, DayPhase.Noon };

    [Header("엔진 진동")]
    [SerializeField] private float shakePositionStrength = 0.02f;
    [SerializeField] private float shakeRotationStrength = 0.4f;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Coroutine loop;

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
        {
            DayPhaseManager.Instance.OnPhaseChanged += ApplyPhase;
            ApplyPhase(DayPhaseManager.Instance.Current);
        }
        else if (autoStart) StartSpawning();
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= ApplyPhase;
    }

    // 낮(activePhases)엔 스폰, 저녁·새벽엔 멈춤. 도로에 있던 차는 트윈 끝나면 자연 소멸.
    private void ApplyPhase(DayPhase phase)
    {
        if (System.Array.IndexOf(activePhases, phase) >= 0) StartSpawning();
        else StopSpawning();
    }

    public void StartSpawning()
    {
        if (loop != null) return;
        if (carPrefabs == null || carPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[CarSpawner] carPrefabs 또는 spawnPoints 가 비어 있음 — 스폰 안 함", this);
            return;
        }
        loop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    // 런타임에 스폰 주기를 조절. 다음 대기부터 반영된다.
    public void SetSpawnInterval(float min, float max)
    {
        spawnInterval = new Vector2(Mathf.Max(0f, min), Mathf.Max(min, max));
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnInterval.x, spawnInterval.y));

            alive.RemoveAll(c => c == null);
            if (alive.Count >= maxAlive) continue;

            Spawn();
        }
    }

    private void Spawn()
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        if (point == null || prefab == null) return;

        // 이동(홀더)과 진동(차체)을 분리해 트윈이 서로 덮어쓰지 않게 한다.
        var holder = new GameObject("Car (spawned)");
        holder.transform.SetPositionAndRotation(point.position, point.rotation);

        GameObject car = Instantiate(prefab, holder.transform);
        car.transform.localPosition = Vector3.zero;
        car.transform.localRotation = Quaternion.identity;
        alive.Add(holder);

        Vector3 target = point.position + point.forward * travelDistance;
        float duration = speed > 0.01f ? travelDistance / speed : 1f;

        holder.transform.DOMove(target, duration)
            .SetEase(Ease.Linear)
            .SetLink(holder)
            .OnComplete(() => Destroy(holder));

        car.transform.DOShakePosition(1f, shakePositionStrength, 28, 90f, false, false)
            .SetLoops(-1)
            .SetLink(car);

        car.transform.DOShakeRotation(1f, shakeRotationStrength, 20, 90f, false)
            .SetLoops(-1)
            .SetLink(car);
    }
}
