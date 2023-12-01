using System;
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    [SerializeField] EspecialAttack[] effects;
    [SerializeField] private AudioSource mainSound;
    [SerializeField] private AudioSource secondaySound;
    [SerializeField] private AudioSource playerSound;
    
    public EspecialAttack[] Effects { get => effects; set => effects = value; }
    public AudioSource MainSound { get => mainSound; set => mainSound = value; }
    public AudioSource SecondaySound { get => secondaySound; set => secondaySound = value; }
    public AudioSource PlayerSound { get => playerSound; set => playerSound = value; }
}

[Serializable]
public class EspecialAttack
{
    [SerializeField] String name;
    [SerializeField] GameObject vxf;
    [SerializeField] AudioClip effectSound;
    [SerializeField] AudioClip secondaryEffectSound;
    [SerializeField] AudioClip playerAttackSound;
    [SerializeField] Transform spawPoint;
    [SerializeField] float duration = 2f;
    [SerializeField] int damage = 10;

    public string Name { get => name;}
    public GameObject Vxf { get => vxf; set => vxf = value; }
    public AudioClip EffectSound { get => effectSound; set => effectSound = value; }
    public AudioClip SecondaryEffectSound { get => secondaryEffectSound; set => secondaryEffectSound = value; }
    public AudioClip PlayerAttackSound { get => playerAttackSound; set => playerAttackSound = value; }
    public Transform SpawPoint { get => spawPoint; set => spawPoint = value; }
    public float Duration { get => duration; set => duration = value; }
    public int Damage { get => damage; set => damage = value; }
}
