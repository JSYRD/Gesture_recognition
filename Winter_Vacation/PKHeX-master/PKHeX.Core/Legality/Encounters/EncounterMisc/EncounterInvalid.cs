﻿using System;

namespace PKHeX.Core
{
    /// <summary>
    /// Invalid Encounter Data
    /// </summary>
    public sealed record EncounterInvalid : IEncounterable
    {
        public static readonly EncounterInvalid Default = new();

        public int Species { get; }
        public int Form { get; }
        public int LevelMin { get; }
        public int LevelMax { get; }
        public bool EggEncounter { get; }
        public int Generation { get; }
        public GameVersion Version { get; }
        public bool IsShiny => false;
        public Shiny Shiny => Shiny.Never;

        public string Name => "Invalid";
        public string LongName => "Invalid";
        public int Location => 0;
        public int EggLocation => 0;
        public AbilityPermission Ability => AbilityPermission.Any12H;
        public Ball FixedBall => Ball.None;

        private EncounterInvalid() { }

        public EncounterInvalid(PKM pkm)
        {
            Species = pkm.Species;
            Form = pkm.Form;
            LevelMin = pkm.Met_Level;
            LevelMax = pkm.CurrentLevel;
            EggEncounter = pkm.WasEgg;
            Generation = pkm.Generation;
            Version = (GameVersion)pkm.Version;
        }

        public PKM ConvertToPKM(ITrainerInfo sav) => ConvertToPKM(sav, EncounterCriteria.Unrestricted);
        public PKM ConvertToPKM(ITrainerInfo sav, EncounterCriteria criteria) => throw new ArgumentException($"Cannot convert an {nameof(EncounterInvalid)} to PKM.");
    }
}
