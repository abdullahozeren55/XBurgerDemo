using UnityEngine;

public class SodaMachineTriggerRelay : MonoBehaviour
{
    [SerializeField] private SodaMachine parentMachine;

    private void OnTriggerEnter(Collider other)
    {
        // Tetiklenince ana sepetteki fonksiyonu çaðýr
        if (parentMachine != null)
        {
            parentMachine.HandleCatch(other);
        }
    }
}
