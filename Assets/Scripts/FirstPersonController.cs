using Febucci.UI.Core;
using System.Collections;
using System.Collections.Generic;
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
    private bool IsHoldingTray => currentGrabable != null && currentGrabable.IsGrabbed && currentGrabable.HandRigType == PlayerManager.HandRigTypes.HoldingTray;

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
    [SerializeField] private Color combineCrosshairColor;
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

    [Header("Inventory System")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private int maxInventorySlots = 4;
    // Envanterdeki eþyalarýn listesi
    private IGrabable[] inventoryItems;
    // YENÝ: Eþyalarýn alýnma sýrasýný tutacak liste
    private List<IGrabable> itemPickupHistory = new List<IGrabable>();

    // Hangi slot seçili? -1 ise el boþ demek. 0, 1, 2, 3 slot numaralarý.
    private int currentSlotIndex = -1;

    [Header("Tray Mode Settings")]
    [SerializeField] private Transform trayParentTransform; // Inspector'dan karakterin Spine veya Chest kemiðini ata
    [SerializeField] private float trayWalkSpeed = 1.5f; // Tepsiyle yürüme hýzý
    [SerializeField] private float traySprintSpeed = 3f; // Tepsiyle koþma hýzý
    [SerializeField] private float trayLookSensitivityMultiplier = 0.5f; // Bakýþ hýzý ne kadar yavaþlasýn?
    [SerializeField] private Vector2 trayLookLimits = new Vector2(30f, 30f); // Yukarý 30, Aþaðý 30 bakabilsin (Kýsýtlý)

    // Eski deðerleri hafýzada tutmak için
    private float defaultWalkSpeed;
    private float defaultSprintSpeed;
    private float defaultLookSpeedX;
    private float defaultLookSpeedY;
    private float defaultUpperLookLimit;
    private float defaultLowerLookLimit;
    private bool defaultCanJump;
    private bool defaultCanCrouch;
    private bool defaultCanGrab;
    private bool defaultCanInteract;

    private float rightHandArmLength;
    private float leftHandArmLength;

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

        defaultCrosshairSize = crosshair.rectTransform.sizeDelta;
        defaultCrosshairColor = crosshair.color;

        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        InteractKeyIsDone = false;

        inventoryItems = new IGrabable[maxInventorySlots];

        DecideOutlineAndCrosshair();

        defaultWalkSpeed = walkSpeed;
        defaultSprintSpeed = sprintSpeed;
        defaultLookSpeedX = lookSpeedX;
        defaultLookSpeedY = lookSpeedY;
        defaultUpperLookLimit = upperLookLimit;
        defaultLowerLookLimit = lowerLookLimit;
        defaultCanJump = CanJump;
        defaultCanCrouch = CanCrouch;
        defaultCanGrab = CanGrab; // Bunu kapatacaðýz ki tepsi varken baþka þey alamasýn
        defaultCanInteract = CanInteract;
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

        CalculateArmLengths();

        if (inventoryUI != null) inventoryUI.UpdateDisplay(inventoryItems, currentSlotIndex);
    }

    private void Update()
    {
        if (CanPlay)
        {
            if (InputManager.Instance.PlayerInteract())
            {
                InteractKeyIsDone = false;
            }

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

            anim.SetFloat("speedY", moveDirection.y);


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
                HandleInventoryInput();
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
        // --- AYARLAR ---
        // Kolu tam kilitlenmeden hemen önce durdur ki titreme payý kalsýn.
        // Koþarken kafa sallanmasý bu %5'lik pay içinde erir, glitch yapmaz.
        float safeLengthFactor = 0.9f;

        // Yukarý bakarken (rotationX negatifken) eli ne kadar geri çekeceðimiz.
        // -30 dereceden sonra devreye girer.
        float lookUpPullBackAmount = 0.3f;

        // --- SOL EL ---
        Transform leftShoulder = twoBoneIKConstraintLeftHand.data.root;
        Vector3 idealLeftPos = mainCamera.transform.TransformPoint(currentPositionOffsetForLeftHand);

        // AÇI TELAFÝSÝ (Pitch Compensation)
        // Eðer yukarý bakýyorsak (rotationX < -20), target'ý kameraya/gövdeye yaklaþtýr.
        if (rotationX < -20f)
        {
            // -20 ile -80 (upperLookLimit) arasýný 0 ile 1 arasýna orantýla
            float t = Mathf.InverseLerp(-20f, -upperLookLimit, rotationX);
            // Yumuþakça geri çek (Kameranýn arkasýna deðil, aþaðý-geriye doðru)
            idealLeftPos -= mainCamera.transform.forward * (t * lookUpPullBackAmount);
            idealLeftPos -= mainCamera.transform.up * (t * lookUpPullBackAmount * 0.5f);
        }

        // UZUNLUK SINIRLAMASI (Clamping)
        if (leftShoulder != null)
        {
            Vector3 directionToTarget = idealLeftPos - leftShoulder.position;
            float currentDist = directionToTarget.magnitude;
            float maxAllowedDist = leftHandArmLength * safeLengthFactor;

            if (currentDist > maxAllowedDist)
            {
                // Sýnýrý aþtýysa, güvenli sýnýra sabitle
                idealLeftPos = leftShoulder.position + (directionToTarget.normalized * maxAllowedDist);
            }
        }

        leftHandTarget.position = idealLeftPos;
        leftHandTarget.rotation = mainCamera.transform.rotation * Quaternion.Euler(currentRotationOffsetForLeftHand);


        // --- SAÐ EL (Ayný Mantýk) ---
        Transform rightShoulder = twoBoneIKConstraintRightHand.data.root;
        Vector3 idealRightPos = mainCamera.transform.TransformPoint(currentPositionOffsetForRightHand);

        // AÇI TELAFÝSÝ (Sað el için)
        if (rotationX < -20f)
        {
            float t = Mathf.InverseLerp(-20f, -upperLookLimit, rotationX);
            idealRightPos -= mainCamera.transform.forward * (t * lookUpPullBackAmount);
            idealRightPos -= mainCamera.transform.up * (t * lookUpPullBackAmount * 0.5f);
        }

        // UZUNLUK SINIRLAMASI (Sað el için)
        if (rightShoulder != null)
        {
            Vector3 directionToTarget = idealRightPos - rightShoulder.position;
            float currentDist = directionToTarget.magnitude;
            float maxAllowedDist = rightHandArmLength * safeLengthFactor;

            if (currentDist > maxAllowedDist)
            {
                idealRightPos = rightShoulder.position + (directionToTarget.normalized * maxAllowedDist);
            }
        }

        rightHandTarget.position = idealRightPos;
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
            // --- EKLENEN KISIM BAÞLANGIÇ ---
            // Eðer þu an çömeliyorsak (isCrouching), zýplamadan önce tavaný kontrol etmeliyiz.
            // HandleCrouch fonksiyonunda kullandýðýnýz Raycast mantýðýnýn aynýsýný kullanýyoruz.
            if (isCrouching)
            {
                // Raycast yukarý doðru (Vector3.up) bakýyor. 
                // Mesafe olarak 'standingHeight/1.7f' kullanýyoruz (mevcut kodunuzdaki tolerans ile ayný).
                // Eðer bir þeye çarparsa, tepemizde engel var demektir; zýplamayý iptal et.
                if (Physics.Raycast(mainCamera.transform.position, Vector3.up, standingHeight / 1.7f))
                {
                    return;
                }
            }
            // --- EKLENEN KISIM BÝTÝÞ ---

            anim.SetBool("isGrounded", false);

            // ESKÝSÝ: anim.SetTrigger("jump");
            // YENÝSÝ: Bool yapýyoruz. Yere deðene kadar true kalacak.
            anim.SetBool("jump", true);

            CheckSurfaceAndPlaySound(1.9f, true, true);

            moveDirection.y = jumpForce;
        }
    }

    private void HandleGravityAndLanding()
    {
        // --- LANDING MANTIÐI (YERE ÝNÝÞ) ---
        if (!wasGrounded && characterController.isGrounded)
        {
            // Yere indik!
            anim.SetBool("isGrounded", true);

            // YENÝ: Yere indiðimiz için zýplama durumunu (veya düþme durumunu) bitir.
            anim.SetBool("jump", false);

            // Düþme hýzý kontrolü
            if (moveDirection.y < landVelocityThreshold)
            {
                CheckSurfaceAndPlaySound(2f, true, false);
            }
            else
            {
                CheckSurfaceAndPlaySound(2f, false, false);
            }

            moveDirection.y = -2f;
        }
        // --- FALLING MANTIÐI (HAVADA OLMA) ---
        else if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;

            if (wasGrounded)
            {
                // Yerden kesildik (Zýplayarak veya yürüyerek düþerek)
                anim.SetBool("isGrounded", false);

                // NOT: Eðer buraya 'ShouldJump' olmadan geldiysek (yürüyerek düþtüysek),
                // 'jump' bool'u zaten false kalacak.
                // Animatörde: isGrounded == false ve jump == false ise -> FALL animasyonu oynat.
            }
        }

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
        {  // <--- BU PARANTEZ EKSÝKTÝ (BAÞLANGIÇ)

            // --- YENÝ EKLENEN: TEPSÝ KONTROLÜ ---
            if (IsHoldingTray)
            {
                currentInteractable.OutlineShouldBeRed = true;
                currentInteractable.OutlineChangeCheck();
                return; // Aþaðýdaki karmaþýk mantýða girmeden çýk
            }
            // ------------------------------------

            // 1. Eþyayý aktif kullanýyorsak (fýrlatma þarjý vb.) -> HER TÜRLÜ KIRMIZI
            if (isUsingGrabbedItem)
            {
                currentInteractable.OutlineShouldBeRed = true;
            }
            else
            {
                // Elimiz dolu mu?
                bool isHandFull = currentGrabable != null && currentGrabable.IsGrabbed;

                // Baktýðýmýz þey "Eþya Veren" tipi mi?
                bool isItemGiver = currentInteractable.HandRigType == PlayerManager.HandRigTypes.SingleHandGrab;

                // SENARYO A: Elim Dolu
                if (isHandFull)
                {
                    if (isItemGiver)
                    {
                        //Yer varsa -> Beyaz
                        if (HasEmptySlot())
                            currentInteractable.OutlineShouldBeRed = false;
                        else
                            currentInteractable.OutlineShouldBeRed = true;
                    }
                    else
                    {
                        currentInteractable.OutlineShouldBeRed = false;
                    }
                }
                // SENARYO B: Elim Boþ
                else
                {
                    // Eðer eþya veren bir þeye bakýyorsak VE hiç boþ yerimiz yoksa -> KIRMIZI
                    if (isItemGiver && !HasEmptySlot())
                    {
                        currentInteractable.OutlineShouldBeRed = true;
                    }
                    else
                    {
                        // Yerimiz var veya eþya vermeyen (kapý, ýþýk vb.) bir þey -> BEYAZ
                        currentInteractable.OutlineShouldBeRed = false;
                    }
                }
            }

            currentInteractable.OutlineChangeCheck();

        } // <--- BU PARANTEZ EKSÝKTÝ (BÝTÝÞ)
    }

    private void DecideGrabableOutlineColor()
    {
        // Önce temizlik: Yeþil bool'larý sýfýrla (Kýrmýzýyý aþaðýda yönetiyoruz ama bunu burada sýfýrlamak güvenli)
        if (otherGrabable != null) otherGrabable.OutlineShouldBeGreen = false;
        if (currentGrabable != null) currentGrabable.OutlineShouldBeGreen = false;

        // --- YENÝ EKLENEN: TEPSÝ KONTROLÜ ---
        if (IsHoldingTray)
        {
            // Elimdeki (Tepsi) kýrmýzý olmasýn, kendi renginde kalsýn veya outline olmasýn
            if (currentGrabable != null) currentGrabable.OutlineShouldBeRed = false;

            // Ama baktýðým diðer yerdeki eþyalar (OtherGrabable) KESÝN KIRMIZI olsun
            if (otherGrabable != null)
            {
                otherGrabable.OutlineShouldBeRed = true;
                otherGrabable.OutlineChangeCheck();
            }
            return; // Aþaðýya devam etme
        }
        // ------------------------------------

        // DURUM 1: Elim dolu ve yerde baþka bir þeye bakýyorum
        if (currentGrabable != null && currentGrabable.IsGrabbed && otherGrabable != null)
        {
            // --- KRÝTÝK DEÐÝÞÝKLÝK BURADA ---

            // 1. Kombinasyon Kontrolü:
            // "Elimdeki yerdekini yer mi?" VEYA "Yerdeki elimdekini yer mi?"
            bool canCombine = currentGrabable.CanCombine(otherGrabable) || otherGrabable.CanCombine(currentGrabable);

            if (canCombine)
            {
                // Birleþebiliyorsa:
                // Kýrmýzý olmasýn (Hata yok)
                otherGrabable.OutlineShouldBeRed = false;
                // Yeþil olsun (Kombinasyon var)
                otherGrabable.OutlineShouldBeGreen = true;
            }
            else
            {
                // Birleþemiyorsa standart prosedür:
                // Yer yoksa VEYA Þu an elimdekini kullanýyorsam (örn: fýrlatma þarjý) -> KIRMIZI
                if (!HasEmptySlot() || isUsingGrabbedItem)
                {
                    otherGrabable.OutlineShouldBeRed = true;
                }
                else
                {
                    otherGrabable.OutlineShouldBeRed = false;
                }
            }

            // Durumu uygula
            otherGrabable.OutlineChangeCheck();
        }
        // DURUM 2: Standart durumlar
        else
        {
            if (otherGrabable != null)
            {
                // BURADA DA AYNISI: Yer yoksa veya Meþgulsek -> KIRMIZI
                if (!HasEmptySlot() || isUsingGrabbedItem)
                    otherGrabable.OutlineShouldBeRed = true;
                else
                    otherGrabable.OutlineShouldBeRed = false;

                otherGrabable.OutlineChangeCheck();
            }

            if (currentGrabable != null)
            {
                // Yerdeki (henüz alýnmamýþ) eþyalar için:
                if (!currentGrabable.IsGrabbed)
                {
                    // Yer yoksa veya Meþgulsek -> KIRMIZI
                    if (!HasEmptySlot() || isUsingGrabbedItem)
                        currentGrabable.OutlineShouldBeRed = true;
                    else
                        currentGrabable.OutlineShouldBeRed = false;
                }
                else
                {
                    // Elimdeki eþya kendi kendine kýrmýzý olmasýn (kullanýrken outline deðiþmesin diyorsan false kalýr)
                    currentGrabable.OutlineShouldBeRed = false;
                }

                currentGrabable.OutlineChangeCheck();
            }
        }
    }

    private void DecideCrosshairColor()
    {
        // 1. KIRMIZI (Zorunlu Durumlar - Yer yok, meþgul vs.)
        // Not: OutlineShouldBeGreen true ise Controller tarafýnda OutlineShouldBeRed'i false yapmýþtýk.
        // O yüzden yeþil durumunda buraya girmez, güvenli.
        if ((currentInteractable != null && currentInteractable.OutlineShouldBeRed) ||
            (otherGrabable != null && otherGrabable.OutlineShouldBeRed) ||
            (currentGrabable != null && currentGrabable.OutlineShouldBeRed))
        {
            ChangeCrosshairColor(useCrosshairColor);
        }
        // --- 2. YEÞÝL (KOMBÝNASYON) --- 
        // YENÝ EKLEME BURASI
        // Hem otherGrabable hem currentGrabable kontrol ediyoruz (ne olur ne olmaz)
        else if ((otherGrabable != null && otherGrabable.OutlineShouldBeGreen) ||
                 (currentGrabable != null && currentGrabable.OutlineShouldBeGreen))
        {
            ChangeCrosshairColor(combineCrosshairColor);
        }
        // ------------------------------
        // 3. TURUNCU / BEYAZ (Standart Grab Durumlarý)
        else if (currentGrabable != null)
        {
            // Elim boþsa ve yere bakýyorsam
            if (!currentGrabable.IsGrabbed)
            {
                // Yer varsa VE Elim meþgul deðilse -> TURUNCU
                if (HasEmptySlot() && !isUsingGrabbedItem)
                    ChangeCrosshairColor(grabCrosshairColor);
                else
                    ChangeCrosshairColor(useCrosshairColor);
            }
            // Elim doluysa ve yere (OtherGrabable) bakýyorsam
            else if (otherGrabable != null)
            {
                // Yer varsa VE Elim meþgul deðilse -> TURUNCU
                // (Buraya gelmesi için Yeþil veya Kýrmýzý olmamasý lazým, demek ki normal bir eþya)
                if (HasEmptySlot() && !isUsingGrabbedItem)
                    ChangeCrosshairColor(grabCrosshairColor); // Turuncu (Alýnabilir)
                else
                    ChangeCrosshairColor(useCrosshairColor);  // Kýrmýzý (Yer yok veya Meþgul)
            }
            // Hiçbiri deðilse standart renk
            else
            {
                ChangeCrosshairColor(defaultCrosshairColor);
            }
        }
        // 4. Durum: Hiçbir þey yok
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
        // --- AYAR KONTROLÜ ---
        if (!showHints)
        {
            SetAllPrompts(false);
            return;
        }

        // --- 1. DURUM ANALÝZÝ ---
        bool isHandBusy = isUsingGrabbedItem;
        bool isHoldingItem = currentGrabable != null && currentGrabable.IsGrabbed;
        bool isLookingAtInteractable = currentInteractable != null;

        // --- DÜZELTME BURADA (TANIMLAMA) ---
        // Yerdeki bir Grabable'a bakýyor muyuz?
        // Ýki durum var:
        // 1. Elim BOÞTUR, baktýðým þey 'currentGrabable'dýr.
        // 2. Elim DOLUDUR, baktýðým þey 'otherGrabable'dýr.
        bool isLookingAtGrabbable = (currentGrabable != null && !currentGrabable.IsGrabbed) || (otherGrabable != null);
        // ------------------------------------

        // --- 2. ACÝL DURUM ---
        if (isHandBusy)
        {
            SetAllPrompts(false);
            return;
        }

        bool showInteract = false;

        if (isLookingAtInteractable)
        {
            // Baktýðýmýz þey bir "Eþya Verici" mi?
            bool isItemGiver = currentInteractable.HandRigType == PlayerManager.HandRigTypes.SingleHandGrab;

            // Senaryo A: Elim boþ.
            if (!isHoldingItem)
            {
                if (isItemGiver && !HasEmptySlot())
                    showInteract = false;
                else
                    showInteract = true;
            }
            // Senaryo B: Elim dolu.
            else if (!isItemGiver || HasEmptySlot())
            {
                showInteract = true;
            }
        }

        // --- DÜZELTME BURADA (SHOW TAKE MANTIÐI) ---
        // Yerdeki eþyayý almak için tek þart: Yerde eþya olmasý VE Boþ slot olmasý.
        // Elimizin dolu olup olmamasý önemli deðil (çünkü doluysa cebe atar).

        bool showTake = isLookingAtGrabbable && HasEmptySlot();

        // -------------------------------------------

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
        if (IsHoldingTray) return;

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
                    // Kýsa týk interact
                    TryToInteract();
                }
                else if (isUsingGrabbedItem)
                {
                    // Uzun basma býrakýldý kullaným bitti
                    currentGrabable.OnUseRelease();

                    // YENÝ: Eþyayý kullandýk ve býraktýk. 
                    // Bu frame içinde baþka bir iþlem (yerden alma gibi) yapýlmasýn diye iþaretliyoruz.
                    InteractKeyIsDone = true;
                }

                interactChargeTimer = 0f;
                isUsingGrabbedItem = false;

                // SÝLÝNDÝ: InteractKeyIsDone = false; 
                // Bu satýrý siliyoruz. Çünkü burada false yaparsak, 
                // hemen arkasýndan çalýþan GrabInput fonksiyonu "aa yasak yokmuþ" deyip eþyayý alýr.
                // Resetleme iþini artýk Update'in baþýnda yapýyoruz.

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
                if ((currentGrabable == null || !currentGrabable.IsGrabbed))
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
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabable") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("GrabableOutlined") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("GrabableOutlinedGreen") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {
                    // --- MAGNETISM ---
                    InputManager.Instance.aimAssistSlowdown = InputManager.Instance.GetMagnetStrength();

                    // 1. Hedefi Bul
                    IGrabable rawGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                    IGrabable targetMaster = rawGrabable?.Master;

                    if (targetMaster == null) return;

                    // --- STATE A: ELÝM BOÞ, ÝLK DEFA BÝR ÞEYE BAKIYORUM ---
                    if (currentGrabable == null)
                    {
                        // Temizlik (ne olur ne olmaz)
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                        }

                        currentGrabable = targetMaster;
                        currentGrabable.OnFocus();

                        // ÝÞLEM BÝTTÝ, ÞÝMDÝ GÜNCELLE
                        DecideOutlineAndCrosshair();
                    }
                    // --- STATE B: ZATEN ELÝMDE BÝR ÞEY VAR (HOLDING) ---
                    else if (currentGrabable.IsGrabbed)
                    {
                        // Baktýðým þey, þu an "Yerdeki Hedefim" (otherGrabable) ile ayný deðilse?
                        if (otherGrabable != targetMaster)
                        {
                            // 1. Eskiyi temizle
                            if (otherGrabable != null)
                            {
                                otherGrabable.OnLoseFocus();
                                otherGrabable = null;
                                // BURADA Decide ÇAÐIRMIYORUZ, ÇÜNKÜ HENÜZ ÝÞÝMÝZ BÝTMEDÝ
                            }

                            // 2. Yeniyi ata (Eðer elimdekiyle ayný þeye bakmýyorsam)
                            if (targetMaster != currentGrabable)
                            {
                                otherGrabable = targetMaster;
                                otherGrabable.OnFocus();
                            }

                            // 3. HER ÞEY HAZIR, TEK SEFERDE GÜNCELLE
                            DecideOutlineAndCrosshair();
                        }
                    }
                    // --- STATE C: ELÝM BOÞ AMA BAÞKA BÝR EÞYAYA GEÇÝÞ YAPIYORUM (SWITCHING) ---
                    // Sorun buradaydý: Arada sürekli Decide çaðýrýp sistemi yoruyorduk.
                    else if (currentGrabable != targetMaster)
                    {
                        // 1. Other Temizliði
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                        }

                        // 2. Eski Current'ý Býrak
                        currentGrabable.OnLoseFocus();

                        // 3. Yeni Current'ý Al
                        currentGrabable = targetMaster;
                        currentGrabable.OnFocus();

                        // 4. FLICKER OLMAMASI ÝÇÝN SADECE EN SONDA ÇAÐIR
                        DecideOutlineAndCrosshair();
                    }
                }
                // --- Hedef Layer deðilse (Ama Raycast çarptýysa) ---
                else
                {
                    CleanUpFocus();
                }
            }
            // --- Collider yoksa ---
            else
            {
                CleanUpFocus();
            }
        }
        // --- Raycast boþa düþtüyse ---
        else
        {
            CleanUpFocus();
        }
    }

    // Kod tekrarýný önlemek için temizlik fonksiyonu
    private void CleanUpFocus()
    {
        bool changed = false;

        if (currentGrabable != null && !currentGrabable.IsGrabbed)
        {
            currentGrabable.OnLoseFocus();
            currentGrabable = null;
            changed = true;
        }

        if (otherGrabable != null)
        {
            otherGrabable.OnLoseFocus();
            otherGrabable = null;
            changed = true;
        }

        if (changed) DecideOutlineAndCrosshair();
    }

    private void HandleGrabInput()
    {
        // --- BÖLÜM 1: ELDEKÝ EÞYAYI KULLANMA / FIRLATMA / BIRAKMA ---
        // Tepsi elimizdeyken de burasý çalýþýr (Drop için).
        if (currentGrabable != null && currentGrabable.IsGrabbed && !isUsingGrabbedItem)
        {
            // SENARYO A: FIRLATILABÝLÝR BÝR EÞYA (Eski Þarjlý Sistem)
            if (currentGrabable.IsThrowable)
            {
                if (InputManager.Instance.PlayerThrow()) // Sað týk basýlý tutma (Þarj)
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

                if (InputManager.Instance.PlayerThrowRelease()) // Sað týk býrakma (Fýrlat)
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

                    // Hedef Hesaplama (Nereye atýyoruz?)
                    Vector3 targetPoint;
                    Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    if (Physics.Raycast(ray, out RaycastHit hit, 50f, throwRaycastLayers))
                        targetPoint = hit.point;
                    else
                        targetPoint = ray.GetPoint(50f);
                    Vector3 throwDir = (targetPoint - grabPoint.position).normalized;

                    // Fýrlatma Gücü Kararý
                    if (throwChargeTimer <= quickTapThreshold)
                        currentGrabable.OnDrop(throwDir, isCrouching ? minThrowForce * 0.5f : IsSprinting ? minThrowForce * 1.5f : minThrowForce);
                    else
                    {
                        SoundManager.Instance.PlaySoundFX(throwSound, grabPoint, throwVolume, throwMinPitch, throwMaxPitch);
                        currentGrabable.OnThrow(throwDir, isCrouching ? currentThrowForce * 0.5f : IsSprinting ? currentThrowForce * 1.5f : currentThrowForce);
                    }

                    // Temizlik
                    RemoveItemFromInventory(currentGrabable);
                    throwChargeTimer = 0f;
                    anim.SetBool("throw", false);

                    if (currentSlotIndex == -1)
                    {
                        currentGrabable = null;
                        SetHandAnimBoolsOff();

                        if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);
                        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));
                    }

                    DecideOutlineAndCrosshair();
                }
            }
            // SENARYO B: FIRLATILAMAZ EÞYA (Tepsi vb. - Anýnda Býrakma)
            else
            {
                // Basar basmaz býrak, þarj bekleme.
                if (InputManager.Instance.PlayerThrow())
                {
                    // Rig ve Animasyonlarý Sýfýrla
                    if (rightHandRigLerpCoroutine != null)
                    {
                        StopCoroutine(rightHandRigLerpCoroutine);
                        rightHandRigLerpCoroutine = null;
                    }
                    rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));

                    ResetHandAnim();
                    SetHandAnimBoolsOff();

                    if (IsHoldingTray)
                        SetTrayMode(false);

                    // Direkt Drop Çaðýr (Throw deðil)
                    currentGrabable.OnDrop(transform.forward, minThrowForce); // Ufak bir itme kuvveti iyidir

                    // Envanterden Sil ve Temizle
                    RemoveItemFromInventory(currentGrabable);

                    if (currentSlotIndex == -1)
                    {
                        currentGrabable = null;
                        // Tepsi modundan çýkýþý ResetGrab/RemoveItemFromInventory hallediyor ama garanti olsun:
                        if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);
                        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));
                    }

                    DecideOutlineAndCrosshair();
                }
            }
        }

        // --- BÖLÜM 2: YERDEN EÞYA ALMA (PICK UP) ---
        bool attemptPickUp = false;

        // --- YENÝ KONTROL: Tepsi varsa ALMA teþebbüsünü engelle ---
        if (!IsHoldingTray)
        {
            if (currentGrabable != null && currentGrabable.IsGrabbed && currentGrabable.IsUseable)
            {
                if (InputManager.Instance.PlayerInteractRelease() && !isUsingGrabbedItem && !InteractKeyIsDone)
                {
                    attemptPickUp = true;
                }
            }
            else
            {
                if (InputManager.Instance.PlayerInteract())
                {
                    attemptPickUp = true;
                }
            }
        }
        // ----------------------------------------------------------

        // Karar verildi, alma iþlemi deneniyor...
        if (attemptPickUp && throwChargeTimer < 0.1f)
        {
            if (PerformInteractionCast(out RaycastHit hit, interactionDistance, grabableLayers))
            {
                IGrabable targetItem = hit.collider.gameObject.GetComponent<IGrabable>()?.Master;

                if (targetItem == null) return;

                // --- KOMBÝNASYON KONTROLLERÝ ---
                if (currentGrabable != null && currentGrabable.IsGrabbed && targetItem != currentGrabable)
                {
                    if (currentGrabable.TryCombine(targetItem))
                    {
                        if (otherGrabable == targetItem) otherGrabable = null;
                        DecideOutlineAndCrosshair();
                        return;
                    }
                    else if (targetItem.TryCombine(currentGrabable))
                    {
                        if (currentSlotIndex != -1)
                        {
                            inventoryItems[currentSlotIndex] = null;
                        }
                        PickUpItem(targetItem);
                        otherGrabable = null;
                        DecideOutlineAndCrosshair();
                        return;
                    }
                }
                // -----------------------------------------

                // ALMA MANTIÐI
                if ((currentGrabable == null || !currentGrabable.IsGrabbed) && targetItem == currentGrabable)
                {
                    PickUpItem(currentGrabable);
                }
                else if (targetItem == otherGrabable)
                {
                    if (HasEmptySlot())
                    {
                        PickUpItem(otherGrabable);
                        otherGrabable = null;
                    }
                }
                else if (!targetItem.IsGrabbed && HasEmptySlot())
                {
                    PickUpItem(targetItem);
                }
            }
        }
    }

    private void SetHandAnimBoolsOff()
    {
        anim.SetInteger("grabInt", 0);
        anim.SetBool("chargingThrow", false);
    }
    private void DecideGrabAnimBool()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed)
        {
            switch (currentGrabable.HandGrabType)
            {
                case PlayerManager.HandGrabTypes.RegularGrab:
                    anim.SetInteger("grabInt", 1);
                    break;

                case PlayerManager.HandGrabTypes.BottleGrab:
                    anim.SetInteger("grabInt", 2);
                    break;

                case PlayerManager.HandGrabTypes.TrashGrab:
                    anim.SetInteger("grabInt", 3);
                    break;

                case PlayerManager.HandGrabTypes.KnifeGrab:
                    anim.SetInteger("grabInt", 4);
                    break;

                case PlayerManager.HandGrabTypes.ThinBurgerIngredientGrab:
                    anim.SetInteger("grabInt", 5);
                    break;

                case PlayerManager.HandGrabTypes.RegularBurgerIngredientGrab:
                    anim.SetInteger("grabInt", 6);
                    break;

                case PlayerManager.HandGrabTypes.ThickBurgerIngredientGrab:
                    anim.SetInteger("grabInt", 7);
                    break;

                case PlayerManager.HandGrabTypes.NoodleGrab:
                    anim.SetInteger("grabInt", 8);
                    break;

                case PlayerManager.HandGrabTypes.KettleGrab:
                    anim.SetInteger("grabInt", 9);
                    break;

                case PlayerManager.HandGrabTypes.WholeIngredientGrab:
                    anim.SetInteger("grabInt", 10);
                    break;

                case PlayerManager.HandGrabTypes.BigWholeIngredientGrab:
                    anim.SetInteger("grabInt", 11);
                    break;

                case PlayerManager.HandGrabTypes.WholeBunGrab:
                    anim.SetInteger("grabInt", 12);
                    break;
                case PlayerManager.HandGrabTypes.WholeBurgerGrab:
                    anim.SetInteger("grabInt", 13);
                    break;
                case PlayerManager.HandGrabTypes.SauceCapsuleGrab:
                    anim.SetInteger("grabInt", 14);
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

    public void ForceUpdateCurrentGrabableReference(IGrabable newReference)
    {
        // 1. Ana referansý güncelle
        currentGrabable = newReference;

        // 2. Envanter dizisini güncelle (Çok Kritik!)
        // Bunu yapmazsak slot deðiþtirdiðinde veya tekrar baktýðýnda eski scripti bulur.
        if (currentSlotIndex != -1 && currentSlotIndex < inventoryItems.Length)
        {
            inventoryItems[currentSlotIndex] = newReference;
        }

        // 3. UI'ý güncelle (Ýkon deðiþmiþ olabilir)
        RefreshInventoryUI();

        // 4. Outline rengini yeni scripte göre tekrar hesapla
        DecideOutlineAndCrosshair();
    }

    public void ChangeCurrentGrabable(IGrabable grabObject)
    {
        PickUpItem(grabObject);
    }

    // Eþyayý envantere ve ele alma iþlemini yapan ana fonksiyon
    private void PickUpItem(IGrabable itemToPickUp)
    {
        // 1. Önce boþ bir slot var mý diye bakýyoruz
        int emptySlotIndex = -1;
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                emptySlotIndex = i;
                break; // Ýlk boþ yeri bulduk, döngüden çýk
            }
        }

        // Eðer boþ yer yoksa (emptySlotIndex hala -1 ise) iþlemi iptal et
        if (emptySlotIndex == -1) return;

        // 2. Eðer þu an elimizde baþka bir eþya varsa onu gizle
        if (currentSlotIndex != -1 && inventoryItems[currentSlotIndex] != null)
        {
            IGrabable oldItem = inventoryItems[currentSlotIndex];

            oldItem.OnHolster();

            ((MonoBehaviour)oldItem).gameObject.SetActive(false);
        }

        // 3. Yeni eþyayý bulduðumuz BOÞ slota yerleþtir
        inventoryItems[emptySlotIndex] = itemToPickUp;

        // --- YENÝ EKLENEN KISIM: GEÇMÝÞE KAYDET ---
        // Eðer listede varsa önce çýkar (yer deðiþtirme ihtimaline karþý), sonra en sona ekle.
        if (itemPickupHistory.Contains(itemToPickUp)) itemPickupHistory.Remove(itemToPickUp);
        itemPickupHistory.Add(itemToPickUp);
        // ------------------------------------------

        // 4. O slotu seçili hale getir
        currentSlotIndex = emptySlotIndex;
        currentGrabable = itemToPickUp;

        // 5. Fiziksel Alma Ýþlemi
        Transform targetGrabPoint = (currentGrabable.HandRigType == PlayerManager.HandRigTypes.HoldingTray)
                                    ? trayParentTransform
                                    : grabPoint;

        currentGrabable.OnGrab(targetGrabPoint);

        // Diðer 'Other' temizlikleri
        if (otherGrabable == itemToPickUp) otherGrabable = null;
        if (focusTextAnim != null)
        {
            focusTextAnim.StopShowingText();
            focusTextAnim.StartDisappearingText();
        }

        // 6. Rig ve Animasyon
        DecideGrabAnimBool();

        // --- DEÐÝÞEN KISIM BURASI ---
        // Eski 4-5 satýrlýk Rig kodunu sildik, yerine bunu yazdýk:
        ApplyGrabHandRig();
        // ----------------------------

        InteractKeyIsDone = true;
        DecideOutlineAndCrosshair();

        // UI Güncelle
        RefreshInventoryUI();
    }

    private void RemoveItemFromInventory(IGrabable itemToRemove)
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == itemToRemove)
            {
                // 1. Önce o slotu boþalt
                inventoryItems[i] = null;

                // --- YENÝ: GEÇMÝÞTEN SÝL ---
                if (itemPickupHistory.Contains(itemToRemove))
                {
                    itemPickupHistory.Remove(itemToRemove);
                }

                // 2. Eðer attýðýmýz eþya þu an elimizde deðilse (pasif bir slotu sildiysek) çýk.
                if (currentSlotIndex != i) break;

                // --- YENÝ: GEÇMÝÞE GÖRE SEÇÝM MANTIÐI ---
                int targetIndex = -1;

                // Geçmiþ listesini SONDAN BAÞA doðru tara (En son ekleneni bulmak için)
                for (int h = itemPickupHistory.Count - 1; h >= 0; h--)
                {
                    IGrabable candidateItem = itemPickupHistory[h];

                    // Bu aday eþya þu an envanterde hangi slotta?
                    // (Hala envanterde mi diye kontrol ediyoruz, belki baþka bir þekilde silinmiþtir)
                    for (int k = 0; k < inventoryItems.Length; k++)
                    {
                        if (inventoryItems[k] == candidateItem)
                        {
                            targetIndex = k; // Slotu bulduk!
                            break;
                        }
                    }

                    // Eðer envanterde bulduysak döngüyü kýr, hedefimiz bu.
                    if (targetIndex != -1) break;
                }

                // 3. Sonucu Uygula
                if (targetIndex != -1)
                {
                    // Bulduðumuz (en son eklenen) slotu seç
                    EquipSlot(targetIndex);
                }
                else
                {
                    // Geçmiþte dönülecek bir eþya yoksa eli boþa düþür.
                    currentSlotIndex = -1;
                }

                break; // Ýþim bitti, döngüden çýk
            }
        }

        RefreshInventoryUI();
    }

    private void HandleInventoryInput()
    {
        if (InputManager.Instance.PlayerSlot1()) ToggleSlot(0);
        if (InputManager.Instance.PlayerSlot2()) ToggleSlot(1);
        if (InputManager.Instance.PlayerSlot3()) ToggleSlot(2);
        if (InputManager.Instance.PlayerSlot4()) ToggleSlot(3);
    }

    private void ToggleSlot(int slotIndex)
    {
        // Array sýnýr kontrolü
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length) return;

        // Eðer o slot BOÞ ise, oraya geçiþ yapma (Veya istersen boþ el yapabilirsin)
        // Hello Neighbor mantýðýnda boþ slota basýnca bir þey olmuyor genelde.
        if (inventoryItems[slotIndex] == null) return;

        // DURUM 1: Zaten bu slottaysak -> HOLSTER
        if (currentSlotIndex == slotIndex)
        {
            HolsterItem();
        }
        // DURUM 2: Baþka (dolu) bir slota geçiþ
        else
        {
            EquipSlot(slotIndex);
        }
    }

    private void EquipSlot(int slotIndex)
    {
        // 1. Elimde bir þey varsa gizle
        if (currentSlotIndex != -1 && inventoryItems[currentSlotIndex] != null)
        {
            IGrabable oldItem = inventoryItems[currentSlotIndex];

            oldItem.OnHolster();

            ((MonoBehaviour)oldItem).gameObject.SetActive(false);
        }

        // 2. Yeni slotu belirle
        currentSlotIndex = slotIndex;
        IGrabable newItem = inventoryItems[currentSlotIndex]; // Bu artýk null olamaz (yukarýda check ettik)

        // 3. Aktif et
        ((MonoBehaviour)newItem).gameObject.SetActive(true);
        currentGrabable = newItem;

        // ... Geri kalan standart Rig/Animasyon kodlarý aynen kalýyor ...
        currentGrabable.OnGrab(grabPoint);
        DecideGrabAnimBool();

        ApplyGrabHandRig();

        InteractKeyIsDone = true;
        DecideOutlineAndCrosshair();

        RefreshInventoryUI();
    }

    private void HolsterItem()
    {
        if (currentSlotIndex == -1) return;

        // Mevcut eþyayý gizle
        if (inventoryItems[currentSlotIndex] != null)
        {
            IGrabable item = inventoryItems[currentSlotIndex];

            item.OnHolster();

            ((MonoBehaviour)item).gameObject.SetActive(false);
        }

        // Deðiþkenleri sýfýrla
        currentSlotIndex = -1;
        currentGrabable = null;

        // Eli indir
        if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);
        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));

        ResetHandAnim();
        SetHandAnimBoolsOff();

        DecideOutlineAndCrosshair();
        RefreshInventoryUI();
    }

    private bool HasEmptySlot()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                return true; // Boþ yer bulduk!
            }
        }
        return false; // Hiç boþ yer yok, hepsi dolu.
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
    public bool ShouldIGoWithOutlineWhenTurningBackToGrabable (IGrabable grabable) {  return (currentGrabable == grabable && !currentGrabable.IsGrabbed) || otherGrabable == grabable; }

    public void SetFocusTextComplete(bool value) => focusTextComplete = value; //GETS TRUE IN TYPEWRITTER ACTIONS WHEN ITS COMPLETE. GETS FALSE WHENEVER TEXT GETS CHANGED ON DECIDEFOCUSTEXT()

    private void ResetHandAnim()
    {
        interactChargeTimer = 0f;
        isUsingGrabbedItem = false;

        IsUsingItemX = false;
        IsUsingItemY = false;
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
            yield return new WaitForSeconds(0.15f);
            if (leftHandRigLerpCoroutine == null)
                leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(false, false));
        }
    }

    private IEnumerator LerpRightHandRig(bool shouldReach, bool shouldGoBack)
    {
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

        rightHandRigLerpCoroutine = null;

        if (shouldGoBack)
        {
            yield return new WaitForSeconds(0.15f);
            if (rightHandRigLerpCoroutine == null)
                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));
        }
    }

    private void SetTrayMode(bool active)
    {
        if (active)
        {
            // --- TRAY MODU AÇILIYOR ---

            // 1. Hareket Kýsýtlamalarý
            CanJump = false;
            CanCrouch = false;
            walkSpeed = trayWalkSpeed;
            sprintSpeed = traySprintSpeed;

            // 2. Bakýþ Kýsýtlamalarý
            upperLookLimit = trayLookLimits.x;
            lowerLookLimit = trayLookLimits.y;

            lookSpeedX = defaultLookSpeedX * trayLookSensitivityMultiplier;
            lookSpeedY = defaultLookSpeedY * trayLookSensitivityMultiplier;

            // Animasyon
            anim.SetBool("lockUpperBody", true);

            // --- ÝPTAL EDÝLENLER ---
            // CanGrab = false;     <-- SÝLDÝK (Raycast atmaya ve Drop yapmaya ihtiyacýmýz var)
            // CanInteract = false; <-- SÝLDÝK (Raycast atmaya ve Outline görmeye ihtiyacýmýz var)
        }
        else
        {
            // --- TRAY MODU KAPANIYOR ---

            // 1. Deðerleri Geri Yükle
            CanJump = defaultCanJump;
            CanCrouch = defaultCanCrouch;
            walkSpeed = defaultWalkSpeed;
            sprintSpeed = defaultSprintSpeed;

            upperLookLimit = defaultUpperLookLimit;
            lowerLookLimit = defaultLowerLookLimit;

            lookSpeedX = defaultLookSpeedX;
            lookSpeedY = defaultLookSpeedY;

            // Animasyon
            anim.SetBool("lockUpperBody", false);

            // --- ÝPTAL EDÝLENLER ---
            // CanGrab = defaultCanGrab;         <-- SÝLDÝK
            // CanInteract = defaultCanInteract; <-- SÝLDÝK
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
               layer == LayerMask.NameToLayer("GrabableOutlined") ||
               layer == LayerMask.NameToLayer("GrabableOutlinedGreen");
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

    

    void CalculateArmLengths()
    {
        // Sað Kol
        if (twoBoneIKConstraintRightHand != null && twoBoneIKConstraintRightHand.data.root != null)
        {
            float upper = Vector3.Distance(twoBoneIKConstraintRightHand.data.root.position, twoBoneIKConstraintRightHand.data.mid.position);
            float lower = Vector3.Distance(twoBoneIKConstraintRightHand.data.mid.position, twoBoneIKConstraintRightHand.data.tip.position);
            // %99.9 ile çarpmak, tam dümdüz olup kilitlenmesini engeller (Soft Limit)
            rightHandArmLength = (upper + lower) * 0.99f;
        }

        // Sol Kol
        if (twoBoneIKConstraintLeftHand != null && twoBoneIKConstraintLeftHand.data.root != null)
        {
            float upper = Vector3.Distance(twoBoneIKConstraintLeftHand.data.root.position, twoBoneIKConstraintLeftHand.data.mid.position);
            float lower = Vector3.Distance(twoBoneIKConstraintLeftHand.data.mid.position, twoBoneIKConstraintLeftHand.data.tip.position);
            leftHandArmLength = (upper + lower) * 0.99f;
        }
    }

    private void RefreshInventoryUI()
    {
        if (inventoryUI != null)
        {
            inventoryUI.UpdateDisplay(inventoryItems, currentSlotIndex);
        }
    }

    private void ApplyGrabHandRig()
    {
        if (currentGrabable == null) return;

        // 1. Önce eski sað el iþlemini durdur (Her ihtimale karþý)
        if (rightHandRigLerpCoroutine != null)
        {
            StopCoroutine(rightHandRigLerpCoroutine);
            rightHandRigLerpCoroutine = null;
        }

        // 2. Ortak Deðiþkenleri Ata
        
        // TELEFON KALDIRILACAK OYUNDAN: coyoteTimeForPhone = phoneCoyoteForGrab;

        // 3. TÝP KONTROLÜ (Switch Case)
        switch (currentGrabable.HandRigType)
        {
            case PlayerManager.HandRigTypes.SingleHandGrab:
                // ESKÝ MANTIK: Sað eli kaldýr
                currentPositionOffsetForRightHand = currentGrabable.GrabPositionOffset;
                currentRotationOffsetForRightHand = currentGrabable.GrabRotationOffset;
                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, false));
                break;

            case PlayerManager.HandRigTypes.HoldingTray:
                SetTrayMode(true);
                break;
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
