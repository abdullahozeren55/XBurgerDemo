using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using static LightSwitch;
using VLB; // <--- EKLEME: VLB Kütüphanesi

public class PlayerInOutTrigger : MonoBehaviour
{
    public enum InOutTriggerType
    {
        Null,
        EnterColdRoom,
        ExitColdRoom,
    }
    [Header("Yön Ayarlarý")]
    [Tooltip("Yeþil Tarafa (Forward) geçince devreye girecek Snapshot")]
    public InOutTriggerType[] greenActions;

    [Tooltip("Kýrmýzý Tarafa (Back) geçince devreye girecek Snapshot")]
    public InOutTriggerType[] redActions;


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

            if (localPos.z > 0)
            {
                foreach (InOutTriggerType action in greenActions)
                {
                    if (action == InOutTriggerType.EnterColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(true);
                    else if (action == InOutTriggerType.ExitColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(false);
                }

            }
            else
            {
                foreach (InOutTriggerType action in redActions)
                {
                    if (action == InOutTriggerType.EnterColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(true);
                    else if (action == InOutTriggerType.ExitColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(false);
                }
            }
        }
    }

    // --- GÖRSELLEÞTÝRME (GÝZMOS) ---
    private void OnDrawGizmos()
    {
        // ... (Gizmos kodun ayný kalabilir) ...
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);

            Gizmos.color = new Color(0, 1, 0, 0.8f);
            Vector3 greenPos = box.center + new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(greenPos, 0.2f);

            Gizmos.color = new Color(1, 0, 0, 0.8f);
            Vector3 redPos = box.center - new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(redPos, 0.2f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(redPos, greenPos);
        }
    }
}