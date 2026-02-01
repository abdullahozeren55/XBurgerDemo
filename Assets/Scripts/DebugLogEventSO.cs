using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scenario Events/Debug Log")]
public class DebugLogEventSO : ScenarioEventSO
{
    public string message = "Event played";

    public override IEnumerator Play(ScenarioContext ctx)
    {
        Debug.Log(message);
        yield return null;
    }
}
