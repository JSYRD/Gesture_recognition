﻿using System;
using System.Collections.Generic;
using static System.Buffers.Binary. BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 7 Mystery Gift Template File
    /// </summary>
    public sealed class WB7 : DataMysteryGift, ILangNick, IAwakened, INature, ILangNicknamedTemplate
    {
        public const int Size = 0x108;
        public const int SizeFull = 0x310;
        private const int CardStart = SizeFull - Size;

        public override int Generation => 7;

        public WB7() : this(new byte[SizeFull]) { }
        public WB7(byte[] data) : base(data) { }

        public override GameVersion Version { get => GameVersion.GG; set { } }

        public byte RestrictVersion { get => Data[0]; set => Data[0] = value; }

        public bool CanBeReceivedByVersion(int v)
        {
            if (v is not ((int)GameVersion.GP or (int)GameVersion.GE))
                return false;
            if (RestrictVersion == 0)
                return true; // no data
            var bitIndex = v - (int)GameVersion.GP;
            var bit = 1 << bitIndex;
            return (RestrictVersion & bit) != 0;
        }

        // General Card Properties
        public override int CardID
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0));
            set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0), (ushort)value);
        }

        public override string CardTitle
        {
            // Max len 36 char, followed by null terminator
            get => StringConverter8.GetString(Data.AsSpan(CardStart + 2, 0x4A));
            set => StringConverter8.SetString(Data.AsSpan(CardStart + 2, 0x4A), value.AsSpan(), 36, StringConverterOption.ClearZero);
        }

        private uint RawDate
        {
            get => ReadUInt32LittleEndian(Data.AsSpan(CardStart + 0x4C));
            set => WriteUInt32LittleEndian(Data.AsSpan(CardStart + 0x4C), value);
        }

        private uint Year
        {
            get => (RawDate / 10000) + 2000;
            set => RawDate = SetDate(value, Month, Day);
        }

        private uint Month
        {
            get => RawDate % 10000 / 100;
            set => RawDate = SetDate(Year, value, Day);
        }

        private uint Day
        {
            get => RawDate % 100;
            set => RawDate = SetDate(Year, Month, value);
        }

        private static uint SetDate(uint year, uint month, uint day) => (Math.Max(0, year - 2000) * 10000) + (month * 100) + day;

        /// <summary>
        /// Gets or sets the date of the card.
        /// </summary>
        public DateTime? Date
        {
            get
            {
                // Check to see if date is valid
                if (!DateUtil.IsDateValid(Year, Month, Day))
                    return null;

                return new DateTime((int)Year, (int)Month, (int)Day);
            }
            set
            {
                if (value.HasValue)
                {
                    // Only update the properties if a value is provided.
                    Year = (ushort)value.Value.Year;
                    Month = (byte)value.Value.Month;
                    Day = (byte)value.Value.Day;
                }
                else
                {
                    // Clear the Met Date.
                    // If code tries to access MetDate again, null will be returned.
                    Year = 0;
                    Month = 0;
                    Day = 0;
                }
            }
        }

        public int CardLocation { get => Data[CardStart + 0x50]; set => Data[CardStart + 0x50] = (byte)value; }

        public int CardType { get => Data[CardStart + 0x51]; set => Data[CardStart + 0x51] = (byte)value; }
        public byte CardFlags { get => Data[CardStart + 0x52]; set => Data[CardStart + 0x52] = value; }

        public bool GiftRepeatable { get => (CardFlags & 1) == 0; set => CardFlags = (byte)((CardFlags & ~1) | (value ? 0 : 1)); }
        public override bool GiftUsed { get => (CardFlags & 2) == 2; set => CardFlags = (byte)((CardFlags & ~2) | (value ? 2 : 0)); }
        public bool GiftOncePerDay { get => (CardFlags & 4) == 4; set => CardFlags = (byte)((CardFlags & ~4) | (value ? 4 : 0)); }

        public bool MultiObtain { get => Data[CardStart + 0x53] == 1; set => Data[CardStart + 0x53] = value ? (byte)1 : (byte)0; }

        // Item Properties
        public override bool IsItem { get => CardType == 1; set { if (value) CardType = 1; } }
        public override int ItemID { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x68)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x68), (ushort)value); }
        public int GetItem(int index) => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x68 + (0x4 * index)));
        public void SetItem(int index, ushort item) => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x68 + (4 * index)), item);
        public int GetQuantity(int index) => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x6A + (0x4 * index)));
        public void SetQuantity(int index, ushort quantity) => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x6A + (4 * index)), quantity);

        public override int Quantity
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x6A));
            set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x6A), (ushort)value);
        }

        // Pokémon Properties
        public override bool IsPokémon { get => CardType == 0; set { if (value) CardType = 0; } }
        public override bool IsShiny => PIDType == Shiny.Always;
        public override Shiny Shiny => PIDType;

        public override int TID
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x68));
            set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x68), (ushort)value);
        }

        public override int SID {
            get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x6A));
            set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x6A), (ushort)value);
        }

        public int OriginGame
        {
            get => ReadInt32LittleEndian(Data.AsSpan(CardStart + 0x6C));
            set => WriteInt32LittleEndian(Data.AsSpan(CardStart + 0x6C), value);
        }

        public uint EncryptionConstant {
            get => ReadUInt32LittleEndian(Data.AsSpan(CardStart + 0x70));
            set => WriteUInt32LittleEndian(Data.AsSpan(CardStart + 0x70), value);
        }

        public override int Ball
        {
            get => Data[CardStart + 0x76];
            set => Data[CardStart + 0x76] = (byte)value; }

        public override int HeldItem // no references
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x78));
            set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x78), (ushort)value);
        }

        public int Move1 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x7A)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x7A), (ushort)value); }
        public int Move2 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x7C)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x7C), (ushort)value); }
        public int Move3 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x7E)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x7E), (ushort)value); }
        public int Move4 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x80)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x80), (ushort)value); }
        public override int Species { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0x82)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0x82), (ushort)value); }
        public override int Form { get => Data[CardStart + 0x84]; set => Data[CardStart + 0x84] = (byte)value; }

        // public int Language { get => Data[CardStart + 0x85]; set => Data[CardStart + 0x85] = (byte)value; }

        // public string Nickname
        // {
        //     get => Util.TrimFromZero(Encoding.Unicode.GetString(Data, CardStart + 0x86, 0x1A));
        //     set => Encoding.Unicode.GetBytes(value.PadRight(12 + 1, '\0')).CopyTo(Data, CardStart + 0x86);
        // }

        public int Nature { get => (sbyte)Data[CardStart + 0xA0]; set => Data[CardStart + 0xA0] = (byte)value; }
        public override int Gender { get => Data[CardStart + 0xA1]; set => Data[CardStart + 0xA1] = (byte)value; }
        public override int AbilityType { get => 3; set => Data[CardStart + 0xA2] = (byte)value; } // no references, always ability 0/1
        public Shiny PIDType { get => (Shiny)Data[CardStart + 0xA3]; set => Data[CardStart + 0xA3] = (byte)value; }
        public override int EggLocation { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xA4)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xA4), (ushort)value); }
        public int MetLocation  { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xA6)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xA6), (ushort)value); }
        public int MetLevel { get => Data[CardStart + 0xA8]; set => Data[CardStart + 0xA8] = (byte)value; }

        public int IV_HP { get => Data[CardStart + 0xAF]; set => Data[CardStart + 0xAF] = (byte)value; }
        public int IV_ATK { get => Data[CardStart + 0xB0]; set => Data[CardStart + 0xB0] = (byte)value; }
        public int IV_DEF { get => Data[CardStart + 0xB1]; set => Data[CardStart + 0xB1] = (byte)value; }
        public int IV_SPE { get => Data[CardStart + 0xB2]; set => Data[CardStart + 0xB2] = (byte)value; }
        public int IV_SPA { get => Data[CardStart + 0xB3]; set => Data[CardStart + 0xB3] = (byte)value; }
        public int IV_SPD { get => Data[CardStart + 0xB4]; set => Data[CardStart + 0xB4] = (byte)value; }

        public int OTGender { get => Data[CardStart + 0xB5]; set => Data[CardStart + 0xB5] = (byte)value; }

        // public override string OT_Name
        // {
        //     get => Util.TrimFromZero(Encoding.Unicode.GetString(Data, CardStart + 0xB6, 0x1A));
        //     set => Encoding.Unicode.GetBytes(value.PadRight(value.Length + 1, '\0')).CopyTo(Data, CardStart + 0xB6);
        // }

        public override int Level { get => Data[CardStart + 0xD0]; set => Data[CardStart + 0xD0] = (byte)value; }
        public override bool IsEgg { get => Data[CardStart + 0xD1] == 1; set => Data[CardStart + 0xD1] = value ? (byte)1 : (byte)0; }
        public ushort AdditionalItem { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xD2)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xD2), value); }

        public uint PID { get => ReadUInt32LittleEndian(Data.AsSpan(0xD4)); set => WriteUInt32LittleEndian(Data.AsSpan(0xD4), value); }
        public int RelearnMove1 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xD8)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xD8), (ushort)value); }
        public int RelearnMove2 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xDA)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xDA), (ushort)value); }
        public int RelearnMove3 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xDC)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xDC), (ushort)value); }
        public int RelearnMove4 { get => ReadUInt16LittleEndian(Data.AsSpan(CardStart + 0xDE)); set => WriteUInt16LittleEndian(Data.AsSpan(CardStart + 0xDE), (ushort)value); }

        public int AV_HP  { get => Data[CardStart + 0xE5]; set => Data[CardStart + 0xE5] = (byte)value; }
        public int AV_ATK { get => Data[CardStart + 0xE6]; set => Data[CardStart + 0xE6] = (byte)value; }
        public int AV_DEF { get => Data[CardStart + 0xE7]; set => Data[CardStart + 0xE7] = (byte)value; }
        public int AV_SPE { get => Data[CardStart + 0xE8]; set => Data[CardStart + 0xE8] = (byte)value; }
        public int AV_SPA { get => Data[CardStart + 0xE9]; set => Data[CardStart + 0xE9] = (byte)value; }
        public int AV_SPD { get => Data[CardStart + 0xEA]; set => Data[CardStart + 0xEA] = (byte)value; }

        // Meta Accessible Properties
        public override int[] IVs
        {
            get => new[] { IV_HP, IV_ATK, IV_DEF, IV_SPE, IV_SPA, IV_SPD };
            set
            {
                if (value.Length != 6) return;
                IV_HP = value[0]; IV_ATK = value[1]; IV_DEF = value[2];
                IV_SPE = value[3]; IV_SPA = value[4]; IV_SPD = value[5];
            }
        }

        public bool GetIsNicknamed(int language) => Data[GetNicknameOffset(language)] != 0;

        private static int GetLanguageIndex(int language)
        {
            var lang = (LanguageID) language;
            if (lang is < LanguageID.Japanese or LanguageID.UNUSED_6 or > LanguageID.ChineseT)
                return (int) LanguageID.English; // fallback
            return lang < LanguageID.UNUSED_6 ? language - 1 : language - 2;
        }

        public override int Location { get => MetLocation; set => MetLocation = (ushort)value; }

        public override IReadOnlyList<int> Moves
        {
            get => new[] { Move1, Move2, Move3, Move4 };
            set
            {
                if (value.Count > 0) Move1 = value[0];
                if (value.Count > 1) Move2 = value[1];
                if (value.Count > 2) Move3 = value[2];
                if (value.Count > 3) Move4 = value[3];
            }
        }

        public override IReadOnlyList<int> Relearn
        {
            get => new[] { RelearnMove1, RelearnMove2, RelearnMove3, RelearnMove4 };
            set
            {
                if (value.Count > 0) RelearnMove1 = value[0];
                if (value.Count > 1) RelearnMove2 = value[1];
                if (value.Count > 2) RelearnMove3 = value[2];
                if (value.Count > 3) RelearnMove4 = value[3];
            }
        }

        public override string OT_Name { get; set; } = string.Empty;
        public string Nickname => string.Empty;
        public bool IsNicknamed => false;
        public int Language => 2;

        public int GetLanguage(int redeemLanguage)
        {
            var languageOffset = GetLanguageIndex(redeemLanguage);
            var value = Data[0x1D8 + languageOffset];
            if (value != 0) // Fixed receiving language
                return value;

            // Use redeeming language (clamped to legal values for our sake)
            if (redeemLanguage is < (int)LanguageID.Japanese or (int)LanguageID.UNUSED_6 or > (int)LanguageID.ChineseT)
                return (int)LanguageID.English; // fallback
            return redeemLanguage;
        }

        public string GetNickname(int language) => StringConverter8.GetString(Data.AsSpan(GetNicknameOffset(language), 0x1A));
        public void SetNickname(int language, string value) => StringConverter8.SetString(Data.AsSpan(GetNicknameOffset(language), 0x1A), value.AsSpan(), 12, StringConverterOption.ClearZero);

        public string GetOT(int language) => StringConverter8.GetString(Data.AsSpan(GetOTOffset(language), 0x1A));
        public void SetOT(int language, string value) => StringConverter8.SetString(Data.AsSpan(GetOTOffset(language), 0x1A), value.AsSpan(), 12, StringConverterOption.ClearZero);

        private static int GetNicknameOffset(int language)
        {
            int index = GetLanguageIndex(language);
            return 0x04 + (index * 0x1A);
        }

        private static int GetOTOffset(int language)
        {
            int index = GetLanguageIndex(language);
            return 0xEE + (index * 0x1A);
        }

        public override PKM ConvertToPKM(ITrainerInfo sav, EncounterCriteria criteria)
        {
            if (!IsPokémon)
                throw new ArgumentException(nameof(IsPokémon));

            var rnd = Util.Rand;

            int currentLevel = Level > 0 ? Level : rnd.Next(1, 101);
            int metLevel = MetLevel > 0 ? MetLevel : currentLevel;
            var pi = PersonalTable.GG.GetFormEntry(Species, Form);

            var redeemLanguage = sav.Language;
            var language = GetLanguage(redeemLanguage);
            var OT = GetOT(redeemLanguage);
            bool isRedeemHT = OT.Length != 0;

            var pk = new PB7
            {
                Species = Species,
                HeldItem = HeldItem,
                TID = TID,
                SID = SID,
                Met_Level = metLevel,
                Form = Form,
                EncryptionConstant = EncryptionConstant != 0 ? EncryptionConstant : Util.Rand32(),
                Version = OriginGame != 0 ? OriginGame : sav.Game,
                Language = language,
                Ball = Ball,
                Move1 = Move1,
                Move2 = Move2,
                Move3 = Move3,
                Move4 = Move4,
                RelearnMove1 = RelearnMove1,
                RelearnMove2 = RelearnMove2,
                RelearnMove3 = RelearnMove3,
                RelearnMove4 = RelearnMove4,
                Met_Location = MetLocation,
                Egg_Location = EggLocation,
                AV_HP = AV_HP,
                AV_ATK = AV_ATK,
                AV_DEF = AV_DEF,
                AV_SPE = AV_SPE,
                AV_SPA = AV_SPA,
                AV_SPD = AV_SPD,

                OT_Name = isRedeemHT ? OT : sav.OT,
                OT_Gender = OTGender != 3 ? OTGender % 2 : sav.Gender,
                CurrentHandler = isRedeemHT ? 1 : 0,

                EXP = Experience.GetEXP(currentLevel, pi.EXPGrowth),

                OT_Friendship = pi.BaseFriendship,
                FatefulEncounter = true,
            };

            if (isRedeemHT)
            {
                pk.HT_Name = sav.OT;
                pk.HT_Gender = sav.Gender;
            }

            pk.SetMaximumPPCurrent();

            if ((sav.Generation > Generation && OriginGame == 0) || !CanBeReceivedByVersion(pk.Version))
            {
                // give random valid game
                do { pk.Version = (int)GameVersion.GP + rnd.Next(2); }
                while (!CanBeReceivedByVersion(pk.Version));
            }

            if (OTGender == 3)
            {
                pk.TID = sav.TID;
                pk.SID = sav.SID;
            }

            pk.MetDate = Date ?? DateTime.Now;
            pk.IsNicknamed = GetIsNicknamed(redeemLanguage);
            pk.Nickname = pk.IsNicknamed ? GetNickname(redeemLanguage) : SpeciesName.GetSpeciesNameGeneration(Species, pk.Language, Generation);

            SetPINGA(pk, criteria);

            if (IsEgg)
                SetEggMetData(pk);
            pk.CurrentFriendship = pk.IsEgg ? pi.HatchCycles : pi.BaseFriendship;

            pk.HeightScalar = rnd.Next(0x100);
            pk.WeightScalar = rnd.Next(0x100);
            pk.ResetCalculatedValues(); // cp & dimensions

            pk.RefreshChecksum();
            return pk;
        }

        private void SetEggMetData(PKM pk)
        {
            pk.IsEgg = true;
            pk.EggMetDate = Date;
            pk.Nickname = SpeciesName.GetSpeciesNameGeneration(0, pk.Language, Generation);
            pk.IsNicknamed = true;
        }

        private void SetPINGA(PKM pk, EncounterCriteria criteria)
        {
            var pi = PersonalTable.GG.GetFormEntry(Species, Form);
            pk.Nature = (int)criteria.GetNature((Nature)Nature);
            pk.Gender = criteria.GetGender(Gender, pi);
            var av = GetAbilityIndex(criteria);
            pk.RefreshAbility(av);
            SetPID(pk);
            SetIVs(pk);
        }

        private int GetAbilityIndex(EncounterCriteria criteria) => AbilityType switch
        {
            00 or 01 or 02 => AbilityType, // Fixed 0/1/2
            03 or 04 => criteria.GetAbilityFromNumber(Ability), // 0/1 or 0/1/H
            _ => throw new ArgumentOutOfRangeException(nameof(AbilityType)),
        };

        public override AbilityPermission Ability => AbilityType switch
        {
            0 => AbilityPermission.OnlyFirst,
            1 => AbilityPermission.OnlySecond,
            2 => AbilityPermission.OnlyHidden,
            3 => AbilityPermission.Any12,
            _ => AbilityPermission.Any12H,
        };

        private void SetPID(PKM pk)
        {
            switch (PIDType)
            {
                case Shiny.FixedValue: // Specified
                    pk.PID = PID;
                    break;
                case Shiny.Random: // Random
                    pk.PID = Util.Rand32();
                    break;
                case Shiny.Always: // Random Shiny
                    pk.PID = Util.Rand32();
                    pk.PID = (uint)(((pk.TID ^ pk.SID ^ (pk.PID & 0xFFFF)) << 16) | (pk.PID & 0xFFFF));
                    break;
                case Shiny.Never: // Random Nonshiny
                    pk.PID = Util.Rand32();
                    if (pk.IsShiny) pk.PID ^= 0x10000000;
                    break;
            }
        }

        private void SetIVs(PKM pk)
        {
            Span<int> finalIVs = stackalloc int[6];
            var ivflag = Array.Find(IVs, iv => (byte)(iv - 0xFC) < 3);
            var rng = Util.Rand;
            if (ivflag == 0) // Random IVs
            {
                for (int i = 0; i < 6; i++)
                    finalIVs[i] = IVs[i] > 31 ? rng.Next(32) : IVs[i];
            }
            else // 1/2/3 perfect IVs
            {
                int IVCount = ivflag - 0xFB;
                do { finalIVs[rng.Next(6)] = 31; }
                while (finalIVs.Count(31) < IVCount);
                for (int i = 0; i < 6; i++)
                    finalIVs[i] = finalIVs[i] == 31 ? 31 : rng.Next(32);
            }
            pk.SetIVs(finalIVs);
        }

        public bool CanHaveLanguage(int language)
        {
            if (language is < (int) LanguageID.Japanese or > (int) LanguageID.ChineseT)
                return false;

            if (CanBeAnyLanguage())
                return true;

            return Array.IndexOf(Data, (byte)language, 0x1D8, 9) >= 0;
        }

        public bool CanBeAnyLanguage()
        {
            for (int i = 0; i < 9; i++)
            {
                if (Data[0x1D8 + i] != 0)
                    return false;
            }
            return true;
        }

        public bool CanHandleOT(int language) => string.IsNullOrEmpty(GetOT(language));

        public override bool IsMatchExact(PKM pkm, DexLevel evo)
        {
            if (pkm.Egg_Location == 0) // Not Egg
            {
                if (OTGender != 3)
                {
                    if (SID != pkm.SID) return false;
                    if (TID != pkm.TID) return false;
                    if (OTGender != pkm.OT_Gender) return false;
                }
                var OT = GetOT(pkm.Language);
                if (!string.IsNullOrEmpty(OT) && OT != pkm.OT_Name) return false;
                if (OriginGame != 0 && OriginGame != pkm.Version) return false;
                if (EncryptionConstant != 0 && EncryptionConstant != pkm.EncryptionConstant) return false;

                if (!CanBeAnyLanguage() && !CanHaveLanguage(pkm.Language))
                    return false;
            }

            if (Form != evo.Form && !FormInfo.IsFormChangeable(Species, Form, pkm.Form, pkm.Format))
                return false;

            if (IsEgg)
            {
                if (EggLocation != pkm.Egg_Location) // traded
                {
                    if (pkm.Egg_Location != Locations.LinkTrade6)
                        return false;
                }
                else if (PIDType == 0 && pkm.IsShiny)
                {
                    return false; // can't be traded away for unshiny
                }

                if (pkm.IsEgg && !pkm.IsNative)
                    return false;
            }
            else
            {
                if (!PIDType.IsValid(pkm)) return false;
                if (EggLocation != pkm.Egg_Location) return false;
                if (MetLocation != pkm.Met_Location) return false;
            }

            if (MetLevel != pkm.Met_Level) return false;
            if (Ball != pkm.Ball) return false;
            if (OTGender < 3 && OTGender != pkm.OT_Gender) return false;
            if (Nature != -1 && pkm.Nature != Nature) return false;
            if (Gender != 3 && Gender != pkm.Gender) return false;

            if (pkm is IAwakened s && s.IsAwakeningBelow(this))
                return false;

            return true;
        }

        protected override bool IsMatchDeferred(PKM pkm) => Species != pkm.Species;
        protected override bool IsMatchPartial(PKM pkm) => false;
    }
}
