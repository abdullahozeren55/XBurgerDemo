using UnityEngine;

public class ChildBurger : MonoBehaviour, IGrabable
{
    [HideInInspector] public WholeBurger parentBurger;

    public IGrabable Master => parentBurger;

    // --- Proxy Logic ---
    // Tüm emirleri Parent'a iletiyoruz.

    public bool IsGrabbed
    {
        get => parentBurger.IsGrabbed;
        set => parentBurger.IsGrabbed = value;
    }

    // Ýkon ve GrabType'ý da Parent yönetsin veya Child'a özel kalabilir.
    // Þimdilik parent'tan çekiyoruz ki tutarlý olsun.
    public Sprite Icon { get => parentBurger.Icon; set => parentBurger.Icon = value; }
    public PlayerManager.HandGrabTypes HandGrabType { get => parentBurger.HandGrabType; set => parentBurger.HandGrabType = value; }

    public bool OutlineShouldBeRed { get => parentBurger.OutlineShouldBeRed; set => parentBurger.OutlineShouldBeRed = value; }
    public bool OutlineShouldBeGreen { get => parentBurger.OutlineShouldBeGreen; set => parentBurger.OutlineShouldBeGreen = value; }

    public bool IsThrowable { get => parentBurger.IsThrowable; set => parentBurger.IsThrowable = value; }
    public float ThrowMultiplier { get => parentBurger.ThrowMultiplier; set => parentBurger.ThrowMultiplier = value; }
    public bool IsUseable { get => parentBurger.IsUseable; set => parentBurger.IsUseable = value; }

    // Offsetler Parent'ýn tutuþuna göre ayarlanmalý
    public Vector3 GrabPositionOffset { get => parentBurger.GrabPositionOffset; set => parentBurger.GrabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => parentBurger.GrabRotationOffset; set => parentBurger.GrabRotationOffset = value; }

    public string FocusTextKey { get => parentBurger.FocusTextKey; set => parentBurger.FocusTextKey = value; }

    // --- Actions ---

    public void OnFocus() => parentBurger.OnFocus();
    public void OnLoseFocus() => parentBurger.OnLoseFocus();

    public void OnGrab(Transform grabPoint) => parentBurger.OnGrab(grabPoint);
    public void OnDrop(Vector3 direction, float force) => parentBurger.OnDrop(direction, force);
    public void OnThrow(Vector3 direction, float force) => parentBurger.OnThrow(direction, force);

    public void OnHolster() => parentBurger.OnHolster();
    public void OnUseHold() => parentBurger.OnUseHold();
    public void OnUseRelease() => parentBurger.OnUseRelease();

    public void OutlineChangeCheck() => parentBurger.OutlineChangeCheck();
    public void ChangeLayer(int layer) => parentBurger.ChangeLayer(layer);

    public bool TryCombine(IGrabable otherItem) => parentBurger.TryCombine(otherItem);
    public bool CanCombine(IGrabable otherItem) => parentBurger.CanCombine(otherItem);
}