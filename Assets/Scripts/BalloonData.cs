using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBalloonData", menuName = "Data/Balloon")]
public class BalloonData : ScriptableObject
{
    public float releaseExplosionForce = 1f;
    public float releaseTorqueForce = 36f;
    public ParticleSystem destroyParticles;
    public AudioClip popSound;
    public float popVolume = 1f;
    public float popMinPitch = 0.9f;
    public float popMaxPitch = 1.1f;
}
