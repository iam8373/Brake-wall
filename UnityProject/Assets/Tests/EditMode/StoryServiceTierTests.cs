using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Performance;
using BreakToEscape.Core.Services;
using BreakToEscape.Core.Story;
using BreakToEscape.Core.World;
using NUnit.Framework;

namespace BreakToEscape.Tests
{
    public class StoryAndEscapeTests
    {
        private static EscapeStateMachine NewEscape() => new EscapeStateMachine(TestData.Story.gateCode);

        [Test]
        public void StoryDataHasTheRequiredContent()
        {
            StoryData s = TestData.Story;
            Assert.AreEqual(new[] { 14, 3, 96 }, s.gateCode);
            Assert.AreEqual(3, s.logbooks.Count);
            foreach (string id in MineLayoutValidator.RequiredLogbooks) Assert.IsTrue(s.logbooks.ContainsKey(id), id);
            Assert.AreEqual(0, s.logbooks["quill"].codeSlot);
            Assert.AreEqual(1, s.logbooks["pike"].codeSlot);
            Assert.AreEqual(2, s.logbooks["bellamy"].codeSlot);
            Assert.GreaterOrEqual(s.notes.Count, 8);
            Assert.LessOrEqual(s.notes.Count, 12);
            Assert.GreaterOrEqual(s.dispatch.Count, 15);
            for (int step = 1; step <= 5; step++) Assert.IsTrue(s.dispatch.ContainsKey(TutorialSequence.StepCaptionKey(step)));
            Assert.IsNotEmpty(s.endingTitle);
            Assert.IsNotEmpty(s.newShiftTeaser);
        }

        [Test]
        public void EveryNoteAndLogbookPlacedInTheMineExistsInTheStory()
        {
            TestData.NewHandBuiltMine(out MineLayout layout);
            StoryData s = TestData.Story;
            foreach (InteractableSpot spot in layout.interactables)
            {
                if (spot.type == InteractableType.WallNote) Assert.IsTrue(s.notes.ContainsKey(spot.payload), spot.payload);
                if (spot.type == InteractableType.Logbook) Assert.IsTrue(s.logbooks.ContainsKey(spot.payload), spot.payload);
                if (spot.type == InteractableType.HardHat) Assert.IsTrue(s.hardHats.ContainsKey(spot.payload), spot.payload);
            }
        }

        [Test]
        public void AllThreeConditionsAreRequired()
        {
            EscapeStateMachine e = NewEscape();
            e.SetDial(0, 14); e.SetDial(1, 3); e.SetDial(2, 96);
            Assert.AreEqual(GateResult.NeedPower, e.TryOpenGate());
            Assert.AreEqual(GeneratorResult.NeedFuelCell, e.TryRepairGenerator());
            e.FindFuelCell();
            Assert.AreEqual(GeneratorResult.Repaired, e.TryRepairGenerator());
            Assert.AreEqual(GateResult.NeedCable, e.TryOpenGate());
            e.RecoverCable();
            Assert.AreEqual(GateResult.Opened, e.TryOpenGate());
            Assert.AreEqual(GateResult.AlreadyOpen, e.TryOpenGate());
            e.MarkEscaped();
            Assert.IsTrue(e.Escaped);
        }

        [Test]
        public void WrongCodeIsRejected_DialsWrap()
        {
            EscapeStateMachine e = NewEscape();
            e.FindFuelCell(); e.TryRepairGenerator(); e.RecoverCable();
            e.SetDial(0, 14); e.SetDial(1, 3); e.SetDial(2, 95);
            Assert.AreEqual(GateResult.WrongCode, e.TryOpenGate());
            e.CycleDial(2, 1);
            Assert.AreEqual(96, e.Dial(2));
            e.CycleDial(1, -4);
            Assert.AreEqual(99, e.Dial(1));
            Assert.AreEqual(GateResult.WrongCode, e.TryOpenGate());
            e.CycleDial(1, 4);
            Assert.AreEqual(GateResult.Opened, e.TryOpenGate());
        }

