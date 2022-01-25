﻿using System;
using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.Legal;
using static PKHeX.Core.GameVersion;

namespace PKHeX.Core
{
    /// <summary>
    /// Logic for obtaining a list of moves.
    /// </summary>
    internal static class MoveList
    {
        internal static IEnumerable<int> GetValidRelearn(PKM pkm, int species, int form, bool inheritlvlmoves, GameVersion version = Any)
        {
            int generation = pkm.Generation;
            if (generation < 6)
                return Array.Empty<int>();

            var r = new List<int>();
            r.AddRange(MoveEgg.GetRelearnLVLMoves(pkm, species, form, 1, version));

            if (pkm.Format == 6 && pkm.Species != (int)Species.Meowstic)
                form = 0;

            r.AddRange(MoveEgg.GetEggMoves(pkm.PersonalInfo, species, form, version, Math.Max(2, generation)));
            if (inheritlvlmoves)
                r.AddRange(MoveEgg.GetRelearnLVLMoves(pkm, species, form, 100, version));
            return r.Distinct();
        }

        internal static int[] GetShedinjaEvolveMoves(PKM pkm, int generation, int lvl)
        {
            if (pkm.Species != (int)Species.Shedinja || lvl < 20)
                return Array.Empty<int>();

            // If Nincada evolves into Ninjask and learns a move after evolution from Ninjask's LevelUp data, Shedinja would appear with that move.
            // Only one move above level 20 is allowed; check the count of Ninjask moves elsewhere.
            return generation switch
            {
                3 when pkm.InhabitedGeneration(3) => LevelUpE[(int)Species.Ninjask].GetMoves(lvl, 20), // Same LevelUp data in all Gen3 games
                4 when pkm.InhabitedGeneration(4) => LevelUpPt[(int)Species.Ninjask].GetMoves(lvl, 20), // Same LevelUp data in all Gen4 games
                _ => Array.Empty<int>(),
            };
        }

        internal static int GetShedinjaMoveLevel(int species, int move, int generation)
        {
            var src = generation == 4 ? LevelUpPt : LevelUpE;
            var moves = src[species];
            return moves.GetLevelLearnMove(move);
        }

        internal static int[] GetBaseEggMoves(PKM pkm, int species, int form, GameVersion gameSource, int lvl)
        {
            if (gameSource == Any)
                gameSource = (GameVersion)pkm.Version;

            switch (gameSource)
            {
                case GSC or GS:
                    // If checking back-transfer specimen (GSC->RBY), remove moves that must be deleted prior to transfer
                    static int[] getRBYCompatibleMoves(int format, int[] moves) => format == 1 ? Array.FindAll(moves, m => m <= MaxMoveID_1) : moves;
                    if (pkm.InhabitedGeneration(2))
                        return getRBYCompatibleMoves(pkm.Format, LevelUpGS[species].GetMoves(lvl));
                    break;
                case C:
                    if (pkm.InhabitedGeneration(2))
                        return getRBYCompatibleMoves(pkm.Format, LevelUpC[species].GetMoves(lvl));
                    break;

                case R or S or RS:
                    if (pkm.InhabitedGeneration(3))
                        return LevelUpRS[species].GetMoves(lvl);
                    break;
                case E:
                    if (pkm.InhabitedGeneration(3))
                        return LevelUpE[species].GetMoves(lvl);
                    break;
                case FR or LG or FRLG:
                    // The only difference in FR/LG is Deoxys, which doesn't breed.
                    if (pkm.InhabitedGeneration(3))
                        return LevelUpFR[species].GetMoves(lvl);
                    break;

                case D or P or DP:
                    if (pkm.InhabitedGeneration(4))
                        return LevelUpDP[species].GetMoves(lvl);
                    break;
                case Pt:
                    if (pkm.InhabitedGeneration(4))
                        return LevelUpPt[species].GetMoves(lvl);
                    break;
                case HG or SS or HGSS:
                    if (pkm.InhabitedGeneration(4))
                        return LevelUpHGSS[species].GetMoves(lvl);
                    break;

                case B or W or BW:
                    if (pkm.InhabitedGeneration(5))
                        return LevelUpBW[species].GetMoves(lvl);
                    break;

                case B2 or W2 or B2W2:
                    if (pkm.InhabitedGeneration(5))
                        return LevelUpB2W2[species].GetMoves(lvl);
                    break;

                case X or Y or XY:
                    if (pkm.InhabitedGeneration(6))
                        return LevelUpXY[species].GetMoves(lvl);
                    break;

                case AS or OR or ORAS:
                    if (pkm.InhabitedGeneration(6))
                        return LevelUpAO[species].GetMoves(lvl);
                    break;

                case SN or MN or SM:
                    if (species > MaxSpeciesID_7)
                        break;
                    if (pkm.InhabitedGeneration(7))
                    {
                        int index = PersonalTable.SM.GetFormIndex(species, form);
                        return LevelUpSM[index].GetMoves(lvl);
                    }
                    break;

                case US or UM or USUM:
                    if (pkm.InhabitedGeneration(7))
                    {
                        int index = PersonalTable.USUM.GetFormIndex(species, form);
                        return LevelUpUSUM[index].GetMoves(lvl);
                    }
                    break;

                case SW or SH or SWSH:
                    if (pkm.InhabitedGeneration(8))
                    {
                        int index = PersonalTable.SWSH.GetFormIndex(species, form);
                        return LevelUpSWSH[index].GetMoves(lvl);
                    }
                    break;

                case BD or SP or BDSP:
                    if (pkm.InhabitedGeneration(8))
                    {
                        int index = PersonalTable.SWSH.GetFormIndex(species, form);
                        return LevelUpBDSP[index].GetMoves(lvl);
                    }
                    break;
            }
            return Array.Empty<int>();
        }

