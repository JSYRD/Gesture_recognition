using static PKHeX.Core.OverworldCorrelation8Requirement;

namespace PKHeX.Core
{
    /// <summary>
    /// Encounter Slot found in <see cref="GameVersion.SWSH"/>.
    /// </summary>
    /// <inheritdoc cref="EncounterSlot"/>
    public sealed record EncounterSlot8 : EncounterSlot, IOverworldCorrelation8
    {
        public readonly AreaWeather8 Weather;
        public readonly AreaSlotType8 SlotType;
        public override string LongName => $"{wild} [{SlotType}] - {Weather.ToString().Replace("_", string.Empty)}";
        public override int Generation => 8;

        public EncounterSlot8(EncounterArea8 area, int species, int form, int min, int max, AreaWeather8 weather, AreaSlotType8 slotType) : base(area, species, form, min, max)
        {
            Weather = weather;
            SlotType = slotType;
        }

        protected override void ApplyDetails(ITrainerInfo sav, EncounterCriteria criteria, PKM pk)
        {
            bool symbol = ((EncounterArea8)Area).PermitCrossover;
            var c = symbol ? EncounterCriteria.Unrestricted : criteria;
            if (!symbol && Location is 30 or 54 && (Weather & AreaWeather8.Fishing) == 0)
                ((PK8)pk).RibbonMarkCurry = true;

            base.ApplyDetails(sav, c, pk);
            if (Weather is AreaWeather8.Heavy_Fog && EncounterArea8.IsBoostedArea60Fog(Location))
                pk.CurrentLevel = pk.Met_Level = EncounterArea8.BoostLevel;

            var req = GetRequirement(pk);
            if (req != MustHave)
            {
                pk.SetRandomEC();
                return;
            }
            // Don't bother honoring shiny state.
            Overworld8RNG.ApplyDetails(pk, c, Shiny.Random);
        }

        public OverworldCorrelation8Requirement GetRequirement(PKM pk)
        {
            if (((EncounterArea8)Area).PermitCrossover)
                return MustHave; // symbol walking overworld

            bool curry = pk is IRibbonSetMark8 {RibbonMarkCurry: true} || (pk.Species == (int)Core.Species.Shedinja && pk is PK8 {AffixedRibbon:(int)RibbonIndex.MarkCurry});
            if (curry)
                return MustNotHave;

            // Tree encounters are generated via the global seed, not the u32
            if ((Weather & AreaWeather8.Shaking_Trees) != 0)
            {
                // Some tree encounters are present in the regular encounters.
                return Weather == AreaWeather8.Shaking_Trees
                    ? MustNotHave
                    : CanBeEither;
            }

            return MustHave;
        }

        public bool IsOverworldCorrelationCorrect(PKM pk)
        {
            var flawless = GetFlawlessIVCount(pk.Met_Level);
            return Overworld8RNG.ValidateOverworldEncounter(pk, flawless: flawless);
        }

        private int GetFlawlessIVCount(int met)
        {
            const int none = 0;
            const int any023 = -1;

            // Brilliant encounters are boosted to max level for the slot.
            if (met < LevelMax)
                return none;

            var area = (EncounterArea8) Area;
            if (area.PermitCrossover)
                return any023; // Symbol
            if ((Weather & AreaWeather8.Fishing) != 0)
                return any023; // Fishing
            return none; // Hidden
        }

        public override EncounterMatchRating GetMatchRating(PKM pkm)
        {
            bool isHidden = pkm.AbilityNumber == 4;
            if (isHidden && this.IsPartialMatchHidden(pkm.Species, Species))
                return EncounterMatchRating.PartialMatch;

            if (pkm is IRibbonSetMark8 m)
            {
                if (m.RibbonMarkCurry && (Weather & AreaWeather8.All) == 0)
                    return EncounterMatchRating.DeferredErrors;
                if (m.RibbonMarkFishing && (Weather & AreaWeather8.Fishing) == 0)
                    return EncounterMatchRating.DeferredErrors;

                // Check if it has a mark and the weather does not permit the mark.
                // Tree/Fishing slots should be deferred here and are checked later.
                if (!Weather.IsMarkCompatible(m))
                    return EncounterMatchRating.DeferredErrors;

                // Galar Mine hidden encounters can only be found via Curry or Fishing.
                if(Location is (30 or 54) && SlotType is AreaSlotType8.HiddenMain && !m.RibbonMarkCurry && !SlotType.CanEncounterViaFishing(Weather))
                    return EncounterMatchRating.DeferredErrors;
            }

            var req = GetRequirement(pkm);
            return req switch
            {
                MustHave when !IsOverworldCorrelationCorrect(pkm) => EncounterMatchRating.DeferredErrors,
                MustNotHave when IsOverworldCorrelationCorrect(pkm) => EncounterMatchRating.DeferredErrors,
                _ => EncounterMatchRating.Match,
            };
        }
    }
}
