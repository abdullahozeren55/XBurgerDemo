using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RetroMarquee : MonoBehaviour
{
    [System.Serializable]
    public class MarqueeGroup
    {
        public TMP_Text textComponent; // Inspector'dan atanacak Text
        [HideInInspector] public RectTransform rect1;
        [HideInInspector] public RectTransform rect2;
        [HideInInspector] public float parentWidth;
        [HideInInspector] public bool isSetup = false;
    }

    [Header("Hedef Textler")]
    public MarqueeGroup worldTextGroup; // Monitör üzerindeki
    public MarqueeGroup focusTextGroup; // Ekrana gelen (Focus UI)

    [Header("Ayarlar")]
    public float pixelsPerStep = 16f;
    public float gap = 64f;

    private float _textWidth; // Metin geniþliði (Ýkisi için de ayný kabul ediyoruz)

    // Setup'ý manuel çaðýracaðýz
    public void RefreshText(string newText)
    {
        // 1. Textleri güncelle
        if (worldTextGroup.textComponent != null) worldTextGroup.textComponent.text = newText;
        if (focusTextGroup.textComponent != null) focusTextGroup.textComponent.text = newText;

        // 2. Hazýrlýðý Baþlat
        SetupGroup(worldTextGroup);
        SetupGroup(focusTextGroup);
    }

    private void SetupGroup(MarqueeGroup group)
    {
        if (group.textComponent == null) return;

        group.isSetup = false;
        group.rect1 = group.textComponent.GetComponent<RectTransform>();

        // Text geniþliðini hesapla (ForceUpdate önemli)
        group.textComponent.ForceMeshUpdate();
        _textWidth = group.textComponent.GetRenderedValues(false).x;
        // Not: Eðer rect.width kullanýyorsan ve AutoSize açýksa yukarýdaki daha güvenlidir, 
        // ama senin yapýnda rect.width kullanýyorduk, yine ona dönelim:
        _textWidth = group.rect1.rect.width;

        // Parent Geniþliði
        if (group.rect1.parent != null)
        {
            RectTransform pRect = group.rect1.parent.GetComponent<RectTransform>();
            group.parentWidth = (pRect != null) ? pRect.rect.width : 500f;
        }

        // Varsa eski klonu temizle
        string cloneName = group.textComponent.name + "_Clone";
        Transform oldClone = group.rect1.parent.Find(cloneName);
        if (oldClone != null) DestroyImmediate(oldClone.gameObject);

        // Kopyayý oluþtur
        GameObject cloneObj = Instantiate(group.textComponent.gameObject, group.rect1.parent);
        cloneObj.name = cloneName;

        // Kopyadaki gereksiz scriptleri (varsa) temizle
        // (Bu script artýk textin üzerinde olmadýðý için recursive sorun olmaz ama temizlik iyidir)

        group.rect2 = cloneObj.GetComponent<RectTransform>();

        // --- BAÞLANGIÇ KONUMLARI ---
        // Ýkisini de ayný mantýkla sýfýrlýyoruz
        group.rect1.anchoredPosition = new Vector2(group.parentWidth, group.rect1.anchoredPosition.y);
        group.rect2.anchoredPosition = new Vector2(group.parentWidth + _textWidth + gap, group.rect1.anchoredPosition.y);

        group.isSetup = true;
    }

    // MonitorManager'ýn Loop'undan çaðrýlacak
    public void Step()
    {
        // Ýki grubu da baðýmsýz ama ayný anda hareket ettir
        MoveGroup(worldTextGroup);
        MoveGroup(focusTextGroup);
    }

    private void MoveGroup(MarqueeGroup group)
    {
        // Obje setup olmamýþsa veya (önemli!) text objesi yok olmuþsa çalýþma
        if (!group.isSetup || group.rect1 == null || group.rect2 == null) return;

        // NOT: group.textComponent.gameObject.activeInHierarchy kontrolü YAPMIYORUZ.
        // Obje kapalý olsa bile RectTransform deðerlerini deðiþtirebiliriz.
        // Böylece açýldýðý anda doðru yerde olur.

        MoveRect(group.rect1, group.rect2);
        MoveRect(group.rect2, group.rect1);
    }

    private void MoveRect(RectTransform current, RectTransform other)
    {
        Vector2 pos = current.anchoredPosition;
        pos.x -= pixelsPerStep;

        // Ekrandan çýktý mý?
        if (pos.x < -_textWidth)
        {
            // Diðerinin arkasýna geç
            pos.x = other.anchoredPosition.x + _textWidth + gap;
        }

        current.anchoredPosition = pos;
    }
}