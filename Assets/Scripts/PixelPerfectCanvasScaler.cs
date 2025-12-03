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

        // Ekran boyutu 0 gelirse (Unity bazen ilk karede yapar) patlamasýn
        if (Screen.height == 0) return;

        float screenHeight = Screen.height;
        float scaleFactor = 1f;

        if (onlyIntegerScale)
        {
            int integerScale = Mathf.FloorToInt(screenHeight / referenceHeight);
            if (integerScale < 1) integerScale = 1;

            scaleFactor = integerScale;
        }
        else
        {
            scaleFactor = screenHeight / referenceHeight;
        }

        _canvasScaler.scaleFactor = scaleFactor;
    }
}