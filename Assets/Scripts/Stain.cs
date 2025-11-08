using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stain : MonoBehaviour
{
    [SerializeField] private float lerpTime = 0.2f;

    private MeshRenderer meshRenderer;
    private Coroutine cleanCoroutine;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        cleanCoroutine = null;
    }

    public void Clear()
    {
        cleanCoroutine = StartCoroutine(ClearStain(meshRenderer.material.color.a <= 80f / 255f));
    }

    private IEnumerator ClearStain(bool shouldDestroy)
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale / 1.2f;

        Color startColor = meshRenderer.material.color;
        float targetAlpha = shouldDestroy ? 0f : startColor.a / 3;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        float elapsedTime = 0f;

        while (elapsedTime < lerpTime)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / lerpTime);
            meshRenderer.material.color = Color.Lerp(startColor, endColor, elapsedTime / lerpTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        meshRenderer.material.color = endColor;

        if (shouldDestroy)
            Destroy(gameObject);

        cleanCoroutine = null;
    }
}
