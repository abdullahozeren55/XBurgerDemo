using UnityEngine;

public class ServingZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Ýçeri giren þey tepsi mi?
        Tray tray = other.GetComponent<Tray>();
        if (tray == null) return;

        // Zone'a girdiðini tepsiye bildir (Drop anýnda kontrol için)
        tray.SetCurrentServingZone(this);

        // Eðer tepsi havadan atýldýysa (Grabbed deðilse) hemen iþle
        if (!tray.IsGrabbed)
        {
            ProcessTray(tray);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Tray tray = other.GetComponent<Tray>();
        if (tray != null)
        {
            // Zone'dan çýktý, referansý sil
            tray.SetCurrentServingZone(null);
        }
    }

    // Bu fonksiyonu hem TriggerEnter hem de Tray.OnDrop çaðýrabilir
    public void ProcessTray(Tray tray)
    {
        // Tepsi mount edildiyse (Customer eline aldýysa) iþlem yapma
        if (tray.transform.parent != null) return;

        CustomerManager.Instance.TryServeTray(tray);
    }
}