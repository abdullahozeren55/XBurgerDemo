using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDragger : MonoBehaviour, IDragHandler
{
    public RectTransform windowRectWorld;
    private RectTransform windowRect;   // Hareket edecek olan ana pencere
    private RectTransform parentRect;   // Sýnýrlarý belirleyen Masaüstü Paneli
    private Canvas canvas;              // Scale faktörünü almak için gerekli

    private void Awake()
    {
        // Handle objesi pencerenin içinde olduðu için parent'ýndan asýl pencereyi buluyoruz
        windowRect = transform.parent.GetComponent<RectTransform>();

        // Masaüstü (Parent) sýnýrlarýný al
        if (windowRect != null)
        {
            parentRect = windowRect.parent as RectTransform;
            canvas = GetComponentInParent<Canvas>();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowRect == null || canvas == null) return;

        // 1. HAREKET (Delta / ScaleFactor Formülü)
        // Canvas scale olduðu için farenin hareketiyle pencerenin hareketi tutmaz.
        // O yüzden canvas.scaleFactor'e bölüyoruz.
        windowRect.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // 2. SINIRLAMA (CLAMP) - Ekran dýþýna çýkmasýn
        ClampToWindow();
    }

    private void ClampToWindow()
    {
        if (parentRect == null) return;

        // Masaüstünün yarým geniþliði ve yüksekliði (Pivot Center varsayýmýyla)
        // Eðer Pivotun Sol-Üst ise buradaki matematik biraz deðiþir. 
        // Aþaðýdaki kod PÝVOT: CENTER (0.5, 0.5) için en saðlýklý çalýþýr.

        Vector3 pos = windowRect.anchoredPosition;

        // Masaüstü boyutlarý
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        // Pencere boyutlarý
        float windowWidth = windowRect.rect.width;
        float windowHeight = windowRect.rect.height;

        // Sýnýrlarý Hesapla
        // X Ekseni: Masaüstü sað kenarýndan pencerenin yarýsýný çýkar
        float minX = (parentWidth / -2f) + (windowWidth / 2f);
        float maxX = (parentWidth / 2f) - (windowWidth / 2f);

        // Y Ekseni: Masaüstü üst kenarýndan pencerenin yarýsýný çýkar
        float minY = (parentHeight / -2f) + (windowHeight / 2f);
        float maxY = (parentHeight / 2f) - (windowHeight / 2f);

        // Pozisyonu kelepçele
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        windowRect.anchoredPosition = pos;
        windowRectWorld.anchoredPosition = pos;
    }
}