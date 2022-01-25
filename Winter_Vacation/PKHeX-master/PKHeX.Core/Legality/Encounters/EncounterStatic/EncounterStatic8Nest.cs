﻿using System;
using static PKHeX.Core.Encounters8Nest;
using static PKHeX.Core.AbilityPermission;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 8 Nest Encounter (Raid)
    /// </summary>
    /// <inheritdoc cref="EncounterStatic"/>
    public abstract record EncounterStatic8Nest<T>(GameVersion Version) : EncounterStatic(Version), IGigantamax, IDynamaxLevel where T : EncounterStatic8Nest<T>
    {
        public sealed override int Generation => 8;
        public static Func<PKM, T, bool>? VerifyCorrelation { private get; set; }
        public static Action<PKM, T, EncounterCriteria>? GenerateData { private get; set; }

        public bool CanGigantamax { get; set; }
        public byte DynamaxLevel { get; set; }
        public override int Location { get => SharedNest; init { } }

        public override bool IsMatchExact(PKM pkm, DexLevel evo)
        {
            if (pkm is IDynamaxLevel d && d.DynamaxLevel < DynamaxLevel)
                return false;

            // Required Ability
            if (Ability == AbilityPermission.OnlyHidden && pkm.AbilityNumber != 4)
                return false; // H

            if (Version != GameVersion.SWSH && pkm.Version != (int)Version && pkm.Met_Location != SharedNest)
                return false;

            if (VerifyCorrelation != null && !VerifyCorrelation(pkm, (T)this))
                return false;

            if (pkm is IRibbonSetMark8 m8 && m8.HasMark())
                return false;
            if (pkm.Species == (int)Core.Species.Shedinja && pkm is PK8 { AffixedRibbon: >= (int)RibbonIndex.MarkLunchtime })
                return false;

            return base.IsMatchExact(pkm, evo);
        }

        protected sealed override EncounterMatchRating IsMatchDeferred(PKM pkm)
        {
            if (Ability != Any12H)
            {
                // HA-Only is a strict match. Ability Capsule and Patch can potentially change these.
                var num = pkm.AbilityNumber;
                if (num == 4)
                {
                    if (Ability is not OnlyHidden && !AbilityVerifier.CanAbilityPatch(8, PersonalTable.SWSH.GetFormEntry(Species, Form).Abilities, pkm.Species))
                        return EncounterMatchRating.DeferredErrors;
                }
                else if (Ability.IsSingleValue(out int index) && 1 << index != num) // Fixed regular ability
                {
                    if (Ability is OnlyFirst or OnlySecond && !AbilityVerifier.CanAbilityCapsule(8, PersonalTable.SWSH.GetFormEntry(Species, Form).Abilities))
                        return EncounterMatchRating.DeferredErrors;
                }
            }

            return base.IsMatchDeferred(pkm);
        }

        protected override bool IsMatchPartial(PKM pkm)
        {
            if (pkm is IGigantamax g && g.CanGigantamax != CanGigantamax && !g.CanToggleGigantamax(pkm.Species, pkm.Form, Species, Form))
                return true;
            if (Species == (int)Core.Species.Alcremie && pkm is IFormArgument { FormArgument: not 0 })
                return true;
            if (Species == (int)Core.Species.Runerigus && pkm is IFormArgument { FormArgument: not 0 })
                return true;

            switch (Shiny)
            {
                case Shiny.Never when pkm.IsShiny:
                case Shiny.Always when !pkm.IsShiny:
                    return true;
            }

            return base.IsMatchPartial(pkm);
        }

        protected override void ApplyDetails(ITrainerInfo sav, EncounterCriteria criteria, PKM pk)
        {
            base.ApplyDetails(sav, criteria, pk);
            if (GenerateData == null)
                pk.SetRandomEC();
        }

        protected sealed override void SetPINGA(PKM pk, EncounterCriteria criteria)
        {
            if (GenerateData != null)
            {
                GenerateData(pk, (T)this, criteria);
                return;
            }

            base.SetPINGA(pk, criteria);
            if (Species == (int) Core.Species.Toxtricity)
            {
                while (true)
                {
                    var result = EvolutionMethod.GetAmpLowKeyResult(pk.Nature);
                    if (result == pk.Form)
                        break;
                    pk.Nature = Util.Rand.Next(25);
                }

                // Might be originally generated with a Neutral nature, then above logic changes to another.
                // Realign the stat nature to Serious mint.
                if (pk.Nature != pk.StatNature && ((Nature)pk.StatNature).IsNeutral())
                    pk.StatNature = (int)Nature.Serious;
            }
            var pid = pk.PID;
            RaidRNG.ForceShinyState(pk, Shiny == Shiny.Always, ref pid);
            pk.PID = pid;
        }
    }
}
