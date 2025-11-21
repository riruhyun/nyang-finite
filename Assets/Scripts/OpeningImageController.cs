using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpeningImageController : MonoBehaviour
{
    public static OpeningImageController Instance;

    private List<GameObject> activeImages = new List<GameObject>();
    private List<GameObject> imagesToClearNextScene = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void ClearAllImagesNextScene()
    {
        foreach (var img in activeImages)
            imagesToClearNextScene.Add(img);
    }

    public void SpawnImage(OpeningImage data)
    {
        StartCoroutine(SpawnRoutine(data, null));
    }

    // Overload: allow passing a uniform scale (same for X/Y)
    public void SpawnImage(OpeningImage data, float uniformScale)
    {
        StartCoroutine(SpawnRoutine(data, new Vector3(uniformScale, uniformScale, 1f)));
    }

    // Overload: allow passing non-uniform scale (X/Y)
    public void SpawnImage(OpeningImage data, Vector2 scale)
    {
        StartCoroutine(SpawnRoutine(data, new Vector3(scale.x, scale.y, 1f)));
    }

    private IEnumerator SpawnRoutine(OpeningImage data, Vector3? overrideScale)
    {
        if (data.GetStartDelay() > 0)
            yield return new WaitForSeconds(data.GetStartDelay());

        Sprite sprite = Resources.Load<Sprite>(data.GetName());
        if (sprite == null)
        {
            Debug.LogError("Sprite not found: " + data.GetName());
            yield break;
        }

        GameObject obj = new GameObject(data.GetName());
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        obj.transform.position = data.GetStartPos();
        obj.transform.localScale = overrideScale.HasValue ? overrideScale.Value : Vector3.one * data.GetSize();

        Color c = sr.color;
        c.a = 0;
        sr.color = c;

        activeImages.Add(obj);

        // --- Fade In ---
        if (data.GetFadeInTime() > 0)
        {
            float t = 0;
            while (t < data.GetFadeInTime())
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0, 1, t / data.GetFadeInTime());
                sr.color = c;
                yield return null;
            }
        }
        else
        {
            c.a = 1;
            sr.color = c;
        }

        // --- Move ---
        if (data.GetMoveDelay() > 0)
            yield return new WaitForSeconds(data.GetMoveDelay());

        if (data.GetMoveToPos().HasValue)
        {
            Vector3 start = obj.transform.position;
            Vector3 dest = data.GetMoveToPos().Value;
            float moveTime = data.GetMoveTime();

            float t = 0;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                obj.transform.position = Vector3.Lerp(start, dest, t / moveTime);
                yield return null;
            }
            obj.transform.position = dest;
        }
    }

    private void LateUpdate()
    {
        if (imagesToClearNextScene.Count > 0)
        {
            foreach (var img in imagesToClearNextScene)
                Destroy(img);

            imagesToClearNextScene.Clear();
        }
    }
}
