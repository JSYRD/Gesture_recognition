﻿using System;
using static PKHeX.Core.LegalityCheckStrings;
using static PKHeX.Core.Species;

namespace PKHeX.Core
{
    /// <summary>
    /// Verifies the <see cref="PKM.Form"/> value.
    /// </summary>
    public sealed class FormVerifier : Verifier
    {
        protected override CheckIdentifier Identifier => CheckIdentifier.Form;

        public override void Verify(LegalityAnalysis data)
        {
            var pkm = data.pkm;
            if (pkm.Format < 4)
                return; // no forms exist
            var result = VerifyForm(data);
            data.AddLine(result);

            if (pkm is IFormArgument f)
                data.AddLine(VerifyFormArgument(data, f));
        }

        private CheckResult VALID => GetValid(LFormValid);

        private CheckResult VerifyForm(LegalityAnalysis data)
        {
            var pkm = data.pkm;
            var PersonalInfo = data.PersonalInfo;

            int count = PersonalInfo.FormCount;
            var form = pkm.Form;
            if (count <= 1 && form == 0)
                return VALID; // no forms to check

            var species = pkm.Species;
            var enc = data.EncounterMatch;
            var Info = data.Info;

            if (!PersonalInfo.IsFormWithinRange(form) && !FormInfo.IsValidOutOfBoundsForm(species, form, Info.Generation))
                return GetInvalid(string.Format(LFormInvalidRange, count - 1, form));

            switch (enc)
            {
                case EncounterEgg e when FormInfo.IsTotemForm(species, form, e.Generation):
                    return GetInvalid(LFormInvalidGame);
            }

            switch ((Species)species)
            {
                case Pikachu when Info.Generation == 6: // Cosplay
                    bool isStatic = enc is EncounterStatic;
                    bool validCosplay = form == (isStatic ? enc.Form : 0);
                    if (!validCosplay)
                        return GetInvalid(isStatic ? LFormPikachuCosplayInvalid : LFormPikachuCosplay);
                    break;

                case Pikachu when form is not 0 && ParseSettings.ActiveTrainer is SAV7b {Version:GameVersion.GE}:
                case Eevee when form is not 0 && ParseSettings.ActiveTrainer is SAV7b {Version:GameVersion.GP}:
                    return GetInvalid(LFormBattle);

                case Pikachu when Info.Generation >= 7: // Cap
                    bool validCap = form == (enc is EncounterInvalid or EncounterEgg ? 0 : enc.Form);
                    if (!validCap)
                    {
                        bool gift = enc is MysteryGift g && g.Form != form;
                        var msg = gift ? LFormPikachuEventInvalid : LFormInvalidGame;
                        return GetInvalid(msg);
                    }
                    break;
                case Unown when Info.Generation == 2 && form >= 26:
                    return GetInvalid(string.Format(LFormInvalidRange, "Z", form == 26 ? "!" : "?"));
                case Giratina when form == 1 ^ pkm.HeldItem == 112: // Giratina, Origin form only with Griseous Orb
                    return GetInvalid(LFormItemInvalid);

                case Arceus:
                    {
                        int arceus = GetArceusFormFromHeldItem(pkm.HeldItem, pkm.Format);
                        return arceus != form ? GetInvalid(LFormItemInvalid) : GetValid(LFormItem);
                    }
                case Keldeo when enc.Generation != 5 || pkm.Format >= 8:
                    // can mismatch in gen5 via BW tutor and transfer up
                    // can mismatch in gen8+ as the form activates in battle when knowing the move; outside of battle can be either state.
                    // Generation 8 patched out the mismatch; always forced to match moves.
                    bool hasSword = pkm.HasMove((int) Move.SecretSword);
                    bool isSword = pkm.Form == 1;
                    if (isSword != hasSword)
                        return GetInvalid(LMoveKeldeoMismatch);
                    break;
                case Genesect:
                    {
                        int genesect = GetGenesectFormFromHeldItem(pkm.HeldItem);
                        return genesect != form ? GetInvalid(LFormItemInvalid) : GetValid(LFormItem);
                    }
                case Greninja:
                    if (form > 1) // Ash Battle Bond active
                        return GetInvalid(LFormBattle);
                    if (form != 0 && enc is not MysteryGift) // Formes are not breedable, MysteryGift already checked
                        return GetInvalid(string.Format(LFormInvalidRange, 0, form));
                    break;

                case Scatterbug or Spewpa:
                    if (form > Vivillon3DS.MaxWildFormID) // Fancy & Pokéball
                        return GetInvalid(LFormVivillonEventPre);
                    if (pkm is not IRegionOrigin tr)
                        break;
                    if (!Vivillon3DS.IsPatternValid(form, tr.ConsoleRegion))
                        return GetInvalid(LFormVivillonInvalid);
                    if (!Vivillon3DS.IsPatternNative(form, tr.Country, tr.Region))
                        data.AddLine(Get(LFormVivillonNonNative, Severity.Fishy));
                    break;
                case Vivillon:
                    if (form > Vivillon3DS.MaxWildFormID) // Fancy & Pokéball
                    {
                        if (enc is not MysteryGift)
                            return GetInvalid(LFormVivillonInvalid);
                        return GetValid(LFormVivillon);
                    }
                    if (pkm is not IRegionOrigin trv)
                        break;
                    if (!Vivillon3DS.IsPatternValid(form, trv.ConsoleRegion))
                        return GetInvalid(LFormVivillonInvalid);
                    if (!Vivillon3DS.IsPatternNative(form, trv.Country, trv.Region))
                        data.AddLine(Get(LFormVivillonNonNative, Severity.Fishy));
                    break;

                case Floette when form == 5: // Floette Eternal Flower -- Never Released
                    if (enc is not MysteryGift)
                        return GetInvalid(LFormEternalInvalid);
                    return GetValid(LFormEternal);
                case Meowstic when form != pkm.Gender:
                    return GetInvalid(LGenderInvalidNone);

                case Silvally:
                    {
                        int silvally = GetSilvallyFormFromHeldItem(pkm.HeldItem);
                        return silvally != form ? GetInvalid(LFormItemInvalid) : GetValid(LFormItem);
                    }

                // Form doesn't exist in SM; cannot originate from that game.
                case Rockruff when enc.Generation == 7 && form == 1 && pkm.SM:
                case Lycanroc when enc.Generation == 7 && form == 2 && pkm.SM:
                    return GetInvalid(LFormInvalidGame);

                // Toxel encounters have already been checked for the nature-specific evolution criteria.
                case Toxtricity when enc.Species == (int)Toxtricity:
                    {
                        // The game enforces the Nature for Toxtricity encounters too!
                        if (pkm.Form != EvolutionMethod.GetAmpLowKeyResult(pkm.Nature))
                            return GetInvalid(LFormInvalidNature);
                        break;
                    }

                // Impossible Egg forms
                case Rotom when pkm.IsEgg && form != 0:
                case Furfrou when pkm.IsEgg && form != 0:
                    return GetInvalid(LEggSpecies);

                // Party Only Forms
                case Shaymin:
                case Furfrou:
                case Hoopa:
                    if (form != 0 && data.SlotOrigin is not SlotOrigin.Party && pkm.Format <= 6) // has form but stored in box
                        return GetInvalid(LFormParty);
                    break;
            }

            var format = pkm.Format;
            if (FormInfo.IsBattleOnlyForm(species, form, format))
                return GetInvalid(LFormBattle);

            if (form == 0)
                return VALID;

            // everything below here is not Form 0, so it has a form.
            if (format >= 7 && Info.Generation < 7)
            {
                if (species == 25 || Legal.AlolanOriginForms.Contains(species) || Legal.AlolanVariantEvolutions12.Contains(enc.Species))
                    return GetInvalid(LFormInvalidGame);
            }
            if (format >= 8 && Info.Generation < 8)
            {
                var orig = enc.Species;
                if (Legal.GalarOriginForms.Contains(species) || Legal.GalarVariantFormEvolutions.Contains(orig))
                {
                    if (species == (int)Meowth && enc.Form != 2)
                    {
                        // We're okay here. There's also Alolan Meowth...
                    }
                    else if (((Species)orig is MrMime or MimeJr) && pkm.CurrentLevel > enc.LevelMin && Info.Generation >= 4)
                    {
                        // We're okay with a Mime Jr. that has evolved via level up.
                    }
                    else if (enc.Version != GameVersion.GO)
                    {
                        return GetInvalid(LFormInvalidGame);
                    }
                }
            }

            return VALID;
        }

