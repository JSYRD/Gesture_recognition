﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    public sealed class Record8 : RecordBlock
    {
        public const int RecordCount = 50;
        public const int WattTotal = 22;
        protected override IReadOnlyList<byte> RecordMax { get; }

        public Record8(SaveFile sav, SCBlock block, IReadOnlyList<byte> maxes) : base(sav, block.Data) =>
            RecordMax = maxes;

        public override int GetRecord(int recordID)
        {
            int ofs = Records.GetOffset(Offset, recordID);
            if (recordID < RecordCount)
                return ReadInt32LittleEndian(Data.AsSpan(ofs));
            Trace.Fail(nameof(recordID));
            return 0;
        }

        public override void SetRecord(int recordID, int value)
        {
            if ((uint)recordID >= RecordCount)
                throw new ArgumentOutOfRangeException(nameof(recordID));
            int ofs = GetRecordOffset(recordID);
            int max = GetRecordMax(recordID);
            if (value > max)
                value = max;
            if (recordID < RecordCount)
                WriteInt32LittleEndian(Data.AsSpan(ofs), value);
            else
                Trace.Fail(nameof(recordID));
        }
    }
}