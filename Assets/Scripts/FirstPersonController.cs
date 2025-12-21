using Febucci.UI.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(10000)]
public class FirstPersonController : MonoBehaviour
{
    public bool CanPlay = true;
    public bool IsUsingItemX = false;
    public bool IsUsingItemY = false;
    private bool IsSprinting => CanSprint && InputManager.Instance.PlayerSprint();
    private bool ShouldJump => InputManager.Instance.PlayerJump() && characterController.isGrounded;
    private bool ShouldCrouch => InputManager.Instance.PlayerCrouch();

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

    [Header("Gamepad Interaction Assist")]
    [SerializeField] private float assistRadius = 0.15f; // Iþýnýn kalýnlýðý (Burger köftesi tutturmak için ideal)

    [Header("Movement Parameters")]
    [SerializeField] private float movementSmoothTime = 0.1f;
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8.0f;
    [SerializeField] private float pushForce = 1f;
    [HideInInspector] public bool isBeingPushedByACustomer = false;
    private Vector2 currentDir = Vector2.zero;
    private Vector2 currentDirVelocity = Vector2.zero;

    [Header("Breathe Parameters")]
    [SerializeField] private Transform breathingParticlePoint;
    [SerializeField] private ParticleSystem breathingParticles;
    [SerializeField] private float breatheCooldownForSprint = 1f;
    [SerializeField] private float breatheCooldownForWalk = 3f;
    [SerializeField] private float breatheCooldownForCrouch = 5f;
    private float currentBreatheCooldown;
    private float breatheParticleLastInstantiateTime;

