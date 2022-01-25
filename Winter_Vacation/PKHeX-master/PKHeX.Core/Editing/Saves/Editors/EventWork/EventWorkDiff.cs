﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static PKHeX.Core.MessageStrings;

namespace PKHeX.Core
{
    /// <summary>
    /// Calculates differences in the Event Blocks between two <see cref="SaveFile"/>.
    /// </summary>
    public class EventBlockDiff
    {
        public string Message { get; protected set; } = string.Empty;
        public readonly List<int> SetFlags = new();
        public readonly List<int> ClearedFlags = new();
        public readonly List<string> WorkDiff = new();

        private const int MAX_SAVEFILE_SIZE = 0x10_0000; // 1 MB
        protected EventBlockDiff() { }

        public EventBlockDiff(string f1, string f2)
        {
            if (!SanityCheckFiles(f1, f2))
                return;
            var s1 = SaveUtil.GetVariantSAV(f1);
            var s2 = SaveUtil.GetVariantSAV(f2);
            if (s1 == null || s2 == null || s1.GetType() != s2.GetType())
            {
                Message = MsgSaveDifferentTypes;
                return;
            }
            Diff(s1, s2);
        }

        public EventBlockDiff(SaveFile s1, SaveFile s2) => Diff(s1, s2);

        protected bool SanityCheckFiles(string f1, string f2)
        {
            if (!File.Exists(f1))
            { Message = string.Format(MsgSaveNumberInvalid, 1); return false; }

            if (!File.Exists(f2))
            { Message = string.Format(MsgSaveNumberInvalid, 2); return false; }

            if (new FileInfo(f1).Length > MAX_SAVEFILE_SIZE)
            { Message = string.Format(MsgSaveNumberInvalid, 1); return false; }

            if (new FileInfo(f2).Length > MAX_SAVEFILE_SIZE)
            { Message = string.Format(MsgSaveNumberInvalid, 2); return false; }

            return true;
        }

        protected bool SanityCheckSaveInfo(SaveFile s1, SaveFile s2)
        {
            if (s1.GetType() != s2.GetType())
            { Message = string.Format(MsgSaveDifferentTypes, $"S1: {s1.GetType().Name}", $"S2: {s2.GetType().Name}"); return false; }

            if (s1.Version != s2.Version)
            { Message = string.Format(MsgSaveDifferentVersions, $"S1: {s1.Version}", $"S2: {s2.Version}"); return false; }

            return true;
        }

        protected virtual void Diff(SaveFile s1, SaveFile s2)
        {
            if (!SanityCheckSaveInfo(s1, s2))
                return;

            bool[] oldBits = s1.GetEventFlags();
            bool[] newBits = s2.GetEventFlags();
            var oldConst = s1.GetEventConsts();
            var newConst = s2.GetEventConsts();

            for (int i = 0; i < newBits.Length; i++)
            {
                if (oldBits[i] != newBits[i])
                    (newBits[i] ? SetFlags : ClearedFlags).Add(i);
            }
            for (int i = 0; i < newConst.Length; i++)
            {
                if (oldConst[i] != newConst[i])
                    WorkDiff.Add($"{i}: {oldConst[i]}->{newConst[i]}");
            }
        }
    }

    public sealed class EventWorkDiff7b : EventBlockDiff
    {
        public readonly List<int> WorkChanged = new();
        private SaveFile? S1;

        public EventWorkDiff7b(string f1, string f2)
        {
            if (!SanityCheckFiles(f1, f2))
                return;
            var s1 = SaveUtil.GetVariantSAV(f1);
            var s2 = SaveUtil.GetVariantSAV(f2);
            if (s1 == null || s2 == null || s1.GetType() != s2.GetType())
            {
                Message = MsgSaveDifferentTypes;
                return;
            }
            Diff(s1, s2);
        }

        public EventWorkDiff7b(SaveFile s1, SaveFile s2) => Diff(s1, s2);

        protected override void Diff(SaveFile s1, SaveFile s2)
        {
            if (!SanityCheckSaveInfo(s1, s2))
                return;

            EventWorkUtil.DiffSavesFlag(((SAV7b)s1).Blocks.EventWork, ((SAV7b)s2).Blocks.EventWork, SetFlags, ClearedFlags);
            EventWorkUtil.DiffSavesWork(((SAV7b)s1).Blocks.EventWork, ((SAV7b)s2).Blocks.EventWork, WorkChanged, WorkDiff);
            S1 = s1;
        }

