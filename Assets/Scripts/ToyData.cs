using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewToyData", menuName = "Data/Toy")]
public class ToyData : ScriptableObject
{
    public string focusTextKey;
    public ToyType toyType;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType = PlayerManager.HandRigTypes.SingleHandGrab;
    public ItemIcon iconData;
    [Space]
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    [Space]
    [Header("Tray Settings")]
    [Tooltip("Tepsideki 6 slot için ayrý ayrý ince ayarlar.")]
    public TraySlotOffset[] slotOffsets;

    [Tooltip("Eline aldýðýnda scale bozulmasýn diye resetlenecek deðer (Genelde 1,1,1)")]
    public Vector3 grabbedLocalScale = Vector3.one;
    public Vector3 trayLocalScale = Vector3.one;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
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

public enum ToyType
{
    Clown,
    Car,
    Ball
}