    [Header("Look Parameters")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform cameraFollow;
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 100)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 100)] private float lowerLookLimit = 45.0f;
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
    [SerializeField] private float landVelocityThreshold = -5f;
    private AudioClip lastPlayedLand;
    private bool wasGrounded;

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
    [SerializeField] private Vector3 footstepCheckSize = new Vector3(0.5f, 0.1f, 0.5f);
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
            // Her karenin baþýnda hýzý varsayýlan (1.0) kabul edelim.
            // Aþaðýdaki fonksiyonlar gerekirse bunu düþürecek.
            if (InputManager.Instance != null)
                InputManager.Instance.aimAssistSlowdown = 1f;

            if (CanLook)
            {
                HandleMouseAndHandControl();
            }

            if (CanMove)
                HandleMovementInput();
            else
                anim.SetFloat("speed", 0f, 0.15f, Time.deltaTime);

            if (CanJump)
                HandleJump();

            HandleGravityAndLanding();


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

            if (InputManager.Instance.PlayerPhone())
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
                HandleGravityAndLanding();
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

            HandleHandTargetPositions();
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

        Vector2 lookInput = InputManager.Instance.GetLookInput();

        // DÝKKAT: Yeni sistem pixel delta döndürür, hassasiyeti dengelemek için 
        // ufak bir çarpan gerekebilir veya lookSpeed deðerlerini arttýrman gerekebilir.
        // Genelde yeni sistemde bu deðerler daha büyük gelir. 
        // Þimdilik 0.1f gibi bir çarpanla deneyelim, gerekirse kaldýrýrsýn.
        float mouseX = lookInput.x;
        float mouseY = lookInput.y;

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
        //head.localRotation = Quaternion.Euler(rotationX, 0, 0);
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
        float targetSpeed = isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;

        // 1. Ham veriyi InputManager'dan çek (Örn: 0 veya 1 gelir)
        Vector2 targetInput = InputManager.Instance.GetMovementInput();

        // 2. Çapraz koþma hilesini (Pythagoras) önlemek için normalize et (Ýsteðe baðlý ama önerilir)
        // Analog kol kullanýrken (0.5 gibi deðerler) bozulmasýn diye ClampMagnitude kullanýyoruz.
        targetInput = Vector2.ClampMagnitude(targetInput, 1f);

        // 3. SMOOTH DAMP (Sihirli Kýsým)
        // Mevcut input deðerini, hedef input deðerine, belirlediðimiz sürede (smoothTime) kaydýr.
        // Bu bize o eski "Hýzlanma" hissini verir.
        currentDir = Vector2.SmoothDamp(currentDir, targetInput, ref currentDirVelocity, movementSmoothTime);

        // 4. Hýzý uygula (Artýk smoothed "currentDir" kullanýyoruz)
        // Not: currentDir 0'dan 1'e yavaþ yavaþ çýkacaðý için hýz da yavaþ yavaþ artacak.
        float vertical = currentDir.y;
        float horizontal = currentDir.x;

        // Y yönünü (yerçekimini) korumak için yedeðe al
        float moveDirectionY = moveDirection.y;

        // Hesaplama
        moveDirection = (transform.TransformDirection(Vector3.forward) * vertical * targetSpeed) +
                        (transform.TransformDirection(Vector3.right) * horizontal * targetSpeed);

        // Y yönünü geri koy
        moveDirection.y = moveDirectionY;

        // --- Animasyon Hýzý ---
        // Animasyon için zaten SmoothDamp yapýlmýþ veriyi kullandýðýmýz için 
        // buradaki dampTime'ý düþürebilirsin veya böyle kalabilir.
        float horizontalSpeed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;
        anim.SetFloat("speed", horizontalSpeed, 0.1f, Time.deltaTime);
    }

    private void HandleJump()
    {
        if (ShouldJump)
        {

            anim.SetBool("isGrounded", false);
            anim.SetTrigger("jump");

            CheckSurfaceAndPlaySound(1.9f, true, true);

            moveDirection.y = jumpForce;

        }
            
    }

    private void HandleGravityAndLanding()
    {

        // --- LANDING MANTIÐI (YERE ÝNÝÞ) ---
        // Eðer geçen kare havadaysak (wasGrounded == false) VE þimdi yerdeysek (isCurrentlyGrounded == true)
        // Demek ki tam bu karede yere bastýk!
        if (!wasGrounded && characterController.isGrounded)
        {
            // Yere inince yapýlacaklar:
            anim.SetBool("isGrounded", true);

            // Düþme hýzý kontrolü (Merdiven inerken zýrt pýrt ses çýkmasýn diye)
            // moveDirection.y negatifse düþüyoruzdur.
            // -0.5f gibi bir eþik deðeri, yürürkenki minik zýplamalarý filtreler.
            if (moveDirection.y < landVelocityThreshold)
            {
                // O çok sevdiðimiz yeni yüzey fonksiyonunu çaðýrýyoruz
                // Mesafe: 2f (Ayak altý), Jump/Land: True, Jumping: False (yani Landing)
                CheckSurfaceAndPlaySound(2f, true, false);
            }
            else
            {
                CheckSurfaceAndPlaySound(2f, false, false);
            }
            
            moveDirection.y = -2f; // Karakteri yere yapýþtýrmak için minik bir negatif kuvvet (Standarttýr)
        }
        // --- FALLING MANTIÐI (HAVADA OLMA) ---
        else if (!characterController.isGrounded)
        {
            // Havadaysak yerçekimini uygula
            moveDirection.y -= gravity * Time.deltaTime;

            // Animasyonu güncelle (Eðer grounded false ise düþüyor animasyonu devreye girer)
            if (wasGrounded) // Yerden yeni kesildiysek
            {
                anim.SetBool("isGrounded", false);
                lastGroundedTime = Time.time; // Coyote time için
            }
        }

        // --- DURUMU KAYDET ---
        // Bu karenin durumu, bir sonraki karenin "geçmiþi" olacak.
        wasGrounded = characterController.isGrounded;
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
        if (PerformInteractionCast(out RaycastHit hit, interactionDistance, interactionLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlined") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {

                    // Bir interactable bulduk! Kamerayý yavaþlat.
                    InputManager.Instance.aimAssistSlowdown = InputManager.Instance.GetMagnetStrength();

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
                else
                {

                    if (currentInteractable != null)
                    {
                        currentInteractable.OnLoseFocus();
                        currentInteractable = null;
                        DecideOutlineAndCrosshair();
                    }
                }
            }
            else
            {

                if (currentInteractable != null)
                {
                    currentInteractable.OnLoseFocus();
                    currentInteractable = null;
                    DecideOutlineAndCrosshair();
                }
            }
        }
        else
        {

            if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                DecideOutlineAndCrosshair();
            }
        }
    }

    private void HandleInteractionInput()
    {
        if (currentGrabable == null || !currentGrabable.IsGrabbed || !currentGrabable.IsUseable)
        {
            // EL BOS normal interact (tap)
            if (InputManager.Instance.PlayerInteract())
            {
                TryToInteract();
            }
        }
        else if (throwChargeTimer < 0.1f)
        {

            if (InputManager.Instance.PlayerInteractHold())
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

            if (InputManager.Instance.PlayerInteractRelease())
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
            if (InputManager.Instance.PlayerInteractRelease())
            {
                TryToInteract();
            }
        }
    }

    private void TryToInteract()
    {
        if (currentInteractable != null && PerformInteractionCast(out RaycastHit hit, interactionDistance, interactionLayers) && !currentInteractable.OutlineShouldBeRed)
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

        if (PerformInteractionCast(out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabable") || hit.collider.gameObject.layer == LayerMask.NameToLayer("GrabableOutlined") || hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {
                    // --- MAGNETISM ---
                    // Eþya bulduk, yavaþlat
                    InputManager.Instance.aimAssistSlowdown = InputManager.Instance.GetMagnetStrength();

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
                if (InputManager.Instance.PlayerPhone() || InputManager.Instance.PlayerThrow())
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
                if (InputManager.Instance.PlayerThrow()) // right click starts
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
                if (InputManager.Instance.PlayerThrowRelease())
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
        else if (InputManager.Instance.PlayerInteract() && currentGrabable != null && PerformInteractionCast(out RaycastHit hit, interactionDistance, grabableLayers))
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
        /*anim.SetBool("regularGrab", false);
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
        anim.SetBool("wholeBunGrab", false);*/
        anim.SetBool("chargingThrow", false);
    }
    private void DecideGrabAnimBool()
    {
        /*if (currentGrabable != null && currentGrabable.IsGrabbed)
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
        }*/
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
        // 1. Önce karakterin YATAY hýzýný (Horizontal Velocity) ölçelim.
        // Y eksenini (zýplama/düþme) sýfýrlýyoruz ki sadece yürüme hýzýný alalým.
        float horizontalSpeed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;

        // 2. KURAL:
        // - Eðer yerde deðilsek (isGrounded false) -> ÇALMA.
        // - Eðer hýzýmýz çok düþükse (0.2f'den azsa, yani duruyorsak veya duvara takýldýysak) -> ÇALMA.
        if (!characterController.isGrounded || horizontalSpeed < 0.2f) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            CheckSurfaceAndPlaySound(1.9f, false, false);

            footstepTimer = GetCurrentOffset;
        }
    }

    private void CheckSurfaceAndPlaySound(float rayDistance, bool isJumpOrLand, bool isJumping)
    {
        // BoxCast Parametreleri:
        // 1. Merkez: RayPoint
        // 2. Boyut (Yarým): footstepCheckSize / 2 (Unity yarým boyut ister)
        // 3. Yön: Aþaðý
        // 4. Hit Info: Çarpýþma bilgisi
        // 5. Rotasyon: Karakterin dönüþüne göre kutu da dönsün
        // 6. Mesafe: rayDistance
        // 7. LayerMask: groundTypeLayers

        if (Physics.BoxCast(groundTypeCheckRayPoint.transform.position, footstepCheckSize / 2, Vector3.down, out RaycastHit hit, groundTypeCheckRayPoint.transform.rotation, rayDistance, groundTypeLayers))
        {
            // --- BURASI AYNEN KALIYOR ---
            if (hit.collider.TryGetComponent<SurfaceIdentity>(out var surface))
            {
                switch (surface.type)
                {
                    case SurfaceType.Wood:
                        SelectAndPlayClips(woodClips, woodJumpClips, woodLandClips, isJumpOrLand, isJumping);
                        break;
                    case SurfaceType.Metal:
                        SelectAndPlayClips(metalClips, metalJumpClips, metalLandClips, isJumpOrLand, isJumping);
                        break;
                    case SurfaceType.Grass:
                        SelectAndPlayClips(grassClips, grassJumpClips, grassLandClips, isJumpOrLand, isJumping);
                        break;
                    case SurfaceType.Stone:
                        SelectAndPlayClips(stoneClips, stoneJumpClips, stoneLandClips, isJumpOrLand, isJumping);
                        break;
                    case SurfaceType.Tile:
                        SelectAndPlayClips(tileClips, tileJumpClips, tileLandClips, isJumpOrLand, isJumping);
                        break;
                    case SurfaceType.Gravel:
                        SelectAndPlayClips(gravelClips, gravelJumpClips, gravelLandClips, isJumpOrLand, isJumping);
                        break;
                }
            }
            else
            {
                SelectAndPlayClips(stoneClips, stoneJumpClips, stoneLandClips, isJumpOrLand, isJumping);
            }
        }
    }

    // Yardýmcý Fonksiyon: Hangi array'i kullanacaðýna karar verir ve oynatýr
    private void SelectAndPlayClips(AudioClip[] walk, AudioClip[] jump, AudioClip[] land, bool isJumpOrLand, bool isJumping)
    {
        if (isJumpOrLand)
        {
            // Zýplama veya Düþme sesi
            AudioClip[] targetClips = isJumping ? jump : land;
            PlayJumpLand(targetClips, isJumping);
        }
        else
        {
            // Adým sesi
            PlayFootstep(walk);
        }
    }

    private void ApplyFinalMovements()
    {
        if (willSlideOnSlopes && IsSliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void ChangeCrosshairSize(Vector2 size)
    {
        crosshair.rectTransform.sizeDelta = size;
    }

    private void ChangeCrosshairColor(Color color) => crosshair.color = color;

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

    // Raycast yerine bunu kullanacaðýz. Hem lazer hem kalýn boru atar.
    // SphereCastAll kullanarak duvar takýlmasýný önler.
    private bool PerformInteractionCast(out RaycastHit hitInfo, float distance, LayerMask mask)
    {
        hitInfo = new RaycastHit();
        Ray ray = mainCamera.ViewportPointToRay(interactionRayPoint);

        // -------------------------------------------------------------
        // 1. AÞAMA: LAZER KONTROLÜ (Direct Hit)
        // -------------------------------------------------------------
        // Buradaki deðiþiklik kritik: Raycast bir þeye çarparsa hemen kabul etmiyoruz.
        // Çarptýðý þeyin "Ýþimize Yarayan" bir layer olup olmadýðýna bakýyoruz.

        if (Physics.Raycast(ray, out RaycastHit directHit, distance, mask))
        {
            if (IsTargetLayer(directHit.collider.gameObject.layer))
            {
                // Tam merkezde bir EÞYA var. En iyisi budur. Al ve çýk.
                hitInfo = directHit;
                return true;
            }
            // Eðer duvara çarptýysa buraya girmez, aþaðýya (SphereCast'e) devam eder.
        }

        // 2. CÝHAZ VE AYAR KONTROLÜ
        // Eðer Mouse kullanýyorsak VEYA Ayarlardan "Kapalý" seçildiyse SphereCast yapma.
        if (InputManager.Instance.IsUsingMouse() || InputManager.Instance.aimAssistLevel == InputManager.AimAssistLevel.Off)
        {
            return false;
        }

        // -------------------------------------------------------------
        // 2. AÞAMA: SPHERE CAST (Aim Assist / Kalýn Iþýn)
        // -------------------------------------------------------------
        // Merkezde eþya yok (ya boþluk ya da duvar var).
        // Þimdi etrafý tarayalým.

        // 3. SPHERE CAST ALL (Dinamik Radius ile)
        // Yarýçapý InputManager'dan alýyoruz
        float dynamicRadius = InputManager.Instance.GetAssistRadius();

        RaycastHit[] hits = Physics.SphereCastAll(ray, dynamicRadius, distance, mask);

        if (hits.Length == 0) return false;

        float closestDistance = distance;
        bool foundValidTarget = false;
        RaycastHit bestHit = new RaycastHit();

        foreach (RaycastHit hit in hits)
        {
            // Kendimize çarpmayalým
            if (hit.transform == transform) continue;

            // Sadece Interactable/Grabable olanlarý filtrele
            // (Duvarlar buraya takýlýr ve elenir)
            if (!IsTargetLayer(hit.collider.gameObject.layer)) continue;

            // En yakýný bulma mantýðý
            if (hit.distance < closestDistance)
            {
                // -------------------------------------------------------------
                // 3. AÞAMA: GÖRÜÞ ÇÝZGÝSÝ (Occlusion Check)
                // -------------------------------------------------------------
                // Eþyayý bulduk ama arada duvar var mý?
                // Kameradan eþyaya ýþýn atýyoruz.

                Vector3 directionToTarget = (hit.point - mainCamera.transform.position).normalized;

                // Bu ýþýn 'mask' yani interactionLayers kullanmalý ki duvarlarý görsün.
                if (Physics.Raycast(mainCamera.transform.position, directionToTarget, out RaycastHit occlusionHit, distance, mask))
                {
                    // Eðer ýþýn direkt eþyaya (veya çocuðuna) çarparsa -> Görüyoruz demektir.
                    if (occlusionHit.collider.gameObject == hit.collider.gameObject ||
                        occlusionHit.collider.transform.IsChildOf(hit.transform) ||
                        hit.transform.IsChildOf(occlusionHit.transform))
                    {
                        bestHit = hit;
                        closestDistance = hit.distance;
                        foundValidTarget = true;
                    }
                    // Eðer ýþýn önce duvara çarparsa -> Buraya girmez, eþya duvar arkasýndadýr.
                }
            }
        }

        if (foundValidTarget)
        {
            hitInfo = bestHit;
            return true;
        }

        return false;
    }

    // Kod tekrarýný önlemek için yardýmcý minik fonksiyon
    private bool IsTargetLayer(int layer)
    {
        return layer == LayerMask.NameToLayer("Interactable") ||
               layer == LayerMask.NameToLayer("InteractableOutlined") ||
               layer == LayerMask.NameToLayer("InteractableOutlinedRed") ||
               layer == LayerMask.NameToLayer("Grabable") ||
               layer == LayerMask.NameToLayer("GrabableOutlined");
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += ForceUpdateFocusText;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= ForceUpdateFocusText;
    }

    private void ForceUpdateFocusText()
    {
        // Hangi objeye baktýðýmýzý bulalým
        string keyToUse = "";

        if (CanInteract && currentInteractable != null)
        {
            keyToUse = currentInteractable.FocusTextKey;
        }
        else if (CanGrab && currentGrabable != null && !currentGrabable.IsGrabbed)
        {
            keyToUse = currentGrabable.FocusTextKey;
        }
        else if (CanGrab && otherGrabable != null)
        {
            keyToUse = otherGrabable.FocusTextKey;
        }
        else
            return;

        // Eðer geçerli bir key bulduysak ve metin görünüyorsa
        if (!string.IsNullOrEmpty(keyToUse) && showInteractText)
        {
            string newText = LocalizationManager.Instance.GetText(keyToUse);

            focusTextAnim.ShowText(newText);
            focusTextAnim.SkipTypewriter();
            SetFocusTextComplete(true);
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Ayak Kontrolü (Eski kodun)
        if (groundTypeCheckRayPoint != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(groundTypeCheckRayPoint.transform.position, groundTypeCheckRayPoint.transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.down * 1.0f, footstepCheckSize); // Temsili
        }

        // 2. ETKÝLEÞÝM RÖNTGENÝ (YENÝ - SphereCast'i Gösterir)
        if (mainCamera != null)
        {
            Gizmos.matrix = Matrix4x4.identity; // Matrisi sýfýrla

            // SphereCast'in aynýsýný burada simüle ediyoruz
            Ray ray = mainCamera.ViewportPointToRay(interactionRayPoint);

            // A. Normal Lazer (Beyaz Çizgi)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * interactionDistance);

            // B. Kalýn SphereCast (Sarý Boru)
            // Eðer bir þeye çarpýyorsa Kýrmýzý, çarpmýyorsa Sarý olsun.
            bool hitSomething = Physics.SphereCast(ray, assistRadius, out RaycastHit hit, interactionDistance, interactionLayers | grabableLayers);

            Gizmos.color = hitSomething ? Color.red : Color.yellow;

            // SphereCast'in gittiði yol kadar çiz
            float dist = hitSomething ? hit.distance : interactionDistance;

            // Baþlangýç küresi
            Gizmos.DrawWireSphere(ray.origin, assistRadius);
            // Bitiþ küresi (Vurduðu yer)
            Gizmos.DrawWireSphere(ray.origin + ray.direction * dist, assistRadius);
            // Aradaki çizgi
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * dist);
        }
    }
}
