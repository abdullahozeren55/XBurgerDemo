using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerBoxData", menuName = "Data/BurgerBox")]
public class BurgerBoxData : ScriptableObject
{
    public bool isUseable = false;
    public bool isThrowable = true;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType = PlayerManager.HandRigTypes.SingleHandGrab;
    public Sprite[] icon;
    [Space]
    [Header("Lid Settings (Adaptive)")]
    public float minLidAngle = 32f; // Küçük burgerlerdeki havalý duruþ
    public float maxLidAngle = 112f; // Dev burgerlerdeki zoraki duruþ
    public float minBurgerHeightLimit = 1f; // Bu deðerin altý min derece
    public float maxBurgerHeightLimit = 1.4f; // Bu deðer ve üstü max derece
    [Space]
    public float timeToPutOnTray = 0.3f;
    public float throwMultiplier = 1f;
    public Vector3 trayRotation = new Vector3(0, 0, -90);
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public string[] focusTextKeys;

    public AudioClip[] audioClips;
    [Space]
    public float grabSoundVolume = 1f;
    public float grabSoundMinPitch = 0.85f;
    public float grabSoundMaxPitch = 1.15f;
    [Space]
    public float dropSoundVolume = 1f;
    public float dropSoundMinPitch = 0.85f;
    public float dropSoundMaxPitch = 1.15f;
    [Space]
    public float throwSoundVolume = 1f;
    public float throwSoundMinPitch = 0.85f;
    public float throwSoundMaxPitch = 1.15f;
    [Space]
    public float closeSoundVolume = 1f;
    public float closeSoundMinPitch = 0.85f;
    public float closeSoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
