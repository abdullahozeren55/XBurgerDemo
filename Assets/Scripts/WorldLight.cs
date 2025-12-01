using UnityEngine;

public class WorldLight : MonoBehaviour
{
    void Awake()
    {
        DayManager.Instance.RegisterLight(gameObject);
        gameObject.SetActive(DayManager.Instance.CurrentDayState.shouldLightsUp);
    }

    void OnDestroy()
    {
        // Eðer oyun kapanýyorsa ve DayManager çoktan gittiyse hata verme
        if (DayManager.Instance != null)
        {
            DayManager.Instance.UnregisterLight(gameObject);
        }
    }
}