        [Test]
        public void StateRestoresAndCannotSkipRequirements()
        {
            EscapeStateMachine e = NewEscape();
            e.Restore(true, true, false, new[] { "quill", "pike" }, new[] { 14, 3, 1 }, gateOpened: true, escaped: true);
            Assert.IsFalse(e.GateOpened, "a save claiming an open gate without the cable is not trusted");
            Assert.IsFalse(e.Escaped);
            Assert.AreEqual(2, e.LogbookCount);
            Assert.AreEqual(1, e.Dial(2));
        }

        [Test]
        public void TutorialAdvancesOnlyOnRealActionsAndRecordsTimes()
        {
            var t = new TutorialSequence();
            var shown = new List<int>();
            t.StepShown += shown.Add;
            t.OnCubeBroken();
            Assert.AreEqual(0, t.Step, "nothing before the tutorial begins");
            t.Begin();
            t.OnCubeVacuumed();
            Assert.AreEqual(1, t.Step, "vacuuming before punching does not skip ahead");
            t.OnCubeBroken();
            t.OnCubeVacuumed();
            t.OnCashEarned(40f);
            t.OnUpgradeBought(70f);
            Assert.AreEqual(new List<int> { 1, 2, 3, 4, 5 }, shown);
            Assert.AreEqual(40f, t.FirstCashTime);
            Assert.AreEqual(70f, t.FirstUpgradeTime);
        }
    }

    public class MonetizationTests
    {
        private static MonetizationRules NewRules() => new MonetizationRules(TestData.Economy.monetization);

        [Test]
        public void ReleaseBuildWithoutSdkNeverGrants()
        {
            MonetizationRules m = NewRules();
            Assert.AreEqual(RewardDecision.Unavailable, m.DecideReward(MonetizationRules.PlacementDoubleCash, 10000f, adReady: false, allowTestFallback: false));
            Assert.AreEqual(RewardDecision.Unavailable, m.DecideReward(MonetizationRules.PlacementFreeDynamite, 10000f, adReady: false, allowTestFallback: false));
            Assert.AreEqual(PurchaseDecision.Unavailable, m.DecideRemoveAdsPurchase(purchaseServiceReady: false, allowTestFallback: false));
            Assert.IsFalse(m.AdsRemoved);
        }

        [Test]
        public void DevelopmentBuildMayUseTheTestFallback()
        {
            MonetizationRules m = NewRules();
            Assert.AreEqual(RewardDecision.GrantTestFallback, m.DecideReward(MonetizationRules.PlacementDoubleCash, 10000f, false, true));
            Assert.AreEqual(PurchaseDecision.GrantTestFallback, m.DecideRemoveAdsPurchase(false, true));
            Assert.AreEqual(RewardDecision.ShowAd, m.DecideReward(MonetizationRules.PlacementDoubleCash, 10000f, true, true), "a ready ad is always preferred");
        }

        [Test]
        public void NoOffersDuringTheFirstFiveMinutes()
        {
            MonetizationRules m = NewRules();
            Assert.AreEqual(300f, m.Settings.adFreeGracePeriodSeconds);
            Assert.AreEqual(RewardDecision.Unavailable, m.DecideReward(MonetizationRules.PlacementDoubleCash, 299f, true, true));
            Assert.AreEqual(RewardDecision.ShowAd, m.DecideReward(MonetizationRules.PlacementDoubleCash, 300f, true, true));
        }

        [Test]
        public void DoubleCashLastsFiveMinutesWithTenMinuteCooldown()
        {
            MonetizationRules m = NewRules();
            m.GrantDoubleCash();
            Assert.IsTrue(m.DoubleCashActive);
            Assert.AreEqual(50, m.BonusFor(50));
            m.Tick(299f);
            Assert.IsTrue(m.DoubleCashActive);
            m.Tick(1f);
            Assert.IsFalse(m.DoubleCashActive);
            Assert.AreEqual(0, m.BonusFor(50));
            Assert.IsFalse(m.CanOfferDoubleCash(10000f), "still in the 10-minute cooldown");
            m.Tick(299f);
            Assert.IsFalse(m.CanOfferDoubleCash(10000f));
            m.Tick(1f);
            Assert.IsTrue(m.CanOfferDoubleCash(10000f));
        }

