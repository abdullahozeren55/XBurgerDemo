using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrabable
{
    public void OnGrab(Transform grabPoint);
    public void OnThrow(Vector3 direction, float force);
    public void OnFocus();
    public void OnLoseFocus();
    public void OnDrop(Vector3 direction, float force);

    public bool IsGrabbed {  get; set; }

    public float HandLerp {  get; set; }
}
