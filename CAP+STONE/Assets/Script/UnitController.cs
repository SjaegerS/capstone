using UnityEngine;
using System.Collections;

public class UnitController : MonoBehaviour
{
    [Header("HP Bar Prefab")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private bool useWorldHealthBar = true;

    [Header("Battle Visuals")]
    [SerializeField] private float attackLungeDistance = 0.35f;
    [SerializeField] private float attackLungeDuration = 0.08f;
    [SerializeField] private float attackReturnDuration = 0.10f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);

    public CharacterStats Stats { get; private set; }
    public bool IsDead => isDead;

    public System.Action OnDeath;
    public event System.Action<float, float> OnHpChanged;

    public int CurrentHp
    {
        get
        {
            if (Stats == null)
                return 0;

            return Mathf.CeilToInt(Stats.CurrentHP);
        }
    }

    public int MaxHp
    {
        get
        {
            if (Stats == null)
                return 1;

            return Mathf.CeilToInt(Stats.MaxHP);
        }
    }

    private UnitController targetUnit;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private HealthBar healthBar;

    private Coroutine moveRoutine;
    private Coroutine attackRoutine;
    private Coroutine hitFlashRoutine;

    private Color originalColor = Color.white;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void NotifyHpChanged()
    {
        if (Stats == null)
            return;

        OnHpChanged?.Invoke(Stats.CurrentHP, Stats.MaxHP);
    }

    public void SetWorldHealthBarEnabled(bool enabled)
    {
        useWorldHealthBar = enabled;

        if (!useWorldHealthBar && healthBar != null)
        {
            Destroy(healthBar.gameObject);
            healthBar = null;
        }
    }

    public void Initialize(CharacterStats stats, Vector3 spawnPosition)
    {
        if (stats == null)
        {
            Debug.LogError($"[UnitController] {gameObject.name}: Initialize에 null Stats 전달됨");
            return;
        }

        Stats = stats;

        if (useWorldHealthBar && healthBar == null && healthBarPrefab != null)
        {
            GameObject barObj = Instantiate(healthBarPrefab);
            healthBar = barObj.GetComponent<HealthBar>();

            if (healthBar != null)
            {
                healthBar.Setup(transform);
            }
        }

        ResetVisuals(spawnPosition);
        NotifyHpChanged();
    }

    public void ApplyStats(CharacterStats stats)
    {
        if (stats == null)
            return;

        float hpRatio = 1f;

        if (Stats != null && Stats.MaxHP > 0f)
        {
            hpRatio = Mathf.Clamp01(Stats.CurrentHP / Stats.MaxHP);
        }

        stats.CurrentHP = Mathf.Clamp(stats.MaxHP * hpRatio, 0f, stats.MaxHP);
        Stats = stats;

        if (healthBar != null)
        {
            healthBar.SetHP(Stats.CurrentHP, Stats.MaxHP);
        }
        NotifyHpChanged();
    }

    public void Revive(Vector3 spawnPosition)
    {
        StopAllCoroutines();

        moveRoutine = null;
        attackRoutine = null;
        hitFlashRoutine = null;

        if (Stats != null)
        {
            Stats.CurrentHP = Stats.MaxHP;
        }

        OnDeath = null;

        ResetVisuals(spawnPosition);
        NotifyHpChanged();
    }

    private void ResetVisuals(Vector3 spawnPosition)
    {
        gameObject.SetActive(true);
        transform.position = spawnPosition;
        isDead = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        if (healthBar != null && Stats != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.SetVisible(true);
            healthBar.SetHP(Stats.CurrentHP, Stats.MaxHP);
        }
    }

    public void SetTarget(UnitController target)
    {
        targetUnit = target;
    }

    public void MoveTo(Vector3 destination)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(destination));
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        if (Stats == null)
            yield break;

        while (!isDead && Vector3.Distance(transform.position, destination) > 0.1f)
        {
            float moveSpeed = Mathf.Max(1f, Stats.MoveSpeed);

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        moveRoutine = null;
    }

    public void StartCombat()
    {
        if (isDead || targetUnit == null)
        {
            Debug.LogWarning(
                $"[UnitController] StartCombat 실패 | unit: {gameObject.name}, " +
                $"isDead: {isDead}, target: {(targetUnit != null ? targetUnit.gameObject.name : "null")}"
            );

            return;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    public void StopCombat()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        targetUnit = null;
    }

    private IEnumerator AttackRoutine()
    {
        if (Stats == null)
            yield break;

        float interval = Stats.AttackSpeed > 0f ? 1f / Stats.AttackSpeed : 1f;

        yield return new WaitForSeconds(interval);

        while (!isDead && targetUnit != null && !targetUnit.IsDead)
        {
            yield return StartCoroutine(PlayAttackVisual());

            if (targetUnit == null || targetUnit.IsDead)
                break;

            float damage = Mathf.Max(
                1f,
                GameBalance.CalculateDamage(Stats.AttackDamage, targetUnit.Stats.Defense)
            );

            targetUnit.TakeDamage(damage);

            yield return new WaitForSeconds(interval);
        }

        attackRoutine = null;
    }

    private IEnumerator PlayAttackVisual()
    {
        if (targetUnit == null)
            yield break;

        Vector3 startPosition = transform.position;

        Vector3 direction = (targetUnit.transform.position - transform.position).normalized;

        if (direction == Vector3.zero)
        {
            direction = Vector3.right;
        }

        Vector3 lungePosition = startPosition + direction * attackLungeDistance;

        yield return MoveVisual(startPosition, lungePosition, attackLungeDuration);
        yield return MoveVisual(lungePosition, startPosition, attackReturnDuration);
    }

    private IEnumerator MoveVisual(Vector3 from, Vector3 to, float duration)
    {
        float safeDuration = Mathf.Max(duration, 0.01f);

        for (float elapsed = 0f; elapsed < safeDuration; elapsed += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, elapsed / safeDuration);
            yield return null;
        }

        transform.position = to;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || Stats == null)
            return;

        Stats.CurrentHP -= damage;
        Stats.CurrentHP = Mathf.Max(0f, Stats.CurrentHP);

        if (healthBar != null)
        {
            healthBar.SetHP(Stats.CurrentHP, Stats.MaxHP);
        }

        NotifyHpChanged();

        if (Stats.CurrentHP > 0f)
        {
            PlayHitFlash();
        }
        else
        {
            Die();
        }
    }

    private void PlayHitFlash()
    {
        if (spriteRenderer == null)
            return;

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color = hitFlashColor;

        yield return new WaitForSeconds(0.08f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        hitFlashRoutine = null;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

         if (Stats != null)
        {
            Stats.CurrentHP = 0f;
            NotifyHpChanged();
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            spriteRenderer.color = originalColor;
        }

        if (healthBar != null)
        {
            healthBar.SetVisible(false);
        }

        OnDeath?.Invoke();
    }

    private void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
            healthBar = null;
        }
    }
}