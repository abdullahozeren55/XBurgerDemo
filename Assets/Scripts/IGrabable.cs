using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IGrabable
{
    public void OnGrab(Transform grabPoint);
    public void OnThrow(Vector3 direction, float force);
    public void OnFocus();
    public void OnLoseFocus();
    public void OnDrop(Vector3 direction, float force);
    public void OnUseHold();
    public void OnUseRelease();
    public void OutlineChangeCheck();

    public PlayerManager.HandGrabTypes HandGrabType { get; set; }

    public Vector3 GrabPositionOffset { get; set; }
    public Vector3 GrabRotationOffset { get; set; }

    public bool IsGrabbed {  get; set; }
    public bool IsUseable { get; set; }
    public bool OutlineShouldBeRed { get; set; }
    public string FocusTextKey { get; set; }
}
