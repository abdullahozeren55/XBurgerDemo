using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICustomer
{
    public enum Action
    {
        GoingToDestination,
        ReadyToOrder,
        ReadyToTalk,
        WaitingForOrder,
        ReceivedTrueBurger,
        ReceivedFalseBurger,
        ReceivedTrueDrink,
        ReceivedFalseDrink,
        ReceivedAllOrder,
        GotAnswerA,
        GotAnswerD,
        NotGotAnswer
    }

    public enum Feeling
    {
        NATURAL,
        HAPPY,
        ANGRY,
        SAD
    }

    public enum CustomerName
    {
        KEMAL,
        HIKMET,
        TARIK,
        NEVZAT,
        SUKRAN,
        ERTAN,
        ALEYNA,
        KEKO,
        NPCCUSTOMER0,
        NPCCUSTOMER1,
        NPCCUSTOMER2,
    }

    public enum DialogueAnim
    {
        NONE,
        TALK,
        LAUGH
    }

    [System.Serializable]
    public class CustomerDayChangesSegment
    {
        [Range(1, 5)]
        public int Day;
        [Range(1, 5)]
        public int RequiredLevel;
        public bool RequiredBoolean;
        public Material Material;
        public GameManager.BurgerTypes BurgerType;
        public GameManager.DrinkTypes DrinkType;
        public DialogueData BeforeOrderDialogueData;
        public DialogueData AfterOrderDialogueData;
        public DialogueData FalseBurgerDialogueData;
        public DialogueData TrueBurgerDialogueData;
        public DialogueData FalseDrinkDialogueData;
        public DialogueData TrueDrinkDialogueData;
        public DialogueData CompleteOrderDialogueData;
        public DialogueData NotAnsweringDialogueData;
        public DialogueData OptionADialogueData;
        public DialogueData OptionDDialogueData;
    }

    public DialogueData BeforeOrderDialogueData { get; set; }
    public DialogueData AfterOrderDialogueData { get; set; }
    public DialogueData FalseBurgerDialogueData { get; set; }
    public DialogueData TrueBurgerDialogueData { get; set; }
    public DialogueData FalseDrinkDialogueData { get; set; }
    public DialogueData TrueDrinkDialogueData { get; set; }
    public DialogueData CompleteOrderDialogueData { get; set; }
    public DialogueData NotAnsweringDialogueData { get; set; }
    public DialogueData OptionADialogueData { get; set; }
    public DialogueData OptionDDialogueData { get; set; }

    public CustomerDayChangesSegment[] CustomerDayChanges { get; set; }

    public CustomerData CustomerData { get; set; }
    public void ReceiveBurger(BurgerBox burgerBox);
    public void ReceiveDrink(Drink drink);
    public void HandleFinishDialogue();
    public void StartPathFollow(Transform destination);
    public void HandlePathFollow(Transform destination);
    public void HandleFootsteps();
    public void HandleIdle();
    public void HandleDialogueAnim(DialogueAnim dialogueAnim);

    public void ChangeLayer(int layer);

    public Action CurrentAction {  get; set; }
    public Feeling CurrentFeeling { get; set; }
    public CustomerName PersonName { get; set; }
    public GameManager.BurgerTypes BurgerType { get; set; }
    public GameManager.DrinkTypes DrinkType { get; set; }

    public Transform CameraLookAt { get; set; }

    public bool TrueDrinkReceived { get; set; }
    public bool TrueBurgerReceived { get; set; }

}
