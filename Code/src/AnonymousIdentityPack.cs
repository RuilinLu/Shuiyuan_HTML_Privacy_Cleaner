using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal static class AnonymousIdentityPack
    {
        private const string ResourceName = "ShuiyuanHtmlPrivacyCleaner.Assets.anonymous_identity_pack.jsonl.gz";
        private static readonly object Gate = new object();
        private static readonly List<Entry> Entries = new List<Entry>();
        private static Stream _resourceStream;
        private static GZipStream _gzipStream;
        private static StreamReader _reader;
        private static bool _initialized;
        private static bool _exhausted;
        private static bool _failed;

        public static AnonymousIdentity TryGetIdentity(int index)
        {
            int safeIndex = Math.Max(index, 1);
            Entry entry = TryGetEntry(safeIndex);
            if (entry == null)
            {
                return null;
            }

            AnonymousIdentity identity = new AnonymousIdentity();
            identity.Index = safeIndex;
            identity.DisplayName = string.IsNullOrWhiteSpace(entry.DisplayName) ? "Anonymous User " + safeIndex.ToString("00000") : entry.DisplayName;
            identity.Username = string.IsNullOrWhiteSpace(entry.Username) ? "anonymous_" + safeIndex.ToString("00000") : entry.Username;
            identity.AvatarDataUri = string.IsNullOrWhiteSpace(entry.AvatarSvg)
                ? null
                : "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(entry.AvatarSvg);
            return identity;
        }

        private static Entry TryGetEntry(int index)
        {
            lock (Gate)
            {
                if (!EnsureLoaded(index))
                {
                    return null;
                }

                if (Entries.Count == 0)
                {
                    return null;
                }

                int safeIndex = index <= Entries.Count ? index : ((index - 1) % Entries.Count) + 1;
                return Entries[safeIndex - 1];
            }
        }

        private static bool EnsureLoaded(int count)
        {
            if (_failed)
            {
                return false;
            }

            if (!_initialized)
            {
                try
                {
                    _resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
                    if (_resourceStream == null)
                    {
                        _failed = true;
                        return false;
                    }

                    _gzipStream = new GZipStream(_resourceStream, CompressionMode.Decompress, leaveOpen: false);
                    _reader = new StreamReader(_gzipStream);
                    _initialized = true;
                }
                catch
                {
                    _failed = true;
                    return false;
                }
            }

            while (!_exhausted && Entries.Count < count)
            {
                string line;
                try
                {
                    line = _reader.ReadLine();
                }
                catch
                {
                    _failed = true;
                    return false;
                }

                if (line == null)
                {
                    _exhausted = true;
                    DisposeReader();
                    break;
                }

                if (line.Length == 0)
                {
                    continue;
                }

                try
                {
                    Entry entry = JsonSerializer.Deserialize<Entry>(line);
                    if (entry != null)
                    {
                        Entries.Add(entry);
                    }
                }
                catch
                {
                    // Ignore a damaged line and continue. The built-in fallback generator remains available.
                }
            }

            return Entries.Count >= count || (_exhausted && Entries.Count > 0);
        }

        private static void DisposeReader()
        {
            try { _reader?.Dispose(); } catch { }
            _reader = null;
            _gzipStream = null;
            _resourceStream = null;
        }

        private sealed class Entry
        {
            public int i { get; set; }
            public string d { get; set; }
            public string u { get; set; }
            public string s { get; set; }
            public string a { get; set; }

            public string DisplayName { get { return d; } }
            public string Username { get { return u; } }
            public string AvatarSvg { get { return a; } }
        }
    }
}
