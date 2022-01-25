﻿using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 5 <see cref="SaveFile"/> object.
    /// </summary>
    public abstract class SAV5 : SaveFile, ISaveBlock5BW
    {
        protected override PKM GetPKM(byte[] data) => new PK5(data);
        protected override byte[] DecryptPKM(byte[] data) => PokeCrypto.DecryptArray45(data);

        protected internal override string ShortSummary => $"{OT} ({(GameVersion)Game}) - {PlayTimeString}";
        public override string Extension => ".sav";

        public override IReadOnlyList<ushort> HeldItems => Legal.HeldItems_BW;
        protected override int SIZE_STORED => PokeCrypto.SIZE_5STORED;
        protected override int SIZE_PARTY => PokeCrypto.SIZE_5PARTY;
        public override PKM BlankPKM => new PK5();
        public override Type PKMType => typeof(PK5);

        public override int BoxCount => 24;
        public override int MaxEV => 255;
        public override int Generation => 5;
        public override int OTLength => 7;
        public override int NickLength => 10;
        protected override int GiftCountMax => 12;

        public override int MaxMoveID => Legal.MaxMoveID_5;
        public override int MaxSpeciesID => Legal.MaxSpeciesID_5;
        public override int MaxAbilityID => Legal.MaxAbilityID_5;
        public override int MaxBallID => Legal.MaxBallID_5;
        public override int MaxGameID => Legal.MaxGameID_5; // B2

        protected SAV5(int size) : base(size)
        {
            Initialize();
            ClearBoxes();
        }

        protected SAV5(byte[] data) : base(data)
        {
            Initialize();
        }

        public override GameVersion Version
        {
            get => (GameVersion)Game;
            protected set => Game = (int)value;
        }

        private void Initialize()
        {
            Box = 0x400;
            Party = 0x18E00;
            AdventureInfo = 0x1D900;
        }

        // Blocks & Offsets
        protected override void SetChecksums() => AllBlocks.SetChecksums(Data);
        public override bool ChecksumsValid => AllBlocks.GetChecksumsValid(Data);
        public override string ChecksumInfo => AllBlocks.GetChecksumInfo(Data);

        protected int CGearInfoOffset;
        protected int CGearDataOffset;
        protected int EntreeForestOffset;
        private int AdventureInfo;
        public abstract int GTS { get; }
        public abstract int Fused { get; }
        public int PGL => AllBlocks[35].Offset + 8; // Dream World Upload

        // Daycare
        public override int DaycareSeedSize => Daycare5.DaycareSeedSize;
        public override bool? IsDaycareOccupied(int loc, int slot) => Daycare.IsOccupied(slot);
        public override int GetDaycareSlotOffset(int loc, int slot) => Daycare.GetPKMOffset(slot);
        public override uint? GetDaycareEXP(int loc, int slot) => Daycare.GetEXP(slot);
        public override string GetDaycareRNGSeed(int loc) => Daycare.GetSeed()?.ToString("X16") ?? string.Empty;
        public override void SetDaycareEXP(int loc, int slot, uint EXP) => Daycare.SetEXP(slot, EXP);
        public override void SetDaycareOccupied(int loc, int slot, bool occupied) => Daycare.SetOccupied(slot, occupied);
        public override void SetDaycareRNGSeed(int loc, string seed) => Daycare.SetSeed(seed);

        // Storage
        public override int PartyCount
        {
            get => Data[Party + 4];
            protected set => Data[Party + 4] = (byte)value;
        }

        public override int GetBoxOffset(int box) => Box + (SIZE_STORED * box * 30) + (box * 0x10);
        public override int GetPartyOffset(int slot) => Party + 8 + (SIZE_PARTY * slot);

        protected override int GetBoxWallpaperOffset(int box) => BoxLayout.GetBoxWallpaperOffset(box);
        public override int GetBoxWallpaper(int box) => BoxLayout.GetBoxWallpaper(box);
        public override void SetBoxWallpaper(int box, int value) => BoxLayout.SetBoxWallpaper(box, value);
        public override string GetBoxName(int box) => BoxLayout[box];
        public override void SetBoxName(int box, string value) => BoxLayout[box] = value;
        public override int CurrentBox { get => BoxLayout.CurrentBox; set => BoxLayout.CurrentBox = value; }

        protected int BattleBoxOffset;

        public bool BattleBoxLocked
        {
            get => Data[BattleBoxOffset + 0x358] != 0; // wifi/live
            set => Data[BattleBoxOffset + 0x358] = value ? (byte)1 : (byte)0;
        }

        protected override void SetPKM(PKM pkm, bool isParty = false)
        {
            var pk5 = (PK5)pkm;
            // Apply to this Save File
            DateTime Date = DateTime.Now;
            if (pk5.Trade(OT, TID, SID, Gender, Date.Day, Date.Month, Date.Year))
                pkm.RefreshChecksum();
        }

        // Player Data
        public override string OT { get => PlayerData.OT; set => PlayerData.OT = value; }
        public override int TID { get => PlayerData.TID; set => PlayerData.TID = value; }
        public override int SID { get => PlayerData.SID; set => PlayerData.SID = value; }
        public override int Language { get => PlayerData.Language; set => PlayerData.Language = value; }
        public override int Game { get => PlayerData.Game; set => PlayerData.Game = value; }
        public override int Gender { get => PlayerData.Gender; set => PlayerData.Gender = value; }
        public override int PlayedHours { get => PlayerData.PlayedHours; set => PlayerData.PlayedHours = value; }
        public override int PlayedMinutes { get => PlayerData.PlayedMinutes; set => PlayerData.PlayedMinutes = value; }
        public override int PlayedSeconds { get => PlayerData.PlayedSeconds; set => PlayerData.PlayedSeconds = value; }
        public override uint Money { get => Misc.Money; set => Misc.Money = value; }
        public override uint SecondsToStart { get => ReadUInt32LittleEndian(Data.AsSpan(AdventureInfo + 0x34)); set => WriteUInt32LittleEndian(Data.AsSpan(AdventureInfo + 0x34), value); }
        public override uint SecondsToFame  { get => ReadUInt32LittleEndian(Data.AsSpan(AdventureInfo + 0x3C)); set => WriteUInt32LittleEndian(Data.AsSpan(AdventureInfo + 0x3C), value); }
        public override MysteryGiftAlbum GiftAlbum { get => Mystery.GiftAlbum; set => Mystery.GiftAlbum = (EncryptedMysteryGiftAlbum)value; }
        public override IReadOnlyList<InventoryPouch> Inventory { get => Items.Inventory; set => Items.Inventory = value; }

        protected override void SetDex(PKM pkm) => Zukan.SetDex(pkm);
        public override bool GetCaught(int species) => Zukan.GetCaught(species);
        public override bool GetSeen(int species) => Zukan.GetSeen(species);

        public sealed override string GetString(ReadOnlySpan<byte> data) => StringConverter5.GetString(data);

        public sealed override int SetString(Span<byte> destBuffer, ReadOnlySpan<char> value, int maxLength, StringConverterOption option)
        {
            return StringConverter5.SetString(destBuffer, value, maxLength, option);
        }

        // DLC
        private int CGearSkinInfoOffset => CGearInfoOffset + (this is SAV5B2W2 ? 0x10 : 0) + 0x24;

        private bool CGearSkinPresent
        {
            get => Data[CGearSkinInfoOffset + 2] == 1;
            set => Data[CGearSkinInfoOffset + 2] = Data[PlayerData.Offset + (this is SAV5B2W2 ? 0x6C : 0x54)] = value ? (byte)1 : (byte)0;
        }

        public byte[] CGearSkinData
        {
            get
            {
                if (CGearSkinPresent)
                    return Data.AsSpan(CGearDataOffset, CGearBackground.SIZE_CGB).ToArray();
                return new byte[CGearBackground.SIZE_CGB];
            }
            set
            {
                SetData(value, CGearDataOffset);

                ushort chk = Checksums.CRC16_CCITT(value);
                var footer = Data.AsSpan(CGearDataOffset + value.Length);

                WriteUInt16LittleEndian(footer, 1); // block updated once
                WriteUInt16LittleEndian(footer[2..], chk); // checksum
                WriteUInt16LittleEndian(footer[0x100..], chk);  // second checksum

                Span<byte> dlcfooter = stackalloc byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x14, 0x27, 0x00, 0x00, 0x27, 0x35, 0x05, 0x31, 0x00, 0x00 };
                dlcfooter.CopyTo(footer[0x102..]);

                ushort skinchkval = Checksums.CRC16_CCITT(footer[0x100..0x104]);
                WriteUInt16LittleEndian(footer[0x112..], skinchkval);

                // Indicate in the save file that data is present
                WriteUInt16LittleEndian(Data.AsSpan(0x19438), 0xC21E);

                WriteUInt16LittleEndian(Data.AsSpan(CGearSkinInfoOffset), chk);
                CGearSkinPresent = true;

                State.Edited = true;
            }
        }

        public EntreeForest EntreeData
        {
            get => new(GetData(EntreeForestOffset, 0x850));
            set => SetData(value.Write(), EntreeForestOffset);
        }

        public abstract IReadOnlyList<BlockInfo> AllBlocks { get; }
        public abstract MyItem Items { get; }
        public abstract Zukan5 Zukan { get; }
        public abstract Misc5 Misc { get; }
        public abstract MysteryBlock5 Mystery { get; }
        public abstract Daycare5 Daycare { get; }
        public abstract BoxLayout5 BoxLayout { get; }
        public abstract PlayerData5 PlayerData { get; }
        public abstract BattleSubway5 BattleSubway { get; }
        public abstract Entralink5 Entralink { get; }
        public abstract Musical5 Musical { get; }
        public abstract Encount5 Encount { get; }

        public static int GetMailOffset(int index) => (index * Mail5.SIZE) + 0x1DD00;
        public byte[] GetMailData(int offset) => GetData(offset, Mail5.SIZE);
        public int GetBattleBoxSlot(int slot) => BattleBoxOffset + (slot * SIZE_STORED);

        public Mail GetMail(int mailIndex)
        {
            int ofs = GetMailOffset(mailIndex);
            var data = GetMailData(ofs);
            return new Mail5(data, ofs);
        }
    }
}
