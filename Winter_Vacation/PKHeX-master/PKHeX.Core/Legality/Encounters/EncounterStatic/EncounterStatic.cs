﻿using System;
using System.Collections.Generic;

namespace PKHeX.Core
{
    /// <summary>
    /// Static Encounter Data
    /// </summary>
    /// <remarks>
    /// Static Encounters are fixed position encounters with properties that are not subject to Wild Encounter conditions.
    /// </remarks>
    public abstract record EncounterStatic(GameVersion Version) : IEncounterable, IMoveset, IEncounterMatch
    {
        public int Species { get; init; }
        public int Form { get; init; }
        public virtual int Level { get; init; }
        public virtual int LevelMin => Level;
        public virtual int LevelMax => Level;
        public abstract int Generation { get; }

        public virtual int Location { get; init; }
        public AbilityPermission Ability { get; init; }
        public Shiny Shiny { get; init; } = Shiny.Random;
        public int Gender { get; init; } = -1;
        public int EggLocation { get; init; }
        public Nature Nature { get; init; } = Nature.Random;
        public bool Gift { get; init; }
        public int Ball { get; init; } = 4; // Only checked when is Gift

        public Ball FixedBall => Gift ? (Ball)Ball : Core.Ball.None;

        public IReadOnlyList<int> Moves { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> IVs { get; init; } = Array.Empty<int>();
        public int FlawlessIVCount { get; init; }

        public int HeldItem { get; init; }
        public int EggCycles { get; init; }

        public bool Fateful { get; init; }
        public bool EggEncounter => EggLocation > 0;

        private const string _name = "Static Encounter";
        public string Name => _name;
        public string LongName => Version == GameVersion.Any ? _name : $"{_name} ({Version})";
        public bool IsShiny => Shiny.IsShiny();

        public bool IsRandomUnspecificForm => Form >= FormDynamic;
        private const int FormDynamic = FormVivillon;
        internal const int FormVivillon = 30;
      //protected const int FormRandom = 31;

        protected virtual PKM GetBlank(ITrainerInfo tr) => PKMConverter.GetBlank(Generation, Version);

        public PKM ConvertToPKM(ITrainerInfo sav) => ConvertToPKM(sav, EncounterCriteria.Unrestricted);

        public PKM ConvertToPKM(ITrainerInfo sav, EncounterCriteria criteria)
        {
            var pk = GetBlank(sav);
            sav.ApplyTo(pk);

            ApplyDetails(sav, criteria, pk);
            return pk;
        }

        protected virtual void ApplyDetails(ITrainerInfo sav, EncounterCriteria criteria, PKM pk)
        {
            pk.EncryptionConstant = Util.Rand32();
            pk.Species = Species;
            pk.Form = Form;

            var version = this.GetCompatibleVersion((GameVersion)sav.Game);
            int lang = (int)Language.GetSafeLanguage(Generation, (LanguageID)sav.Language, version);
            int level = GetMinimalLevel();

            pk.Version = (int)version;
            pk.Language = lang = GetEdgeCaseLanguage(pk, lang);
            pk.Nickname = SpeciesName.GetSpeciesNameGeneration(Species, lang, Generation);

            pk.CurrentLevel = level;
            pk.Ball = Ball;
            pk.HeldItem = HeldItem;
            pk.OT_Friendship = pk.PersonalInfo.BaseFriendship;

            var today = DateTime.Today;
            SetMetData(pk, level, today);
            if (EggEncounter)
                SetEggMetData(pk, sav, today);

            SetPINGA(pk, criteria);
            SetEncounterMoves(pk, version, level);

            if (Fateful)
                pk.FatefulEncounter = true;

            if (pk.Format < 6)
                return;

            if (this is IRelearn relearn)
                pk.SetRelearnMoves(relearn.Relearn);

            sav.ApplyHandlingTrainerInfo(pk);

            if (pk is IScaledSize s)
            {
                s.HeightScalar = PokeSizeUtil.GetRandomScalar();
                s.WeightScalar = PokeSizeUtil.GetRandomScalar();
            }
            if (this is IGigantamax g && pk is IGigantamax pg)
                pg.CanGigantamax = g.CanGigantamax;
            if (this is IDynamaxLevel d && pk is IDynamaxLevel pd)
                pd.DynamaxLevel = d.DynamaxLevel;
        }

        protected virtual int GetMinimalLevel() => LevelMin;

        protected virtual void SetPINGA(PKM pk, EncounterCriteria criteria)
        {
            var pi = pk.PersonalInfo;
            int gender = criteria.GetGender(Gender, pi);
            int nature = (int)criteria.GetNature(Nature);
            int ability = criteria.GetAbilityFromNumber(Ability);

            var pidtype = GetPIDType();
            PIDGenerator.SetRandomWildPID(pk, pk.Format, nature, ability, gender, pidtype);
            SetIVs(pk);
            pk.StatNature = pk.Nature;
        }

        private void SetEggMetData(PKM pk, ITrainerInfo tr, DateTime today)
        {
            pk.Met_Location = Math.Max(0, EncounterSuggestion.GetSuggestedEggMetLocation(pk));
            pk.Met_Level = EncounterSuggestion.GetSuggestedEncounterEggMetLevel(pk);

            if (Generation >= 4)
            {
                bool traded = (int)Version == tr.Game;
                pk.Egg_Location = EncounterSuggestion.GetSuggestedEncounterEggLocationEgg(Generation, Version, traded);
                pk.EggMetDate = today;
            }
            pk.Egg_Location = EggLocation;
            pk.EggMetDate = today;
        }

        protected virtual void SetMetData(PKM pk, int level, DateTime today)
        {
            if (pk.Format <= 2)
                return;

            pk.Met_Location = Location;
            pk.Met_Level = level;
            if (pk.Format >= 4)
                pk.MetDate = today;
        }

        private void SetEncounterMoves(PKM pk, GameVersion version, int level)
        {
            var moves = Moves.Count > 0 ? Moves : MoveLevelUp.GetEncounterMoves(pk, level, version);
            pk.SetMoves(moves);
            pk.SetMaximumPPCurrent(moves);
        }

        protected void SetIVs(PKM pk)
        {
            if (IVs.Count != 0)
                pk.SetRandomIVs(IVs, FlawlessIVCount);
            else if (FlawlessIVCount > 0)
                pk.SetRandomIVs(flawless: FlawlessIVCount);
        }

        private int GetEdgeCaseLanguage(PKM pk, int lang)
        {
            switch (this)
            {
                // Cannot trade between languages
                case IFixedGBLanguage e:
                    return e.Language == EncounterGBLanguage.Japanese ? 1 : 2;

                // E-Reader was only available to Japanese games.
                case EncounterStaticShadow {EReader: true}:
                // Old Sea Map was only distributed to Japanese games.
                case EncounterStatic3 when Species == (int)Core.Species.Mew:
                    pk.OT_Name = "ゲーフリ";
                    return (int)LanguageID.Japanese;

                // Deoxys for Emerald was not available for Japanese games.
                case EncounterStatic3 when Species == (int)Core.Species.Deoxys && Version == GameVersion.E && lang == 1:
                    pk.OT_Name = "GF";
                    return (int)LanguageID.English;

                default:
                    return lang;
            }
        }

        private PIDType GetPIDType()
        {
            switch (Generation)
            {
                case 3 when this is EncounterStatic3 {Roaming: true, Version: not GameVersion.E}: // Roamer IV glitch was fixed in Emerald
                    return PIDType.Method_1_Roamer;
                case 4 when Shiny == Shiny.Always: // Lake of Rage Gyarados
                    return PIDType.ChainShiny;
                case 4 when Species == (int)Core.Species.Pichu: // Spiky Eared Pichu
                case 4 when Location == Locations.PokeWalker4: // Pokéwalker
                    return PIDType.Pokewalker;
                case 5 when Shiny == Shiny.Always:
                    return PIDType.G5MGShiny;

                default: return PIDType.None;
            }
        }

        public virtual bool IsMatchExact(PKM pkm, DexLevel evo)
        {
            if (Nature != Nature.Random && pkm.Nature != (int) Nature)
                return false;

            if (!IsMatchEggLocation(pkm))
                return false;
            if (!IsMatchLocation(pkm))
                return false;
            if (!IsMatchLevel(pkm, evo))
                return false;
            if (!IsMatchGender(pkm))
                return false;
            if (!IsMatchForm(pkm, evo))
                return false;
            if (!IsMatchIVs(pkm))
                return false;

            if (this is IContestStats es && pkm is IContestStats s && s.IsContestBelow(es))
                return false;

            // Defer to EC/PID check
            // if (e.Shiny != null && e.Shiny != pkm.IsShiny)
            // continue;

            // Defer ball check to later
            // if (e.Gift && pkm.Ball != 4) // PokéBall
            // continue;

            return true;
        }

        private bool IsMatchIVs(PKM pkm)
        {
            if (IVs.Count == 0)
                return true; // nothing to check, IVs are random
            if (Generation <= 2 && pkm.Format > 2)
                return true; // IVs are regenerated on VC transfer upward

            return Legal.GetIsFixedIVSequenceValidSkipRand(IVs, pkm);
        }

        protected virtual bool IsMatchForm(PKM pkm, DexLevel evo)
        {
            if (IsRandomUnspecificForm)
                return true;
            return Form == evo.Form || FormInfo.IsFormChangeable(Species, Form, pkm.Form, pkm.Format);
        }

        // override me if the encounter type has any eggs
        protected virtual bool IsMatchEggLocation(PKM pkm) => pkm.Egg_Location == 0;

        private bool IsMatchGender(PKM pkm)
        {
            if (Gender == -1 || Gender == pkm.Gender)
                return true;

            if (Species == (int) Core.Species.Azurill && Generation == 4 && Location == 233 && pkm.Gender == 0)
                return PKX.GetGenderFromPIDAndRatio(pkm.PID, 0xBF) == 1;

            return false;
        }

        protected virtual bool IsMatchLocation(PKM pkm)
        {
            if (EggEncounter)
                return true;
            if (Location == 0)
                return true;
            if (!pkm.HasOriginalMetLocation)
                return true;
            return Location == pkm.Met_Location;
        }

        protected virtual bool IsMatchLevel(PKM pkm, DexLevel evo)
        {
            return pkm.Met_Level == Level;
        }

        public virtual EncounterMatchRating GetMatchRating(PKM pkm)
        {
            if (IsMatchPartial(pkm))
                return EncounterMatchRating.PartialMatch;
            return IsMatchDeferred(pkm);
        }

        /// <summary>
        /// Checks if the provided <see cref="pkm"/> might not be the best match, or even a bad match due to minor reasons.
        /// </summary>
        protected virtual EncounterMatchRating IsMatchDeferred(PKM pkm) => EncounterMatchRating.Match;

        /// <summary>
        /// Checks if the provided <see cref="pkm"/> is not an exact match due to minor reasons.
        /// </summary>
        protected virtual bool IsMatchPartial(PKM pkm)
        {
            if (pkm.Format >= 5 && pkm.AbilityNumber == 4 && this.IsPartialMatchHidden(pkm.Species, Species))
                return true;
            return pkm.FatefulEncounter != Fateful;
        }
    }
}
