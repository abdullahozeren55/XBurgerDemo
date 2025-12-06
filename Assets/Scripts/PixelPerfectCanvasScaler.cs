using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class PixelPerfectCanvasScaler : MonoBehaviour
{
    [Header("Tasarým Yaptýðýn Çözünürlük")]
    public float referenceHeight = 360f; // 360p (Retro standart)

    [Header("Ayarlar")]
    public bool onlyIntegerScale = true;

    private CanvasScaler _canvasScaler;

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        // Modu garantiye al
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private void Start()
    {
        UpdateScale();

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.RegisterScaler(this);
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
            // --- SADELEÞTÝRÝLMÝÞ MANTIK ---
            // Sadece ekran yüksekliðini referansa böl ve aþaðý yuvarla.
            // Örn: 1080 / 360 = 3 (Tam 3x)
            // Örn: 1440 / 360 = 4 (Tam 4x)
            // Örn: 768 / 360 = 2.13 -> 2 (Tam 2x)

            int integerScale = Mathf.FloorToInt(screenHeight / referenceHeight);

            // En az 1 olsun, yoksa UI görünmez
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