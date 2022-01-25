﻿using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 1 <see cref="SaveFile"/> object.
    /// </summary>
    public sealed class SAV1 : SaveFile, ILangDeviantSave
    {
        protected internal override string ShortSummary => $"{OT} ({Version}) - {PlayTimeString}";
        public override string Extension => ".sav";
        public bool IsVirtualConsole => State.Exportable && Metadata.FileName is { } s && s.StartsWith("sav") && s.Contains(".dat"); // default to GB-Era for non-exportable

        public int SaveRevision => Japanese ? 0 : 1;
        public string SaveRevisionString => (Japanese ? "J" : "U") + (IsVirtualConsole ? "VC" : "GB");
        public bool Japanese { get; }
        public bool Korean => false;

        public override PersonalTable Personal { get; }
        public override IReadOnlyList<ushort> HeldItems => Array.Empty<ushort>();

        public override IReadOnlyList<string> PKMExtensions => Array.FindAll(PKM.Extensions, f =>
        {
            int gen = f[^1] - 0x30;
            return gen is 1 or 2;
        });

        public SAV1(GameVersion version = GameVersion.RBY, bool japanese = false) : base(SaveUtil.SIZE_G1RAW)
        {
            Version = version;
            Japanese = japanese;
            Offsets = Japanese ? SAV1Offsets.JPN : SAV1Offsets.INT;
            Personal = version == GameVersion.YW ? PersonalTable.Y : PersonalTable.RB;
            Initialize(version);
            ClearBoxes();
        }

        public SAV1(byte[] data, GameVersion versionOverride = GameVersion.Any) : base(data)
        {
            Japanese = SaveUtil.GetIsG1SAVJ(Data);
            Offsets = Japanese ? SAV1Offsets.JPN : SAV1Offsets.INT;

            Version = versionOverride != GameVersion.Any ? versionOverride : SaveUtil.GetIsG1SAV(data);
            Personal = Version == GameVersion.YW ? PersonalTable.Y : PersonalTable.RB;
            if (Version == GameVersion.Invalid)
                return;

            Initialize(versionOverride);
        }

        private void Initialize(GameVersion versionOverride)
        {
            // see if RBY can be differentiated
            if (Starter != 0 && versionOverride is not (GameVersion.RB or GameVersion.YW))
                Version = Yellow ? GameVersion.YW : GameVersion.RB;

            Box = Data.Length;
            Array.Resize(ref Data, Data.Length + SIZE_RESERVED);
            Party = GetPartyOffset(0);

            // Stash boxes after the save file's end.
            int stored = SIZE_STOREDBOX;
            int baseDest = Data.Length - SIZE_RESERVED;
            var capacity = Japanese ? PokeListType.StoredJP : PokeListType.Stored;
            for (int i = 0; i < BoxCount; i++)
            {
                int ofs = GetBoxRawDataOffset(i);
                var box = GetData(ofs, stored);
                var boxDest = baseDest + (i * SIZE_BOX);
                var boxPL = new PokeList1(box, capacity, Japanese);
                for (int j = 0; j < boxPL.Pokemon.Length; j++)
                {
                    var dest = boxDest + (j * SIZE_STORED);
                    var pkDat = (j < boxPL.Count)
                        ? new PokeList1(boxPL[j]).Write()
                        : new byte[PokeList1.GetDataLength(PokeListType.Single, Japanese)];
                    pkDat.CopyTo(Data, dest);
                }
            }

            var current = GetData(Offsets.CurrentBox, SIZE_STOREDBOX);
            var curBoxPL = new PokeList1(current, capacity, Japanese);
            for (int i = 0; i < curBoxPL.Pokemon.Length; i++)
            {
                var dest = Data.Length - SIZE_RESERVED + (CurrentBox * SIZE_BOX) + (i * SIZE_STORED);
                var pkDat = i < curBoxPL.Count
                    ? new PokeList1(curBoxPL[i]).Write()
                    : new byte[PokeList1.GetDataLength(PokeListType.Single, Japanese)];
                pkDat.CopyTo(Data, dest);
            }

            var party = GetData(Offsets.Party, SIZE_STOREDPARTY);
            var partyPL = new PokeList1(party, PokeListType.Party, Japanese);
            for (int i = 0; i < partyPL.Pokemon.Length; i++)
            {
                var dest = GetPartyOffset(i);
                var pkDat = i < partyPL.Count
                    ? new PokeList1(partyPL[i]).Write()
                    : new byte[PokeList1.GetDataLength(PokeListType.Single, Japanese)];
                pkDat.CopyTo(Data, dest);
            }

            Span<byte> rawDC = stackalloc byte[0x38];
            Data.AsSpan(Offsets.Daycare, rawDC.Length).CopyTo(rawDC);
            byte[] TempDaycare = new byte[PokeList1.GetDataLength(PokeListType.Single, Japanese)];
            TempDaycare[0] = rawDC[0];

            rawDC.Slice(1, StringLength).CopyTo(TempDaycare.AsSpan(2 + 1 + PokeCrypto.SIZE_1PARTY + StringLength));
            rawDC.Slice(1 + StringLength, StringLength).CopyTo(TempDaycare.AsSpan(2 + 1 + PokeCrypto.SIZE_1PARTY));
            rawDC.Slice(1 + (2 * StringLength), PokeCrypto.SIZE_1STORED).CopyTo(TempDaycare.AsSpan(2 + 1));

            PokeList1 daycareList = new(TempDaycare, PokeListType.Single, Japanese);
            daycareList.Write().CopyTo(Data, GetPartyOffset(7));
            DaycareOffset = GetPartyOffset(7);

            EventFlag = Offsets.EventFlag;

            // Enable Pokedex editing
            PokeDex = 0;
        }

        private readonly SAV1Offsets Offsets;

        // Event Flags
        protected override int EventFlagMax => 0xA00; // 320 * 8
        protected override int EventConstMax => 0;

        private const int SIZE_RESERVED = 0x8000; // unpacked box data

        protected override byte[] GetFinalData()
        {
            var capacity = Japanese ? PokeListType.StoredJP : PokeListType.Stored;
            for (int i = 0; i < BoxCount; i++)
            {
                var boxPL = new PokeList1(capacity, Japanese);
                int slot = 0;
                for (int j = 0; j < boxPL.Pokemon.Length; j++)
                {
                    PK1 boxPK = (PK1)GetPKM(GetData(GetBoxOffset(i) + (j * SIZE_STORED), SIZE_STORED));
                    if (boxPK.Species > 0)
                        boxPL[slot++] = boxPK;
                }

                // copy to box location
                var boxdata = boxPL.Write();
                int ofs = GetBoxRawDataOffset(i);
                SetData(boxdata, ofs);

                // copy to active loc if current box
                if (i == CurrentBox)
                    SetData(boxdata, Offsets.CurrentBox);
            }

            var partyPL = new PokeList1(PokeListType.Party, Japanese);
            int pSlot = 0;
            for (int i = 0; i < 6; i++)
            {
                PK1 partyPK = (PK1)GetPKM(GetData(GetPartyOffset(i), SIZE_STORED));
                if (partyPK.Species > 0)
                    partyPL[pSlot++] = partyPK;
            }
            partyPL.Write().CopyTo(Data, Offsets.Party);

            // Daycare is read-only, but in case it ever becomes editable, copy it back in.
            Span<byte> rawDC = Data.AsSpan(GetDaycareSlotOffset(loc: 0, slot: 0), SIZE_STORED);
            Span<byte> dc = stackalloc byte[1 + (2 * StringLength) + PokeCrypto.SIZE_1STORED];
            dc[0] = IsDaycareOccupied(0, 0) == true ? (byte)1 : (byte)0;
            rawDC.Slice(2 + 1 + PokeCrypto.SIZE_1PARTY + StringLength, StringLength).CopyTo(dc[1..]);
            rawDC.Slice(2 + 1 + PokeCrypto.SIZE_1PARTY, StringLength).CopyTo(dc[(1 + StringLength)..]);
            rawDC.Slice(2 + 1, PokeCrypto.SIZE_1STORED).CopyTo(dc[(1 + (2 * StringLength))..]);
            dc.CopyTo(Data.AsSpan(Offsets.Daycare));

            SetChecksums();
            return Data.AsSpan()[..^SIZE_RESERVED].ToArray();
        }

        private int GetBoxRawDataOffset(int box)
        {
            if (box < BoxCount / 2)
                return 0x4000 + (box * SIZE_STOREDBOX);
            return 0x6000 + ((box - (BoxCount / 2)) * SIZE_STOREDBOX);
        }

        // Configuration
        protected override SaveFile CloneInternal() => new SAV1(Write(), Version);

        protected override int SIZE_STORED => Japanese ? PokeCrypto.SIZE_1JLIST : PokeCrypto.SIZE_1ULIST;
        protected override int SIZE_PARTY => Japanese ? PokeCrypto.SIZE_1JLIST : PokeCrypto.SIZE_1ULIST;
        private int SIZE_BOX => BoxSlotCount*SIZE_STORED;
        private int SIZE_STOREDBOX => PokeList1.GetDataLength(Japanese ? PokeListType.StoredJP : PokeListType.Stored, Japanese);
        private int SIZE_STOREDPARTY => PokeList1.GetDataLength(PokeListType.Party, Japanese);

        public override PKM BlankPKM => new PK1(Japanese);
        public override Type PKMType => typeof(PK1);

        public override int MaxMoveID => Legal.MaxMoveID_1;
        public override int MaxSpeciesID => Legal.MaxSpeciesID_1;
        public override int MaxAbilityID => Legal.MaxAbilityID_1;
        public override int MaxItemID => Legal.MaxItemID_1;
        public override int MaxBallID => 0; // unused
        public override int MaxGameID => 99; // unused
        public override int MaxMoney => 999999;
        public override int MaxCoins => 9999;

        public override int BoxCount => Japanese ? 8 : 12;
        public override int MaxEV => 65535;
        public override int MaxIV => 15;
        public override int Generation => 1;
        protected override int GiftCountMax => 0;
        public override int OTLength => Japanese ? 5 : 7;
        public override int NickLength => Japanese ? 5 : 10;
        public override int BoxSlotCount => Japanese ? 30 : 20;

        public override bool HasParty => true;
        private int StringLength => Japanese ? GBPKML.StringLengthJapanese : GBPKML.StringLengthNotJapan;

        public override bool IsPKMPresent(ReadOnlySpan<byte> data) => PKX.IsPKMPresentGB(data);

        // Checksums
        protected override void SetChecksums() => Data[Offsets.ChecksumOfs] = GetRBYChecksum(Offsets.OT, Offsets.ChecksumOfs);
        public override bool ChecksumsValid => Data[Offsets.ChecksumOfs] == GetRBYChecksum(Offsets.OT, Offsets.ChecksumOfs);
        public override string ChecksumInfo => ChecksumsValid ? "Checksum valid." : "Checksum invalid";

        private byte GetRBYChecksum(int start, int end)
        {
            byte chksum = 0;
            for (int i = start; i < end; i++)
                chksum += Data[i];
            chksum ^= 0xFF;
            return chksum;
        }

        // Trainer Info
        public override GameVersion Version { get; protected set; }

        public override string OT
        {
            get => GetString(Offsets.OT, OTLength);
            set => SetString(Data.AsSpan(Offsets.OT, OTLength), value.AsSpan(), OTLength, StringConverterOption.Clear50);
        }

        public Span<byte> OT_Trash { get => Data.AsSpan(Offsets.OT, StringLength); set { if (value.Length == StringLength) value.CopyTo(Data.AsSpan(Offsets.OT)); } }

        public override int Gender
        {
            get => 0;
            set { }
        }

        public override int TID
        {
            get => ReadUInt16BigEndian(Data.AsSpan(Offsets.TID));
            set => WriteUInt16BigEndian(Data.AsSpan(Offsets.TID), (ushort)value);
        }

        public override int SID { get => 0; set { } }

        public string Rival
        {
            get => GetString(Offsets.Rival, OTLength);
            set => SetString(Data.AsSpan(Offsets.Rival, OTLength), value.AsSpan(), OTLength, StringConverterOption.Clear50);
        }

        public Span<byte> Rival_Trash { get => Data.AsSpan(Offsets.Rival, StringLength); set { if (value.Length == StringLength) value.CopyTo(Data.AsSpan(Offsets.Rival)); } }

        public bool Yellow => Starter == 0x54; // Pikachu
        public int Starter => Data[Offsets.Starter];

        public byte PikaFriendship
        {
            get => Data[Offsets.PikaFriendship];
            set => Data[Offsets.PikaFriendship] = value;
        }

        public int PikaBeachScore
        {
            get => BinaryCodedDecimal.ToInt32LE(Data.AsSpan(Offsets.PikaBeachScore, 2));
            set => BinaryCodedDecimal.WriteBytesLE(Data.AsSpan(Offsets.PikaBeachScore, 2), Math.Min(9999, value));
        }

        public override string PlayTimeString => !PlayedMaximum ? base.PlayTimeString : $"{base.PlayTimeString} {Checksums.CRC16_CCITT(Data):X4}";

        public override int PlayedHours
        {
            get => Data[Offsets.PlayTime + 0];
            set
            {
                if (value >= byte.MaxValue) // Set 255:00:00.00 and flag
                {
                    PlayedMaximum = true;
                    value = byte.MaxValue;
                    PlayedMinutes = PlayedSeconds = PlayedFrames = 0;
                }
                Data[Offsets.PlayTime + 0] = (byte) value;
            }
        }

        public bool PlayedMaximum
        {
            get => Data[Offsets.PlayTime + 1] != 0;
            set => Data[Offsets.PlayTime + 1] = value ? (byte)1 : (byte)0;
        }

        public override int PlayedMinutes
        {
            get => Data[Offsets.PlayTime + 2];
            set => Data[Offsets.PlayTime + 2] = (byte)value;
        }

        public override int PlayedSeconds
        {
            get => Data[Offsets.PlayTime + 3];
            set => Data[Offsets.PlayTime + 3] = (byte)value;
        }

        public int PlayedFrames
        {
            get => Data[Offsets.PlayTime + 4];
            set => Data[Offsets.PlayTime + 4] = (byte)value;
        }

        public int Badges
        {
            get => Data[Offsets.Badges];
            set { if (value < 0) return; Data[Offsets.Badges] = (byte)value; }
        }

        private byte Options
        {
            get => Data[Offsets.Options];
            set => Data[Offsets.Options] = value;
        }

        public bool BattleEffects
        {
            get => (Options & 0x80) == 0;
            set => Options = (byte)((Options & 0x7F) | (value ? 0 : 0x80));
        }

        public bool BattleStyleSwitch
        {
            get => (Options & 0x40) == 0;
            set => Options = (byte)((Options & 0xBF) | (value ? 0 : 0x40));
        }

        public int Sound
        {
            get => (Options & 0x30) >> 4;
            set => Options = (byte)((Options & 0xCF) | ((value & 3) << 4));
        }

        public int TextSpeed
        {
            get => Options & 0x7;
            set => Options = (byte)((Options & 0xF8) | (value & 7));
        }

        // yellow only
        public byte GBPrinterBrightness { get => Data[Offsets.PrinterBrightness]; set => Data[Offsets.PrinterBrightness] = value; }

        public override uint Money
        {
            get => (uint)BinaryCodedDecimal.ToInt32BE(Data.AsSpan(Offsets.Money, 3));
            set
            {
                value = (uint)Math.Min(value, MaxMoney);
                BinaryCodedDecimal.WriteBytesBE(Data.AsSpan(Offsets.Money, 3), (int)value);
            }
        }

        public uint Coin
        {
            get => (uint)BinaryCodedDecimal.ToInt32BE(Data.AsSpan(Offsets.Coin, 2));
            set
            {
                value = (ushort)Math.Min(value, MaxCoins);
                BinaryCodedDecimal.WriteBytesBE(Data.AsSpan(Offsets.Coin, 2), (int)value);
            }
        }

        private readonly ushort[] LegalItems = Legal.Pouch_Items_RBY;

        public override IReadOnlyList<InventoryPouch> Inventory
        {
            get
            {
                ushort[] legalItems = LegalItems;
                InventoryPouch[] pouch =
                {
                    new InventoryPouchGB(InventoryType.Items, legalItems, 99, Offsets.Items, 20),
                    new InventoryPouchGB(InventoryType.PCItems, legalItems, 99, Offsets.PCItems, 50),
                };
                return pouch.LoadAll(Data);
            }
            set => value.SaveAll(Data);
        }

        public override int GetDaycareSlotOffset(int loc, int slot)
        {
            return DaycareOffset;
        }

        public override uint? GetDaycareEXP(int loc, int slot)
        {
            return null;
        }

        public override bool? IsDaycareOccupied(int loc, int slot)
        {
            if (loc == 0 && slot == 0)
                return Data[Offsets.Daycare] == 0x01;
            return null;
        }

        public override void SetDaycareEXP(int loc, int slot, uint EXP)
        {
            // todo
        }

        public override void SetDaycareOccupied(int loc, int slot, bool occupied)
        {
            // todo
        }

        // Storage
        public override int PartyCount
        {
            get => Data[Offsets.Party];
            protected set => Data[Offsets.Party] = (byte)value;
        }

        public override int GetBoxOffset(int box)
        {
            return Data.Length - SIZE_RESERVED + (box * SIZE_BOX);
        }

        public override int GetPartyOffset(int slot)
        {
            return Data.Length - SIZE_RESERVED + (BoxCount * SIZE_BOX) + (slot * SIZE_STORED);
        }

        public override int CurrentBox
        {
            get => Data[Offsets.CurrentBoxIndex] & 0x7F;
            set => Data[Offsets.CurrentBoxIndex] = (byte)((Data[Offsets.CurrentBoxIndex] & 0x80) | (value & 0x7F));
        }

        public override string GetBoxName(int box)
        {
            return $"BOX {box + 1}";
        }

        public override void SetBoxName(int box, string value)
        {
            // Don't allow for custom box names
        }

        protected override PKM GetPKM(byte[] data)
        {
            if (data.Length == SIZE_STORED)
                return new PokeList1(data, PokeListType.Single, Japanese)[0];
            return new PK1(data);
        }

        protected override byte[] DecryptPKM(byte[] data)
        {
            return data;
        }

        // Pokédex
        protected override void SetDex(PKM pkm)
        {
            int species = pkm.Species;
            if (!CanSetDex(species))
                return;

            SetCaught(pkm.Species, true);
            SetSeen(pkm.Species, true);
        }

        private bool CanSetDex(int species)
        {
            if (species <= 0)
                return false;
            if (species > MaxSpeciesID)
                return false;
            if (Version == GameVersion.Invalid)
                return false;
            return true;
        }

        public override bool GetSeen(int species) => GetDexFlag(Offsets.DexSeen, species);
        public override bool GetCaught(int species) => GetDexFlag(Offsets.DexCaught, species);
        public override void SetSeen(int species, bool seen) => SetDexFlag(Offsets.DexSeen, species, seen);
        public override void SetCaught(int species, bool caught) => SetDexFlag(Offsets.DexCaught, species, caught);

        private bool GetDexFlag(int region, int species)
        {
            int bit = species - 1;
            int ofs = bit >> 3;
            return GetFlag(region + ofs, bit & 7);
        }

        private void SetDexFlag(int region, int species, bool value)
        {
            int bit = species - 1;
            int ofs = bit >> 3;
            SetFlag(region + ofs, bit & 7, value);
        }

        public override void WriteSlotFormatStored(PKM pkm, Span<byte> data, int offset)
        {
            // pkm that have never been boxed have yet to save the 'current level' for box indication
            // set this value at this time
            ((PK1)pkm).Stat_LevelBox = pkm.CurrentLevel;
            base.WriteSlotFormatStored(pkm, Data, offset);
        }

        public override void WriteBoxSlot(PKM pkm, Span<byte> data, int offset)
        {
            // pkm that have never been boxed have yet to save the 'current level' for box indication
            // set this value at this time
            ((PK1)pkm).Stat_LevelBox = pkm.CurrentLevel;
            base.WriteBoxSlot(pkm, Data, offset);
        }

        private const int SpawnFlagCount = 0xF0;

        public bool[] EventSpawnFlags
        {
            get
            {
                // RB uses 0xE4 (0xE8) flags, Yellow uses 0xF0 flags. Just grab 0xF0
                bool[] data = new bool[SpawnFlagCount];
                for (int i = 0; i < data.Length; i++)
                    data[i] = GetFlag(Offsets.ObjectSpawnFlags + (i >> 3), i & 7);
                return data;
            }
            set
            {
                if (value.Length != SpawnFlagCount)
                    return;
                for (int i = 0; i < value.Length; i++)
                    SetFlag(Offsets.ObjectSpawnFlags + (i >> 3), i & 7, value[i]);
            }
        }

        public override string GetString(ReadOnlySpan<byte> data) => StringConverter12.GetString(data, Japanese);

        public override int SetString(Span<byte> destBuffer, ReadOnlySpan<char> value, int maxLength, StringConverterOption option)
        {
            return StringConverter12.SetString(destBuffer, value, maxLength, Japanese, option);
        }
    }
}
