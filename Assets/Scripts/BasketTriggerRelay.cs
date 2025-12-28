using UnityEngine;

public class BasketTriggerRelay : MonoBehaviour
{
    [SerializeField] private FryerBasket parentBasket;

    private void OnTriggerEnter(Collider other)
    {
        // Tetiklenince ana sepetteki fonksiyonu çaðýr
        if (parentBasket != null)
        {
            parentBasket.HandleCatch(other);
        }
    }
}