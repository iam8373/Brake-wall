using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BreakToEscape.Core.Save;
using UnityEngine;

/// <summary>
/// Local save file handling. The snapshot is taken on the main thread by <see cref="GameBootstrap"/>
/// (it knows every system); encoding and the crash-safe write (<see cref="SaveFileStore"/>) run on a
/// background thread so the game does not hitch. Auto-saves every 45 s and saves synchronously when
/// the app is paused or closed (Android may kill a paused app at any time).
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Tooltip("Seconds between automatic saves.")]
    public float autoSaveIntervalSeconds = 45f;
    public string fileName = "break_to_escape_save.dat";

    /// <summary>Raised on the main thread with the encoded bytes after a successful write (hook for cloud save later).</summary>
    public event Action<byte[]> Saved;

    public string LastLoadReport { get; private set; } = "";

    private SaveFileStore store;
    private float timer;
    private int writing;
    private Func<SaveData> snapshotProvider;
    private byte[] completedBytes;
    private readonly object completedLock = new object();

    void Awake()
    {
        Instance = this;
        store = new SaveFileStore(Path.Combine(Application.persistentDataPath, fileName));
    }

    /// <summary>The bootstrap registers how to take a snapshot of the game.</summary>
    public void SetSnapshotProvider(Func<SaveData> provider) => snapshotProvider = provider;

    public SaveData Load()
    {
        SaveData data;
        string report;
        try
        {
            data = store.Load(out report);
        }
        catch (Exception e)
        {
            data = null;
            report = "load failed: " + e.Message;
        }
        LastLoadReport = report;
        Debug.Log("SaveManager: " + report);
        return data;
    }

    void Update()
    {
        if (snapshotProvider == null) return;
        timer += Time.unscaledDeltaTime;
        if (timer >= autoSaveIntervalSeconds)
        {
            timer = 0f;
            SaveInBackground();
        }
        byte[] done = null;
        lock (completedLock)
        {
            if (completedBytes != null) { done = completedBytes; completedBytes = null; }
        }
        if (done != null) Saved?.Invoke(done);
    }

    /// <summary>Takes a snapshot now and writes it on a worker thread. Skipped if a write is still running.</summary>
    public void SaveInBackground()
    {
        if (snapshotProvider == null || Interlocked.CompareExchange(ref writing, 1, 0) != 0) return;
        SaveData snapshot = snapshotProvider();
        Task.Run(() =>
        {
            try
            {
                byte[] bytes = SaveCodec.Encode(snapshot);
                store.Write(bytes);
                lock (completedLock) completedBytes = bytes;
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveManager: background save failed: " + e.Message);
            }
            finally
            {
                Interlocked.Exchange(ref writing, 0);
            }
        });
    }

    /// <summary>Synchronous save (app pause, quit, escape). Waits briefly for a running background write.</summary>
    public void SaveNow()
    {
        if (snapshotProvider == null) return;
        for (int i = 0; i < 50 && Interlocked.CompareExchange(ref writing, 1, 0) != 0; i++) Thread.Sleep(10);
        try
        {
            byte[] bytes = SaveCodec.Encode(snapshotProvider());
            store.Write(bytes);
            Saved?.Invoke(bytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveManager: save failed: " + e.Message);
        }
        finally
        {
            Interlocked.Exchange(ref writing, 0);
            timer = 0f;
        }
    }

    /// <summary>Deletes every save copy (Settings: start a new run).</summary>
    public void DeleteSave() => store.DeleteAll();

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    void OnApplicationQuit() => SaveNow();
}
