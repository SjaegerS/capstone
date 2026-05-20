using UnityEngine;
using System.Collections;

public class UnitController : MonoBehaviour
{
    [Header("HP Bar Prefab")]
    [SerializeField] private GameObject healthBarPrefab;

    public CharacterStats Stats { get; private set; }
    public bool IsDead => isDead;

    public System.Action OnDeath;

    private UnitController targetUnit;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private HealthBar healthBar;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(CharacterStats stats, Vector3 spawnPosition)
    {
        if (stats == null)
        {
            Debug.LogError($"[UnitController] {gameObject.name}: Initialize에 null Stats 전달됨");
            return;
        }
        Stats = stats;

        // 씬 루트에 독립 생성 → LateUpdate로 추적 (부모-자식 Canvas 충돌 방지)
        if (healthBar == null && healthBarPrefab != null)
        {
            GameObject barObj = Instantiate(healthBarPrefab);
            healthBar = barObj.GetComponent<HealthBar>();
            if (healthBar != null)
                healthBar.Setup(transform);
        }

        ResetVisuals(spawnPosition);
    }

    public void Revive(Vector3 spawnPosition)
    {
        StopAllCoroutines();
        if (Stats != null) Stats.CurrentHP = Stats.MaxHP;
        OnDeath = null;
        ResetVisuals(spawnPosition);
    }

    void ResetVisuals(Vector3 spawnPosition)
    {
        gameObject.SetActive(true);
        transform.position = spawnPosition;
        isDead = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.SetHP(Stats.CurrentHP, Stats.MaxHP);
        }
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
            float dmg = Mathf.Max(1f, GameBalance.CalculateDamage(Stats.AttackDamage, targetUnit.Stats.Defense));
            targetUnit.TakeDamage(dmg);
            yield return new WaitForSeconds(interval);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead || Stats == null) return;
        Stats.CurrentHP -= damage;
        healthBar?.SetHP(Stats.CurrentHP, Stats.MaxHP);
        if (Stats.CurrentHP <= 0f) Die();
    }

    // 유닛 파괴 시 씬 루트에 남아있는 HP바도 함께 제거
    void OnDestroy()
    {
        if (healthBar != null)
            Destroy(healthBar.gameObject);
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        healthBar?.SetVisible(false);
        OnDeath?.Invoke();
    }
}
