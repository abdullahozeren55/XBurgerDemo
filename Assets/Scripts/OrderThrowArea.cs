using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderThrowArea : MonoBehaviour
{
    [HideInInspector] public bool ShouldReceive;

    [Header("Sukran Settings")]
    [SerializeField] private BurgerBox sukranBurgerBox;

    private void Awake()
    {
        GameManager.Instance.orderThrowArea = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldReceive)
        {
            if (GameManager.Instance.CurrentCustomer.PersonName == ICustomer.CustomerName.SUKRAN && other.CompareTag("WholeIngredient"))
            {
                WholeIngredient wholeIngredient = other.GetComponent<WholeIngredient>();

                if (wholeIngredient.data.Type == WholeIngredientData.WholeIngredientType.PICKLE)
                {
                    GameManager.Instance.CurrentCustomer.ReceiveBurger(sukranBurgerBox);

                    Destroy(other.gameObject);
                }
            }
            else
            {
                if (other.CompareTag("Drink"))
                {
                    Drink drink = other.GetComponent<Drink>();

                    if (!drink.IsGrabbed && drink.CanBeReceived)
                    {
                        GameManager.Instance.CustomerReceiveDrink(drink);
                    }

                }
                else if (other.CompareTag("BurgerBoxClosed"))
                {
                    BurgerBox burgerBox = other.GetComponent<BurgerBox>();

                    if (!burgerBox.IsGrabbed && burgerBox.CanBeReceived)
                    {
                        GameManager.Instance.CustomerReceiveBurger(burgerBox);
                    }

                }
            }
            
        }
    }
}
