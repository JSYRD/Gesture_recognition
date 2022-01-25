﻿using System.Linq;
using static PKHeX.Core.LegalityCheckStrings;

namespace PKHeX.Core
{
    /// <summary>
    /// Verifies the <see cref="RibbonIndex"/> values for markings.
    /// </summary>
    public sealed class MarkVerifier : Verifier
    {
        protected override CheckIdentifier Identifier => CheckIdentifier.RibbonMark;

        public override void Verify(LegalityAnalysis data)
        {
            var pkm = data.pkm;
            if (pkm is not IRibbonIndex m)
                return;

            if (data.Info.Generation != 8 || (pkm.Species == (int)Species.Shedinja && data.EncounterOriginal.Species is not (int)Species.Shedinja)) // Shedinja doesn't copy Ribbons or Marks
                VerifyNoMarksPresent(data, m);
            else
                VerifyMarksPresent(data, m);

            VerifyAffixedRibbonMark(data, m);
        }

        private void VerifyNoMarksPresent(LegalityAnalysis data, IRibbonIndex m)
        {
            for (var x = RibbonIndex.MarkLunchtime; x <= RibbonIndex.MarkSlump; x++)
            {
                if (m.GetRibbon((int)x))
                    data.AddLine(GetInvalid(string.Format(LRibbonMarkingFInvalid_0, x)));
            }
        }

        private void VerifyMarksPresent(LegalityAnalysis data, IRibbonIndex m)
        {
            bool hasOne = false;
            for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
            {
                bool has = m.GetRibbon((int)mark);
                if (!has)
                    continue;

                if (hasOne)
                {
                    data.AddLine(GetInvalid(string.Format(LRibbonMarkingFInvalid_0, mark)));
                    return;
                }

                bool result = IsMarkValid(mark, data.pkm, data.EncounterMatch);
                if (!result)
                {
                    data.AddLine(GetInvalid(string.Format(LRibbonMarkingFInvalid_0, mark)));
                    return;
                }

                hasOne = true;
            }
        }

        public static bool IsMarkValid(RibbonIndex mark, PKM pk, IEncounterTemplate enc)
        {
            return IsMarkAllowedAny(enc) && IsMarkAllowedSpecific(mark, pk, enc);
        }

        public static bool IsMarkAllowedSpecific(RibbonIndex mark, PKM pk, IEncounterTemplate x) => mark switch
        {
            RibbonIndex.MarkCurry when !IsMarkAllowedCurry(pk, x) => false,
            RibbonIndex.MarkFishing when !IsMarkAllowedFishing(x) => false,
            RibbonIndex.MarkMisty when pk.Met_Level < EncounterArea8.BoostLevel && EncounterArea8.IsBoostedArea60Fog(pk.Met_Location) => false,
            RibbonIndex.MarkDestiny => false,
            >= RibbonIndex.MarkCloudy and <= RibbonIndex.MarkMisty => IsWeatherPermitted(mark, x),
            _ => true,
        };

        private static bool IsWeatherPermitted(RibbonIndex mark, IEncounterTemplate enc)
        {
            var permit = mark.GetWeather8();

            // Encounter slots check location weather, while static encounters check weather per encounter.
            return enc switch
            {
                EncounterSlot8 w => IsSlotWeatherPermitted(permit, w),
                EncounterStatic8 s => s.Weather.HasFlag(permit),
                _ => false,
            };
        }

        private static bool IsSlotWeatherPermitted(AreaWeather8 permit, EncounterSlot8 s)
        {
            var location = s.Location;
            // If it's not in the main table, it can only have Normal weather.
            if (!EncounterArea8.WeatherbyArea.TryGetValue(location, out var weather))
                weather = AreaWeather8.Normal;
            if (weather.HasFlag(permit))
                return true;

            // Valid tree/fishing weathers should have returned with main area weather.
            if ((s.Weather & (AreaWeather8.Shaking_Trees | AreaWeather8.Fishing)) != 0)
                return false;

            // Check bleed conditions otherwise.
            return EncounterArea8.IsWeatherBleedPossible(s.SlotType, permit, location);
        }

        public static bool IsMarkAllowedAny(IEncounterTemplate enc) => enc.Generation == 8 && enc switch
        {
            // Gen 8
            WC8 or EncounterEgg or EncounterTrade or EncounterSlot8GO
                or EncounterStatic8U or EncounterStatic8N or EncounterStatic8ND or EncounterStatic8NC
                or EncounterStatic8 {Gift: true}
                or EncounterStatic8 {ScriptedNoMarks: true}
                => false,
            _ => true,
        };

