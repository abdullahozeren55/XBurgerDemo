using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IGrabable
{
    // YENÝ: Herkes gerçek sahibini göstersin
    IGrabable Master { get; }
    public void OnGrab(Transform grabPoint);
    public void OnThrow(Vector3 direction, float force);
    public void OnFocus();
    public void OnLoseFocus();
    public void OnDrop(Vector3 direction, float force);
    public void OnHolster();
    public void OnUseHold();
    public void OnUseRelease();
    public void OutlineChangeCheck();
    public void ChangeLayer(int layer);
    public bool TryCombine(IGrabable otherItem);
    bool CanCombine(IGrabable otherItem);

    public PlayerManager.HandGrabTypes HandGrabType { get; set; }

    public Vector3 GrabPositionOffset { get; set; }
    public Vector3 GrabRotationOffset { get; set; }

    public float ThrowMultiplier { get; set; }

    public bool IsThrowable { get; set; }
    public bool IsGrabbed {  get; set; }
    public bool IsUseable { get; set; }
    public bool OutlineShouldBeRed { get; set; }
    public bool OutlineShouldBeGreen { get; set; }
    public string FocusTextKey { get; set; }
    public Sprite Icon { get; set; }
}
