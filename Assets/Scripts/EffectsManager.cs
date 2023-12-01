using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    [SerializeField] private EspecialAttack[] effects;

    public EspecialAttack[] Effects { get => effects; set => effects = value; }
}

[Serializable]
public class EspecialAttack 
{
    public GameObject vxf;
    public AudioClip effectSound;
    public AudioClip playerAttackSound;
    public Transform spawPoint;
    public float duration = 2f;
    public int damage = 10;
}