        private static readonly ushort[] Arceus_PlateIDs = { 303, 306, 304, 305, 309, 308, 310, 313, 298, 299, 301, 300, 307, 302, 311, 312, 644 };
        private static readonly ushort[] Arceus_ZCrystal = { 782, 785, 783, 784, 788, 787, 789, 792, 777, 778, 780, 779, 786, 781, 790, 791, 793 };

        public static int GetArceusFormFromHeldItem(int item, int format)
        {
            if (item is >= 777 and <= 793)
                return Array.IndexOf(Arceus_ZCrystal, (ushort)item) + 1;

            int form = 0;
            if (item is >= 298 and <= 313 or 644)
                form = Array.IndexOf(Arceus_PlateIDs, (ushort)item) + 1;
            if (format == 4 && form >= 9)
                return form + 1; // ??? type Form shifts everything by 1
            return form;
        }

        public static int GetSilvallyFormFromHeldItem(int item)
        {
            if (item is >= 904 and <= 920)
                return item - 903;
            return 0;
        }

        public static int GetGenesectFormFromHeldItem(int item)
        {
            if (item is >= 116 and <= 119)
                return item - 115;
            return 0;
        }

        private CheckResult VerifyFormArgument(LegalityAnalysis data, IFormArgument f)
        {
            var pkm = data.pkm;
            var enc = data.EncounterMatch;
            var arg = f.FormArgument;

            var unusedMask = pkm.Format == 6 ? 0xFFFF_FF00 : 0xFF00_0000;
            if ((arg & unusedMask) != 0)
                return GetInvalid(LFormArgumentHigh);

            return (Species)pkm.Species switch
            {
                // Transfer Edge Cases -- Bank wipes the form but keeps old FormArgument value.
                Furfrou when pkm.Format == 7 && pkm.Form == 0 &&
                    ((enc.Generation == 6 && f.FormArgument <= byte.MaxValue) || IsFormArgumentDayCounterValid(f, 5, true))
                    => GetValid(LFormArgumentValid),

                Furfrou when pkm.Form != 0 => !IsFormArgumentDayCounterValid(f, 5, true) ? GetInvalid(LFormArgumentInvalid) :GetValid(LFormArgumentValid),
                Hoopa when pkm.Form == 1 => !IsFormArgumentDayCounterValid(f, 3) ? GetInvalid(LFormArgumentInvalid) : GetValid(LFormArgumentValid),
                Yamask when pkm.Form == 1 => arg switch
                {
                    not 0 when pkm.IsEgg => GetInvalid(LFormArgumentNotAllowed),
                    > 9_999 => GetInvalid(LFormArgumentHigh),
                    _ => GetValid(LFormArgumentValid),
                },
                Runerigus when enc.Species == (int)Runerigus => arg switch
                {
                    not 0 => GetInvalid(LFormArgumentNotAllowed),
                    _ => GetValid(LFormArgumentValid),
                },
                Runerigus => arg switch // From Yamask-1
                {
                    < 49 => GetInvalid(LFormArgumentLow),
                    > 9_999 => GetInvalid(LFormArgumentHigh),
                    _ => GetValid(LFormArgumentValid),
                },
                Alcremie when enc.Species == (int)Alcremie => arg switch
                {
                    not 0 => GetInvalid(LFormArgumentNotAllowed),
                    _ => GetValid(LFormArgumentValid),
                },
                Alcremie => arg switch // From Milcery
                {
                    > (uint) AlcremieDecoration.Ribbon => GetInvalid(LFormArgumentHigh),
                    _ => GetValid(LFormArgumentValid),
                },
                _ => VerifyFormArgumentNone(pkm, f),
            };
        }