        public IReadOnlyList<string> Summarize()
        {
            if (S1 == null)
                return Array.Empty<string>();
            var ew = ((SAV7b)S1).Blocks.EventWork;

            var fOn = SetFlags.Select(z => FlagSummary.Get(z, ew).ToString());
            var fOff = ClearedFlags.Select(z => FlagSummary.Get(z, ew).ToString());
            var wt = WorkChanged.Select((z, i) => WorkSummary.Get(z, ew, WorkDiff[i]).ToString());

            var list = new List<string> { "Flags: ON", "=========" };
            list.AddRange(fOn);
            if (SetFlags.Count == 0)
                list.Add("None.");

            list.Add("");
            list.Add("Flags: OFF");
            list.Add("==========");
            list.AddRange(fOff);
            if (ClearedFlags.Count == 0)
                list.Add("None.");

            list.Add("");
            list.Add("Work:");
            list.Add("=====");
            if (WorkChanged.Count == 0)
                list.Add("None.");
            list.AddRange(wt);

            return list;
        }

        private readonly record struct FlagSummary(EventVarType Type, int Index, int Raw)
        {
            public override string ToString() => $"{Raw:0000}\t{false}\t{Index:0000}\t{Type}";

            public static FlagSummary Get(int rawIndex, EventWork7b ew)
            {
                var type = ew.GetFlagType(rawIndex, out var subIndex);
                return new FlagSummary(type, subIndex, rawIndex);
            }
        }

        private readonly record struct WorkSummary(EventVarType Type, int Index, int Raw, string Text)
        {
            public override string ToString() => $"{Raw:000}\t{Text}\t{Index:000}\t{Type}";

            public static WorkSummary Get(int rawIndex, EventWork7b ew, string text)
            {
                var type = ew.GetFlagType(rawIndex, out var subIndex);
                return new WorkSummary(type, subIndex, rawIndex, text);
            }
        }
    }

    public sealed class EventWorkDiff8b : EventBlockDiff
    {
        public readonly List<int> WorkChanged = new();
        private SaveFile? S1;
        public readonly List<int> SetSystem = new();
        public readonly List<int> ClearedSystem = new();

        public EventWorkDiff8b(string f1, string f2)
        {
            if (!SanityCheckFiles(f1, f2))
                return;
            var s1 = SaveUtil.GetVariantSAV(f1);
            var s2 = SaveUtil.GetVariantSAV(f2);
            if (s1 == null || s2 == null || s1.GetType() != s2.GetType())
            {
                Message = MsgSaveDifferentTypes;
                return;
            }
            Diff(s1, s2);
        }

        public EventWorkDiff8b(SaveFile s1, SaveFile s2) => Diff(s1, s2);

        protected override void Diff(SaveFile s1, SaveFile s2)
        {
            if (!SanityCheckSaveInfo(s1, s2))
                return;

            EventWorkUtil.DiffSavesFlag(((SAV8BS)s1).Work, ((SAV8BS)s2).Work, SetFlags, ClearedFlags);
            EventWorkUtil.DiffSavesSystem(((SAV8BS)s1).Work, ((SAV8BS)s2).Work, SetSystem, ClearedSystem);
            EventWorkUtil.DiffSavesWork(((SAV8BS)s1).Work, ((SAV8BS)s2).Work, WorkChanged, WorkDiff);
            S1 = s1;
        }

        public IReadOnlyList<string> Summarize()
        {
            if (S1 == null)
                return Array.Empty<string>();

            var fOn = SetFlags.Select(z => new FlagSummary(z).ToString());
            var fOff = ClearedFlags.Select(z => new FlagSummary(z).ToString());

            var sOn = SetSystem.Select(z => new FlagSummary(z).ToString());
            var sOff = ClearedSystem.Select(z => new FlagSummary(z).ToString());

            var wt = WorkChanged.Select((z, i) => new WorkSummary(z, WorkDiff[i]).ToString());

            var list = new List<string> { "Flags: ON", "=========" };
            list.AddRange(fOn);
            if (SetFlags.Count == 0)
                list.Add("None.");

            list.Add("");
            list.Add("Flags: OFF");
            list.Add("==========");
            list.AddRange(fOff);
            if (ClearedFlags.Count == 0)
                list.Add("None.");

            list.Add("System: ON");
            list.Add("=========");
            list.AddRange(sOn);
            if (SetFlags.Count == 0)
                list.Add("None.");

            list.Add("");
            list.Add("System: OFF");
            list.Add("==========");
            list.AddRange(sOff);
            if (ClearedSystem.Count == 0)
                list.Add("None.");

            list.Add("");
            list.Add("Work:");
            list.Add("=====");
            if (WorkChanged.Count == 0)
                list.Add("None.");
            list.AddRange(wt);

            return list;
        }

        private readonly record struct FlagSummary(int Raw)
        {
            public override string ToString() => $"{Raw:0000}\t{false}";
        }

        private readonly record struct WorkSummary(int Raw, string Text)
        {
            public override string ToString() => $"{Raw:000}\t{Text}";
        }
    }
}
