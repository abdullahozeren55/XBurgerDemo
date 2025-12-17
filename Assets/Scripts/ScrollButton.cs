using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Basýlý tutmayý algýlamak için lazým

public class ScrollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Ayarlar")]
    public ScrollRect targetScrollRect; // Hangi listeyi kaydýracak?
    public ScrollRect targetScrollRectWorld; // Hangi listeyi kaydýracak?
    public float scrollSpeed = 0.5f;    // Kaydýrma hýzý (0.1 - 1 arasý dene)

    [Tooltip("Ýþaretliyse Yukarý, deðilse Aþaðý kaydýrýr")]
    public bool isUpButton = false;

    private bool _isPressed = false;

    // Butona basýldýðý an
    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
    }

    // Buton býrakýldýðý an
    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
    }

    private void Update()
    {
        // Eðer butona basýlýyorsa ve ScrollRect tanýmlýysa
        if (_isPressed && targetScrollRect != null)
        {
            // Scroll mantýðý:
            // verticalNormalizedPosition 1 ise en tepedir.
            // verticalNormalizedPosition 0 ise en alttýr.

            // Yukarý butonu -> 1'e doðru gitmeli (+ ekle)
            // Aþaðý butonu -> 0'a doðru gitmeli (- çýkar)

            float direction = isUpButton ? 1f : -1f;

            // Time.unscaledDeltaTime kullanýyoruz ki oyun pause olsa bile Win95 çalýþsýn
            float change = scrollSpeed * Time.unscaledDeltaTime * direction;

            targetScrollRect.verticalNormalizedPosition += change;
            targetScrollRectWorld.verticalNormalizedPosition += change;

            // Deðerin 0 ile 1 dýþýna taþmasýný engelle (Garantici yaklaþým)
            targetScrollRect.verticalNormalizedPosition = Mathf.Clamp01(targetScrollRect.verticalNormalizedPosition);
            targetScrollRectWorld.verticalNormalizedPosition = Mathf.Clamp01(targetScrollRect.verticalNormalizedPosition);
        }
    }
}