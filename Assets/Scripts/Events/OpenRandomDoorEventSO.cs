using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scenario Events/Open Random Door")]
public class OpenRandomDoorEventSO : ScenarioEventSO
{
    public float delayAfterTriggered = 4f;
    public DoorType[] DoorsToOpen;
    public override IEnumerator Play(ScenarioContext ctx)
    {
        yield return new WaitForSeconds(delayAfterTriggered);

        if (EventManager.Instance)
            EventManager.Instance.OpenRandomDoor(DoorsToOpen);
    }
}
