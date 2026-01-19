using DG.Tweening;
using UnityEngine;

public class SodaButton : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public SodaMachine machine;
    [Tooltip("0: Cola, 1: Fanta, 2: Sprite...")]
    public int flavorIndex;

    [Tooltip("Bardaða atanacak mantýksal içecek türü")]
    public GameManager.DrinkTypes drinkType; // <-- YENÝ: Enum ekledik

    // --- IInteractable ---
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract = true;

    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;
    public PlayerManager.HandRigTypes HandRigType { get => PlayerManager.HandRigTypes.Interaction; set { } }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    // Görsel için baþlangýç pozisyonu
    [HideInInspector] public Vector3 initialPos;

    // Layer Cache
    private int interactableLayer;
    private int interactableOutlinedLayer;

    private void Awake()
    {
        initialPos = transform.localPosition;

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        // Direkt Makineye "Ben Tetiklendim" de, gerisine karýþma.
        if (machine != null)
        {
            machine.OnButtonTriggered(this);
        }
    }

    // Makine tarafýndan çaðrýlacak yardýmcý fonksiyonlar
    public void ChangeLayer(int layer) => gameObject.layer = layer;

    // --- Outline ---
    public void OnFocus() { if (CanInteract) ChangeLayer(interactableOutlinedLayer); }
    public void OnLoseFocus() { ChangeLayer(interactableLayer); }
    public void OutlineChangeCheck() { }
    public void HandleFinishDialogue() { }
}