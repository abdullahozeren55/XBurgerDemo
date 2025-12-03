using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Settings : MonoBehaviour
{
    [Header("UI Referansý")]
    public TMP_Dropdown resolutionDropdown;

    // Filtrelenmiþ, temiz çözünürlükleri burada tutacaðýz
    private List<Resolution> filteredResolutions;

    void Start()
    {
        Resolution[] allResolutions = Screen.resolutions;

        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            // --- MAÐARA ADAMI FÝLTRESÝ ---
            // Eðer geniþlik 1280'den küçükse veya yükseklik 720'den küçükse
            // bu turu pas geç (continue), listeye ekleme.
            if (allResolutions[i].width < 1280 || allResolutions[i].height < 720)
                continue;
            // -----------------------------

            string option = allResolutions[i].width + " x " + allResolutions[i].height;

            if (!options.Contains(option))
            {
                options.Add(option);
                filteredResolutions.Add(allResolutions[i]);

                if (allResolutions[i].width == Screen.width &&
                    allResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        // Eðer (çok düþük ihtimalle) adamýn ekraný 1280'den küçükse ve liste boþ kaldýysa
        // En azýndan adamýn mevcut çözünürlüðünü ekleyelim ki oyun bozulmasýn.
        if (options.Count == 0)
        {
            string currentOption = Screen.width + " x " + Screen.height;
            options.Add(currentOption);
            filteredResolutions.Add(Screen.currentResolution);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // Dropdown deðiþince bu çalýþacak
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

        // --- EKLENEN SATIR ---
        // Ekran boyutu deðiþtiði için UI'larý uyar
        // (Ekran deðiþiminin algýlanmasý bazen 1 kare gecikebilir, o yüzden 
        // garanti olsun diye ufak bir Coroutine ile de çaðrýlabilir ama genelde direkt çalýþýr)
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.RefreshAllCanvases();
        }
    }
}