        [Test]
        public void FreeDynamiteHasASessionCap_RemoveAdsIsGentler()
        {
            MonetizationRules m = NewRules();
            int cap = m.Settings.freeDynamitePerSession;
            for (int i = 0; i < cap; i++)
            {
                Assert.IsTrue(m.CanOfferFreeDynamite(10000f));
                Assert.AreEqual(2, m.GrantFreeDynamite());
            }
            Assert.IsFalse(m.CanOfferFreeDynamite(10000f));

            MonetizationRules paid = NewRules();
            paid.SetAdsRemoved(true);
            Assert.AreEqual(RewardDecision.GrantWithoutAd, paid.DecideReward(MonetizationRules.PlacementDoubleCash, 0f, false, false));
            paid.GrantDoubleCash();
            Assert.AreEqual(paid.Settings.removeAdsDoubleCashCooldownSeconds, paid.DoubleCashCooldownRemaining);
            Assert.Greater(paid.Settings.removeAdsDoubleCashCooldownSeconds, paid.Settings.doubleCashCooldownSeconds);
            Assert.AreEqual(PurchaseDecision.AlreadyOwned, paid.DecideRemoveAdsPurchase(true, true));
        }

        [Test]
        public void RulesComeFromData()
        {
            MonetizationSettings s = TestData.Economy.monetization;
            Assert.AreEqual(300f, s.doubleCashDurationSeconds);
            Assert.AreEqual(600f, s.doubleCashCooldownSeconds);
        }
    }

    public class DeviceTierTests
    {
        [Test]
        public void ClassifiesTypicalPhones()
        {
            TierDetectionSettings s = TestData.Economy.tierDetection;
            Assert.AreEqual(QualityTier.Low, DeviceTierPolicy.Classify(3800, 8, 2048, s));
            Assert.AreEqual(QualityTier.Low, DeviceTierPolicy.Classify(6000, 4, 2048, s));
            Assert.AreEqual(QualityTier.Mid, DeviceTierPolicy.Classify(6000, 8, 2048, s));
            Assert.AreEqual(QualityTier.High, DeviceTierPolicy.Classify(7400, 8, 0, s), "an 8 GB phone that reports 7.4 GB is High; unknown GPU memory is ignored");
            Assert.AreEqual(QualityTier.Mid, DeviceTierPolicy.Classify(7400, 6, 4096, s));
        }

        [Test]
        public void TierCapsMatchTheTechnicalPlan()
        {
            EconomyData e = TestData.Economy;
            Assert.AreEqual(100, e.Tier("low").rubbleCap);
            Assert.That(e.Tier("mid").rubbleCap, Is.InRange(150, 250));
            Assert.AreEqual(400, e.Tier("high").rubbleCap);
            Assert.AreEqual(0.7f, e.Tier("low").renderScale, 1e-6);
            Assert.That(e.Tier("mid").renderScale, Is.InRange(0.85f, 1.0f));
            Assert.AreEqual(1f, e.Tier("high").renderScale, 1e-6);
            Assert.AreEqual(30, e.Tier("low").targetFps);
        }

        [Test]
        public void ThermalGuardLowersRubbleCapBeforeFrameRate()
        {
            ThermalGuardSettings s = TestData.Economy.thermalGuard;
            Assert.AreEqual(GuardAction.None, DeviceTierPolicy.Evaluate(58f, 60, 200, s, out int cap));
            Assert.AreEqual(200, cap);
            Assert.AreEqual(GuardAction.LowerRubbleCap, DeviceTierPolicy.Evaluate(40f, 60, 200, s, out cap));
            Assert.AreEqual(150, cap);
            Assert.AreEqual(GuardAction.LowerRubbleCap, DeviceTierPolicy.Evaluate(40f, 60, 100, s, out cap));
            Assert.AreEqual(80, cap);
            Assert.AreEqual(GuardAction.DropFrameTarget, DeviceTierPolicy.Evaluate(40f, 60, 80, s, out cap));
            Assert.AreEqual(GuardAction.None, DeviceTierPolicy.Evaluate(20f, 30, 80, s, out cap), "nothing left to lower");
        }
    }
}
