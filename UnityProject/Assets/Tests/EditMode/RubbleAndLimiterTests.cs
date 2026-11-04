using System;
using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.Limits;
using BreakToEscape.Core.Rubble;
using NUnit.Framework;

namespace BreakToEscape.Tests
{
    public class RubblePoolTests
    {
        [Test]
        public void ReturnIsIdempotentAndNeverDuplicatesASlot()
        {
            var book = new RubblePoolBook(10, 10, 0.6f);
            int slot = book.TrySpawn(5, 1, out _, out _);
            Assert.IsTrue(book.Return(slot));
            Assert.IsFalse(book.Return(slot), "second return ignored");
            Assert.AreEqual(10, book.InPoolCount);
            Assert.IsTrue(book.CheckInvariants());
        }

        [Test]
        public void SweepOnlyTakesSettledActiveCubes_AndPaysOnce()
        {
            var book = new RubblePoolBook(3, 3, 0.6f);
            int a = book.TrySpawn(10, 1, out _, out _);
            int b = book.TrySpawn(10, 1, out _, out _);
            int c = book.TrySpawn(10, 1, out _, out _);
            Assert.IsTrue(book.BeginProcessing(a), "a is being vacuumed");
            book.SetSettled(b, true);

            int d = book.TrySpawn(20, 1, out int swept, out long refund);
            Assert.AreEqual(b, swept, "only the settled, unprocessed cube is swept");
            Assert.AreEqual(6, refund);
            Assert.AreEqual(b, d, "the freed slot is reused");

            // Nothing settled and nothing free: the new cube is refused but its cash is paid now.
            int e = book.TrySpawn(20, 1, out int swept2, out long refund2);
            Assert.AreEqual(-1, e);
            Assert.AreEqual(-1, swept2);
            Assert.AreEqual(12, refund2);

            // The vacuum routine finishing after a sweep attempt can't return a cube twice.
            Assert.IsTrue(book.Return(a));
            Assert.IsFalse(book.Return(a));
            Assert.IsTrue(book.CheckInvariants());
            Assert.AreEqual(6 + 12, book.TotalRefundsPaid);
            Assert.AreEqual(2, book.LiveCount);
            Assert.That(c, Is.Not.EqualTo(-1));
        }

        [Test]
        public void ProcessingCubesCannotBeSweptOrProcessedTwice()
        {
            var book = new RubblePoolBook(2, 1, 0.6f);
            int a = book.TrySpawn(10, 1, out _, out _);
            book.SetSettled(a, true);
            Assert.IsTrue(book.BeginProcessing(a));
            Assert.IsFalse(book.BeginProcessing(a));
            int b = book.TrySpawn(10, 1, out int swept, out long refund);
            Assert.AreEqual(-1, b, "cap 1 reached by a processing cube; it must not be swept");
            Assert.AreEqual(-1, swept);
            Assert.AreEqual(6, refund);
            Assert.IsTrue(book.CancelProcessing(a));
            Assert.AreEqual(RubbleState.Active, book.State(a));
        }

        [Test]
        public void RandomStress_TotalsAndCashStayConsistent()
        {
            var rng = new Random(1234);
            var book = new RubblePoolBook(64, 40, 0.6f);
            var tank = new TankInventory(30);
            var live = new List<int>();
            var inFlight = new List<int>();
            long cashSpawnedValue = 0, cashCrushed = 0, cashRefunded = 0;

            for (int step = 0; step < 10000; step++)
            {
                int op = rng.Next(6);
                if (op <= 1)
                {
                    int value = rng.Next(1, 50);
                    cashSpawnedValue += value;
                    int slot = book.TrySpawn(value, 1, out int swept, out long refund);
                    cashRefunded += refund;
                    if (swept >= 0) live.Remove(swept);
                    if (slot >= 0) live.Add(slot);
                }
                else if (op == 2 && live.Count > 0)
                {
                    int slot = live[rng.Next(live.Count)];
                    book.SetSettled(slot, rng.Next(2) == 0);
                }
                else if (op == 3 && live.Count > 0 && tank.TryReserve())
                {
                    int slot = live[rng.Next(live.Count)];
                    if (book.BeginProcessing(slot)) { live.Remove(slot); inFlight.Add(slot); }
                    else tank.CancelReservation();
                }
                else if (op == 4 && inFlight.Count > 0)
                {
                    int slot = inFlight[0];
                    inFlight.RemoveAt(0);
                    Assert.IsTrue(tank.CompleteReservation(book.Material(slot), book.Value(slot)));
                    Assert.IsTrue(book.Return(slot));
                    Assert.IsFalse(book.Return(slot));
                }
                else if (op == 5 && tank.TryPop(out TankItem item))
                {
                    cashCrushed += item.cashValue;
                }
                Assert.LessOrEqual(book.LiveCount, book.Cap);
                Assert.LessOrEqual(tank.Count + tank.Reserved, tank.Capacity);
            }
            Assert.IsTrue(book.CheckInvariants());
            Assert.AreEqual(book.TotalRefundsPaid, cashRefunded);
            Assert.AreEqual(64, book.InPoolCount + book.LiveCount);
            Assert.AreEqual(live.Count + inFlight.Count, book.LiveCount);
            Assert.Greater(cashCrushed, 0);
            Assert.Greater(cashSpawnedValue, cashCrushed);
        }

