using UnityEngine;

public class BoxChild : MonoBehaviour, IGrabable
{
    [HideInInspector] public BurgerBox parentBox;

    public IGrabable Master => parentBox;

    // --- Proxy Logic ---
    // Gelen tüm istekleri Parent'a yönlendiriyoruz.

    // IGrabable Propertyleri (Parent'tan oku/yaz)
    public bool IsGrabbed { get => parentBox.IsGrabbed; set => parentBox.IsGrabbed = value; }
    public ItemIcon IconData { get => parentBox.IconData; set { } } // Set boþ çünkü parent yönetiyor
    public PlayerManager.HandGrabTypes HandGrabType { get => parentBox.HandGrabType; set { } }
    public PlayerManager.HandRigTypes HandRigType { get => parentBox.HandRigType; set => parentBox.HandRigType = value; }

    // Outline durumlarý
    public bool OutlineShouldBeRed { get => parentBox.OutlineShouldBeRed; set => parentBox.OutlineShouldBeRed = value; }
    public bool OutlineShouldBeGreen { get => parentBox.OutlineShouldBeGreen; set => parentBox.OutlineShouldBeGreen = value; }

    public bool IsThrowable { get => parentBox.IsThrowable; set => parentBox.IsThrowable = value; }
    public float ThrowMultiplier { get => parentBox.ThrowMultiplier; set => parentBox.ThrowMultiplier = value; }
    public bool IsUseable { get => parentBox.IsUseable; set => parentBox.IsUseable = value; }

    // Offsetler
    public Vector3 GrabPositionOffset { get => parentBox.GrabPositionOffset; set { } }
    public Vector3 GrabRotationOffset { get => parentBox.GrabRotationOffset; set { } }
    public Vector3 GrabLocalPositionOffset { get => parentBox.GrabLocalPositionOffset; set { } }
    public Vector3 GrabLocalRotationOffset { get => parentBox.GrabLocalRotationOffset; set { } }

    public string FocusTextKey { get => parentBox.FocusTextKey; set { } }

    // --- Actions ---
    public void OnFocus() => parentBox.OnFocus();
    public void OnLoseFocus() => parentBox.OnLoseFocus();

    public void OnGrab(Transform grabPoint) => parentBox.OnGrab(grabPoint);
    public void OnDrop(Vector3 direction, float force) => parentBox.OnDrop(direction, force);
    public void OnThrow(Vector3 direction, float force) => parentBox.OnThrow(direction, force);

    public void OnHolster() => parentBox.OnHolster();
    public void OnUseHold() => parentBox.OnUseHold();
    public void OnUseRelease() => parentBox.OnUseRelease();

    public void OutlineChangeCheck() => parentBox.OutlineChangeCheck();

    // Layer deðiþimi önemli: Parent deðiþince biz de deðiþmeliyiz, 
    // ama ChangeLayer fonksiyonu parentta zaten children'ý tarýyor olacak.
    public void ChangeLayer(int layer) => parentBox.ChangeLayer(layer);

    public bool TryCombine(IGrabable otherItem) => parentBox.TryCombine(otherItem);
    public bool CanCombine(IGrabable otherItem) => parentBox.CanCombine(otherItem);
}