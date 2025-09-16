using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

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
        DontDestroyOnLoad(gameObject);

        playableDirector = GetComponent<PlayableDirector>();
    }

    public void PlayCutscene(CutsceneType type)
    {
        GameManager.Instance.ChangePlayerCanMove(false);
        GameManager.Instance.TurnCrosshairOnOff(false);

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
        GameManager.Instance.ChangePlayerCanMove(true);
        GameManager.Instance.TurnCrosshairOnOff(true);
        playableDirector.Stop();
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