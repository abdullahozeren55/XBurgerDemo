using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public enum HandRigTypes
    {
        Interaction,
        SingleHandGrab,
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

    public void ResetPlayerInteract(IInteractable interactable, bool shouldBeUninteractable)
    {
        firstPersonController.ResetInteract(interactable, shouldBeUninteractable);
    }

    public void PlayerOnUseReleaseGrabable(bool shouldDecideOutlineAndCrosshair)
    {
        firstPersonController.OnUseReleaseGrabable(shouldDecideOutlineAndCrosshair);
    }

    public void SetPlayerBasicMovements(bool can)
    {
        firstPersonController.CanMove = can;
        firstPersonController.CanSprint = can;
        firstPersonController.CanJump = can;
        firstPersonController.CanCrouch = can;
        firstPersonController.CanInteract = can;
        firstPersonController.CanGrab = can;
        firstPersonController.CanLook = can;
        firstPersonController.CanFootstep = can;
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

    public void SetPlayerCanPlay(bool can)
    {
        firstPersonController.CanPlay = can;
    }

    public void SetPlayerCanHeadBob(bool can)
    {
        firstPersonController.CanUseHeadbob = can;
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

    public void SetPlayerLeftUseHandLerp(Vector3 targetPos, Vector3 targetRot)
    {
        firstPersonController.SetLeftUseHandLerp(targetPos, targetRot);
    }

    public void PlayerResetLeftHandLerp()
    {
        firstPersonController.ResetLeftHandLerp();
    }

    public void PlayerStopUsingObject()
    {
        firstPersonController.StopUsingObject();
    }

    public void SetPlayerIsUsingItemXY(bool xValue, bool yValue)
    {
        firstPersonController.IsUsingItemX = xValue;
        firstPersonController.IsUsingItemY = yValue;
    }

    public void TryChangingFocusText(IInteractable interactable, string text)
    {
        firstPersonController.TryChangingFocusText(interactable, text);
    }

    public void TryChangingFocusText(IGrabable grabable, string text)
    {
        firstPersonController.TryChangingFocusText(grabable, text);
    }

    public void DecideUIText()
    {
        firstPersonController.DecideUIText();
    }

    public void HandlePlayerEnterExitColdRoom(bool isEntering)
    {

        if (isEntering)
        {
            CameraManager.Instance.PlayColdRoomEffects(true);

            firstPersonController.CanBreathe = true;
        }
        else
        {
            CameraManager.Instance.PlayColdRoomEffects(false);

            firstPersonController.CanBreathe = false;
        }
            
    }
}
