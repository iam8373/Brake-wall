using System;

namespace BreakToEscape.Core.Limits
{
    /// <summary>
    /// Fixed-rate action limiter with a short input buffer. Enforces "N actions per second"
    /// from the economy tables (for example Punch Speed). A request that arrives slightly early
    /// (within the buffer window) is remembered and fires as soon as the cooldown ends, so taps
    /// feel responsive without exceeding the rate.
    /// </summary>
    public sealed class CooldownLimiter
    {
        private float interval;
        private float nextReady;
        private bool buffered;
        private float bufferedAt;

        public float BufferWindow { get; set; }
        public float RatePerSecond { get; private set; }

        public CooldownLimiter(float ratePerSecond, float bufferWindowSeconds)
        {
            SetRate(ratePerSecond);
            BufferWindow = Math.Max(0f, bufferWindowSeconds);
            nextReady = float.NegativeInfinity;
        }

        public void SetRate(float ratePerSecond)
        {
            RatePerSecond = Math.Max(0.01f, ratePerSecond);
            interval = 1f / RatePerSecond;
        }

        public float Interval => interval;
        public bool HasBufferedRequest => buffered;

        public bool IsReady(float now) => now >= nextReady;

        /// <summary>Requests an action at time <paramref name="now"/>. Returns true if it fires now.</summary>
        public bool Request(float now)
        {
            if (now >= nextReady)
            {
                Consume(now);
                return true;
            }
            if (nextReady - now <= BufferWindow)
            {
                buffered = true;
                bufferedAt = now;
            }
            return false;
        }

        /// <summary>Call every frame. Returns true if a buffered request fires now.</summary>
        public bool Poll(float now)
        {
            if (!buffered) return false;
            if (now - bufferedAt > BufferWindow + interval)
            {
                buffered = false;
                return false;
            }
            if (now < nextReady) return false;
            buffered = false;
            Consume(now);
            return true;
        }

        private void Consume(float now)
        {
            // Keep a steady cadence when requests arrive back to back; restart after idle time.
            float start = (now - nextReady) < interval ? nextReady : now;
            if (float.IsNegativeInfinity(start)) start = now;
            nextReady = start + interval;
        }

        public void Reset()
        {
            nextReady = float.NegativeInfinity;
            buffered = false;
        }
    }

    /// <summary>
    /// Token bucket: allows at most <see cref="RatePerSecond"/> actions per second on average,
    /// with a small burst. Used to cap vacuum suction to the Vacuum Suction table value.
    /// </summary>
    public sealed class TokenBucket
    {
        private float tokens;

        public float RatePerSecond { get; private set; }
        public float Burst { get; private set; }

        public TokenBucket(float ratePerSecond, float burst)
        {
            Configure(ratePerSecond, burst);
            tokens = Burst;
        }

        public void Configure(float ratePerSecond, float burst)
        {
            RatePerSecond = Math.Max(0f, ratePerSecond);
            Burst = Math.Max(1f, burst);
            if (tokens > Burst) tokens = Burst;
        }

        public float Tokens => tokens;

        public void Refill(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            tokens = Math.Min(Burst, tokens + RatePerSecond * deltaSeconds);
        }

        public bool TryTake()
        {
            if (tokens < 1f) return false;
            tokens -= 1f;
            return true;
        }
    }

    /// <summary>Counts events and reports the rate over the last completed one-second window (debug overlay).</summary>
    public sealed class RateMeter
    {
        private float windowStart = float.NaN;
        private int count;

        public float LastRate { get; private set; }

        public void Record(float now, int events = 1)
        {
            Tick(now);
            count += events;
        }

        public void Tick(float now)
        {
            if (float.IsNaN(windowStart)) windowStart = now;
            float elapsed = now - windowStart;
            if (elapsed >= 1f)
            {
                LastRate = count / elapsed;
                count = 0;
                windowStart = now;
            }
        }
    }
}
