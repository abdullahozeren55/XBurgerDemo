using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public enum HandRigTypes
    {
        Interaction,
        SingleHandGrab,
        Talk,
        Nothing,
    }

    public enum HandGrabTypes
    {
        RegularGrab,
        BottleGrab,
        TrashGrab,
        KnifeGrab,
        ThinBurgerIngredientGrab,
        RegularBurgerIngredientGrab,
        ThickBurgerIngredientGrab,
        NoodleGrab,
        KettleGrab,
        WholeIngredientGrab,
        BigWholeIngredientGrab,
        WholeBunGrab
    }

    private FirstPersonController firstPersonController;
    private CharacterController characterController;

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        firstPersonController = FindFirstObjectByType<FirstPersonController>();
        characterController = FindFirstObjectByType<CharacterController>();
    }

    public IInteractable GetCurrentInteractable()
    {
        return firstPersonController.GetCurrentInteractable();
    }

    public IGrabable GetCurrentGrabable()
    {
        return firstPersonController?.GetCurrentGrabable();
    }

    public void ResetPlayerGrabAndInteract()
    {
        firstPersonController.ResetGrabAndInteract();
    }

    public void ResetPlayerGrab(IGrabable grabable)
    {
        firstPersonController.ResetGrab(grabable);
    }

    public void ResetPlayerInteract(IInteractable interactable)
    {
        firstPersonController.ResetInteract(interactable);
    }

    public void SetPlayerCanInteract(bool can)
    {
        firstPersonController.CanInteract = can;
    }

    public void SetPlayerCanGrab(bool can)
    {
        firstPersonController.CanGrab = can;
    }

    public void SetPlayerCanGrabAndInteract(bool can)
    {
        firstPersonController.CanGrab = can;
        firstPersonController.CanInteract = can;
    }

    public void ChangePlayerCanMove(bool canMove)
    {
        firstPersonController.CanMove = canMove;
    }

    public void ChangePlayerCurrentGrabable(IGrabable objectToGrab)
    {
        firstPersonController.ChangeCurrentGrabable(objectToGrab);
    }

    public void SetPlayerPushedByCustomer(bool value)
    {
        firstPersonController.isBeingPushedByACustomer = value;
    }

    public void MovePlayer(Vector3 moveForce)
    {
        characterController.Move(moveForce);
    }

    public void SetPlayerAnimBool(string boolName, bool value)
    {
        firstPersonController.SetAnimBool(boolName, value);
    }

    public void SetPlayerUseHandLerp(Vector3 targetPos, Vector3 targetRot, float timeToDo)
    {
        firstPersonController.SetUseHandLerp(targetPos, targetRot, timeToDo);
    }

    public void SetPlayerIsUsingItemXY(bool xValue, bool yValue)
    {
        firstPersonController.IsUsingItemX = xValue;
        firstPersonController.IsUsingItemY = yValue;
    }

    public void TryChangingFocusText(IInteractable interactable, Sprite sprite)
    {
        firstPersonController.TryChangingFocusText(interactable, sprite);
    }

    public void TryChangingFocusText(IGrabable grabable, Sprite sprite)
    {
        firstPersonController.TryChangingFocusText(grabable, sprite);
    }
}
