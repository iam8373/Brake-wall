using System;
using System.Collections.Generic;

namespace BreakToEscape.Core.Rubble
{
    public enum RubbleState : byte
    {
        InPool = 0,
        Active = 1,
        BeingProcessed = 2
    }

    /// <summary>
    /// Bookkeeping for the pooled rubble cubes, independent of Unity. The scene object for a
    /// slot is owned by RubbleSuctionManager; this class decides what may happen to it.
    /// Rules:
    ///  - Each slot is in exactly one state; the total never changes.
    ///  - Returning a slot to the pool is idempotent (a second return is ignored), so a cube can
    ///    never sit in the pool twice or be paid twice.
    ///  - When the cap is reached, only a settled, Active cube (not being vacuumed or crushed)
    ///    may be swept; the sweep pays the refund ratio exactly once.
    ///  - If nothing can be swept, the new cube is not spawned and its refund is paid at once,
    ///    so the broken cube's cash is never lost.
    /// </summary>
    public sealed class RubblePoolBook
    {
        private readonly RubbleState[] state;
        private readonly int[] value;
        private readonly byte[] material;
        private readonly bool[] settled;
        private readonly long[] spawnOrder;
        private readonly Stack<int> free;
        private long spawnCounter;
        private int activeCount;
        private int processingCount;

        public float RefundRatio { get; }
        public int Capacity => state.Length;
        public int Cap { get; private set; }
        public int ActiveCount => activeCount;
        public int ProcessingCount => processingCount;
        public int InPoolCount => free.Count;
        /// <summary>Cubes in the world (active or being processed).</summary>
        public int LiveCount => activeCount + processingCount;
        public long TotalRefundsPaid { get; private set; }

        public RubblePoolBook(int capacity, int cap, float refundRatio)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            state = new RubbleState[capacity];
            value = new int[capacity];
            material = new byte[capacity];
            settled = new bool[capacity];
            spawnOrder = new long[capacity];
            free = new Stack<int>(capacity);
            for (int i = capacity - 1; i >= 0; i--) free.Push(i);
            RefundRatio = refundRatio;
            SetCap(cap);
        }

        public void SetCap(int cap) => Cap = Math.Max(1, Math.Min(Capacity, cap));

        public RubbleState State(int slot) => state[slot];
        public int Value(int slot) => value[slot];
        public byte Material(int slot) => material[slot];
        public bool IsSettled(int slot) => settled[slot];

        public int RefundFor(int cashValue) => Math.Max(1, (int)Math.Round(cashValue * RefundRatio, MidpointRounding.AwayFromZero));

        /// <summary>
        /// Requests a new cube. Returns the slot to activate, or −1 if the cube could not be spawned.
        /// <paramref name="sweptSlot"/> is a slot that was swept to make room (−1 if none); the caller
        /// must hide its object. <paramref name="refundPaid"/> is the cash to award now.
        /// </summary>
        public int TrySpawn(int cashValue, byte materialId, out int sweptSlot, out long refundPaid)
        {
            sweptSlot = -1;
            refundPaid = 0;
            if (LiveCount >= Cap || free.Count == 0)
            {
                int oldest = FindOldestSweepable();
                if (oldest < 0)
                {
                    refundPaid = RefundFor(cashValue);
                    TotalRefundsPaid += refundPaid;
                    return -1;
                }
                refundPaid = RefundFor(value[oldest]);
                TotalRefundsPaid += refundPaid;
                ReturnInternal(oldest);
                sweptSlot = oldest;
            }

            int slot = free.Pop();
            state[slot] = RubbleState.Active;
            value[slot] = cashValue;
            material[slot] = materialId;
            settled[slot] = false;
            spawnOrder[slot] = ++spawnCounter;
            activeCount++;
            return slot;
        }

        /// <summary>True if a cube could be spawned now (a free slot under the cap, or a settled cube to sweep).</summary>
        public bool CanSpawn => free.Count > 0 && LiveCount < Cap || FindOldestSweepable() >= 0;

        /// <summary>Sweeps settled cubes until the live count is within the cap (used when the cap drops).</summary>
        public long SweepExcess(List<int> sweptSlots)
        {
            long paid = 0;
            while (LiveCount > Cap)
            {
                int oldest = FindOldestSweepable();
                if (oldest < 0) break;
                long refund = RefundFor(value[oldest]);
                paid += refund;
                TotalRefundsPaid += refund;
                ReturnInternal(oldest);
                sweptSlots.Add(oldest);
            }
            return paid;
        }

        private int FindOldestSweepable()
        {
            int best = -1;
            long bestOrder = long.MaxValue;
            for (int i = 0; i < state.Length; i++)
            {
                if (state[i] != RubbleState.Active || !settled[i]) continue;
                if (spawnOrder[i] < bestOrder) { bestOrder = spawnOrder[i]; best = i; }
            }
            return best;
        }

        public void SetSettled(int slot, bool isSettled)
        {
            if (state[slot] == RubbleState.Active) settled[slot] = isSettled;
        }