        internal static IReadOnlyList<int>[] GetValidMovesAllGens(PKM pkm, IReadOnlyList<EvoCriteria>[] evoChains, MoveSourceType types = MoveSourceType.ExternalSources, bool RemoveTransferHM = true)
        {
            var result = new IReadOnlyList<int>[evoChains.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = Array.Empty<int>();

            var min = pkm is IBattleVersion b ? Math.Max(0, b.GetMinGeneration()) : 1;
            for (int i = min; i < evoChains.Length; i++)
            {
                var evos = evoChains[i];
                if (evos.Count == 0)
                    continue;

                result[i] = GetValidMoves(pkm, evos, i, types, RemoveTransferHM).ToList();
            }
            return result;
        }

        internal static IEnumerable<int> GetValidMoves(PKM pkm, IReadOnlyList<EvoCriteria> evoChain, int generation, MoveSourceType types = MoveSourceType.ExternalSources, bool RemoveTransferHM = true)
        {
            GameVersion version = (GameVersion)pkm.Version;
            if (!pkm.IsMovesetRestricted(generation))
                version = Any;
            return GetValidMoves(pkm, version, evoChain, generation, types: types, RemoveTransferHM: RemoveTransferHM);
        }

        internal static IEnumerable<int> GetValidRelearn(PKM pkm, int species, int form, GameVersion version = Any)
        {
            return GetValidRelearn(pkm, species, form, Breeding.GetCanInheritMoves(species), version);
        }

        /// <summary>
        /// ONLY CALL FOR GEN2 EGGS
        /// </summary>
        internal static IEnumerable<int> GetExclusivePreEvolutionMoves(PKM pkm, int Species, IReadOnlyList<EvoCriteria> evoChain, int generation, GameVersion Version)
        {
            var preevomoves = new List<int>();
            var evomoves = new List<int>();
            var index = EvolutionChain.GetEvoChainSpeciesIndex(evoChain, Species);
            for (int i = 0; i < evoChain.Count; i++)
            {
                int minLvLG2;
                var evo = evoChain[i];
                if (ParseSettings.AllowGen2MoveReminder(pkm))
                    minLvLG2 = 0;
                else if (i == evoChain.Count - 1) // minimum level, otherwise next learnable level
                    minLvLG2 = 5;
                else if (evo.RequiresLvlUp)
                    minLvLG2 = evo.Level + 1;
                else
                    minLvLG2 = evo.Level;

                var moves = GetMoves(pkm, evo.Species, evo.Form, evo.Level, 0, minLvLG2, Version: Version, types: MoveSourceType.ExternalSources, RemoveTransferHM: false, generation: generation);
                var list = i >= index ? preevomoves : evomoves;
                list.AddRange(moves);
            }
            return preevomoves.Except(evomoves).Distinct();
        }

        internal static IEnumerable<int> GetValidMoves(PKM pkm, GameVersion version, IReadOnlyList<EvoCriteria> chain, int generation, MoveSourceType types = MoveSourceType.Reminder, bool RemoveTransferHM = true)
        {
            var r = new List<int> { 0 };
            int species = pkm.Species;

            if (FormChangeMovesRetain.Contains(species)) // Deoxys & Shaymin & Giratina (others don't have extra but whatever)
                return GetValidMovesAllForms(pkm, chain, version, generation, types, RemoveTransferHM, species, r);

            // Generation 1 & 2 do not always have move relearning capability, so the bottom bound for learnable indexes needs to be determined.
            var minLvLG1 = 0;
            var minLvLG2 = 0;
            for (var i = 0; i < chain.Count; i++)
            {
                var evo = chain[i];
                bool encounteredEvo = i == chain.Count - 1;

                if (generation <= 2)
                {
                    if (encounteredEvo) // minimum level, otherwise next learnable level
                        minLvLG1 = evo.MinLevel + 1;
                    else // learns level up moves immediately after evolving
                        minLvLG1 = evo.MinLevel;

                    if (!ParseSettings.AllowGen2MoveReminder(pkm))
                        minLvLG2 = minLvLG1;
                }

                var maxLevel = evo.Level;
                if (!encounteredEvo) // evolution
                    ++maxLevel; // allow lvlmoves from the level it evolved to the next species
                var moves = GetMoves(pkm, evo.Species, evo.Form, maxLevel, minLvLG1, minLvLG2, version, types, RemoveTransferHM, generation);
                r.AddRange(moves);
            }

            if (pkm.Format <= 3)
                return r.Distinct();

            if (types.HasFlagFast(MoveSourceType.LevelUp))
                MoveTutor.AddSpecialFormChangeMoves(r, pkm, generation, species);
            if (types.HasFlagFast(MoveSourceType.SpecialTutor))
                MoveTutor.AddSpecialTutorMoves(r, pkm, generation, species);
            if (types.HasFlagFast(MoveSourceType.RelearnMoves) && generation >= 6)
                r.AddRange(pkm.RelearnMoves);
            return r.Distinct();
        }

        internal static IEnumerable<int> GetValidMovesAllForms(PKM pkm, IReadOnlyList<EvoCriteria> chain, GameVersion version, int generation, MoveSourceType types, bool RemoveTransferHM, int species, List<int> r)
        {
            // These don't evolve, so don't bother iterating for all entries in the evolution chain (should always be count==1).
            int formCount;

            // In gen 3 deoxys has different forms depending on the current game, in the PersonalInfo there is no alternate form info
            if (pkm.Format == 3 && species == (int) Species.Deoxys)
                formCount = 4;
            else
                formCount = pkm.PersonalInfo.FormCount;

            for (int form = 0; form < formCount; form++)
                r.AddRange(GetMoves(pkm, species, form, chain[0].Level, 0, 0, version, types, RemoveTransferHM, generation));
            if (types.HasFlagFast(MoveSourceType.RelearnMoves))
                r.AddRange(pkm.RelearnMoves);
            return r.Distinct();
        }

        private static IEnumerable<int> GetMoves(PKM pkm, int species, int form, int maxLevel, int minlvlG1, int minlvlG2, GameVersion Version, MoveSourceType types, bool RemoveTransferHM, int generation)
        {
            var r = new List<int>();
            if (types.HasFlagFast(MoveSourceType.LevelUp))
                r.AddRange(MoveLevelUp.GetMovesLevelUp(pkm, species, form, maxLevel, minlvlG1, minlvlG2, Version, types.HasFlagFast(MoveSourceType.Reminder), generation));
            if (types.HasFlagFast(MoveSourceType.Machine))
                r.AddRange(MoveTechnicalMachine.GetTMHM(pkm, species, form, generation, Version, RemoveTransferHM));
            if (types.HasFlagFast(MoveSourceType.TechnicalRecord))
                r.AddRange(MoveTechnicalMachine.GetRecords(pkm, species, form, generation));
            if (types.HasFlagFast(MoveSourceType.AllTutors))
                r.AddRange(MoveTutor.GetTutorMoves(pkm, species, form, types.HasFlagFast(MoveSourceType.SpecialTutor), generation));
            return r.Distinct();
        }
    }

    [Flags]
#pragma warning disable RCS1154 // Sort enum members.
    public enum MoveSourceType
#pragma warning restore RCS1154 // Sort enum members.
    {
        None,
        LevelUp         = 1 << 0,
        RelearnMoves    = 1 << 1,
        Machine         = 1 << 2,
        TypeTutor       = 1 << 3,
        SpecialTutor    = 1 << 4,
        EnhancedTutor   = 1 << 5,
        SharedEggMove   = 1 << 6,
        TechnicalRecord = 1 << 7,

        AllTutors = TypeTutor | SpecialTutor | EnhancedTutor,
        AllMachines = Machine | TechnicalRecord,

        Reminder = LevelUp | RelearnMoves | TechnicalRecord,
        Encounter = LevelUp | RelearnMoves,
        ExternalSources = Reminder | AllMachines | AllTutors,
        All = ExternalSources | SharedEggMove | RelearnMoves,
    }

    public static class MoveSourceTypeExtensions
    {
        public static bool HasFlagFast(this MoveSourceType value, MoveSourceType flag) => (value & flag) != 0;
        public static MoveSourceType ClearNonEggSources(this MoveSourceType value) => value & MoveSourceType.Encounter;
    }
}
