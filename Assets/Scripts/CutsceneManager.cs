using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using DG.Tweening;
public enum CutsceneType
{
    AfterFirstNoodleP1,
    AfterFirstNoodleP2,
    FirstMim
    // Buraya istediðin kadar cutscene ekleyebilirsin
}

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [System.Serializable]
    public struct CutsceneEntry
    {
        public CutsceneType type;
        public PlayableAsset cutscene;
    }

    [SerializeField] private CinemachineVirtualCamera dollyCam;
    [Space]
    [SerializeField] private CutsceneEntry[] cutscenes;
    [Space]
    [Header("UI Settings")]
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject focusText;
    [SerializeField] private GameObject UITexts;
    [SerializeField] private GameObject[] cutsceneBars; //0 top, 1 bottom

    private PlayableAsset currentCutscene;

    private PlayableDirector playableDirector;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        playableDirector = GetComponent<PlayableDirector>();
    }

    public void PlayCutscene(CutsceneType type)
    {
        PlayerManager.Instance.SetPlayerBasicMovements(false);

        GetReadyForCutscene(true);

        if (playableDirector.state == PlayState.Playing)
            StopCutscene();

        // Enum’a göre PlayableDirector bul
        foreach (var entry in cutscenes)
        {
            if (entry.type == type)
            {
                currentCutscene = entry.cutscene;
                playableDirector.playableAsset = currentCutscene;

                playableDirector.time = 0;
                playableDirector.Evaluate();
                playableDirector.Play();
                break;
            }
        }
    }

    public void StopCutscene()
    {
        PlayerManager.Instance.SetPlayerBasicMovements(true);

        GetReadyForCutscene(false);

        playableDirector.Stop();
    }

    public void GetReadyForCutscene(bool shouldGetReady)
    {
        TurnCrosshairOnOff(!shouldGetReady);

        // --- Letterbox bar animasyonu ---
        if (cutsceneBars.Length >= 2)
        {
            RectTransform topBar = cutsceneBars[0].GetComponent<RectTransform>();
            RectTransform bottomBar = cutsceneBars[1].GetComponent<RectTransform>();

            float barHeight = topBar.sizeDelta.y;
            float duration = 1.5f;

            // Barlarýn anchored position’larýný hedef pozisyonlara göre ayarla
            Vector2 topTarget = shouldGetReady ? Vector2.zero : new Vector2(0, barHeight);
            Vector2 bottomTarget = shouldGetReady ? Vector2.zero : new Vector2(0, -barHeight);

            // Tweene baþlamadan önce eski tween’leri iptal et
            topBar.DOKill();
            bottomBar.DOKill();

            topBar.DOAnchorPos(topTarget, duration).SetEase(Ease.OutQuad);
            bottomBar.DOAnchorPos(bottomTarget, duration).SetEase(Ease.OutQuad);
        }
    }

    public void TurnCrosshairOnOff(bool value)
    {
        crosshair.SetActive(value);
        focusText.SetActive(value);
        UITexts.SetActive(value);
    }

    public bool IsPlaying()
    {
        return playableDirector != null && playableDirector.state == PlayState.Playing;
    }

    public void StartSelfDialogue(DialogueData data)
    {
        DialogueManager.Instance.StartSelfDialogueInCutscene(data);
    }

    public void SetDollyCamPath(CinemachineSmoothPath path)
    {
        dollyCam.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = path;
    }

    public void CloseDoor(Door door)
    {
        if (door.isOpened)
        {
            door.HandleRotation();
        }
    }
}