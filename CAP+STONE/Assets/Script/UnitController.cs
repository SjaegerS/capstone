using UnityEngine;
using System.Collections;

public class UnitController : MonoBehaviour
{
    public CharacterStats Stats { get; private set; }
    public bool IsDead => isDead;

    // BattleManager가 구독해 라운드 종료를 감지
    public System.Action OnDeath;

    private UnitController targetUnit;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private HealthBar healthBar;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthBar = GetComponent<HealthBar>();
        if (healthBar == null)
            healthBar = gameObject.AddComponent<HealthBar>();
    }

    // 최초 생성 시 호출
    public void Initialize(CharacterStats stats, Vector3 spawnPosition)
    {
        if (stats == null)
        {
            Debug.LogError($"[UnitController] {gameObject.name}: Initialize에 null Stats 전달됨");
            return;
        }
        Stats = stats;
        ResetVisuals(spawnPosition);
    }

    // 라운드 재시작 시 플레이어 재사용 (HP 풀 리셋 + 위치 복귀)
    public void Revive(Vector3 spawnPosition)
    {
        StopAllCoroutines();
        if (Stats != null) Stats.CurrentHP = Stats.MaxHP;
        OnDeath = null;
        ResetVisuals(spawnPosition);
    }

    void ResetVisuals(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        isDead = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        healthBar?.SetVisible(true);
        healthBar?.SetRatio(1f);
    }

    public void SetTarget(UnitController target) => targetUnit = target;

    public void MoveTo(Vector3 destination)
    {
        StartCoroutine(MoveRoutine(destination));
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        if (Stats == null) yield break;
        while (!isDead && Vector3.Distance(transform.position, destination) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, Stats.MoveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void StartCombat()
    {
        if (!isDead && targetUnit != null)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (Stats == null) yield break;
        float interval = Stats.AttackSpeed > 0f ? 1f / Stats.AttackSpeed : 1f;

        while (!isDead && targetUnit != null && !targetUnit.IsDead)
        {
            float dmg = Mathf.Max(1f, Stats.AttackDamage - targetUnit.Stats.Defense);
            targetUnit.TakeDamage(dmg);
            yield return new WaitForSeconds(interval);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead || Stats == null) return;
        Stats.CurrentHP -= damage;
        healthBar?.SetRatio(Stats.CurrentHP / Stats.MaxHP);
        if (Stats.CurrentHP <= 0f) Die();
    }

    private void Die()
    {
        isDead = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        healthBar?.SetVisible(false);
        OnDeath?.Invoke();
    }
}
