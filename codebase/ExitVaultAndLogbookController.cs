using System.Collections;
using UnityEngine;
using TMPro;

public class ExitVaultAndLogbookController : MonoBehaviour
{
    [Header("3 Escape Requirements (GDD Section 9: Power + Cable + 3 Crew Logbook Codes)")]
    public bool pumpRoomGeneratorPowered = false;
    public bool oreVeinWinchCableInstalled = false;

    [Header("Gate Code Clues (Foreman Quill = 14, Surveyor Pike = 03, Cook Bellamy = 96)")]
    public int correctCodeQuill = 14;
    public int correctCodePike = 3;
    public int correctCodeBellamy = 96;

    [Header("Current Player Dial Inputs")]
    public int inputCodeA = 14;
    public int inputCodeB = 2;
    public int inputCodeC = 0;

    [Header("3D Main Shaft Lift Gate Transforms")]
    public Transform combinationDial;
    public Transform valveWheel;
    public Transform vaultDoorHinge;
    public Light escapeWhiteLight;

    [Header("UI Overlays & Credits Screen")]
    public GameObject logbookPopupPanel;
    public TextMeshProUGUI logbookTitleText;
    public TextMeshProUGUI logbookBodyText;
    public GameObject vaultCodeCardPanel;
    public TextMeshProUGUI codeDisplayA, codeDisplayB, codeDisplayC;
    public GameObject endingCreditsPanel;
    public TextMeshProUGUI endingSummaryText;

    private bool isVaultOpen = false;
    private bool isAnimatingDoor = false;
    private float runStartTime;

    void Start()
    {
        runStartTime = Time.time;
    }

    // Call when player repairs the Generator in the South Pump Room with the Fuel Cell
    public void RepairPumpRoomGenerator()
    {
        pumpRoomGeneratorPowered = true;
        if (CompanyStoreEconomyManager.Instance)
            CompanyStoreEconomyManager.Instance.ShowTutorialCaption("DISPATCH: "Reserve generator online. Main Shaft lift has power."");
    }

    // Call when player collects the Winch Cable in Band 5 (Ore Veins)
    public void RecoverWinchCable()
    {
        oreVeinWinchCableInstalled = true;
        if (CompanyStoreEconomyManager.Instance)
            CompanyStoreEconomyManager.Instance.ShowTutorialCaption("DISPATCH: "Winch cable secured. Only the lift-gate code remains."");
    }

    // Crew Logbooks: Foreman Quill (Old Gallery), Surveyor Pike (Equipment Bay), Cook Bellamy (Pump Room)
    public void OpenCrewLogbook(string crewHeader, string logEntry)
    {
        if (!logbookPopupPanel) return;
        logbookTitleText.text = crewHeader;
        logbookBodyText.text = logEntry;
        logbookPopupPanel.SetActive(true);
    }

    public void CloseCrewLogbook()
    {
        if (logbookPopupPanel) logbookPopupPanel.SetActive(false);
    }

    public void OpenVaultCodeCard()
    {
        if (isVaultOpen || isAnimatingDoor) return;
        if (vaultCodeCardPanel) vaultCodeCardPanel.SetActive(true);
        RefreshCodeCardText();
    }

    public void CloseVaultCodeCard()
    {
        if (vaultCodeCardPanel) vaultCodeCardPanel.SetActive(false);
    }

    public void CycleCodeNumber(int slotIndex, int delta)
    {
        if (slotIndex == 0) inputCodeA = (inputCodeA + delta + 100) % 100;
        else if (slotIndex == 1) inputCodeB = (inputCodeB + delta + 100) % 100;
        else if (slotIndex == 2) inputCodeC = (inputCodeC + delta + 100) % 100;

        RefreshCodeCardText();
    }

    private void RefreshCodeCardText()
    {
        if (codeDisplayA) codeDisplayA.text = inputCodeA.ToString("00");
        if (codeDisplayB) codeDisplayB.text = inputCodeB.ToString("00");
        if (codeDisplayC) codeDisplayC.text = inputCodeC.ToString("00");
    }

    public void TryUnlockExitVault()
    {
        if (isVaultOpen || isAnimatingDoor) return;
        CloseVaultCodeCard();
        StartCoroutine(VaultUnlockSequenceRoutine());
    }

