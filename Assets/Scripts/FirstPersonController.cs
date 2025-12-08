using Cinemachine;
using Febucci.UI.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class FirstPersonController : MonoBehaviour
{
    public bool CanPlay = true;
    public bool IsUsingItemX = false;
    public bool IsUsingItemY = false;
    private bool IsSprinting => CanSprint && Input.GetKey(sprintKey);
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKey(crouchKey);

    [Header("Functional Options")]
    public bool CanSprint = true;
    public bool CanJump = true;
    public bool CanCrouch = true;
    public bool CanUseHeadbob = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    public bool CanInteract = true;
    public bool CanGrab = true;
    public bool CanFootstep = true;
    public bool CanMove = true;
    public bool CanLook = true;
    public bool CanBreathe = true;

    [Header("Controls")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode interactKey = KeyCode.Mouse0;
    public KeyCode throwKey = KeyCode.Mouse1;
    public KeyCode phoneKey = KeyCode.Tab;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8.0f;
    [SerializeField] private float pushForce = 1f;
    [HideInInspector] public bool isBeingPushedByACustomer = false;

    [Header("Breathe Parameters")]
    [SerializeField] private Transform breathingParticlePoint;
    [SerializeField] private ParticleSystem breathingParticles;
    [SerializeField] private float breatheCooldownForSprint = 1f;
    [SerializeField] private float breatheCooldownForWalk = 3f;
    [SerializeField] private float breatheCooldownForCrouch = 5f;
    private float currentBreatheCooldown;
    private float breatheParticleLastInstantiateTime;

    [Header("Look Parameters")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform cameraFollow;
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 100)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 100)] private float lowerLookLimit = 45.0f;
    private float controlWeight = 1f;
    private float horizSpeed;
    private float vertSpeed;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;
    [SerializeField] private AudioSource jumpLandAudioSource = default;
    [SerializeField] private AudioClip[] woodJumpClips = default;
    [SerializeField] private AudioClip[] metalJumpClips = default;
    [SerializeField] private AudioClip[] grassJumpClips = default;
    [SerializeField] private AudioClip[] stoneJumpClips = default;
    [SerializeField] private AudioClip[] tileJumpClips = default;
    [SerializeField] private AudioClip[] gravelJumpClips = default;
    private float lastGroundedTime;
    private AudioClip lastPlayedJump;

    [Header("Landing Parameters")]
    [SerializeField] private AudioClip[] woodLandClips = default;
    [SerializeField] private AudioClip[] metalLandClips = default;
    [SerializeField] private AudioClip[] grassLandClips = default;
    [SerializeField] private AudioClip[] stoneLandClips = default;
    [SerializeField] private AudioClip[] tileLandClips = default;
    [SerializeField] private AudioClip[] gravelLandClips = default;
    [SerializeField] private GameObject landGroundCheckPoint;
    [SerializeField] private float noLandSoundCoyoteTime = 0.1f;
    private AudioClip lastPlayedLand;
    private bool justJumped = false;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchingHeight = 0.5f;
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float timeToCrouchStand = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 standingCenter = new Vector3(0f, 0f, 0f);
    private bool isCrouching;
    private Coroutine CrouchStandCoroutine = null;

    [Header("Headbob Parameters")]
    [SerializeField] private Transform eyeLevel;

    [Header("Sliding Parameters")]
    private Vector3 hitPointNormal;
    private bool IsSliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f, groundTypeLayers))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }

    [Header("Interaction Parameters")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private LayerMask interactionLayers = default;
    private float interactChargeTimer = 0f;
    private IInteractable currentInteractable;
    public bool InteractKeyIsDone;

    [Header("Grab Parameters")]
    [SerializeField] private LayerMask grabableLayers;
    [SerializeField] private LayerMask throwRaycastLayers;
    [SerializeField] private UnityEngine.UI.Image crosshair;
    [SerializeField] private Vector2 grabCrosshairSize = new Vector2(4f, 4f);
    [SerializeField] private Color grabCrosshairColor;
    [SerializeField] private Color useCrosshairColor;
    [SerializeField] private float maxThrowForce = 1.3f;
    [SerializeField] private float minThrowForce = 0.3f;
    [SerializeField] private float throwMaxChargeTime = 1.5f;
    [SerializeField] private float quickTapThreshold = 0.2f;
    public bool isUsingGrabbedItem = false;
    private float throwChargeTimer = 0f;
    private float currentThrowForce = 0f;
    private Vector2 defaultCrosshairSize;
    private Color defaultCrosshairColor;
    private IGrabable currentGrabable;
    private IGrabable otherGrabable;

    [Header("Footstep Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] woodClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] stoneClips = default;
    [SerializeField] private AudioClip[] tileClips = default;
    [SerializeField] private AudioClip[] gravelClips = default;
    [SerializeField] private LayerMask groundTypeLayers;
    [SerializeField] private GameObject groundTypeCheckRayPoint;
    private float footstepTimer = 0f;
    private Material currentGroundMaterial;
    private AudioClip lastPlayedFootstep;

    private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepMultiplier : IsSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Interaction Hand Parameters")]
    [SerializeField] private TwoBoneIKConstraint twoBoneIKConstraintLeftHand;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Vector3 positionOffsetForLeftHandInteraction;
    [SerializeField] private Vector3 rotationOffsetForLeftHandInteraction;
    private Vector3 currentPositionOffsetForLeftHand;
    private Vector3 currentRotationOffsetForLeftHand;
    private Coroutine leftHandRigLerpCoroutine;
    [Space]
    [SerializeField] private TwoBoneIKConstraint twoBoneIKConstraintRightHand;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Vector3 positionOffsetForRightHandInteraction;
    [SerializeField] private Vector3 rotationOffsetForRightHandInteraction;
    private Vector3 currentPositionOffsetForRightHand;
    private Vector3 currentRotationOffsetForRightHand;
    private Coroutine rightHandRigLerpCoroutine;
    [Space]
    [SerializeField] private float lerpTimeForInteractionHand = 0.2f;

    [Header("Grab Parameters")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private TMP_Text focusText;
    [SerializeField] private TypewriterCore focusTextAnim;
    private bool focusTextComplete;
    private Coroutine grabbedUseCoroutine;

    [Header("HandControlSettings")]
    [SerializeField, Range(0, 10)] private float handControlSpeedX = 0.2f;
    [SerializeField, Range(0, 10)] private float handControlSpeedY = 0.2f;
    private Vector3 handUseStartOffset;
    private Vector3 handUseDelta; // mouse hareketlerini biriktirecek

    [Header("Phone Settings")]
    [SerializeField] private GameObject phoneGO;
    [SerializeField] private float phoneCoyoteForInteract = 0.35f;
    [SerializeField] private float phoneCoyoteForGrab = 0.19f;
    private IGrabable phoneGrabable;
    private bool canUsePhone;
    private float lastPhoneKeyPressedTime;
    private float coyoteTimeForPhone;

    [Header("UI Settings")]
    [SerializeField] private GameObject useUI;
    [SerializeField] private GameObject takeUI;
    [SerializeField] private GameObject interactUI; 
    [SerializeField] private GameObject dropUI;
    [SerializeField] private GameObject throwUI;
    [SerializeField] private GameObject useTakeInteractParent;
    [SerializeField] private GameObject throwDropParent;
    private bool showHints = true;
    private bool showInteractText = true;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private float throwVolume = 1f;
    [SerializeField] private float throwMinPitch = 0.85f;
    [SerializeField] private float throwMaxPitch = 1.15f;

    private Coroutine singleHandThrowCoroutine;

    private Camera mainCamera;
    private CharacterController characterController;
    private Animator anim;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private int uninteractableLayer;

    private float rotationX = 0f;

    private void Awake()
    {

        mainCamera = Camera.main;
        characterController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        phoneGrabable = phoneGO.GetComponent<IGrabable>();

        defaultCrosshairSize = crosshair.rectTransform.sizeDelta;
        defaultCrosshairColor = crosshair.color;

        canUsePhone = true;

        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        InteractKeyIsDone = false;

        DecideOutlineAndCrosshair();
    }

    void Start()
    {

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.updateWhenOffscreen = true; // ensures it's never culled
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 5f); // overly large bounds to guarantee visibility
        }

        CameraManager.Instance.InitializeCamera(CameraManager.CameraName.FirstPerson);

        RefreshUISettings();

        PhoneManager.Instance.SetMissionText("Merhaba! Ben Volkan Konak!");
    }

    private void Update()
    {
        if (CanPlay)
        {
            if (CanMove)
                HandleMovementInput();
            else
                anim.SetFloat("speed", 0f, 0.15f, Time.deltaTime);

            if (CanJump)
                HandleJump();
            else if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
                characterController.Move(moveDirection * Time.deltaTime);
            }

            if (!characterController.isGrounded)
                HandleLand();
            else
            {
                anim.SetBool("isGrounded", true);
                lastGroundedTime = Time.time;
            }
                

            if (CanCrouch)
                HandleCrouch();
            else
            {
                if (CrouchStandCoroutine != null)
                {
                    StopCoroutine(CrouchStandCoroutine);
                    CrouchStandCoroutine = null;
                }

                CrouchStandCoroutine = StartCoroutine(CrouchStand(false));
            }

            if (CanInteract)
            {
                HandleInteractionInput();
                HandleInteractionCheck();
            }

            if (CanGrab)
            {
                HandleGrabInput();
                HandleGrabCheck();
            }

            else if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                DecideOutlineAndCrosshair();
            }
            

            if (CanFootstep)
                HandleFootsteps();

            if (Input.GetKeyDown(phoneKey))
            {
                lastPhoneKeyPressedTime = Time.time;

                if (!phoneGrabable.IsGrabbed && canUsePhone)
                {
                    phoneGrabable.IsUseable = true;
                    ChangeCurrentGrabable(phoneGrabable);
                }
                    
            }
                
            if (CanMove)
                ApplyFinalMovements();
        }

        else
        {
            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
                characterController.Move(moveDirection * Time.deltaTime);

                HandleLand();
            }

            if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                DecideOutlineAndCrosshair();
            }

            anim.SetFloat("speed", 0f, 0.15f, Time.deltaTime);

        }

        if (CanBreathe)
        {
            HandleBreatheParticle();
        }

    }

    private void LateUpdate()
    {   
        if (CanPlay)
        {
            if (CanLook)
            {
                HandleMouseAndHandControl();

                HandleHandTargetPositions();
            }
            
        }

        if (CanUseHeadbob)
            HandleHeadbob();
    }

    private void HandleBreatheParticle()
    {
        currentBreatheCooldown = IsSprinting ? breatheCooldownForSprint : isCrouching ? breatheCooldownForCrouch : breatheCooldownForWalk;

        if (Time.time > breatheParticleLastInstantiateTime + currentBreatheCooldown)
        {
            ParticleSystem ps = Instantiate(breathingParticles, breathingParticlePoint.position, breathingParticlePoint.rotation, breathingParticlePoint);

            breatheParticleLastInstantiateTime = Time.time;
        }
    }

    private void HandleMouseAndHandControl()
    {
        float targetWeight = CanLook ? 1f : 0f;
        controlWeight = Mathf.Lerp(controlWeight, targetWeight, Time.deltaTime * 10f);

        float mouseX = Input.GetAxis("Mouse X") * controlWeight;
        float mouseY = Input.GetAxis("Mouse Y") * controlWeight;

        // --- Kamera Hareketi ---
        horizSpeed = lookSpeedX;
        vertSpeed = lookSpeedY;

        if (IsUsingItemX)
        {
            horizSpeed = handControlSpeedX * 7f;
            vertSpeed /= 3f;
        }

        if (IsUsingItemY)
        {
            vertSpeed = handControlSpeedY * 7f;
            horizSpeed /= 3f;
        }

        // Vertical look (Pitch)
        rotationX -= mouseY * vertSpeed;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        headBone.localRotation = Quaternion.Euler(rotationX, 0, 0);
        cameraFollow.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Horizontal look (Yaw)
        transform.Rotate(Vector3.up * mouseX * horizSpeed);

        // --- El Hareketi ---
        if (IsUsingItemX || IsUsingItemY)
        {
            Vector3 delta = Vector3.zero;
            if (IsUsingItemX) delta.x = mouseX * handControlSpeedX;
            if (IsUsingItemY) delta.y = mouseY * handControlSpeedY;

            handUseDelta += delta;

            // Toplam offset = baþlangýç + delta
            currentPositionOffsetForRightHand = handUseStartOffset + handUseDelta;

            // Clamp lokal eksende
            handUseDelta.x = Mathf.Clamp(handUseDelta.x, -1.5f, 1.5f);
            handUseDelta.y = Mathf.Clamp(handUseDelta.y, -1.5f, 1.5f);
            handUseDelta.z = Mathf.Clamp(handUseDelta.z, 0.1f, 0.3f); // ileri-geri
        }
    }

    private void HandleHandTargetPositions()
    {

        leftHandTarget.position = mainCamera.transform.position + mainCamera.transform.TransformDirection(currentPositionOffsetForLeftHand);
        leftHandTarget.rotation = mainCamera.transform.rotation * Quaternion.Euler(currentRotationOffsetForLeftHand);
        rightHandTarget.position = mainCamera.transform.position + mainCamera.transform.TransformDirection(currentPositionOffsetForRightHand);
        rightHandTarget.rotation = mainCamera.transform.rotation * Quaternion.Euler(currentRotationOffsetForRightHand);
    }

    private void HandleMovementInput()
    {
        float speed = isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        currentInput = new Vector2(speed * vertical, speed * horizontal);

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) +
                        (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;

        // Calculate movement magnitude (ignores Y)
        float horizontalSpeed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;
        anim.SetFloat("speed", horizontalSpeed, 0.15f, Time.deltaTime);
    }

    private void HandleJump()
    {
        if (ShouldJump)
        {

            anim.SetBool("isGrounded", false);
            anim.SetTrigger("jump");

            FindCurrentGroundMaterial(1.9f);

            if (currentGroundMaterial != null)
            {
                if (currentGroundMaterial.name.Contains("Wood"))
                    PlayJumpLand(woodJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Metal"))
                    PlayJumpLand(metalJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Grass"))
                    PlayJumpLand(grassJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Stone"))
                    PlayJumpLand(stoneJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Tile"))
                    PlayJumpLand(tileJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Gravel"))
                    PlayJumpLand(gravelJumpClips, true);
            }

            moveDirection.y = jumpForce;

            justJumped = true;
        }
            
    }

    private void HandleLand()
    {
        Physics.Raycast(landGroundCheckPoint.transform.position, Vector3.down, out RaycastHit hit, 1f, groundTypeLayers);

        if (hit.collider != null && moveDirection.y <= -0.2f)
        {

            if ((!jumpLandAudioSource.isPlaying && !isBeingPushedByACustomer && Time.time > lastGroundedTime + noLandSoundCoyoteTime) || justJumped)
            {
                FindCurrentGroundMaterial(10f);

                if (currentGroundMaterial != null)
                {
                    if (currentGroundMaterial.name.Contains("Wood"))
                        PlayJumpLand(woodLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Metal"))
                        PlayJumpLand(metalLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Grass"))
                        PlayJumpLand(grassLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Stone"))
                        PlayJumpLand(stoneLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Tile"))
                        PlayJumpLand(tileLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Gravel"))
                        PlayJumpLand(gravelLandClips, false);
                }
            }

            justJumped = false;
        }
    }

    private void HandleCrouch()
    {
        if ((ShouldCrouch && !isCrouching) || (isCrouching && !ShouldCrouch && !Physics.Raycast(mainCamera.transform.position, Vector3.up, standingHeight/1.7f)))
        {
            if (CrouchStandCoroutine != null)
            {
                StopCoroutine(CrouchStandCoroutine);
                CrouchStandCoroutine = null;
            }

            CrouchStandCoroutine = StartCoroutine(CrouchStand(ShouldCrouch));
        }
    }

    private void HandleHeadbob()
    {
        cameraFollow.position = eyeLevel.position;
    }

    private void DecideInteractableOutlineColor()
    {
        if (currentInteractable != null)
        {
            if (isUsingGrabbedItem ||
            (((currentGrabable != null && currentGrabable.IsGrabbed) || phoneGrabable.IsGrabbed) && currentInteractable.HandRigType == PlayerManager.HandRigTypes.SingleHandGrab))
            {
                currentInteractable.OutlineShouldBeRed = true;
            }
            else
            {
                currentInteractable.OutlineShouldBeRed = false;
            }

            currentInteractable.OutlineChangeCheck();
        }
        
    }

    private void DecideGrabableOutlineColor()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed && otherGrabable != null)
        {
            otherGrabable.OutlineShouldBeRed = true;
            otherGrabable.OutlineChangeCheck();
        }
        else if (phoneGrabable.IsGrabbed && currentGrabable != null && currentGrabable != phoneGrabable)
        {
            currentGrabable.OutlineShouldBeRed = true;
            currentGrabable.OutlineChangeCheck();
        }
        else
        {
            if (otherGrabable != null)
            {
                otherGrabable.OutlineShouldBeRed = false;
                otherGrabable.OutlineChangeCheck();
            }

            if (currentGrabable != null && currentGrabable != phoneGrabable)
            {
                currentGrabable.OutlineShouldBeRed = false;
                currentGrabable.OutlineChangeCheck();
            }

        }
    }

    private void DecideCrosshairColor()
    {
        if ((currentInteractable != null && currentInteractable.OutlineShouldBeRed) || (currentGrabable == phoneGrabable && isUsingGrabbedItem))
        {
            ChangeCrosshairColor(useCrosshairColor);
        }
        else if (currentGrabable != null)
        {
            ChangeCrosshairColor(phoneGrabable.IsGrabbed && currentGrabable != phoneGrabable ? useCrosshairColor : !currentGrabable.IsGrabbed ? grabCrosshairColor : otherGrabable != null ? useCrosshairColor : defaultCrosshairColor);
        }
        else
        {
            ChangeCrosshairColor(defaultCrosshairColor);
        }
    }

    private void DecideCrosshairSize()
    {
        if (currentInteractable != null || (currentGrabable != null && (!currentGrabable.IsGrabbed || otherGrabable != null)))
        {
            ChangeCrosshairSize(grabCrosshairSize);
        }
        else
        {
            ChangeCrosshairSize(defaultCrosshairSize);
        }
    }

    private void DecideFocusText()
    {
        if (!showInteractText)
        {
            return;
        }

        // 1 — ÝNTERACTABLE
        if (currentInteractable != null)
        {
            string localizedText = LocalizationManager.Instance.GetText(currentInteractable.FocusTextKey);

            focusText.color = crosshair.color;
            focusTextAnim.StopDisappearingText();

            if (focusText.text != localizedText)
            {
                SetFocusTextComplete(false);
                focusTextAnim.ShowText(localizedText);
            }
            else if (!focusTextComplete)
            {
                focusTextAnim.StartShowingText();
            }
        }
        // 2 — GRABABLE (elde olmayan)
        else if (currentGrabable != null && !currentGrabable.IsGrabbed)
        {
            string localizedText = LocalizationManager.Instance.GetText(currentGrabable.FocusTextKey);

            focusText.color = crosshair.color;
            focusTextAnim.StopDisappearingText();

            if (focusText.text != localizedText)
            {
                SetFocusTextComplete(false);
                focusTextAnim.ShowText(localizedText);
            }
            else if (!focusTextComplete)
            {
                focusTextAnim.StartShowingText();
            }
        }
        // 3 — OTHER GRABABLE
        else if (otherGrabable != null)
        {
            string localizedText = LocalizationManager.Instance.GetText(otherGrabable.FocusTextKey);

            focusText.color = crosshair.color;
            focusTextAnim.StopDisappearingText();

            if (focusText.text != localizedText)
            {
                SetFocusTextComplete(false);
                focusTextAnim.ShowText(localizedText);
            }
            else if (!focusTextComplete)
            {
                focusTextAnim.StartShowingText();
            }
        }
        // 4 — NULL / TEMÝZLEME
        else
        {
            SetFocusTextComplete(false);
            focusTextAnim.StopShowingText();
            focusTextAnim.StartDisappearingText();
        }
    }

    public void DecideUIText()
    {
        // --- AYAR KONTROLÜ (YENÝ) ---
        if (!showHints)
        {
            SetAllPrompts(false); // Hepsini kapat ve çýk
            return;
        }

        // --- 1. DURUM ANALÝZÝ ---
        bool isHandBusy = isUsingGrabbedItem;

        // Elimde bir þey var mý?
        bool isHoldingItem = currentGrabable != null && currentGrabable.IsGrabbed;

        // Bir Interactable'a bakýyor muyum?
        bool isLookingAtInteractable = currentInteractable != null;

        // Yerdeki bir Grabable'a bakýyor muyum?
        bool isLookingAtGrabbable = currentGrabable != null && !currentGrabable.IsGrabbed;


        // --- 2. ACÝL DURUM ---
        if (isHandBusy)
        {
            SetAllPrompts(false);
            return;
        }

        bool showInteract = false;

        if (isLookingAtInteractable)
        {
            // Senaryo A: Elim boþ.
            // Býçaklýða da baksam, ýþýða da baksam etkileþime girebilirim.
            if (!isHoldingItem)
            {
                showInteract = true;
            }
            // Senaryo B: Elim dolu.
            // Sadece "Eþya Vermeyen" (Yani tipi SingleHandGrab OLMAYAN) þeylerle etkileþime girebilirim.
            // Örn: Iþýk düðmesi, Kapý kolu.
            else if (currentInteractable.HandRigType != PlayerManager.HandRigTypes.SingleHandGrab)
            {
                showInteract = true;
            }
            // Senaryo C: Elim dolu ve Býçaklýða bakýyorum.
            // showInteract = false kalýr. Çünkü elimdekini býrakmadan býçak alamam.
        }


        // --- DÝÐERLERÝ AYNI ---
        bool showTake = isLookingAtGrabbable && !isHoldingItem;
        bool showUse = isHoldingItem && currentGrabable.IsUseable;
        bool showDrop = isHoldingItem;
        bool showThrow = isHoldingItem;


        // --- 4. UYGULAMA ---
        if (interactUI) interactUI.SetActive(showInteract);
        if (takeUI) takeUI.SetActive(showTake);
        if (useUI) useUI.SetActive(showUse);
        if (dropUI) dropUI.SetActive(showDrop);
        if (throwUI) throwUI.SetActive(showThrow);

        // --- 5. PARENT YÖNETÝMÝ ---
        if (useTakeInteractParent != null)
        {
            bool parentActive = showUse || showTake || showInteract;
            useTakeInteractParent.SetActive(parentActive);
        }

        if (throwDropParent != null)
        {
            bool parentActive = showDrop || showThrow;
            throwDropParent.SetActive(parentActive);
        }
    }

    // Yardýmcý fonksiyonu da güncelle ki hepsini kapatsýn
    private void SetAllPrompts(bool isActive)
    {
        if (interactUI) interactUI.SetActive(isActive);
        if (takeUI) takeUI.SetActive(isActive);
        if (useUI) useUI.SetActive(isActive);
        if (dropUI) dropUI.SetActive(isActive);
        if (throwUI) throwUI.SetActive(isActive);

        // Parentlarý da kapat
        if (useTakeInteractParent) useTakeInteractParent.SetActive(isActive);
        if (throwDropParent) throwDropParent.SetActive(isActive);
    }

    private void DecideOutlineAndCrosshair()
    {
        DecideInteractableOutlineColor();
        DecideGrabableOutlineColor();
        DecideCrosshairColor();
        DecideFocusText();
        DecideUIText();
        DecideCrosshairSize();
    }

    private void HandleInteractionCheck()
    {
        if (Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlined") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {

                    if (currentInteractable == null)
                    {
                        currentInteractable = hit.collider.gameObject.GetComponent<IInteractable>();

                        if (currentInteractable != null)
                        {
                            DecideOutlineAndCrosshair();
                            currentInteractable.OnFocus();
                        }
                        
                    }
                    else if (currentInteractable != hit.collider.gameObject.GetComponent<IInteractable>())
                    {

                        currentInteractable.OnLoseFocus();
                        currentInteractable = null;

                        currentInteractable = hit.collider.gameObject.GetComponent<IInteractable>();

                        if (currentInteractable != null)
                        {
                            DecideOutlineAndCrosshair();
                            currentInteractable.OnFocus();
                        }
                    }
                }
                else if (currentInteractable != null)
                {
                    currentInteractable.OnLoseFocus();
                    currentInteractable = null;
                    DecideOutlineAndCrosshair();
                }
            }
            else if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                DecideOutlineAndCrosshair();
            }
        }
        else if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
            DecideOutlineAndCrosshair();
        }
    }

    private void HandleInteractionInput()
    {
        if (currentGrabable == null || !currentGrabable.IsGrabbed || !currentGrabable.IsUseable)
        {
            // EL BOS normal interact (tap)
            if (Input.GetKeyDown(interactKey))
            {
                TryToInteract();
            }
        }
        else if (throwChargeTimer < 0.1f)
        {

            if (Input.GetKey(interactKey))
            {
                interactChargeTimer += Time.deltaTime;

                if (!isUsingGrabbedItem && interactChargeTimer > quickTapThreshold)
                {
                    // threshold’u geçtiði an kullanmaya baþla
                    currentGrabable.OnUseHold();
                    isUsingGrabbedItem = true;

                    DecideOutlineAndCrosshair();
                }
            }

            if (Input.GetKeyUp(interactKey))
            {
                if (!isUsingGrabbedItem && !InteractKeyIsDone)
                {
                    // kýsa týk interact
                    TryToInteract();
                }
                else if (isUsingGrabbedItem)
                {
                    // uzun basma býrakýldý kullaným bitti
                    currentGrabable.OnUseRelease();
                }

                interactChargeTimer = 0f;
                isUsingGrabbedItem = false;
                InteractKeyIsDone = false;

                DecideOutlineAndCrosshair();
            }
        }
        else
        {
            if (Input.GetKeyUp(interactKey))
            {
                TryToInteract();
            }
        }
    }

    private void TryToInteract()
    {
        if (currentInteractable != null && Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayers) && !currentInteractable.OutlineShouldBeRed)
        {
            if (hit.collider.gameObject.GetComponent<IInteractable>() == currentInteractable)
            {
                if ((currentGrabable == null || !currentGrabable.IsGrabbed) && !phoneGrabable.IsGrabbed)
                {

                    if (rightHandRigLerpCoroutine != null)
                    {
                        StopCoroutine(rightHandRigLerpCoroutine);

                        rightHandRigLerpCoroutine = null;
                    }

                    if (currentInteractable.HandRigType == PlayerManager.HandRigTypes.Interaction)
                    {
                        currentPositionOffsetForRightHand = positionOffsetForRightHandInteraction;
                        currentRotationOffsetForRightHand = rotationOffsetForRightHandInteraction;

                        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, true));
                    }

                    coyoteTimeForPhone = phoneCoyoteForInteract;
                }
                else
                {
                    if (leftHandRigLerpCoroutine != null) StopCoroutine(leftHandRigLerpCoroutine);

                    if (currentInteractable.HandRigType == PlayerManager.HandRigTypes.Interaction)
                    {
                        currentPositionOffsetForLeftHand = positionOffsetForLeftHandInteraction;
                        currentRotationOffsetForLeftHand = rotationOffsetForLeftHandInteraction;

                        leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(true, true));
                    }
                }

                currentInteractable.OnInteract();
            }
        }
    }

    private void HandleGrabCheck()
    {

        if (Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabable") || hit.collider.gameObject.layer == LayerMask.NameToLayer("GrabableOutlined") || hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {
                    if (currentGrabable == null)
                    {
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                            DecideOutlineAndCrosshair();
                        }

                        currentGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                        
                        if (currentGrabable != null)
                            currentGrabable.OnFocus();

                        DecideOutlineAndCrosshair();
                    }
                    else if (currentGrabable.IsGrabbed)
                    {
                        if (otherGrabable != null && hit.collider.gameObject.GetComponent<IGrabable>() != otherGrabable)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                            DecideOutlineAndCrosshair();
                        }

                        if (otherGrabable == null)
                        {
                            otherGrabable = hit.collider.gameObject.GetComponent<IGrabable>();

                            if (otherGrabable != null)
                            {
                                otherGrabable.OnFocus();
                                DecideOutlineAndCrosshair();
                            }
                        }

                        
                    }
                    else if (currentGrabable != hit.collider.gameObject.GetComponent<IGrabable>())
                    {
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                            DecideOutlineAndCrosshair();
                        }

                        currentGrabable.OnLoseFocus();
                        DecideOutlineAndCrosshair();
                        currentGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                        currentGrabable.OnFocus();
                        DecideOutlineAndCrosshair();
                    }
                }
                else
                {
                    if (currentGrabable != null && !currentGrabable.IsGrabbed)
                    {
                        currentGrabable.OnLoseFocus();
                        currentGrabable = null;
                        DecideOutlineAndCrosshair();
                    }

                    if (otherGrabable != null)
                    {
                        otherGrabable.OnLoseFocus();
                        otherGrabable = null;
                        DecideOutlineAndCrosshair();
                    }
                }

            }
            else
            {
                if (currentGrabable != null && !currentGrabable.IsGrabbed)
                {
                    currentGrabable.OnLoseFocus();
                    currentGrabable = null;
                    DecideOutlineAndCrosshair();
                }

                if (otherGrabable != null)
                {
                    otherGrabable.OnLoseFocus();
                    otherGrabable = null;
                    DecideOutlineAndCrosshair();
                }
            }
        }
        else
        {
            if (currentGrabable != null && !currentGrabable.IsGrabbed)
            {
                currentGrabable.OnLoseFocus();
                currentGrabable = null;
                DecideOutlineAndCrosshair();
            }

            if (otherGrabable != null)
            {
                otherGrabable.OnLoseFocus();
                otherGrabable = null;
                DecideOutlineAndCrosshair();
            }
        }
    }

    private void HandleGrabInput()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed && !isUsingGrabbedItem)
        {
            if (phoneGrabable.IsGrabbed)
            {
                if (Input.GetKeyDown(phoneKey) || Input.GetKeyDown(throwKey))
                {
                    phoneGrabable.IsUseable = false;

                    if (rightHandRigLerpCoroutine != null)
                    {
                        StopCoroutine(rightHandRigLerpCoroutine);
                        rightHandRigLerpCoroutine = null;
                    }

                    rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));

                    ResetHandAnim();

                    SetHandAnimBoolsOff();

                    phoneGrabable.OnDrop(Vector3.zero, 0f);

                    currentGrabable = null;

                    Invoke("DecideOutlineAndCrosshair", 0.16f);
                }
                
            }
            else
            {
                if (Input.GetKeyDown(throwKey)) // right click starts
                {
                    ResetHandAnim();

                    CameraManager.Instance.PlayThrowEffects(true);

                    anim.SetBool("chargingThrow", true);

                    DecideOutlineAndCrosshair();

                    if (singleHandThrowCoroutine != null)
                    {
                        StopCoroutine(singleHandThrowCoroutine);
                        singleHandThrowCoroutine = null;
                    }

                    singleHandThrowCoroutine = StartCoroutine(SingleHandThrow());

                }
                if (Input.GetKeyUp(throwKey))
                {

                    CameraManager.Instance.PlayThrowEffects(false);

                    

                    if (singleHandThrowCoroutine != null)
                    {
                        StopCoroutine(singleHandThrowCoroutine);
                        singleHandThrowCoroutine = null;
                    }

                    if (rightHandRigLerpCoroutine != null)
                    {
                        StopCoroutine(rightHandRigLerpCoroutine);
                        rightHandRigLerpCoroutine = null;
                    }

                    rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));

                    ResetHandAnim();

                    //TARGET CALCULATION START

                    Vector3 targetPoint;
                    Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                    if (Physics.Raycast(ray, out RaycastHit hit, 50f, throwRaycastLayers))
                        targetPoint = hit.point; // crosshair neye bakýyorsa
                    else
                        targetPoint = ray.GetPoint(50f); // boþluða doðru uzaða

                    Vector3 throwDir = (targetPoint - grabPoint.position).normalized;

                    //TARGET CALCULATION END

                    if (throwChargeTimer <= quickTapThreshold)
                    {
                        currentGrabable.OnDrop(throwDir, isCrouching ? minThrowForce * 0.5f : IsSprinting ? minThrowForce * 1.5f : minThrowForce);
                    }
                    else
                    {
                        SoundManager.Instance.PlaySoundFX(throwSound, grabPoint, throwVolume, throwMinPitch, throwMaxPitch);
                        currentGrabable.OnThrow(throwDir, isCrouching ? currentThrowForce * 0.5f : IsSprinting ? currentThrowForce * 1.5f : currentThrowForce);
                    }

                    throwChargeTimer = 0f;

                    anim.SetBool("throw", false);

                    currentGrabable = null;
                    
                    SetHandAnimBoolsOff();

                    DecideOutlineAndCrosshair();
                }
            }
            
        }
        else if (Input.GetKeyDown(interactKey) && currentGrabable != null && Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider.gameObject.GetComponent<IGrabable>() == currentGrabable && !phoneGrabable.IsGrabbed)
            {

                InteractKeyIsDone = true;
                currentGrabable.OnGrab(grabPoint);
                DecideOutlineAndCrosshair();
                DecideGrabAnimBool();

                if (rightHandRigLerpCoroutine != null)
                {
                    StopCoroutine(rightHandRigLerpCoroutine);
                    rightHandRigLerpCoroutine = null;
                }

                currentPositionOffsetForRightHand = currentGrabable.GrabPositionOffset;
                currentRotationOffsetForRightHand = currentGrabable.GrabRotationOffset;

                coyoteTimeForPhone = phoneCoyoteForGrab;

                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, false));

                DecideOutlineAndCrosshair();
            }
        }
    }

    private void SetHandAnimBoolsOff()
    {
        anim.SetBool("regularGrab", false);
        anim.SetBool("bottleGrab", false);
        anim.SetBool("trashGrab", false);
        anim.SetBool("knifeGrab", false);
        anim.SetBool("thinBurgerIngredientGrab", false);
        anim.SetBool("regularBurgerIngredientGrab", false);
        anim.SetBool("thickBurgerIngredientGrab", false);
        anim.SetBool("noodleGrab", false);
        anim.SetBool("kettleGrab", false);
        anim.SetBool("wholeIngredientGrab", false);
        anim.SetBool("bigWholeIngredientGrab", false);
        anim.SetBool("wholeBunGrab", false);
        anim.SetBool("chargingThrow", false);
    }
    private void DecideGrabAnimBool()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed)
        {
            anim.SetTrigger("endThrowInstantly");

            switch (currentGrabable.HandGrabType)
            {
                case PlayerManager.HandGrabTypes.RegularGrab:
                    anim.SetBool("regularGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.BottleGrab:
                    anim.SetBool("bottleGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.TrashGrab:
                    anim.SetBool("trashGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.KnifeGrab:
                    anim.SetBool("knifeGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.ThinBurgerIngredientGrab:
                    anim.SetBool("thinBurgerIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.RegularBurgerIngredientGrab:
                    anim.SetBool("regularBurgerIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.ThickBurgerIngredientGrab:
                    anim.SetBool("thickBurgerIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.NoodleGrab:
                    anim.SetBool("noodleGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.KettleGrab:
                    anim.SetBool("kettleGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.WholeIngredientGrab:
                    anim.SetBool("wholeIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.BigWholeIngredientGrab:
                    anim.SetBool("bigWholeIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.WholeBunGrab:
                    anim.SetBool("wholeBunGrab", true);
                    break;
            }
        }
    }

    public void ResetGrabAndInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }

        if (currentGrabable != null)
        {
            currentGrabable.OnLoseFocus();
            currentGrabable = null;
        }

        if (otherGrabable != null)
        {
            otherGrabable.OnLoseFocus();
            otherGrabable = null;
        }

        DecideOutlineAndCrosshair();
    }

    public void StopUsingObject()
    {
        interactChargeTimer = 0f;
        isUsingGrabbedItem = false;

        DecideOutlineAndCrosshair();
    }

    public void ResetGrab(IGrabable grabable)
    {
        if (currentGrabable != null)
        {
            if(currentGrabable == grabable)
            {
                currentGrabable.OnLoseFocus();
                currentGrabable = null;
            }
        }

        if (otherGrabable != null)
        {
            if (otherGrabable == grabable)
            {
                otherGrabable.OnLoseFocus();
                otherGrabable = null;
            }
        }

        DecideOutlineAndCrosshair();
    }

    public void ResetInteract(IInteractable interactable, bool shouldBeUninteractable)
    {
        if (currentInteractable == interactable)
        {
            currentInteractable.OnLoseFocus();

            if (shouldBeUninteractable)
            {
                currentInteractable.CanInteract = false;
                currentInteractable.ChangeLayer(uninteractableLayer);
            }   

            currentInteractable = null;
            DecideOutlineAndCrosshair();
        }
        else if (interactable != null)
        {
            if (shouldBeUninteractable)
            {
                interactable.CanInteract = false;
                interactable.ChangeLayer(uninteractableLayer);
            }
        }
    }

    public void ChangeCurrentGrabable(IGrabable grabObject)
    {
        currentGrabable = grabObject;
        currentGrabable.OnGrab(grabPoint);
        DecideGrabAnimBool();
        DecideOutlineAndCrosshair();

        if (rightHandRigLerpCoroutine != null)
        {
            StopCoroutine(rightHandRigLerpCoroutine);
            rightHandRigLerpCoroutine = null;
        }

        currentPositionOffsetForRightHand = currentGrabable.GrabPositionOffset;
        currentRotationOffsetForRightHand = currentGrabable.GrabRotationOffset;

        coyoteTimeForPhone = phoneCoyoteForGrab;

        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, false));
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded || currentInput == Vector2.zero) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            FindCurrentGroundMaterial(1.9f);

            if(currentGroundMaterial != null)
            {
                if (currentGroundMaterial.name.Contains("Wood"))
                    PlayFootstep(woodClips);
                else if (currentGroundMaterial.name.Contains("Metal"))
                    PlayFootstep(metalClips);
                else if (currentGroundMaterial.name.Contains("Grass"))
                    PlayFootstep(grassClips);
                else if (currentGroundMaterial.name.Contains("Stone"))
                    PlayFootstep(stoneClips);
                else if (currentGroundMaterial.name.Contains("Tile"))
                    PlayFootstep(tileClips);
                else if (currentGroundMaterial.name.Contains("Gravel"))
                    PlayFootstep(gravelClips);

                footstepTimer = GetCurrentOffset;
            }      
        }
    }

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (willSlideOnSlopes && IsSliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void ChangeCrosshairSize(Vector2 size)
    {
        crosshair.rectTransform.sizeDelta = size;
    }

    private void ChangeCrosshairColor(Color color) => crosshair.color = color;

    private int GetSubMeshIndex(Mesh mesh, int triangleIndex)
    {
        if (!mesh.isReadable) return 0;

        var triangleCounter = 0;
        for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            var indexCount = mesh.GetSubMesh(subMeshIndex).indexCount;
            triangleCounter += indexCount / 3;
            if (triangleIndex < triangleCounter) return subMeshIndex;
        }

        return 0;
    }

    private void FindCurrentGroundMaterial(float rayDistance)
    {
        Physics.Raycast(groundTypeCheckRayPoint.transform.position, Vector3.down, out RaycastHit hit, rayDistance, groundTypeLayers);

        if (hit.collider)
        {
            hit.collider.TryGetComponent<MeshRenderer>(out var renderer);

            if (renderer != null)
            {
                var materials = renderer.materials;
                var index = hit.triangleIndex;
                var mesh = hit.transform.GetComponent<MeshFilter>().mesh;
                var subMeshIndex = GetSubMeshIndex(mesh, index);
                currentGroundMaterial = materials[subMeshIndex];
            }
        }
    }

    private void PlayFootstep(AudioClip[] audioClips)
    {
        var audio = audioClips[Random.Range(0, audioClips.Length - 1)];

        if (audio == lastPlayedFootstep)
            PlayFootstep(audioClips);
        else
        {
            footstepAudioSource.PlayOneShot(audio);
            lastPlayedFootstep = audio;
        }
    }

    private void PlayJumpLand(AudioClip[] audioClips, bool isJumping)
    {

        if (audioClips.Length > 2)
        {
            var audio = audioClips[Random.Range(0, audioClips.Length - 1)];

            if (audio == lastPlayedJump || audio == lastPlayedLand)
                PlayJumpLand(audioClips, isJumping);
            else
            {
                jumpLandAudioSource.PlayOneShot(audio);

                if (isJumping)
                    lastPlayedJump = audio;
                else
                    lastPlayedLand = audio;
            }
        }
        else if (audioClips.Length == 2)
        {
            if (isJumping)
            {
                if (audioClips[0] == lastPlayedJump)
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[1]);
                    lastPlayedJump = audioClips[1];
                }
                    
                else
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[0]);
                    lastPlayedJump = audioClips[0];
                }
                    
            }
            else
            {
                if (audioClips[0] == lastPlayedLand)
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[1]);
                    lastPlayedLand = audioClips[1];
                }
                    
                else
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[0]);
                    lastPlayedLand = audioClips[0];
                }
                    
            }
        }
        else
        {
            jumpLandAudioSource.PlayOneShot(audioClips[0]);

            if (isJumping)
                lastPlayedJump = audioClips[0];
            else
                lastPlayedLand = audioClips[0];
        }


    }

    public void RefreshUISettings()
    {
        // PlayerPrefs'ten oku (0 = Visible = true)
        showHints = PlayerPrefs.GetInt("ShowHints", 0) == 0;
        showInteractText = PlayerPrefs.GetInt("ShowInteractText", 0) == 0;

        // Anlýk temizlik: Eðer kapattýysak ekrandakileri hemen yok et
        if (!showHints)
        {
            // Ýpuçlarýný kapatan yardýmcý fonksiyonun (dün yazmýþtýk)
            SetAllPrompts(false); // Veya UpdateInteractionPrompts içinde halledilecek
        }

        if (!showInteractText)
        {
            // Crosshair altýndaki metni temizle
            if (focusTextAnim != null)
            {
                focusTextAnim.StopShowingText();
                focusTextAnim.StartDisappearingText();
                focusTextAnim.SkipTypewriter();
            }
        }
    }

    public void TryChangingFocusText(IInteractable interactable, string textKey)
    {
        if (!showInteractText)
        {
            return;
        }

        string localizedText = LocalizationManager.Instance.GetText(textKey);

        if (currentInteractable != null && currentInteractable == interactable && focusText.text != localizedText)
        {
            focusTextAnim.ShowText(localizedText);
            focusTextAnim.SkipTypewriter();
            SetFocusTextComplete(true);
        }
    }

    public void TryChangingFocusText(IGrabable grabable, string textKey)
    {
        if (!showInteractText)
        {
            return;
        }

        string localizedText = LocalizationManager.Instance.GetText(textKey);

        if (currentGrabable != null && !currentGrabable.IsGrabbed && currentGrabable == grabable && focusText.text != localizedText)
        {
            focusTextAnim.ShowText(localizedText);
            focusTextAnim.SkipTypewriter();
            SetFocusTextComplete(true);
        }
        else if (otherGrabable != null && otherGrabable == grabable && focusText.text != localizedText)
        {
            focusTextAnim.ShowText(localizedText);
            focusTextAnim.SkipTypewriter();
            SetFocusTextComplete(true);
        }
    }

    public void SetUseHandLerp(Vector3 targetPos, Vector3 targetRot, float timeToDo)
    {
        if (grabbedUseCoroutine != null)
        {
            StopCoroutine(grabbedUseCoroutine);
            grabbedUseCoroutine = null;
        }

        grabbedUseCoroutine = StartCoroutine(UseGrabbed(targetPos, targetRot, timeToDo));
    }

    public void SetLeftUseHandLerp(Vector3 targetPos, Vector3 targetRot)
    {
        currentPositionOffsetForLeftHand = targetPos;
        currentRotationOffsetForLeftHand = targetRot;

        if (leftHandRigLerpCoroutine != null)
        {
            StopCoroutine(leftHandRigLerpCoroutine);
            leftHandRigLerpCoroutine = null;
        }

        leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(true, true));
    }

    public void ResetLeftHandLerp()
    {
        if (leftHandRigLerpCoroutine != null)
        {
            StopCoroutine(leftHandRigLerpCoroutine);
            leftHandRigLerpCoroutine = null;
        }

        leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(false, false));
    }

    public void OnUseReleaseGrabable(bool shouldDecideOutlineAndCrosshair)
    {
        if (isUsingGrabbedItem)
        {
            // uzun basma býrakýldý kullaným bitti
            currentGrabable.OnUseRelease();
        }

        interactChargeTimer = 0f;
        isUsingGrabbedItem = false;

        if (shouldDecideOutlineAndCrosshair)
            DecideOutlineAndCrosshair();
    }

    public IInteractable GetCurrentInteractable() { return currentInteractable; }
    public IGrabable GetCurrentGrabable() { return currentGrabable; }

    public void SetFocusTextComplete(bool value) => focusTextComplete = value; //GETS TRUE IN TYPEWRITTER ACTIONS WHEN ITS COMPLETE. GETS FALSE WHENEVER TEXT GETS CHANGED ON DECIDEFOCUSTEXT()

    private void ResetHandAnim()
    {
        interactChargeTimer = 0f;
        isUsingGrabbedItem = false;

        IsUsingItemX = false;
        IsUsingItemY = false;
    }
        

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            rb.velocity += hit.moveDirection * pushForce;
        }
    }

    private IEnumerator LerpLeftHandRig(bool shouldReach, bool shouldGoBack)
    {
        float startWeight = twoBoneIKConstraintLeftHand.weight;
        float targetWeight = shouldReach ? 1.0f : 0.0f;

        float timeElapsed = 0f;

        while (timeElapsed < lerpTimeForInteractionHand)
        {
            twoBoneIKConstraintLeftHand.weight = Mathf.Lerp(startWeight, targetWeight, timeElapsed/lerpTimeForInteractionHand);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        twoBoneIKConstraintLeftHand.weight = targetWeight;
        leftHandRigLerpCoroutine = null;

        if (shouldGoBack)
        {
            yield return new WaitForSeconds(0.1f);
            if (leftHandRigLerpCoroutine == null)
                leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(false, false));
        }
    }

    private IEnumerator LerpRightHandRig(bool shouldReach, bool shouldGoBack)
    {
        canUsePhone = false;

        float startWeight = twoBoneIKConstraintRightHand.weight;
        float targetWeight = shouldReach ? 1.0f : 0.0f;

        float timeElapsed = 0f;

        while (timeElapsed < lerpTimeForInteractionHand)
        {
            twoBoneIKConstraintRightHand.weight = Mathf.Lerp(startWeight, targetWeight, timeElapsed / lerpTimeForInteractionHand);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        twoBoneIKConstraintRightHand.weight = targetWeight;

        if (targetWeight == 0f)
        {
            canUsePhone = true;

            if (lastPhoneKeyPressedTime + coyoteTimeForPhone > Time.time)
            {
                if (!phoneGrabable.IsGrabbed && canUsePhone)
                {
                    phoneGrabable.IsUseable = true;
                    ChangeCurrentGrabable(phoneGrabable);
                }
            }
        }

        rightHandRigLerpCoroutine = null;

        if (shouldGoBack)
        {
            yield return new WaitForSeconds(0.1f);
            if (rightHandRigLerpCoroutine == null)
                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));
        }
    }

    private IEnumerator SingleHandThrow()
    {
        float startWeight = twoBoneIKConstraintRightHand.weight;
        currentThrowForce = minThrowForce;
        throwChargeTimer = 0f;
        float value;

        while (throwChargeTimer < throwMaxChargeTime)
        {
            value = throwChargeTimer / throwMaxChargeTime;

            if (value > 0.6f)
                anim.SetBool("throw", true);

            currentThrowForce = Mathf.Lerp(minThrowForce, maxThrowForce, value);

            twoBoneIKConstraintRightHand.weight = Mathf.Lerp(startWeight, 0f, value * 2);

            throwChargeTimer += Time.deltaTime;
            yield return null;
        }

        currentThrowForce = maxThrowForce;

        twoBoneIKConstraintRightHand.weight = 0f;
    }

    private IEnumerator UseGrabbed(Vector3 targetPos, Vector3 targetRot, float timeToDo)
    {
        handUseStartOffset = currentPositionOffsetForRightHand;
        handUseDelta = Vector3.zero; // sýfýrla

        Vector3 startPos = currentPositionOffsetForRightHand;
        Vector3 startRot = currentRotationOffsetForRightHand;
        float timeElapsed = 0f;
        float value = 0f;

        while (timeElapsed < timeToDo)
        {
            value = timeElapsed / timeToDo;

            currentPositionOffsetForRightHand = Vector3.Lerp(startPos, targetPos, value);
            currentRotationOffsetForRightHand = Vector3.Lerp(startRot, targetRot, value);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        currentPositionOffsetForRightHand = targetPos;
        currentRotationOffsetForRightHand = targetRot;

        grabbedUseCoroutine = null;
    }


    private IEnumerator CrouchStand(bool shouldCrouch)
    {
        isCrouching = shouldCrouch;

        float horizontalSpeed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;
        anim.SetBool("isCrouching", isCrouching);

        float currentHeight = characterController.height;
        Vector3 currentCenter = characterController.center;

        float targetHeight = shouldCrouch ? crouchingHeight : standingHeight;
        Vector3 targetCenter = shouldCrouch ? crouchingCenter : standingCenter;

        float timeElapsed = 0f;

        while (timeElapsed < timeToCrouchStand)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed/timeToCrouchStand);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouchStand);

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter; 
    }
}
