using System;
using System.Collections.Generic;
using System.Text;
using BreakToEscape.Core.Services;
using UnityEngine;

/// <summary>
/// Analytics and crash reporting placeholders. In editor and development builds analytics events
/// are written to the log so the event list can be checked; release builds use null services until
/// a provider is integrated (see docs/ANDROID_BUILD_CHECKLIST.md).
/// </summary>
public sealed class LogAnalyticsService : IAnalyticsService
{
    public void LogEvent(string name, IDictionary<string, object> parameters)
    {
        var sb = new StringBuilder("[analytics] ").Append(name);
        if (parameters != null) foreach (var kv in parameters) sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
        Debug.Log(sb.ToString());
    }
}

/// <summary>Forwards Unity exceptions to an <see cref="ICrashService"/> (null until a provider is installed).</summary>
public sealed class CrashForwarder : IDisposable
{
    private readonly ICrashService crash;

    public CrashForwarder(ICrashService service)
    {
        crash = service;
        Application.logMessageReceived += OnLog;
    }

    public void Dispose() => Application.logMessageReceived -= OnLog;

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception) crash.RecordException(new Exception(condition + "\n" + stackTrace));
        else if (type == LogType.Error) crash.Log(condition);
    }
}
