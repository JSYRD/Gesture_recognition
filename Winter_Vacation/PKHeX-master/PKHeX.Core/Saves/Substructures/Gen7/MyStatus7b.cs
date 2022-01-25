using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    public sealed class MyStatus7b : SaveBlock
    {
        public MyStatus7b(SAV7b sav, int offset) : base(sav) => Offset = offset;

        // Player Information

        // idb uint8 offset: 0x58

        public int TID
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(Offset + 0));
            set => WriteUInt16LittleEndian(Data.AsSpan(Offset + 0), (ushort)value);
        }

        public int SID
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(Offset + 2));
            set => WriteUInt16LittleEndian(Data.AsSpan(Offset + 2), (ushort)value);
        }

        public int Game
        {
            get => Data[Offset + 4];
            set => Data[Offset + 4] = (byte)value;
        }

        public int Gender
        {
            get => Data[Offset + 5];
            set => Data[Offset + 5] = OverworldGender = (byte)value;
        }

        public const int GameSyncIDSize = 16; // 8 bytes

        public string GameSyncID
        {
            get => Util.GetHexStringFromBytes(Data, Offset + 0x10, GameSyncIDSize / 2);
            set
            {
                if (value.Length > 16)
                    throw new ArgumentException(nameof(value));

                var data = Util.GetBytesFromHexString(value);
                SAV.SetData(data, Offset + 0x10);
            }
        }

        public int Language
        {
            get => Data[Offset + 0x35];
            set => Data[Offset + 0x35] = (byte)value;
        }

        private Span<byte> OT_Trash => Data.AsSpan(Offset + 0x38, 0x1A);

        public string OT
        {
            get => SAV.GetString(OT_Trash);
            set => SAV.SetString(OT_Trash, value.AsSpan(), SAV.OTLength, StringConverterOption.ClearZero);
        }

        // The value here corresponds to a Trainer Class string (ranging from 000 to 383, use pkNX to get a full list).
        public byte TrainerClassIndex
        {
            get => Data[Offset + 0x076];
            set => Data[Offset + 0x076] = value;
        }

        public byte StarterGender
        {
            get => Data[Offset + 0x0B9];
            set => Data[Offset + 0x0B9] = value;
        }

        public byte OverworldGender // Model
        {
            get => Data[Offset + 0x108];
            set => Data[Offset + 0x108] = value;
        }
    }
}