        /// <summary>Marks an active cube as being vacuumed or crushed. Fails for pooled or already-processing cubes.</summary>
        public bool BeginProcessing(int slot)
        {
            if (slot < 0 || slot >= state.Length || state[slot] != RubbleState.Active) return false;
            state[slot] = RubbleState.BeingProcessed;
            settled[slot] = false;
            activeCount--;
            processingCount++;
            return true;
        }

        /// <summary>Returns a processing cube to the world (for example the tank filled while it was in flight).</summary>
        public bool CancelProcessing(int slot)
        {
            if (slot < 0 || slot >= state.Length || state[slot] != RubbleState.BeingProcessed) return false;
            state[slot] = RubbleState.Active;
            processingCount--;
            activeCount++;
            return true;
        }

        /// <summary>Returns a cube to the pool. Idempotent: returns false if it was already pooled.</summary>
        public bool Return(int slot)
        {
            if (slot < 0 || slot >= state.Length || state[slot] == RubbleState.InPool) return false;
            ReturnInternal(slot);
            return true;
        }

        private void ReturnInternal(int slot)
        {
            if (state[slot] == RubbleState.Active) activeCount--;
            else if (state[slot] == RubbleState.BeingProcessed) processingCount--;
            state[slot] = RubbleState.InPool;
            settled[slot] = false;
            value[slot] = 0;
            free.Push(slot);
        }

        /// <summary>Invariant check used by tests: every slot is counted exactly once.</summary>
        public bool CheckInvariants()
        {
            int pooled = 0, active = 0, processing = 0;
            var seen = new HashSet<int>();
            foreach (int s in free)
            {
                if (!seen.Add(s)) return false;
                if (state[s] != RubbleState.InPool) return false;
            }
            for (int i = 0; i < state.Length; i++)
            {
                if (state[i] == RubbleState.InPool) pooled++;
                else if (state[i] == RubbleState.Active) active++;
                else processing++;
            }
            return pooled == free.Count && active == activeCount && processing == processingCount && pooled + active + processing == Capacity;
        }
    }

    /// <summary>One cube held in the vacuum tank.</summary>
    public struct TankItem
    {
        public byte material;
        public int cashValue;
    }

    /// <summary>
    /// The vacuum tank. Cubes on their way to the nozzle hold a reservation, so in-flight cubes
    /// count against capacity and a cube is never pulled in when there is no room for it.
    /// </summary>
    public sealed class TankInventory
    {
        private readonly List<TankItem> items = new List<TankItem>(256);

        public int Capacity { get; private set; }
        public int Count => items.Count;
        public int Reserved { get; private set; }
        public bool IsFull => items.Count + Reserved >= Capacity;
        public long TotalValue { get; private set; }

        public TankInventory(int capacity) { Capacity = Math.Max(1, capacity); }

        /// <summary>Capacity follows the Tank Capacity upgrade; it never drops below the current contents.</summary>
        public void SetCapacity(int capacity) => Capacity = Math.Max(Math.Max(1, capacity), items.Count + Reserved);

        public bool TryReserve()
        {
            if (IsFull) return false;
            Reserved++;
            return true;
        }

        public void CancelReservation()
        {
            if (Reserved > 0) Reserved--;
        }

        /// <summary>Completes a reservation by storing the cube.</summary>
        public bool CompleteReservation(byte material, int cashValue)
        {
            if (Reserved <= 0) return false;
            Reserved--;
            items.Add(new TankItem { material = material, cashValue = cashValue });
            TotalValue += cashValue;
            return true;
        }

        public bool TryPop(out TankItem item)
        {
            if (items.Count == 0) { item = default; return false; }
            item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            TotalValue -= item.cashValue;
            return true;
        }

        public void Clear()
        {
            items.Clear();
            Reserved = 0;
            TotalValue = 0;
        }

        /// <summary>Contents as runs of identical (material, value) for compact saving, in stack order.</summary>
        public void GetRuns(List<int> materials, List<int> values, List<int> counts)
        {
            for (int i = 0; i < items.Count; i++)
            {
                TankItem it = items[i];
                int last = counts.Count - 1;
                if (last >= 0 && materials[last] == it.material && values[last] == it.cashValue) counts[last]++;
                else { materials.Add(it.material); values.Add(it.cashValue); counts.Add(1); }
            }
        }

        public void RestoreRuns(IList<int> materials, IList<int> values, IList<int> counts)
        {
            Clear();
            int n = Math.Min(materials.Count, Math.Min(values.Count, counts.Count));
            for (int r = 0; r < n; r++)
            {
                int c = Math.Max(0, Math.Min(counts[r], 100000));
                for (int k = 0; k < c; k++)
                {
                    var it = new TankItem { material = (byte)materials[r], cashValue = Math.Max(0, values[r]) };
                    items.Add(it);
                    TotalValue += it.cashValue;
                }
            }
            if (items.Count > Capacity) Capacity = items.Count;
        }
    }
}
