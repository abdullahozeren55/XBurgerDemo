using UnityEngine;

public class TrayTrigger : MonoBehaviour
{
    [SerializeField] private Tray parentTray;

    private void OnTriggerEnter(Collider other)
    {
        if (parentTray != null)
        {
            parentTray.TryPlaceItem(other);
        }
    }
}