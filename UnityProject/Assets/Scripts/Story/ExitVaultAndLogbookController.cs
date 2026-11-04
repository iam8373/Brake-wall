using System.Collections;
using System.Text;
using BreakToEscape.Core.Services;
using BreakToEscape.Core.Story;
using TMPro;
using UnityEngine;

/// <summary>
/// Lift gate, text popups and the ending. The gate needs power (generator), the winch cable and the
/// three-number code from the crew logbooks (rules in <see cref="EscapeStateMachine"/>; numbers in
/// story.json). On success: the gate rises, daylight floods in, Dispatch's last line, then the
/// credits screen with total time and cubes broken, and the New Shift teaser.
/// </summary>
public class ExitVaultAndLogbookController : MonoBehaviour
{
    [Header("Text popup (logbooks, notes, hard hats)")]
    public GameObject textPopupPanel;
    public TextMeshProUGUI textPopupTitle;
    public TextMeshProUGUI textPopupBody;

    [Header("Gate code card")]
    public GameObject codeCardPanel;
    public TextMeshProUGUI codeDisplayA, codeDisplayB, codeDisplayC;
    public TextMeshProUGUI codeStatusText;

    [Header("Ending")]
    public GameObject endingPanel;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingBodyText;
    public Light escapeLight;
    public float gateRiseMeters = 2.8f;
    public float gateOpenSeconds = 1.6f;

    private EscapeStateMachine escape;
    private StoryData story;
    private UIScreenAndAccessibilityManager ui;
    private Transform gateRoot;
    private Transform gateDoor;
    private bool animating;

    public void Initialize(EscapeStateMachine escapeState, StoryData storyData, UIScreenAndAccessibilityManager uiManager)
    {
        escape = escapeState;
        story = storyData;
        ui = uiManager;
        if (textPopupPanel) textPopupPanel.SetActive(false);
        if (codeCardPanel) codeCardPanel.SetActive(false);
        if (endingPanel) endingPanel.SetActive(false);
        if (!textPopupPanel || !codeCardPanel || !endingPanel) Debug.LogError("ExitVaultAndLogbookController: a UI panel is not assigned.", this);
    }

    /// <summary>Called when the gate placeholder is spawned from the layout.</summary>
    public void AttachGate(Transform gate)
    {
        gateRoot = gate;
        gateDoor = gate.Find("GateDoor");
        if (escapeLight)
        {
            escapeLight.transform.position = gate.position + gate.forward * -1.5f + Vector3.up * 1.6f;
            escapeLight.gameObject.SetActive(false);
        }
        if (escape != null && escape.GateOpened && gateDoor) gateDoor.localPosition = Vector3.up * gateRiseMeters;
    }

    // ------------------------------------------------------------------ popups

    public void ShowText(string title, string body)
    {
        if (!textPopupPanel) return;
        if (textPopupTitle) textPopupTitle.text = title;
        if (textPopupBody) textPopupBody.text = body;
        if (ui) ui.ShowOverlay(textPopupPanel);
        else textPopupPanel.SetActive(true);
    }

    public void CloseText()
    {
        if (ui) ui.HideOverlay(textPopupPanel);
        else if (textPopupPanel) textPopupPanel.SetActive(false);
    }

    // ------------------------------------------------------------------ code card

    public void OpenVaultCodeCard()
    {
        if (escape == null || animating) return;
        if (escape.GateOpened) { ShowEnding(); return; }
        if (codeStatusText) codeStatusText.text = StatusLine();
        RefreshDials();
        if (ui) ui.ShowOverlay(codeCardPanel);
        else if (codeCardPanel) codeCardPanel.SetActive(true);
    }

    public void CloseVaultCodeCard()
    {
        if (ui) ui.HideOverlay(codeCardPanel);
        else if (codeCardPanel) codeCardPanel.SetActive(false);
    }

    private string StatusLine()
    {
        var sb = new StringBuilder();
        sb.Append(escape.GeneratorRepaired ? "POWER: ON" : "POWER: OFF");
        sb.Append("   ");
        sb.Append(escape.CableRecovered ? "CABLE: FITTED" : "CABLE: MISSING");
        sb.Append("   LOGBOOKS: ").Append(escape.LogbookCount).Append("/3");
        return sb.ToString();
    }

