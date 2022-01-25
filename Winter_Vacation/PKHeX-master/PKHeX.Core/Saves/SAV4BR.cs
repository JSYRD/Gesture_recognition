﻿using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 4 <see cref="SaveFile"/> object for Pokémon Battle Revolution saves.
    /// </summary>
    public sealed class SAV4BR : SaveFile
    {
        protected internal override string ShortSummary => $"{Version} #{SaveCount:0000}";
        public override string Extension => string.Empty;
        public override PersonalTable Personal => PersonalTable.DP;
        public override IReadOnlyList<ushort> HeldItems => Legal.HeldItems_DP;

        private const int SAVE_COUNT = 4;

        public SAV4BR() : base(SaveUtil.SIZE_G4BR)
        {
            ClearBoxes();
        }

        public SAV4BR(byte[] data) : base(data)
        {
            InitializeData(data);
        }

        private void InitializeData(ReadOnlySpan<byte> data)
        {
            Data = DecryptPBRSaveData(data);

            // Detect active save
            var first  = ReadUInt32BigEndian(Data.AsSpan(0x00004C));
            var second = ReadUInt32BigEndian(Data.AsSpan(0x1C004C));
            SaveCount = Math.Max(second, first);
            if (second > first)
            {
                // swap halves
                byte[] tempData = new byte[0x1C0000];
                Array.Copy(Data, 0, tempData, 0, 0x1C0000);
                Array.Copy(Data, 0x1C0000, Data, 0, 0x1C0000);
                tempData.CopyTo(Data, 0x1C0000);
            }

            var names = (string[]) SaveNames;
            for (int i = 0; i < SAVE_COUNT; i++)
            {
                var name = GetOTName(i);
                if (string.IsNullOrWhiteSpace(name))
                    name = $"Empty {i + 1}";
                else if (_currentSlot == -1)
                    _currentSlot = i;
                names[i] = name;
            }

            if (_currentSlot == -1)
                _currentSlot = 0;

            CurrentSlot = _currentSlot;
        }

        /// <summary> Amount of times the primary save has been saved </summary>
        private uint SaveCount;

        protected override byte[] GetFinalData()
        {
            SetChecksums();
            return EncryptPBRSaveData(Data);
        }

        // Configuration
        protected override SaveFile CloneInternal() => new SAV4BR(Write());

        public readonly IReadOnlyList<string> SaveNames = new string[SAVE_COUNT];

        private int _currentSlot = -1;
        private const int SIZE_SLOT = 0x6FF00;

        public int CurrentSlot
        {
            get => _currentSlot;
            // 4 save slots, data reading depends on current slot
            set
            {
                _currentSlot = value;
                var ofs = SIZE_SLOT * _currentSlot;
                Box = ofs + 0x978;
                Party = ofs + 0x13A54; // first team slot after boxes
                BoxName = ofs + 0x58674;
            }
        }

        protected override int SIZE_STORED => PokeCrypto.SIZE_4STORED;
        protected override int SIZE_PARTY => PokeCrypto.SIZE_4STORED + 4;
        public override PKM BlankPKM => new BK4();
        public override Type PKMType => typeof(BK4);

        public override int MaxMoveID => 467;
        public override int MaxSpeciesID => Legal.MaxSpeciesID_4;
        public override int MaxAbilityID => Legal.MaxAbilityID_4;
        public override int MaxItemID => Legal.MaxItemID_4_HGSS;
        public override int MaxBallID => Legal.MaxBallID_4;
        public override int MaxGameID => Legal.MaxGameID_4;

        public override int MaxEV => 255;
        public override int Generation => 4;
        protected override int GiftCountMax => 1;
        public override int OTLength => 7;
        public override int NickLength => 10;
        public override int MaxMoney => 999999;
        public override int Language => (int)LanguageID.English; // prevent KOR from inhabiting

        public override int BoxCount => 18;

        public override int PartyCount
        {
            get
            {
                int ctr = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (Data[GetPartyOffset(i) + 4] != 0) // sanity
                        ctr++;
                }
                return ctr;
            }
            protected set
            {
                // Ignore, value is calculated
            }
        }

        // Checksums
        protected override void SetChecksums()
        {
            SetChecksum(Data, 0, 0x100, 8);
            SetChecksum(Data, 0, 0x1C0000, 0x1BFF80);
            SetChecksum(Data, 0x1C0000, 0x100, 0x1C0008);
            SetChecksum(Data, 0x1C0000, 0x1C0000, 0x1BFF80 + 0x1C0000);
        }

        public override bool ChecksumsValid => IsChecksumsValid(Data);
        public override string ChecksumInfo => $"Checksums valid: {ChecksumsValid}.";

        public static bool IsChecksumsValid(Span<byte> sav)
        {
            return VerifyChecksum(sav, 0x000000, 0x1C0000, 0x1BFF80)
                && VerifyChecksum(sav, 0x000000, 0x000100, 0x000008)
                && VerifyChecksum(sav, 0x1C0000, 0x1C0000, 0x1BFF80 + 0x1C0000)
                && VerifyChecksum(sav, 0x1C0000, 0x000100, 0x1C0008);
        }

        // Trainer Info
        public override GameVersion Version { get => GameVersion.BATREV; protected set { } }

        private string GetOTName(int slot)
        {
            var ofs = 0x390 + (0x6FF00 * slot);
            var span = Data.AsSpan(ofs, 16);
            return GetString(span);
        }

        private void SetOTName(int slot, string name)
        {
            var ofs = 0x390 + (0x6FF00 * slot);
            var span = Data.AsSpan(ofs, 16);
            SetString(span, name.AsSpan(), 7, StringConverterOption.ClearZero);
        }

        public string CurrentOT { get => GetOTName(_currentSlot); set => SetOTName(_currentSlot, value); }

        // Storage
        public override int GetPartyOffset(int slot) => Party + (SIZE_PARTY * slot);
        public override int GetBoxOffset(int box) => Box + (SIZE_STORED * box * 30);

        public override int TID
        {
            get => (Data[(_currentSlot * SIZE_SLOT) + 0x12867] << 8) | Data[(_currentSlot * SIZE_SLOT) + 0x12860];
            set
            {
                Data[(_currentSlot * SIZE_SLOT) + 0x12867] = (byte)(value >> 8);
                Data[(_currentSlot * SIZE_SLOT) + 0x12860] = (byte)(value & 0xFF);
            }
        }

        public override int SID
        {
            get => (Data[(_currentSlot * SIZE_SLOT) + 0x12865] << 8) | Data[(_currentSlot * SIZE_SLOT) + 0x12866];
            set
            {
                Data[(_currentSlot * SIZE_SLOT) + 0x12865] = (byte)(value >> 8);
                Data[(_currentSlot * SIZE_SLOT) + 0x12866] = (byte)(value & 0xFF);
            }
        }

        // Save file does not have Box Name / Wallpaper info
        private int BoxName = -1;
        private const int BoxNameLength = 0x28;

        public override string GetBoxName(int box)
        {
            if (BoxName < 0)
                return $"BOX {box + 1}";

            int ofs = BoxName + (box * BoxNameLength);
            var span = Data.AsSpan(ofs, BoxNameLength);
            if (span.Count((byte)0) == span.Length)
                return $"BOX {box + 1}";
            return GetString(ofs, BoxNameLength);
        }

        public override void SetBoxName(int box, string value)
        {
            if (BoxName < 0)
                return;

            int ofs = BoxName + (box * BoxNameLength);
            var span = Data.AsSpan(ofs, BoxNameLength);
            if (span.Count((byte)0) == span.Length)
                return;

            SetString(span, value.AsSpan(), BoxNameLength / 2, StringConverterOption.ClearZero);
        }

        protected override PKM GetPKM(byte[] data)
        {
            if (data.Length != SIZE_STORED)
                Array.Resize(ref data, SIZE_STORED);
            return BK4.ReadUnshuffle(data);
        }

        protected override byte[] DecryptPKM(byte[] data) => data;

        protected override void SetDex(PKM pkm) { /* There's no PokéDex */ }

        protected override void SetPKM(PKM pkm, bool isParty = false)
        {
            var pk4 = (BK4)pkm;
            // Apply to this Save File
            DateTime Date = DateTime.Now;
            if (pk4.Trade(OT, TID, SID, Gender, Date.Day, Date.Month, Date.Year))
                pkm.RefreshChecksum();
        }

        protected override void SetPartyValues(PKM pkm, bool isParty)
        {
            if (pkm is G4PKM g4)
                g4.Sanity = isParty ? (ushort)0xC000 : (ushort)0x4000;
        }

        public static byte[] DecryptPBRSaveData(ReadOnlySpan<byte> input)
        {
            byte[] output = new byte[input.Length];
            Span<ushort> keys = stackalloc ushort[4];
            for (int i = 0; i < SaveUtil.SIZE_G4BR; i += 0x1C0000)
            {
                ReadKeys(input, i, keys);
                input.Slice(i, 8).CopyTo(output.AsSpan(i, 8));
                GeniusCrypto.Decrypt(input, i + 8, i + 0x1C0000, keys, output);
            }
            return output;
        }

        private static byte[] EncryptPBRSaveData(ReadOnlySpan<byte> input)
        {
            byte[] output = new byte[input.Length];
            Span<ushort> keys = stackalloc ushort[4];
            for (int i = 0; i < SaveUtil.SIZE_G4BR; i += 0x1C0000)
            {
                ReadKeys(input, i, keys);
                input.Slice(i, 8).CopyTo(output.AsSpan(i, 8));
                GeniusCrypto.Encrypt(input, i + 8, i + 0x1C0000, keys, output);
            }
            return output;
        }

        private static void ReadKeys(ReadOnlySpan<byte> input, int ofs, Span<ushort> keys)
        {
            for (int i = 0; i < keys.Length; i++)
                keys[i] = ReadUInt16BigEndian(input[(ofs + (i * 2))..]);
        }

        public static bool VerifyChecksum(Span<byte> input, int offset, int len, int checksum_offset)
        {
            Span<uint> originalChecksums = stackalloc uint[16];
            for (int i = 0; i < originalChecksums.Length; i++)
            {
                var chk = input.Slice(checksum_offset + (i * 4), 4);
                originalChecksums[i] = ReadUInt32BigEndian(chk);
                chk.Clear();
            }

            Span<uint> checksums = stackalloc uint[16];
            var span = input.Slice(offset, len);
            for (int i = 0; i < span.Length; i += 2)
            {
                uint val = ReadUInt16BigEndian(span[i..]);
                for (int j = 0; j < 16; j++)
                    checksums[j] += ((val >> j) & 1);
            }

            // Restore original checksums
            for (int i = 0; i < originalChecksums.Length; i++)
            {
                var chk = originalChecksums[i];
                var dest = input[(checksum_offset + (i * 4))..];
                WriteUInt32BigEndian(dest, chk);
            }

            // Check if they match
            for (int i = 0; i < originalChecksums.Length; i++)
            {
                if (originalChecksums[i] != checksums[i])
                    return false;
            }
            return true;
        }

        private static void SetChecksum(Span<byte> input, int offset, int len, int checksum_offset)
        {
            // Wipe Checksum region.
            input.Slice(checksum_offset, 4 * 16).Clear();

            Span<uint> checksums = stackalloc uint[16];
            var span = input.Slice(offset, len);
            for (int i = 0; i < len; i += 2)
            {
                uint val = ReadUInt16BigEndian(span[i..]);
                for (int j = 0; j < 16; j++)
                    checksums[j] += ((val >> j) & 1);
            }

            for (int i = 0; i < checksums.Length; i++)
            {
                var chk = checksums[i];
                var dest = input[(checksum_offset + (i * 4))..];
                WriteUInt32BigEndian(dest, chk);
            }
        }

        public override string GetString(ReadOnlySpan<byte> data) => StringConverter4GC.GetStringUnicode(data);

        public override int SetString(Span<byte> destBuffer, ReadOnlySpan<char> value, int maxLength, StringConverterOption option) => StringConverter4GC.SetStringUnicode(value, destBuffer, maxLength, option);
    }
}
