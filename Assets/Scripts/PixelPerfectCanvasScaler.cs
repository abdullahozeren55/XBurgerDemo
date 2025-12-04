using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class PixelPerfectCanvasScaler : MonoBehaviour
{
    [Header("Tasarým Yaptýðýn Çözünürlük")]
    public float referenceHeight = 360f;

    [Header("Ayarlar")]
    public bool onlyIntegerScale = true;

    private CanvasScaler _canvasScaler;

    private void Awake()
    {
        // Component'i al, bu güvenli.
        _canvasScaler = GetComponent<CanvasScaler>();

        // Þunu da garantiye alalým: Mod yanlýþsa kod çalýþmaz.
        // Kodla zorla "Constant Pixel Size" yapýyoruz.
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private void Start()
    {
        // --- DEÐÝÞÝKLÝK BURADA ---
        // Start'ta MenuManager'ýn uyanmýþ olduðundan %100 eminiz.

        // 1. Önce kendini güncelle
        UpdateScale();

        // 2. Sonra Patron'a kaydol
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.RegisterScaler(this);
        }
        else
        {
            // Debug için (Eðer yine çalýþmazsa bunu görürsün)
            Debug.LogError("PixelPerfectScaler: MenuManager bulunamadý!");
        }
    }

    private void OnDestroy()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.UnregisterScaler(this);
        }
    }

    public void UpdateScale()
    {
        if (_canvasScaler == null) return;
        if (Screen.height == 0) return;

        float screenHeight = Screen.height;
        float scaleFactor = 1f;

        if (onlyIntegerScale)
        {
            // 1. Önce normal tam sayý katýný bul (Örn: 1080 / 360 = 3)
            int rawScale = Mathf.FloorToInt(screenHeight / referenceHeight);
            if (rawScale < 1) rawScale = 1;

            // 2. SADECE 2'NÝN KUVVETLERÝ (1, 2, 4, 8, 16...)
            // Mantýk: rawScale'den küçük veya eþit olan en büyük 2'nin kuvvetini bul.

            // Eðer 3 geldiyse -> 2 olsun.
            // Eðer 5 geldiyse -> 4 olsun.
            // Eðer 7 geldiyse -> 4 olsun.
            // Eðer 8 geldiyse -> 8 olsun.

            // Matematiksel hile: Logaritma 2 tabanýnda alýp aþaðý yuvarla, sonra 2 üzeri yap.
            // Örnek: 3 için -> Log2(3) = 1.58 -> Floor = 1 -> 2^1 = 2.
            // Örnek: 5 için -> Log2(5) = 2.32 -> Floor = 2 -> 2^2 = 4.

            int powerOfTwoExponent = Mathf.FloorToInt(Mathf.Log(rawScale, 2));
            int powerOfTwoScale = (int)Mathf.Pow(2, powerOfTwoExponent);

            // Güvenlik: 1'in altýna düþmesin
            if (powerOfTwoScale < 1) powerOfTwoScale = 1;

            scaleFactor = powerOfTwoScale;
        }
        else
        {
            scaleFactor = screenHeight / referenceHeight;
        }

        _canvasScaler.scaleFactor = scaleFactor;
    }
}