    public void DialAUp() => Cycle(0, 1);
    public void DialADown() => Cycle(0, -1);
    public void DialBUp() => Cycle(1, 1);
    public void DialBDown() => Cycle(1, -1);
    public void DialCUp() => Cycle(2, 1);
    public void DialCDown() => Cycle(2, -1);

    public void CycleCodeNumber(int slot, int delta) => Cycle(slot, delta);

    private void Cycle(int slot, int delta)
    {
        if (escape == null) return;
        escape.CycleDial(slot, delta);
        RefreshDials();
    }

    private void RefreshDials()
    {
        if (escape == null) return;
        if (codeDisplayA) codeDisplayA.text = escape.Dial(0).ToString("00");
        if (codeDisplayB) codeDisplayB.text = escape.Dial(1).ToString("00");
        if (codeDisplayC) codeDisplayC.text = escape.Dial(2).ToString("00");
    }

    public void TryUnlockExitVault()
    {
        if (escape == null || animating) return;
        GateResult result = escape.TryOpenGate();
        Dispatch d = Dispatch.Instance;
        switch (result)
        {
            case GateResult.NeedPower: if (d) d.Say("gate_need_power"); break;
            case GateResult.NeedCable: if (d) d.Say("gate_need_cable"); break;
            case GateResult.WrongCode: if (d) d.Say("gate_wrong_code"); break;
            case GateResult.Opened:
                CloseVaultCodeCard();
                StartCoroutine(OpenSequence());
                return;
            case GateResult.AlreadyOpen:
                CloseVaultCodeCard();
                ShowEnding();
                return;
        }
        if (codeStatusText) codeStatusText.text = StatusLine();
    }

    private IEnumerator OpenSequence()
    {
        animating = true;
        if (Dispatch.Instance) Dispatch.Instance.Say("gate_open");
        if (escapeLight) { escapeLight.gameObject.SetActive(true); escapeLight.intensity = 0f; }
        Vector3 closed = gateDoor ? gateDoor.localPosition : Vector3.zero;
        for (float t = 0f; t < gateOpenSeconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / gateOpenSeconds);
            if (gateDoor) gateDoor.localPosition = closed + Vector3.up * (gateRiseMeters * k);
            if (escapeLight) escapeLight.intensity = Mathf.Lerp(0f, ui && ui.ReduceFlashing ? 6f : 18f, k);
            yield return null;
        }
        if (Dispatch.Instance) Dispatch.Instance.Say("escape");
        yield return new WaitForSeconds(2.5f);
        escape.MarkEscaped();
        if (GameBootstrap.Instance)
        {
            GameBootstrap.Instance.Analytics(AnalyticsEvents.EscapeCompleted, "runMinutes", GameBootstrap.Instance.RunSeconds / 60f, "cubesBroken", VoxelWorldManager.Instance ? VoxelWorldManager.Instance.Grid.BrokenCount : 0);
            GameBootstrap.Instance.SaveNow();
        }
        animating = false;
        ShowEnding();
    }

    private void ShowEnding()
    {
        if (!endingPanel || story == null) return;
        float minutes = GameBootstrap.Instance ? GameBootstrap.Instance.RunSeconds / 60f : 0f;
        int broken = VoxelWorldManager.Instance && VoxelWorldManager.Instance.Grid != null ? VoxelWorldManager.Instance.Grid.BrokenCount : 0;
        var sb = new StringBuilder();
        foreach (string line in story.endingLines) sb.Append(line).Append("\n\n");
        sb.Append("Total time: ").Append(minutes.ToString("F1")).Append(" min\n");
        sb.Append("Cubes broken: ").Append(broken.ToString("N0")).Append("\n\n");
        foreach (string line in story.credits) sb.Append(line).Append('\n');
        sb.Append('\n').Append(story.newShiftTeaser);
        if (endingTitleText) endingTitleText.text = story.endingTitle;
        if (endingBodyText) endingBodyText.text = sb.ToString();
        if (ui) ui.ShowOverlay(endingPanel);
        else endingPanel.SetActive(true);
    }

    public void CloseEnding()
    {
        if (ui) ui.HideOverlay(endingPanel);
        else if (endingPanel) endingPanel.SetActive(false);
    }
}
