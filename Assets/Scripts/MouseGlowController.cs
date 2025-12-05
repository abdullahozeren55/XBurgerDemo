using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MouseGlowController : MonoBehaviour
{
    public enum GlowMode
    {
        Click, // Hýzlý yanýp sönme (Interact, Take)
        Hold   // Yavaþça dolma ve nefes alma (Use, Throw)
    }

    [Header("Ayarlar")]
    public GlowMode mode = GlowMode.Click;

    [Header("Click Modu Ayarlarý")]
    [Tooltip("Ne kadar hýzlý yanýp sönsün?")]
    public float clickSpeed = 8f;

    [Header("Hold Modu Ayarlarý")]
    [Tooltip("0'dan 1'e ne kadar sürede çýksýn? (Dolma süresi)")]
    public float chargeDuration = 0.5f;
    [Tooltip("Dolduktan sonra hangi hýzda nefes alsýn?")]
    public float breathSpeed = 2f;
    [Tooltip("Nefes alýrken parlaklýk hangi aralýkta gidip gelsin?")]
    public Vector2 breathRange = new Vector2(0.6f, 1.0f);

    private Image _targetImage;
    private Material _materialInstance;
    private int _glowAmountID;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
        _glowAmountID = Shader.PropertyToID("_GlowAmount"); // Shaderdaki deðiþkenin adý

        if (_targetImage != null)
        {
            // Materyali kopyalýyoruz ki diðer ikonlar etkilenmesin
            _materialInstance = Instantiate(_targetImage.material);
            _targetImage.material = _materialInstance;
        }
    }

    private void OnEnable()
    {
        if (_materialInstance != null)
        {
            StartCoroutine(AnimateGlow());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator AnimateGlow()
    {
        // Baþlangýçta glow'u sýfýrla
        _materialInstance.SetFloat(_glowAmountID, 0f);

        if (mode == GlowMode.Click)
        {
            // --- CLICK MODU (Kalp Atýþý / Siren) ---
            while (true)
            {
                // PingPong: 0 ile 1 arasýnda sürekli gidip gelir
                // unscaledTime kullanýyoruz ki oyun dursa (Pause) bile UI çalýþsýn
                float glow = Mathf.PingPong(Time.unscaledTime * clickSpeed, 1f);

                _materialInstance.SetFloat(_glowAmountID, glow);
                yield return null;
            }
        }
        else
        {
            // --- HOLD MODU (Þarj Ol + Nefes Al) ---

            // 1. ÞARJ EVRESÝ (0 -> 1)
            float timer = 0f;
            while (timer < chargeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = timer / chargeDuration;

                // SmoothStep ile yumuþakça parlasýn
                float glow = Mathf.SmoothStep(0f, 1f, progress);

                _materialInstance.SetFloat(_glowAmountID, glow);
                yield return null;
            }

            // 2. NEFES ALMA EVRESÝ (Breath Range arasýnda git gel)
            // Dolduktan sonra maksimumda sabit kalmasýn, canlý olduðu belli olsun diye hafif oynasýn
            float breathBase = breathRange.x;
            float breathDiff = breathRange.y - breathRange.x;

            while (true)
            {
                // Sinüs dalgasý kullanarak yumuþak bir nefes efekti (0 ile 1 arasý)
                // (Sinüs -1 ile 1 arasý verir, onu 0-1 arasýna çekiyoruz: (Sin+1)/2)
                float wave = (Mathf.Sin(Time.unscaledTime * breathSpeed) + 1f) / 2f;

                // Aralýða yayýyoruz (Örn: 0.6 ile 1.0 arasýna)
                float glow = breathBase + (wave * breathDiff);

                _materialInstance.SetFloat(_glowAmountID, glow);
                yield return null;
            }
        }
    }

    private void OnDestroy()
    {
        if (_materialInstance != null) Destroy(_materialInstance);
    }
}