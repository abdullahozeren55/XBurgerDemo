using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCookableData", menuName = "Data/Cookable")]
public class CookableData : ScriptableObject
{
    public ParticleSystem cookingParticles;
    public ParticleSystem smokeParticlesWorld;
    public ParticleSystem smokeParticlesLocal;
    public AudioClip cookingSound;
    public float cookingSoundVolume = 1f;
    public float cookingSoundMinPitch = 0.85f;
    public float cookingSoundMaxPitch = 1.15f;

    public Material[] materials = new Material[3]; //0 raw, 1 regular, 2 burnt
    public float[] cookTime = new float[2]; //0 raw to regular, 1 regular to burnt
}
