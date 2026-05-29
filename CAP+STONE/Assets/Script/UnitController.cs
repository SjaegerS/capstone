using UnityEngine;
using System.Collections;

public class UnitController : MonoBehaviour
{
    [Header("HP Bar Prefab")]
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Battle Visuals")]
    [SerializeField] private float attackLungeDistance = 0.35f;
    [SerializeField] private float attackLungeDuration = 0.08f;
    [SerializeField] private float attackReturnDuration = 0.10f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);

    public CharacterStats Stats { get; private set; }
    public bool IsDead => isDead;

    public System.Action OnDeath;

    private UnitController targetUnit;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private HealthBar healthBar;
    private Coroutine moveRoutine;
    private Coroutine attackRoutine;
    private Coroutine hitFlashRoutine;
    private Color originalColor = Color.white;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
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

    public void ApplyStats(CharacterStats stats)
    {
        if (stats == null)
        {
            return;
        }

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
    }

    public void Revive(Vector3 spawnPosition)
    {
        StopAllCoroutines();
        moveRoutine = null;
        attackRoutine = null;
        hitFlashRoutine = null;
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
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        SetAnimatorBool("IsMoving", false);
        SetAnimatorBool("isMoving", false);
        SetAnimatorBool("Moving", false);
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.SetHP(Stats.CurrentHP, Stats.MaxHP);
        }
    }

    public void SetTarget(UnitController target) => targetUnit = target;

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
        if (Stats == null) yield break;

        SetMovingAnimation(true);

        while (!isDead && Vector3.Distance(transform.position, destination) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, Stats.MoveSpeed * Time.deltaTime);
            yield return null;
        }

        SetMovingAnimation(false);
        moveRoutine = null;
    }

    public void StartCombat()
    {
        if (!isDead && targetUnit != null)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        if (Stats == null) yield break;
        float interval = Stats.AttackSpeed > 0f ? 1f / Stats.AttackSpeed : 1f;

        yield return new WaitForSeconds(interval);

        while (!isDead && targetUnit != null && !targetUnit.IsDead)
        {
            yield return StartCoroutine(PlayAttackVisual());
            float dmg = Mathf.Max(1f, GameBalance.CalculateDamage(Stats.AttackDamage, targetUnit.Stats.Defense));
            targetUnit.TakeDamage(dmg);
            yield return new WaitForSeconds(interval);
        }

        attackRoutine = null;
    }

    private IEnumerator PlayAttackVisual()
    {
        PlayAnimatorTrigger("Attack");
        PlayAnimatorTrigger("attack");

        if (targetUnit == null)
        {
            yield break;
        }

        Vector3 start = transform.position;
        Vector3 direction = (targetUnit.transform.position - transform.position).normalized;

        if (direction == Vector3.zero)
        {
            direction = Vector3.right;
        }

        Vector3 lungePosition = start + direction * attackLungeDistance;

        yield return MoveVisual(start, lungePosition, attackLungeDuration);
        yield return MoveVisual(lungePosition, start, attackReturnDuration);
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
        if (isDead || Stats == null) return;
        Stats.CurrentHP -= damage;
        healthBar?.SetHP(Stats.CurrentHP, Stats.MaxHP);

        if (Stats.CurrentHP > 0f)
        {
            PlayAnimatorTrigger("Hit");
            PlayAnimatorTrigger("hit");

            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
            }

            hitFlashRoutine = StartCoroutine(HitFlashRoutine());
        }

        if (Stats.CurrentHP <= 0f) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = originalColor;
        hitFlashRoutine = null;
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
        PlayAnimatorTrigger("Die");
        PlayAnimatorTrigger("die");
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        healthBar?.SetVisible(false);
        OnDeath?.Invoke();
    }

    private void SetMovingAnimation(bool isMoving)
    {
        SetAnimatorBool("IsMoving", isMoving);
        SetAnimatorBool("isMoving", isMoving);
        SetAnimatorBool("Moving", isMoving);
        SetAnimatorBool("Run", isMoving);
    }

    private void PlayAnimatorTrigger(string parameterName)
    {
        if (animator == null || !HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        animator.SetTrigger(parameterName);
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null || !HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        animator.SetBool(parameterName, value);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