        public static bool IsMarkAllowedCurry(PKM pkm, IEncounterTemplate enc)
        {
            // Curry are only encounter slots, from the hidden table (not symbol). Slots taken from area's current weather(?).
            if (enc is not EncounterSlot8 s || !s.SlotType.CanEncounterViaCurry())
                return false;

            var weather = s.Weather;
            if ((weather & AreaWeather8.All) == 0)
                return false;

            if (EncounterArea8.IsWildArea(s.Location))
                return false;
            var ball = pkm.Ball;
            return (uint)(ball - 2) <= 2;
        }

        public static bool IsMarkAllowedFishing(IEncounterTemplate enc)
        {
            // Fishing are only encounter slots, from the hidden table (not symbol).
            if (enc is not EncounterSlot8 s)
                return false;

            return s.SlotType.CanEncounterViaFishing(s.Weather);
        }

        private void VerifyAffixedRibbonMark(LegalityAnalysis data, IRibbonIndex m)
        {
            if (m is not PK8 pk8)
                return;

            var affix = pk8.AffixedRibbon;
            if (affix == -1) // None
                return;

            if ((byte)affix > (int)RibbonIndex.MarkSlump)
            {
                data.AddLine(GetInvalid(string.Format(LRibbonMarkingAffixedF_0, affix)));
                return;
            }

            if (pk8.Species == (int)Species.Shedinja && data.EncounterOriginal.Species is not (int)Species.Shedinja)
            {
                VerifyShedinjaAffixed(data, affix, pk8);
                return;
            }
            EnsureHasRibbon(data, pk8, affix);
        }

        private void VerifyShedinjaAffixed(LegalityAnalysis data, sbyte affix, PK8 pk8)
        {
            // Does not copy ribbons or marks, but retains the Affixed Ribbon value.
            // Try re-verifying to see if it could have had the Ribbon/Mark.

            var enc = data.EncounterOriginal;
            if ((byte) affix >= (int) RibbonIndex.MarkLunchtime)
            {
                if (!IsMarkValid((RibbonIndex)affix, pk8, enc))
                    data.AddLine(GetInvalid(string.Format(LRibbonMarkingAffixedF_0, (RibbonIndex) affix)));
                return;
            }

            if (enc.Generation <= 4 && (pk8.Ball != (int)Ball.Poke || IsMoveSetEvolvedShedinja(pk8)))
            {
                // Evolved in a prior generation.
                EnsureHasRibbon(data, pk8, affix);
                return;
            }

            var clone = pk8.Clone();
            clone.Species = (int) Species.Nincada;
            ((IRibbonIndex) clone).SetRibbon(affix);
            var parse = RibbonVerifier.GetRibbonResults(clone, data.Info.EvoChainsAllGens, enc);
            var expect = $"Ribbon{(RibbonIndex) affix}";
            var name = RibbonStrings.GetName(expect);
            bool invalid = parse.FirstOrDefault(z => z.Name == name)?.Invalid == true;
            var severity = invalid ? Severity.Invalid : Severity.Fishy;
            data.AddLine(Get(string.Format(LRibbonMarkingAffixedF_0, affix), severity));
        }

        private static bool IsMoveSetEvolvedShedinja(PK8 pk8)
        {
            // Check for gen3/4 exclusive moves that are Ninjask glitch only.
            if (pk8.HasMove((int) Move.Screech))
                return true;
            if (pk8.HasMove((int) Move.SwordsDance))
                return true;
            if (pk8.HasMove((int) Move.Slash))
                return true;
            if (pk8.HasMove((int) Move.BatonPass))
                return true;
            return pk8.HasMove((int)Move.Agility) && !pk8.GetMoveRecordFlag(12); // TR12 (Agility)
        }

        private void EnsureHasRibbon(LegalityAnalysis data, IRibbonIndex pk8, sbyte affix)
        {
            var hasRibbon = pk8.GetRibbonIndex((RibbonIndex) affix);
            if (!hasRibbon)
                data.AddLine(GetInvalid(string.Format(LRibbonMarkingAffixedF_0, (RibbonIndex) affix)));
        }
    }
}
