using System;
using BreakToEscape.Core.Economy;

namespace BreakToEscape.Core.Performance
{
    public enum QualityTier
    {
        Low = 0,
        Mid = 1,
        High = 2
    }

    public enum GuardAction
    {
        None,
        LowerRubbleCap,
        DropFrameTarget
    }

    /// <summary>
    /// Device tier classification and the thermal / frame-rate guard, independent of Unity.
    /// Thresholds come from economy.json ("tiers.detection") and are starting values to be
    /// tuned from measured phones.
    /// </summary>
    public static class DeviceTierPolicy
    {
        public static string Key(QualityTier tier) => tier == QualityTier.Low ? "low" : (tier == QualityTier.Mid ? "mid" : "high");

        /// <summary>
        /// Low if RAM, CPU cores or graphics memory are at or below the low limits; High if RAM and
        /// cores reach the high limits; otherwise Mid. Graphics memory is ignored when the device
        /// reports 0 (some Android drivers do).
        /// </summary>
        public static QualityTier Classify(int ramMb, int cores, int graphicsMb, TierDetectionSettings s)
        {
            bool lowGpu = graphicsMb > 0 && graphicsMb < s.lowMaxGraphicsMb;
            if (ramMb < s.lowMaxRamMb || cores <= s.lowMaxCores || lowGpu) return QualityTier.Low;
            if (ramMb >= s.highMinRamMb && cores >= s.highMinCores) return QualityTier.High;
            return QualityTier.Mid;
        }

        /// <summary>
        /// Decides how to react to a measured average frame rate. The rubble cap is lowered first
        /// (it is the main cost); only when it is already at the minimum is the frame target dropped
        /// from 60 to 30.
        /// </summary>
        public static GuardAction Evaluate(float averageFps, int targetFps, int currentCap, ThermalGuardSettings s, out int newCap)
        {
            newCap = currentCap;
            float threshold = targetFps >= 60 ? s.minAverageFpsAt60 : s.minAverageFpsAt30;
            if (averageFps >= threshold) return GuardAction.None;
            if (currentCap > s.minimumRubbleCap)
            {
                newCap = Math.Max(s.minimumRubbleCap, (int)Math.Floor(currentCap * s.rubbleCapStepDown));
                return GuardAction.LowerRubbleCap;
            }
            return targetFps > 30 ? GuardAction.DropFrameTarget : GuardAction.None;
        }
    }
}
