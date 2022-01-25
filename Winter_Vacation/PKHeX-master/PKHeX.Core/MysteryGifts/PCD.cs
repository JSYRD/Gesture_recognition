﻿using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 4 Mystery Gift Template File
    /// </summary>
    /// <remarks>
    /// Big thanks to Grovyle91's Pokémon Mystery Gift Editor, from which the structure was referenced.
    /// https://projectpokemon.org/home/profile/859-grovyle91/
    /// https://projectpokemon.org/home/forums/topic/5870-pok%C3%A9mon-mystery-gift-editor-v143-now-with-bw-support/
    /// See also: http://tccphreak.shiny-clique.net/debugger/pcdfiles.htm
    /// </remarks>
    public sealed class PCD : DataMysteryGift, IRibbonSetEvent3, IRibbonSetEvent4
    {
        public const int Size = 0x358; // 856
        public override int Generation => 4;

        public override int Level
        {
            get => Gift.Level;
            set => Gift.Level = value;
        }

        public override int Ball
        {
            get => Gift.Ball;
            set => Gift.Ball = value;
        }

        public PCD() : this(new byte[Size]) { }
        public PCD(byte[] data) : base(data) { }

        public override byte[] Write()
        {
            // Ensure PGT content is encrypted
            var clone = new PCD((byte[])Data.Clone());
            if (clone.Gift.VerifyPKEncryption())
                clone.Gift = clone.Gift;
            return clone.Data;
        }

        public PGT Gift
        {
            get => _gift ??= new PGT(Data.Slice(0, PGT.Size));
            set => (_gift = value).Data.CopyTo(Data, 0);
        }

        private PGT? _gift;

        public Span<byte> GetMetadata() => Data.AsSpan(PGT.Size);
        public void SetMetadata(ReadOnlySpan<byte> data) => data.CopyTo(Data.AsSpan(PGT.Size));

        public override bool GiftUsed { get => Gift.GiftUsed; set => Gift.GiftUsed = value; }
        public override bool IsPokémon { get => Gift.IsPokémon; set => Gift.IsPokémon = value; }
        public override bool IsItem { get => Gift.IsItem; set => Gift.IsItem = value; }
        public override int ItemID { get => Gift.ItemID; set => Gift.ItemID = value; }

        public override int CardID
        {
            get => ReadUInt16LittleEndian(Data.AsSpan(0x150));
            set => WriteUInt16LittleEndian(Data.AsSpan(0x150), (ushort)value);
        }

        private const int TitleLength = 0x48;

        private Span<byte> CardTitleSpan => Data.AsSpan(0x104, TitleLength);

        public override string CardTitle
        {
            get => StringConverter4.GetString(CardTitleSpan);
            set => StringConverter4.SetString(CardTitleSpan, value.AsSpan(), TitleLength / 2, StringConverterOption.ClearFF);
        }

        public ushort CardCompatibility => ReadUInt16LittleEndian(Data.AsSpan(0x14C)); // rest of bytes we don't really care about

        public override int Species { get => Gift.IsManaphyEgg ? 490 : Gift.Species; set => Gift.Species = value; }
        public override IReadOnlyList<int> Moves { get => Gift.Moves; set => Gift.Moves = value; }
        public override int HeldItem { get => Gift.HeldItem; set => Gift.HeldItem = value; }
        public override bool IsShiny => Gift.IsShiny;
        public override Shiny Shiny => Gift.Shiny;
        public override bool IsEgg { get => Gift.IsEgg; set => Gift.IsEgg = value; }
        public override int Gender { get => Gift.Gender; set => Gift.Gender = value; }
        public override int Form { get => Gift.Form; set => Gift.Form = value; }
        public override int TID { get => Gift.TID; set => Gift.TID = value; }
        public override int SID { get => Gift.SID; set => Gift.SID = value; }
        public override string OT_Name { get => Gift.OT_Name; set => Gift.OT_Name = value; }
        public override AbilityPermission Ability => Gift.Ability;

        // ILocation overrides
        public override int Location { get => IsEgg ? 0 : Gift.EggLocation + 3000; set { } }
        public override int EggLocation { get => IsEgg ? Gift.EggLocation + 3000 : 0; set { } }

        public bool GiftEquals(PGT pgt)
        {
            // Skip over the PGT's "Corresponding PCD Slot" @ 0x02
            byte[] g = pgt.Data;
            byte[] c = Gift.Data;
            if (g.Length != c.Length || g.Length < 3)
                return false;
            for (int i = 0; i < 2; i++)
            {
                if (g[i] != c[i])
                    return false;
            }

            for (int i = 3; i < g.Length; i++)
            {
                if (g[i] != c[i])
                    return false;
            }

            return true;
        }

        public override PKM ConvertToPKM(ITrainerInfo sav, EncounterCriteria criteria)
        {
            return Gift.ConvertToPKM(sav, criteria);
        }

        public bool CanBeReceivedBy(int pkmVersion) => (CardCompatibility >> pkmVersion & 1) == 1;

        public override bool IsMatchExact(PKM pkm, DexLevel evo)
        {
            var wc = Gift.PK;
            if (!wc.IsEgg)
            {
                if (wc.TID != pkm.TID) return false;
                if (wc.SID != pkm.SID) return false;
                if (wc.OT_Name != pkm.OT_Name) return false;
                if (wc.OT_Gender != pkm.OT_Gender) return false;
                if (wc.Language != 0 && wc.Language != pkm.Language) return false;

                if (pkm.Format != 4) // transferred
                {
                    // met location: deferred to general transfer check
                    if (wc.CurrentLevel > pkm.Met_Level) return false;
                }
                else
                {
                    if (wc.Egg_Location + 3000 != pkm.Met_Location) return false;
                    if (wc.CurrentLevel != pkm.Met_Level) return false;
                }
            }
            else // Egg
            {
                if (wc.Egg_Location + 3000 != pkm.Egg_Location && pkm.Egg_Location != Locations.LinkTrade4) // traded
                    return false;
                if (wc.CurrentLevel != pkm.Met_Level)
                    return false;
                if (pkm.IsEgg && !pkm.IsNative)
                    return false;
            }

            if (wc.Form != evo.Form && !FormInfo.IsFormChangeable(wc.Species, wc.Form, pkm.Form, pkm.Format))
                return false;

            if (wc.Ball != pkm.Ball) return false;
            if (wc.OT_Gender < 3 && wc.OT_Gender != pkm.OT_Gender) return false;

            // Milotic is the only gift to come with Contest stats.
            if (wc.Species == (int)Core.Species.Milotic && pkm is IContestStats s && s.IsContestBelow(wc))
                return false;

            if (IsRandomPID())
            {
                // Random PID, never shiny
                // PID=0 was never used (pure random).
                if (pkm.IsShiny)
                    return false;

                // Don't check gender. All gifts that have PID=1 are genderless, with one exception.
                // The KOR Arcanine can end up with either gender, even though the template may have a specified gender.
            }
            else
            {
                // Fixed PID
                if (wc.Gender != pkm.Gender)
                    return false;
            }

            return true;
        }

        protected override bool IsMatchPartial(PKM pkm) => CanBeReceivedBy(pkm.Version);
        protected override bool IsMatchDeferred(PKM pkm) => Species != pkm.Species;

        public bool RibbonEarth { get => Gift.RibbonEarth; set => Gift.RibbonEarth = value; }
        public bool RibbonNational { get => Gift.RibbonNational; set => Gift.RibbonNational = value; }
        public bool RibbonCountry { get => Gift.RibbonCountry; set => Gift.RibbonCountry = value; }
        public bool RibbonChampionBattle { get => Gift.RibbonChampionBattle; set => Gift.RibbonChampionBattle = value; }
        public bool RibbonChampionRegional { get => Gift.RibbonChampionRegional; set => Gift.RibbonChampionRegional = value; }
        public bool RibbonChampionNational { get => Gift.RibbonChampionNational; set => Gift.RibbonChampionNational = value; }
        public bool RibbonClassic { get => Gift.RibbonClassic; set => Gift.RibbonClassic = value; }
        public bool RibbonWishing { get => Gift.RibbonWishing; set => Gift.RibbonWishing = value; }
        public bool RibbonPremier { get => Gift.RibbonPremier; set => Gift.RibbonPremier = value; }
        public bool RibbonEvent { get => Gift.RibbonEvent; set => Gift.RibbonEvent = value; }
        public bool RibbonBirthday { get => Gift.RibbonBirthday; set => Gift.RibbonBirthday = value; }
        public bool RibbonSpecial { get => Gift.RibbonSpecial; set => Gift.RibbonSpecial = value; }
        public bool RibbonWorld { get => Gift.RibbonWorld; set => Gift.RibbonWorld = value; }
        public bool RibbonChampionWorld { get => Gift.RibbonChampionWorld; set => Gift.RibbonChampionWorld = value; }
        public bool RibbonSouvenir { get => Gift.RibbonSouvenir; set => Gift.RibbonSouvenir = value; }

        public bool IsFixedPID() => Gift.PK.PID != 1;
        public bool IsRandomPID() => Gift.PK.PID == 1; // nothing used 0 (full random), always anti-shiny
    }
}
