using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FryableData;

[CreateAssetMenu(fileName = "NewHolderData", menuName = "Data/Holder")]
public class HolderData : ScriptableObject
{
    [Header("Identity")]
    public Sprite icon;
    public string[] focusTextKeys;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType = PlayerManager.HandRigTypes.SingleHandGrab;
    [Space]
    [Header("Tray Settings")]
    [Tooltip("Tepsideki 6 slot için ayrý ayrý ince ayarlar.")]
    public TraySlotOffset[] slotOffsets; // Inspector'da 6 eleman açýp ayarla

    [Tooltip("Eline aldýðýnda scale bozulmasýn diye resetlenecek deðer (Genelde 1,1,1)")]
    public Vector3 grabbedLocalScale = Vector3.one;
    public Vector3 trayLocalScale = Vector3.one;
    [Space]
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
}