        private CheckResult VerifyFormArgumentNone(PKM pkm, IFormArgument f)
        {
            if (pkm is not PK6 pk6)
            {
                if (f.FormArgument != 0)
                {
                    if (pkm.Species == (int)Furfrou && pkm.Form == 0 && (f.FormArgument & ~0xFF_00_00u) == 0)
                        return GetValid(LFormArgumentValid);
                    return GetInvalid(LFormArgumentNotAllowed);
                }
                return GetValid(LFormArgumentValid);
            }

            if (f.FormArgument != 0)
            {
                if (pkm.Species == (int)Furfrou && pkm.Form == 0 && (f.FormArgument & ~0xFFu) == 0)
                    return GetValid(LFormArgumentValid);
                return GetInvalid(LFormArgumentNotAllowed);
            }

            // Stored separately from main form argument value
            if (pk6.FormArgumentRemain != 0)
                return GetInvalid(LFormArgumentNotAllowed);
            if (pk6.FormArgumentElapsed != 0)
                return GetInvalid(LFormArgumentNotAllowed);

            return GetValid(LFormArgumentValid);
        }

        private static bool IsFormArgumentDayCounterValid(IFormArgument f, uint maxSeed, bool canRefresh = false)
        {
            var remain = f.FormArgumentRemain;
            var elapsed = f.FormArgumentElapsed;
            var maxElapsed = f.FormArgumentMaximum;
            if (canRefresh)
            {
                if (maxElapsed < elapsed)
                    return false;

                if (remain + elapsed < maxSeed)
                    return false;
            }
            else
            {
                if (maxElapsed != 0)
                    return false;

                if (remain + elapsed != maxSeed)
                    return false;
            }
            if (remain > maxSeed)
                return false;
            return remain != 0;
        }
    }
}
