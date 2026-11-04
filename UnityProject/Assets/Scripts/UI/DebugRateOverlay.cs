using TMPro;
using UnityEngine;

/// <summary>
/// Tuning overlay: measured punches, cubes vacuumed and cubes crushed per second, next to the
/// table values they should not exceed, plus frame rate, tier and live rubble. Toggle it in Settings.
/// </summary>
public class DebugRateOverlay : MonoBehaviour
{
    public TextMeshProUGUI text;
    public CompanyStoreEconomyManager economyManager;
    public RubbleSuctionManager suction;
    public Crusher07Controller crusher;
    public DeviceTierScaler tierScaler;
    public float refreshSeconds = 0.5f;

    private float timer;
    private int frames;
    private float frameTime;

    void Update()
    {
        frames++;
        frameTime += Time.unscaledDeltaTime;
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f || !text || !economyManager || economyManager.State == null) return;
        timer = refreshSeconds;
        float fps = frameTime > 0f ? frames / frameTime : 0f;
        frames = 0;
        frameTime = 0f;
        var s = economyManager.State;
        text.text =
            "PUNCH " + economyManager.PunchRate.LastRate.ToString("0.00") + " / " + s.PunchesPerSecond.ToString("0.00") + " per s\n" +
            "VACUUM " + (suction ? suction.VacuumRate.LastRate.ToString("0.0") : "-") + " / " + s.VacuumCubesPerSecond.ToString("0.0") + " cubes/s\n" +
            "CRUSH " + (crusher ? crusher.CrushRate.LastRate.ToString("0.0") : "-") + " / " + s.CrusherCubesPerSecond.ToString("0.0") + " cubes/s\n" +
            "RUBBLE " + (suction && suction.Book != null ? suction.Book.LiveCount + " / " + suction.Book.Cap : "-") + "\n" +
            "FPS " + fps.ToString("0") + "  TIER " + (tierScaler ? tierScaler.CurrentTier.ToString() : "-");
    }
}
