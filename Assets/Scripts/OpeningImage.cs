using UnityEngine;

// Simple data container for OpeningImageController
public class OpeningImage
{
    private string nameKey;
    private Vector3 startPos = Vector3.zero;
    private float startDelay = 0f;
    private float size = 1f;
    private float fadeInTime = 0f;
    private float moveDelay = 0f;
    private Vector3? moveToPos = null;
    private float moveTime = 0f;

    public OpeningImage(string nameKey)
    {
        this.nameKey = nameKey;
    }

    // Getters used by OpeningImageController
    public float GetStartDelay() => startDelay;
    public string GetName() => nameKey;
    public Vector3 GetStartPos() => startPos;
    public float GetSize() => size;
    public float GetFadeInTime() => fadeInTime;
    public float GetMoveDelay() => moveDelay;
    public Vector3? GetMoveToPos() => moveToPos;
    public float GetMoveTime() => moveTime;

    // Optional fluent setters for convenience
    public OpeningImage SetStartDelay(float value) { startDelay = value; return this; }
    public OpeningImage SetStartPos(Vector3 pos) { startPos = pos; return this; }
    public OpeningImage SetSize(float s) { size = s; return this; }
    public OpeningImage SetFadeInTime(float t) { fadeInTime = t; return this; }
    public OpeningImage SetMoveDelay(float d) { moveDelay = d; return this; }
    public OpeningImage SetMoveToPos(Vector3? p) { moveToPos = p; return this; }
    public OpeningImage SetMoveTime(float t) { moveTime = t; return this; }
}