    private IEnumerator VaultUnlockSequenceRoutine()
    {
        isAnimatingDoor = true;

        bool codeMatches = (inputCodeA == correctCodeQuill && inputCodeB == correctCodePike && inputCodeC == correctCodeBellamy);
        bool allRequirementsMet = codeMatches && pumpRoomGeneratorPowered && oreVeinWinchCableInstalled;

        if (combinationDial)
        {
            float elapsed = 0f;
            while (elapsed < 0.7f)
            {
                elapsed += Time.deltaTime;
                float spin = Mathf.Sin(elapsed * 14f) * 360f;
                combinationDial.localRotation = Quaternion.Euler(0f, 0f, spin);
                yield return null;
            }
        }

        if (!allRequirementsMet)
        {
            if (CompanyStoreEconomyManager.Instance)
            {
                if (!pumpRoomGeneratorPowered)
                    CompanyStoreEconomyManager.Instance.ShowTutorialCaption("LIFT LOCKED: Repair the Pump Room generator first.");
                else if (!oreVeinWinchCableInstalled)
                    CompanyStoreEconomyManager.Instance.ShowTutorialCaption("LIFT LOCKED: Missing Winch Cable from the Ore Veins.");
                else
                    CompanyStoreEconomyManager.Instance.ShowTutorialCaption("GATE CODE INCORRECT: Check Quill, Pike, and Bellamy's logbooks.");
            }

            if (valveWheel)
            {
                Quaternion startRot = valveWheel.localRotation;
                for (int i = 0; i < 6; i++)
                {
                    valveWheel.localRotation = startRot * Quaternion.Euler(0f, 0f, (i % 2 == 0) ? 8f : -8f);
                    yield return new WaitForSeconds(0.04f);
                }
                valveWheel.localRotation = startRot;
            }
            isAnimatingDoor = false;
            yield break;
        }

        // All 3 Escape Conditions Met: Open Main Shaft Lift Gate, flood with daylight, play Dispatch line & Credits!
        isVaultOpen = true;
        if (valveWheel)
        {
            Quaternion startRot = valveWheel.localRotation;
            float elapsed = 0f;
            float wheelTime = 0.75f;
            while (elapsed < wheelTime)
            {
                elapsed += Time.deltaTime;
                float angle = Mathf.SmoothStep(0f, 540f, elapsed / wheelTime);
                valveWheel.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
        }

        if (escapeWhiteLight)
        {
            escapeWhiteLight.gameObject.SetActive(true);
            escapeWhiteLight.intensity = 0f;
        }

        Quaternion closedRot = vaultDoorHinge ? vaultDoorHinge.localRotation : Quaternion.identity;
        Quaternion openRot = closedRot * Quaternion.Euler(0f, -105f, 0f);

        float doorElapsed = 0f;
        float doorDuration = 1.4f;
        while (doorElapsed < doorDuration)
        {
            doorElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, doorElapsed / doorDuration);
            if (vaultDoorHinge) vaultDoorHinge.localRotation = Quaternion.Slerp(closedRot, openRot, t);
            if (escapeWhiteLight) escapeWhiteLight.intensity = Mathf.Lerp(0f, 18f, t);
            yield return null;
        }

        if (CompanyStoreEconomyManager.Instance)
        {
            CompanyStoreEconomyManager.Instance.ShowTutorialCaption("DISPATCH: "Shift 9 complete. Your final store balance will be deducted from your pension."");
        }

        if (endingCreditsPanel)
        {
            float totalMinutes = (Time.time - runStartTime) / 60f;
            int broken = VoxelWorldManager.Instance ? VoxelWorldManager.Instance.totalCubesBroken : 0;
            if (endingSummaryText)
            {
                endingSummaryText.text = $"ESCAPED GREYWATER DEEP\n\nTotal Time: {totalMinutes:F1} min\nTotal Cubes Broken: {broken:N0}\n\nComing Soon: NEW SHIFT (Procedural Mines)";
            }
            endingCreditsPanel.SetActive(true);
        }

        isAnimatingDoor = false;
    }
}
