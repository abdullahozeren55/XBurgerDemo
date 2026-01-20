using UnityEngine;

public class GrabableChild : MonoBehaviour, IGrabable
{
    [HideInInspector] public IGrabable parentGrabable;

    // Eðer parent atanmadýysa Master null dönmeli ki oyun patlamasýn
    public IGrabable Master => parentGrabable != null ? parentGrabable.Master : null;

    // --- Proxy Logic ---
    // Tüm emirleri Parent'a iletiyoruz.

    // Not: Null check ekledim (?.) ki editörde parent koparsa hata vermesin.
    public bool IsGrabbed
    {
        get => parentGrabable != null && parentGrabable.IsGrabbed;
        set { if (parentGrabable != null) parentGrabable.IsGrabbed = value; }
    }

    public ItemIcon IconData { get => parentGrabable?.IconData; set { if (parentGrabable != null) parentGrabable.IconData = value; } }

    // Enum olduðu için null check biraz farklý, default deðer döndürüyoruz
    public PlayerManager.HandGrabTypes HandGrabType
    {
        get => parentGrabable != null ? parentGrabable.HandGrabType : PlayerManager.HandGrabTypes.RegularGrab;
        set { if (parentGrabable != null) parentGrabable.HandGrabType = value; }
    }

    public PlayerManager.HandRigTypes HandRigType { get => parentGrabable.HandRigType; set => parentGrabable.HandRigType = value; }

    public bool OutlineShouldBeRed
    {
        get => parentGrabable != null && parentGrabable.OutlineShouldBeRed;
        set { if (parentGrabable != null) parentGrabable.OutlineShouldBeRed = value; }
    }

    public bool OutlineShouldBeGreen
    {
        get => parentGrabable != null && parentGrabable.OutlineShouldBeGreen;
        set { if (parentGrabable != null) parentGrabable.OutlineShouldBeGreen = value; }
    }

    public bool IsThrowable
    {
        get => parentGrabable != null && parentGrabable.IsThrowable;
        set { if (parentGrabable != null) parentGrabable.IsThrowable = value; }
    }

    public float ThrowMultiplier
    {
        get => parentGrabable != null ? parentGrabable.ThrowMultiplier : 1f;
        set { if (parentGrabable != null) parentGrabable.ThrowMultiplier = value; }
    }

    public bool IsUseable
    {
        get => parentGrabable != null && parentGrabable.IsUseable;
        set { if (parentGrabable != null) parentGrabable.IsUseable = value; }
    }

    public Vector3 GrabPositionOffset
    {
        get => parentGrabable != null ? parentGrabable.GrabPositionOffset : Vector3.zero;
        set { if (parentGrabable != null) parentGrabable.GrabPositionOffset = value; }
    }

    public Vector3 GrabRotationOffset
    {
        get => parentGrabable != null ? parentGrabable.GrabRotationOffset : Vector3.zero;
        set { if (parentGrabable != null) parentGrabable.GrabRotationOffset = value; }
    }

    public string FocusTextKey
    {
        get => parentGrabable?.FocusTextKey;
        set { if (parentGrabable != null) parentGrabable.FocusTextKey = value; }
    }

    // --- Actions ---

    public void OnFocus() => parentGrabable?.OnFocus();
    public void OnLoseFocus() => parentGrabable?.OnLoseFocus();

    public void OnGrab(Transform grabPoint) => parentGrabable?.OnGrab(grabPoint);
    public void OnDrop(Vector3 direction, float force) => parentGrabable?.OnDrop(direction, force);
    public void OnThrow(Vector3 direction, float force) => parentGrabable?.OnThrow(direction, force);

    public void OnHolster() => parentGrabable?.OnHolster();
    public void OnUseHold() => parentGrabable?.OnUseHold();
    public void OnUseRelease() => parentGrabable?.OnUseRelease();

    public void OutlineChangeCheck() => parentGrabable?.OutlineChangeCheck();
    public void ChangeLayer(int layer) => parentGrabable?.ChangeLayer(layer);

    public bool TryCombine(IGrabable otherItem) => parentGrabable != null && parentGrabable.TryCombine(otherItem);
    public bool CanCombine(IGrabable otherItem) => parentGrabable != null && parentGrabable.CanCombine(otherItem);

    private void Awake()
    {
        // --- DÜZELTME BURADA ---
        // GetComponentInParent yerine, aramayý bir üst objeden (Transform.parent) baþlatýyoruz.
        // Böylece kendini bulup sonsuz döngüye girmiyor.
        if (transform.parent != null)
        {
            parentGrabable = transform.parent.GetComponentInParent<IGrabable>();
        }

        if (parentGrabable == null)
        {
            Debug.LogError($"[GrabableChild] {gameObject.name} bir Parent IGrabable bulamadý! Hiyerarþiyi kontrol et.");
        }
    }
}