using UnityEngine;
using UnityEngine.EventSystems; // Mouse olaylarý için þart

public class LockedControlUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Ayarý")]
    [Tooltip("Mouse üstüne gelince açýlacak olan 'DEÐÝÞTÝRÝLEMEZ' yazýsý")]
    [SerializeField] private GameObject tooltipObject;

    private void Start()
    {
        // Baþlangýçta tooltipe mutlaka kapalý olsun
        if (tooltipObject != null) tooltipObject.SetActive(false);
    }

    // Mouse üstüne gelince
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipObject != null) tooltipObject.SetActive(true);
    }

    // Mouse üstünden gidince
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject != null) tooltipObject.SetActive(false);
    }

    // Opsiyonel: Týklanýrsa bir "Hata Sesi" çaldýrabilirsin
    // public void OnPointerClick(PointerEventData eventData) { ... }
}