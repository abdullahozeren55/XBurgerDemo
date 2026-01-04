using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDrinkCupData", menuName = "Data/DrinkCup")]
public class DrinkCupData : ScriptableObject
{
    public string focusTextKey;
    public GameManager.CupSize cupSize;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType;
    public Sprite icon;
    [Space]
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    public float fillDuration = 3.0f;
    public Vector3 grabbedLocalScale;
    public Vector3 trayLocalScale = Vector3.one;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    [Header("Tray Slot Settings")]
    [Tooltip("Tepsideki 6 slot için ayrý ayrý ince ayarlar. Sýrasýyla 0'dan 5'e.")]
    public TraySlotOffset[] slotOffsets; // Buraya Inspector'dan 6 eleman ekle
    [Space]
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
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