        [Test]
        public void TankCountsInFlightCubesAgainstCapacity()
        {
            var tank = new TankInventory(2);
            Assert.IsTrue(tank.TryReserve());
            Assert.IsTrue(tank.TryReserve());
            Assert.IsFalse(tank.TryReserve(), "two cubes already in flight fill a 2-cube tank");
            Assert.IsTrue(tank.IsFull);
            tank.CompleteReservation(1, 5);
            tank.CancelReservation();
            Assert.AreEqual(1, tank.Count);
            Assert.AreEqual(0, tank.Reserved);
            Assert.IsTrue(tank.TryReserve());
        }

        [Test]
        public void TankContentsRoundTripAsRuns()
        {
            var tank = new TankInventory(10);
            foreach (var (m, v) in new[] { (1, 2), (1, 2), (2, 4), (2, 4), (2, 4), (7, 45) })
            {
                tank.TryReserve();
                tank.CompleteReservation((byte)m, v);
            }
            var mats = new List<int>(); var vals = new List<int>(); var counts = new List<int>();
            tank.GetRuns(mats, vals, counts);
            Assert.AreEqual(new List<int> { 2, 3, 1 }, counts);
            var restored = new TankInventory(10);
            restored.RestoreRuns(mats, vals, counts);
            Assert.AreEqual(6, restored.Count);
            Assert.AreEqual(2 + 2 + 12 + 45, restored.TotalValue);
            Assert.IsTrue(restored.TryPop(out TankItem top));
            Assert.AreEqual(7, top.material);
        }
    }

    public class LimiterTests
    {
        [Test]
        public void CooldownLimiterEnforcesPunchesPerSecond()
        {
            var limiter = new CooldownLimiter(1.5f, 0.12f);
            int fired = 0;
            float dt = 1f / 60f;
            for (int frame = 0; frame < 600; frame++) // 10 seconds of a player mashing every frame
            {
                float now = frame * dt;
                if (limiter.Request(now)) fired++;
                if (limiter.Poll(now)) fired++;
            }
            Assert.AreEqual(15, fired, 1, "1.5 punches per second for 10 seconds");
        }

        [Test]
        public void EarlyTapIsBufferedAndFiresWhenReady()
        {
            var limiter = new CooldownLimiter(2f, 0.12f);
            Assert.IsTrue(limiter.Request(0f));
            Assert.IsFalse(limiter.Request(0.42f), "0.08 s early");
            Assert.IsTrue(limiter.HasBufferedRequest);
            Assert.IsFalse(limiter.Poll(0.45f));
            Assert.IsTrue(limiter.Poll(0.5f));
            Assert.IsFalse(limiter.Request(0.1f + 0.5f), "too early to buffer");
            Assert.IsFalse(limiter.HasBufferedRequest);
        }

        [Test]
        public void TokenBucketCapsVacuumRate()
        {
            var bucket = new TokenBucket(6f, 2f);
            int pulled = 0;
            for (int frame = 0; frame < 600; frame++)
            {
                bucket.Refill(1f / 60f);
                while (bucket.TryTake()) pulled++;
            }
            Assert.AreEqual(62, pulled, 2, "6 cubes/s for 10 s plus the initial burst");
        }

        [Test]
        public void RateMeterReportsTheLastWindow()
        {
            var meter = new RateMeter();
            for (int i = 0; i <= 120; i++) meter.Record(i / 60f, i % 15 == 0 ? 1 : 0);
            Assert.AreEqual(4f, meter.LastRate, 0.5f);
        }
    }
}
