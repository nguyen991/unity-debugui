using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ng.DebugTools
{
    internal enum LogKind { Log, Warning, Error }

    internal sealed class LogEntry
    {
        public readonly string Message;
        public readonly string StackTrace;
        public readonly LogKind Kind;

        /// <summary>"HH:mm:ss first line of the message", for list rows.</summary>
        public readonly string Summary;

        public LogEntry(string message, string stackTrace, LogType type)
        {
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Kind = type == LogType.Log ? LogKind.Log : type == LogType.Warning ? LogKind.Warning : LogKind.Error;
            int newline = Message.IndexOf('\n');
            Summary = DateTime.Now.ToString("HH:mm:ss") + " " + (newline < 0 ? Message : Message.Substring(0, newline));
        }
    }

    /// <summary>
    /// Thread-safe ring buffer of log entries with per-kind counts. When full, the oldest entry is
    /// dropped. <see cref="Version"/> changes on every add/clear so views can refresh only on change.
    /// </summary>
    internal sealed class LogStore
    {
        readonly object gate = new object();
        readonly int[] counts = new int[3];
        LogEntry[] ring;
        int head;   // index of the oldest entry
        int count;
        int version;
        bool attached;

        public LogStore(int capacity)
        {
            ring = new LogEntry[Math.Max(1, capacity)];
        }

        public int Version
        {
            get { lock (gate) return version; }
        }

        public int CountOf(LogKind kind)
        {
            lock (gate) return counts[(int)kind];
        }

        public void Add(string message, string stackTrace, LogType type)
        {
            var entry = new LogEntry(message, stackTrace, type);
            lock (gate)
            {
                if (count == ring.Length)
                {
                    counts[(int)ring[head].Kind]--;
                    ring[head] = entry;
                    head = (head + 1) % ring.Length;
                }
                else
                {
                    ring[(head + count) % ring.Length] = entry;
                    count++;
                }
                counts[(int)entry.Kind]++;
                version++;
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                Array.Clear(ring, 0, ring.Length);
                head = count = 0;
                Array.Clear(counts, 0, counts.Length);
                version++;
            }
        }

        /// <summary>Keeps the newest entries when shrinking.</summary>
        public void SetCapacity(int capacity)
        {
            capacity = Math.Max(1, capacity);
            lock (gate)
            {
                if (capacity == ring.Length)
                    return;
                var kept = new LogEntry[capacity];
                int keep = Math.Min(count, capacity);
                Array.Clear(counts, 0, counts.Length);
                for (int i = 0; i < keep; i++)
                {
                    var e = ring[(head + count - keep + i) % ring.Length];
                    kept[i] = e;
                    counts[(int)e.Kind]++;
                }
                ring = kept;
                head = 0;
                count = keep;
                version++;
            }
        }

        /// <summary>Fills <paramref name="destination"/> with matching entries, oldest first.</summary>
        public void CopyTo(List<LogEntry> destination, Predicate<LogEntry> filter)
        {
            destination.Clear();
            lock (gate)
            {
                for (int i = 0; i < count; i++)
                {
                    var e = ring[(head + i) % ring.Length];
                    if (filter(e))
                        destination.Add(e);
                }
            }
        }

        public void Attach()
        {
            if (attached)
                return;
            attached = true;
            Application.logMessageReceivedThreaded += OnLog;
        }

        public void Detach()
        {
            if (!attached)
                return;
            attached = false;
            Application.logMessageReceivedThreaded -= OnLog;
        }

        void OnLog(string message, string stackTrace, LogType type) => Add(message, stackTrace, type);

        // ---------- Shared instance used by DebugUI.LogPanel ----------

        static LogStore shared;

        internal static LogStore GetShared(int capacity)
        {
            if (shared == null)
            {
                shared = new LogStore(capacity);
                shared.Attach();
            }
            else
            {
                shared.SetCapacity(capacity);
            }
            return shared;
        }

        /// <summary>Also runs on entering play mode (domain reload may be disabled).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetShared()
        {
            shared?.Detach();
            shared = null;
        }
    }
}
