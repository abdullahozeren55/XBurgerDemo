using System.Collections;
using UnityEngine;

public abstract class ScenarioEventSO : ScriptableObject
{
    public abstract IEnumerator Play(ScenarioContext ctx);
}

public class ScenarioContext
{
    public ScenarioManager scenario;
    public CustomerManager customers;
}