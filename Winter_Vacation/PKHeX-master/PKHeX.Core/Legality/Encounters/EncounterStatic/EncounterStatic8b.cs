﻿using System;
using System.Linq;
using static PKHeX.Core.StaticCorrelation8bRequirement;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 7 Static Encounter
    /// </summary>
    /// <inheritdoc cref="EncounterStatic"/>
    public sealed record EncounterStatic8b : EncounterStatic, IStaticCorrelation8b
    {
        public override int Generation => 8;

        public bool Roaming { get; init; }

        public EncounterStatic8b(GameVersion game) : base(game)
        {
            EggLocation = Locations.Default8bNone;
        }

        protected override bool IsMatchLocation(PKM pkm)
        {
            if (!Roaming)
                return base.IsMatchLocation(pkm);
            return Roaming_MetLocation_BDSP.Contains(pkm.Met_Location);
        }

        public StaticCorrelation8bRequirement GetRequirement(PKM pk) => Roaming
            ? MustHave
            : MustNotHave;

        public bool IsStaticCorrelationCorrect(PKM pk)
        {
            return Roaming8bRNG.ValidateRoamingEncounter(pk, Shiny == Shiny.Random ? Shiny.FixedValue : Shiny, FlawlessIVCount);
        }

        protected override bool IsMatchEggLocation(PKM pkm)
        {
            var eggloc = (short)pkm.Egg_Location;
            if (!EggEncounter)
                return eggloc == (short)EggLocation;

            if (!pkm.IsEgg) // hatched
                return eggloc == (short)EggLocation || eggloc == Locations.LinkTrade6NPC;

            // Unhatched:
            if (eggloc != (short)EggLocation)
                return false;
            if ((short)pkm.Met_Location is not (Locations.Default8bNone or Locations.LinkTrade6NPC))
                return false;
            return true;
        }

        protected override void ApplyDetails(ITrainerInfo sav, EncounterCriteria criteria, PKM pk)
        {
            pk.Met_Location = pk.Egg_Location = Locations.Default8bNone;
            base.ApplyDetails(sav, criteria, pk);
            var req = GetRequirement(pk);
            if (req == MustHave) // Roamers
            {
                var shiny = Shiny == Shiny.Random ? Shiny.FixedValue : Shiny;
                Roaming8bRNG.ApplyDetails(pk, criteria, shiny, FlawlessIVCount);
            }
            else
            {
                var shiny = Shiny == Shiny.Never ? Shiny.Never : Shiny.Random;
                Wild8bRNG.ApplyDetails(pk, criteria, shiny, FlawlessIVCount);
            }
        }

        protected override void SetMetData(PKM pk, int level, DateTime today)
        {
            pk.Met_Level = level;
            pk.Met_Location = !Roaming ? Location : Roaming_MetLocation_BDSP[0];
            pk.MetDate = today;
        }

        // defined by mvpoke in encounter data
        private static readonly int[] Roaming_MetLocation_BDSP =
        {
            197, 201, 354, 355, 356, 357, 358, 359, 361, 362, 364, 365, 367, 373, 375, 377,
            378, 379, 383, 385, 392, 394, 395, 397, 400, 403, 404, 407,
            485,
        };
    }

    public interface IStaticCorrelation8b
    {
        StaticCorrelation8bRequirement GetRequirement(PKM pk);
        bool IsStaticCorrelationCorrect(PKM pk);
    }

    public enum StaticCorrelation8bRequirement
    {
        CanBeEither,
        MustHave,
        MustNotHave,
    }
}
