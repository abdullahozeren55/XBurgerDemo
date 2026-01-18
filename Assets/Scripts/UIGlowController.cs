using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIGlowController : MonoBehaviour
{
    public enum GlowMode { Click, Hold }

    [Header("Mod Ayarý")]
    public GlowMode mode = GlowMode.Click;

    [Header("Parlaklýk Sýnýrlarý (0.0 - 1.0)")]
    [Range(0f, 2f)] public float minBrightness = 0.3f; // En sönük hali (Yazý kaybolmasýn diye 0.3 iyi)
    [Range(0f, 2f)] public float maxBrightness = 1.0f; // En parlak hali

    [Header("Hýz Ayarlarý")]
    public float clickSpeed = 8f; // Hýzlý yanýp sönme hýzý
    public float holdSpeed = 2f;  // Yavaþ nefes alma hýzý (Eski breathSpeed)

    [Header("Hedefler")]
    public Image targetImage;
    public GameObject keyboardParent;
    public TMP_Text targetText;

    // --- MATERYALLER ---
    private Material _baseImageMat;
    private Material _baseTextMat;
    private Material _imageMatInstance;
    private Material _textMatInstance;

    // --- SHADER ID'LERÝ ---
    private int _glowAmountID;
    private int _emissionMapID;
    private int _glowColorID;
    private int _faceColorID;

    private bool _isUsingImage = false;
    private Color _currentTextGlowColor;

    // Titreþimi önleyen yerel zamanlayýcý
    private float _localTimer = 0f;

    private void Awake()
    {
        _glowAmountID = Shader.PropertyToID("_GlowAmount");
        _emissionMapID = Shader.PropertyToID("_EmissionMap");
        _glowColorID = Shader.PropertyToID("_GlowColor");
        _faceColorID = Shader.PropertyToID("_FaceColor");

        if (targetImage != null) _baseImageMat = targetImage.material;
        if (targetText != null) _baseTextMat = targetText.fontSharedMaterial;
    }

    public void SetVisualData(bool useImage, Texture2D emissionTex, Color glowColor)
    {
        _isUsingImage = useImage;
        _currentTextGlowColor = glowColor;

        StopAllCoroutines();

        if (_imageMatInstance) Destroy(_imageMatInstance);
        if (_textMatInstance) Destroy(_textMatInstance);

        _localTimer = 0f;

        if (useImage && targetImage != null)
        {
            // --- IMAGE MODU ---
            targetImage.gameObject.SetActive(true);
            if (keyboardParent) keyboardParent.SetActive(false);

            if (_baseImageMat != null)
            {
                _imageMatInstance = Instantiate(_baseImageMat);
                targetImage.material = _imageMatInstance;

                if (emissionTex != null)
                    _imageMatInstance.SetTexture(_emissionMapID, emissionTex);

                _imageMatInstance.SetColor(_glowColorID, glowColor);
            }
        }
        else if (!useImage && targetText != null)
        {
            // --- TEXT MODU ---
            if (targetImage) targetImage.gameObject.SetActive(false);
            if (keyboardParent) keyboardParent.SetActive(true);
            targetText.gameObject.SetActive(true);

            if (_baseTextMat != null)
            {
                _textMatInstance = Instantiate(_baseTextMat);
                targetText.fontSharedMaterial = _textMatInstance;

                targetText.fontSharedMaterial.EnableKeyword("GLOW_ON");
                targetText.UpdateMeshPadding();

                // Baþlangýçta minBrightness deðerine set et (Patlamayý önler)
                ApplyGlow(0f);
            }
        }

        StartCoroutine(AnimateGlow());
    }

    private IEnumerator AnimateGlow()
    {
        while (true)
        {
            _localTimer += Time.unscaledDeltaTime;

            float waveValue = 0f;

            if (mode == GlowMode.Click)
            {
                // Click: Sinüs dalgasý (0'dan baþlar, yukarý çýkar)
                waveValue = (Mathf.Sin(_localTimer * clickSpeed) + 1f) / 2f;
            }
            else
            {
                // Hold: Cosinüs dalgasý (1'den baþlar, aþaðý iner - Nefes alma efekti)
                waveValue = (Mathf.Cos(_localTimer * holdSpeed) + 1f) / 2f;
            }

            // Hesaplanan 0-1 arasý dalgayý ApplyGlow'a gönderiyoruz
            ApplyGlow(waveValue);
            yield return null;
        }
    }

    private void ApplyGlow(float normalizedValue)
    {
        // Gelen deðer her zaman 0 ile 1 arasýndadýr.
        // Bunu Inspector'dan girdiðin Min ve Max deðerlerine dönüþtürüyoruz.
        // Örn: normalizedValue 0 ise -> 0.3 (Min), 1 ise -> 1.0 (Max) olur.
        float finalIntensity = Mathf.Lerp(minBrightness, maxBrightness, normalizedValue);

        if (_isUsingImage && _imageMatInstance != null)
        {
            _imageMatInstance.SetFloat(_glowAmountID, finalIntensity);
        }
        else if (!_isUsingImage && _textMatInstance != null)
        {
            // Sadece Parlaklýðý (RGB) deðiþtir, Alpha'yý (A) elleme.
            Color finalColor = _currentTextGlowColor;
            finalColor.r *= finalIntensity;
            finalColor.g *= finalIntensity;
            finalColor.b *= finalIntensity;

            _textMatInstance.SetColor(_faceColorID, finalColor);
        }
    }

    private void OnDisable() => StopAllCoroutines();

    private void OnDestroy()
    {
        if (_imageMatInstance) Destroy(_imageMatInstance);
        if (_textMatInstance) Destroy(_textMatInstance);
    }
}