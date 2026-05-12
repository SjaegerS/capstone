using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    public string ID;
    public string Name;
    public float MaxHP;
    public float CurrentHP;
    public float AttackDamage;
    public float AttackSpeed;
    public float MoveSpeed;
    public float Defense;

    // 초기화를 위한 생성자 추가
    public CharacterStats(string id, string name, float hp, float dmg, float spd, float move, float def)
    {
        ID = id;
        Name = name;
        MaxHP = hp;
        CurrentHP = hp;
        AttackDamage = dmg;
        AttackSpeed = spd;
        MoveSpeed = move;
        Defense = def;
    }
}