using System.Collections;
using System.Collections.Generic;
using BreakToEscape.Core.Story;
using TMPro;
using UnityEngine;

/// <summary>
/// Dispatch, the mine's automated voice: short caption lines in a banner at the top of the screen.
/// Lines come from story.json by key; they queue so none are lost, and repeats of the line already
/// showing are dropped.
/// </summary>
public class Dispatch : MonoBehaviour
{
    public static Dispatch Instance { get; private set; }

    public RectTransform bannerRect;
    public TextMeshProUGUI bannerText;
    public float slideSeconds = 0.25f;
    public float holdSeconds = 3.4f;
    public Vector2 hiddenPosition = new Vector2(0f, 120f);
    public Vector2 shownPosition = new Vector2(0f, -28f);
    public int maxQueued = 4;

    private StoryData story;
    private readonly Queue<string> queue = new Queue<string>();
    private string showing;
    private bool running;

    void Awake()
    {
        Instance = this;
        if (!bannerRect || !bannerText) Debug.LogError("Dispatch: banner references are not assigned.", this);
        if (bannerRect) bannerRect.gameObject.SetActive(false);
    }

    public void Initialize(StoryData storyData) => story = storyData;

    /// <summary>Shows a story.json dispatch line. {0} in the line is replaced with <paramref name="arg"/>.</summary>
    public void Say(string key, string arg = null)
    {
        if (story == null) return;
        string line = story.Line(key);
        if (arg != null) line = line.Replace("{0}", arg);
        SayText(line);
    }

    public void SayText(string line)
    {
        if (string.IsNullOrEmpty(line) || line == showing || queue.Contains(line)) return;
        if (queue.Count >= maxQueued) queue.Dequeue();
        queue.Enqueue(line);
        if (!running && isActiveAndEnabled) StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;
        while (queue.Count > 0)
        {
            showing = queue.Dequeue();
            if (bannerText) bannerText.text = showing;
            if (bannerRect)
            {
                bannerRect.gameObject.SetActive(true);
                for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
                {
                    bannerRect.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, Mathf.SmoothStep(0f, 1f, t / slideSeconds));
                    yield return null;
                }
                bannerRect.anchoredPosition = shownPosition;
            }
            yield return new WaitForSecondsRealtime(holdSeconds);
            if (bannerRect)
            {
                for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
                {
                    bannerRect.anchoredPosition = Vector2.Lerp(shownPosition, hiddenPosition, Mathf.SmoothStep(0f, 1f, t / slideSeconds));
                    yield return null;
                }
                bannerRect.gameObject.SetActive(false);
            }
        }
        showing = null;
        running = false;
    }
}
