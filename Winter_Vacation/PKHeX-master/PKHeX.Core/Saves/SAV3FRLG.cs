﻿using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 5 <see cref="SaveFile"/> object for <see cref="GameVersion.FRLG"/>.
    /// </summary>
    /// <inheritdoc cref="SAV3" />
    public sealed class SAV3FRLG : SAV3, IGen3Joyful, IGen3Wonder
    {
        // Configuration
        protected override SaveFile CloneInternal() => new SAV3FRLG(Write());
        public override GameVersion Version { get; protected set; } = GameVersion.FR; // allow mutation
        private PersonalTable _personal = PersonalTable.FR;
        public override PersonalTable Personal => _personal;

        protected override int EventFlagMax => 8 * 288;
        protected override int EventConstMax => 0x100;
        protected override int DaycareSlotSize => SIZE_STORED + 0x3C; // 0x38 mail + 4 exp
        public override int DaycareSeedSize => 4; // 16bit
        protected override int EggEventFlag => 0x266;
        protected override int BadgeFlagStart => 0x820;

        public SAV3FRLG(bool japanese = false) : base(japanese) => Initialize();

        public SAV3FRLG(byte[] data) : base(data)
        {
            Initialize();

            // Fix save files that have an overflow corruption with the Pokédex.
            // Future loads of this save file will cause it to be recognized as FR/LG correctly.
            if (IsCorruptPokedexFF())
                WriteUInt32LittleEndian(Small.AsSpan(0xAC), 1);
        }

        private void Initialize()
        {
            // small
            PokeDex = 0x18;

            // large
            EventFlag = 0xEE0;
            EventConst = 0x1000;
            DaycareOffset = 0x2F80;

            // storage
            Box = 0;
        }

        public bool ResetPersonal(GameVersion g)
        {
            if (g is not (GameVersion.FR or GameVersion.LG))
                return false;
            _personal = g == GameVersion.FR ? PersonalTable.FR : PersonalTable.LG;
            Version = g;
            return true;
        }

        #region Small
        public override bool NationalDex
        {
            get => PokedexNationalMagicFRLG == PokedexNationalUnlockFRLG;
            set
            {
                PokedexNationalMagicFRLG = value ? PokedexNationalUnlockFRLG : (byte)0; // magic
                SetEventFlag(0x840, value);
                SetEventConst(0x4E, PokedexNationalUnlockWorkFRLG);
            }
        }

        public ushort JoyfulJumpInRow           { get => ReadUInt16LittleEndian(Small.AsSpan(0xB00)); set => WriteUInt16LittleEndian(Small.AsSpan(0xB00), Math.Min((ushort)9999, value)); }
        // u16 field2;
        public ushort JoyfulJump5InRow          { get => ReadUInt16LittleEndian(Small.AsSpan(0xB04)); set => WriteUInt16LittleEndian(Small.AsSpan(0xB04), Math.Min((ushort)9999, value)); }
        public ushort JoyfulJumpGamesMaxPlayers { get => ReadUInt16LittleEndian(Small.AsSpan(0xB06)); set => WriteUInt16LittleEndian(Small.AsSpan(0xB06), Math.Min((ushort)9999, value)); }
        // u32 field8;
        public uint   JoyfulJumpScore           { get => ReadUInt16LittleEndian(Small.AsSpan(0xB0C)); set => WriteUInt32LittleEndian(Small.AsSpan(0xB0C), Math.Min(9999, value)); }

        public uint   JoyfulBerriesScore        { get => ReadUInt16LittleEndian(Small.AsSpan(0xB10)); set => WriteUInt32LittleEndian(Small.AsSpan(0xB10), Math.Min(9999, value)); }
        public ushort JoyfulBerriesInRow        { get => ReadUInt16LittleEndian(Small.AsSpan(0xB14)); set => WriteUInt16LittleEndian(Small.AsSpan(0xB14), Math.Min((ushort)9999, value)); }
        public ushort JoyfulBerries5InRow       { get => ReadUInt16LittleEndian(Small.AsSpan(0xB16)); set => WriteUInt16LittleEndian(Small.AsSpan(0xB16), Math.Min((ushort)9999, value)); }

        public uint BerryPowder
        {
            get => ReadUInt32LittleEndian(Small.AsSpan(0xAF8)) ^ SecurityKey;
            set => WriteUInt32LittleEndian(Small.AsSpan(0xAF8), value ^ SecurityKey);
        }

        public override uint SecurityKey
        {
            get => ReadUInt32LittleEndian(Small.AsSpan(0xF20));
            set => WriteUInt32LittleEndian(Small.AsSpan(0xF20), value);
        }
        #endregion

        #region Large
        public override int PartyCount { get => Large[0x034]; protected set => Large[0x034] = (byte)value; }
        public override int GetPartyOffset(int slot) => 0x038 + (SIZE_PARTY * slot);

        public override uint Money
        {
            get => ReadUInt32LittleEndian(Large.AsSpan(0x0290)) ^ SecurityKey;
            set => WriteUInt32LittleEndian(Large.AsSpan(0x0290), value ^ SecurityKey);
        }

        public override uint Coin
        {
            get => (ushort)(ReadUInt16LittleEndian(Large.AsSpan(0x0294)) ^ SecurityKey);
            set => WriteUInt16LittleEndian(Large.AsSpan(0x0294), (ushort)(value ^ SecurityKey));
        }

        private const int OFS_PCItem = 0x0298;
        private const int OFS_PouchHeldItem = 0x0310;
        private const int OFS_PouchKeyItem = 0x03B8;
        private const int OFS_PouchBalls = 0x0430;
        private const int OFS_PouchTMHM = 0x0464;
        private const int OFS_PouchBerry = 0x054C;

        protected override InventoryPouch3[] GetItems()
        {
            const int max = 999;
            var PCItems = ArrayUtil.ConcatAll(Legal.Pouch_Items_RS, Legal.Pouch_Key_FRLG, Legal.Pouch_Ball_RS, Legal.Pouch_TMHM_RS, Legal.Pouch_Berries_RS);
            return new InventoryPouch3[]
            {
                new(InventoryType.Items, Legal.Pouch_Items_RS, max, OFS_PouchHeldItem, (OFS_PouchKeyItem - OFS_PouchHeldItem) / 4),
                new(InventoryType.KeyItems, Legal.Pouch_Key_FRLG, 1, OFS_PouchKeyItem, (OFS_PouchBalls - OFS_PouchKeyItem) / 4),
                new(InventoryType.Balls, Legal.Pouch_Ball_RS, max, OFS_PouchBalls, (OFS_PouchTMHM - OFS_PouchBalls) / 4),
                new(InventoryType.TMHMs, Legal.Pouch_TMHM_RS, max, OFS_PouchTMHM, (OFS_PouchBerry - OFS_PouchTMHM) / 4),
                new(InventoryType.Berries, Legal.Pouch_Berries_RS, 999, OFS_PouchBerry, 43),
                new(InventoryType.PCItems, PCItems, 999, OFS_PCItem, (OFS_PouchHeldItem - OFS_PCItem) / 4),
            };
        }

        protected override int SeenOffset2 => 0x5F8;
        protected override int MailOffset => 0x2CD0;

        protected override int GetDaycareEXPOffset(int slot) => GetDaycareSlotOffset(0, slot + 1) - 4; // @ end of each pkm slot
        public override string GetDaycareRNGSeed(int loc) => ReadUInt16LittleEndian(Large.AsSpan(GetDaycareEXPOffset(2))).ToString("X4"); // after the 2nd slot EXP, before the step counter
        public override void SetDaycareRNGSeed(int loc, string seed) => WriteUInt16LittleEndian(Large.AsSpan(GetDaycareEXPOffset(2)), (ushort)Util.GetHexValue(seed));

        protected override int ExternalEventData => 0x30A7;

        #region eBerry
        private const int OFFSET_EBERRY = 0x30EC;
        private const int SIZE_EBERRY = 0x134;

        public byte[] GetEReaderBerry() => Large.Slice(OFFSET_EBERRY, SIZE_EBERRY);
        public void SetEReaderBerry(byte[] data) => SetData(Large, data, OFFSET_EBERRY);

        public override string EBerryName => GetString(Large.AsSpan(OFFSET_EBERRY, 7));
        public override bool IsEBerryEngima => Large[OFFSET_EBERRY] is 0 or 0xFF;
        #endregion

        public int WonderOffset => WonderNewsOffset;
        private const int WonderNewsOffset = 0x3120;
        private const int WonderCardOffset = WonderNewsOffset + WonderNews3.SIZE;
        private const int WonderCardExtraOffset = WonderCardOffset + WonderCard3.SIZE;

        public WonderNews3 WonderNews { get => new(Large.Slice(WonderNewsOffset, WonderNews3.SIZE)); set => SetData(Large, value.Data, WonderOffset); }
        public WonderCard3 WonderCard { get => new(Large.Slice(WonderCardOffset, WonderCard3.SIZE)); set => SetData(Large, value.Data, WonderCardOffset); }
        public WonderCard3Extra WonderCardExtra { get => new(Large.Slice(WonderCardExtraOffset, WonderCard3Extra.SIZE)); set => SetData(Large, value.Data, WonderCardExtraOffset); }
        // 0x338: 4 easy chat words
        // 0x340: news MENewsJisanStruct
        // 0x344: uint[5], uint[5] tracking?

        public override MysteryEvent3 MysteryEvent
        {
            get => new(Large.Slice(0x361C, MysteryEvent3.SIZE));
            set => SetData(Large, value.Data, 0x361C);
        }

        protected override int SeenOffset3 => 0x3A18;

        public string RivalName
        {
            get => GetString(Large.AsSpan(0x3A4C, 8));
            set => SetString(Large.AsSpan(0x3A4C, 8), value.AsSpan(), 7, StringConverterOption.ClearZero);
        }

        #endregion
    }
}
