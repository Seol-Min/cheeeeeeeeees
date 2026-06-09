using UnityEngine;
using System.Collections;

public class TempEscapeAnim : MonoBehaviour
{
    [SerializeField] private float animationSpeed = 5f;

    private void OnEnable()
    {
        StartCoroutine(ScaleAnimation());
    }

    private IEnumerator ScaleAnimation()
    {
        float scaleX = 0f;

        transform.localScale = new Vector3(0f, 1f, 1f);

        while (scaleX < 1f)
        {
            scaleX += animationSpeed * Time.unscaledDeltaTime;

            transform.localScale = new Vector3(
                Mathf.Clamp01(scaleX),
                1f,
                1f
            );

            yield return null;
        }

        transform.localScale = Vector3.one;
    }
}