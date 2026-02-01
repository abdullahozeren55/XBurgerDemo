using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scenario/Events/Pause Spawning")]
public class PauseSpawningEvent : ScenarioEventSO
{
    public override IEnumerator Play(ScenarioContext ctx)
    {
        ctx.scenario.PauseSpawning();
        yield return null; // Anýnda gerçekleþir, bekleme yapmaz
    }
}

[CreateAssetMenu(menuName = "Scenario/Events/Resume Spawning")]
public class ResumeSpawningEvent : ScenarioEventSO
{
    [Tooltip("Resume etmeden önce kaç saniye beklesin?")]
    public float delayBeforeResume = 0f;

    public override IEnumerator Play(ScenarioContext ctx)
    {
        if (delayBeforeResume > 0)
            yield return new WaitForSeconds(delayBeforeResume);

        ctx.scenario.ResumeSpawning();
    }
}