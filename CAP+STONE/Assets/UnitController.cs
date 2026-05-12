using UnityEngine;
using System.Collections;

public class UnitController : MonoBehaviour
{
    public CharacterStats Stats { get; private set; }
    private UnitController targetUnit;
    private bool isDead = false;

    public void Initialize(CharacterStats stats, Vector3 spawnPosition)
    {
        Stats = stats;
        transform.position = spawnPosition;
        isDead = false;
        gameObject.SetActive(true);
    }

    public void SetTarget(UnitController target)
    {
        targetUnit = target;
    }

    public void MoveTo(Vector3 destination)
    {
        StartCoroutine(MoveRoutine(destination));
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, Stats.MoveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void StartCombat()
    {
        if (!isDead && targetUnit != null) StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (!isDead && targetUnit != null && !targetUnit.isDead)
        {
            float finalDamage = Mathf.Max(1, Stats.AttackDamage - targetUnit.Stats.Defense);
            targetUnit.TakeDamage(finalDamage);
            yield return new WaitForSeconds(1f / Stats.AttackSpeed);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        Stats.CurrentHP -= damage;
        if (Stats.CurrentHP <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        gameObject.SetActive(false);
    }
}