﻿using System.Collections.Generic;

namespace PKHeX.Core
{
    /// <summary>
    /// Shadow Pokémon Encounter found in <see cref="GameVersion.CXD"/>
    /// </summary>
    /// <inheritdoc cref="EncounterStatic"/>
    /// <param name="ID">Initial Shadow Gauge value.</param>
    /// <param name="Gauge">Initial Shadow Gauge value.</param>
    /// <param name="Locks">Team Specification with required <see cref="Species"/>, <see cref="Nature"/> and Gender.</param>
    // ReSharper disable NotAccessedPositionalProperty.Global
    public sealed record EncounterStaticShadow(GameVersion Version, byte ID, short Gauge, TeamLock[] Locks) : EncounterStatic(Version)
    {
        // ReSharper restore NotAccessedPositionalProperty.Global
        public override int Generation => 3;

        /// <summary>
        /// Originates from the EReader scans (Japanese Only)
        /// </summary>
        public bool EReader => ReferenceEquals(IVs, EReaderEmpty);

        public static readonly IReadOnlyList<int> EReaderEmpty = new[] {0,0,0,0,0,0};

        protected override bool IsMatchLocation(PKM pkm)
        {
            if (pkm.Format != 3)
                return true; // transfer location verified later

            var met = pkm.Met_Location;
            if (met == Location)
                return true;

            // XD can re-battle with Miror B
            // Realgam Tower, Rock, Oasis, Cave, Pyrite Town
            return Version == GameVersion.XD && met is (59 or 90 or 91 or 92 or 113);
        }

        protected override bool IsMatchLevel(PKM pkm, DexLevel evo)
        {
            if (pkm.Format != 3) // Met Level lost on PK3=>PK4
                return Level <= evo.Level;

            return pkm.Met_Level == Level;
        }

        protected override void ApplyDetails(ITrainerInfo sav, EncounterCriteria criteria, PKM pk)
        {
            base.ApplyDetails(sav, criteria, pk);
            ((IRibbonSetEvent3)pk).RibbonNational = true;
        }

        protected override void SetPINGA(PKM pk, EncounterCriteria criteria)
        {
            var pi = pk.PersonalInfo;
            int gender = criteria.GetGender(-1, pi);
            int nature = (int)criteria.GetNature(Nature.Random);
            int ability = criteria.GetAbilityFromNumber(0);

            // Ensure that any generated specimen has valid Shadow Locks
            // This can be kinda slow, depending on how many locks / how strict they are.
            // Cancel this operation if too many attempts are made to prevent infinite loops.
            int ctr = 0;
            const int max = 100_000;
            do
            {
                PIDGenerator.SetRandomWildPID(pk, 3, nature, ability, gender, PIDType.CXD);
                var pidiv = MethodFinder.Analyze(pk);
                var result = LockFinder.IsAllShadowLockValid(this, pidiv, pk);
                if (result)
                    break;
            }
            while (++ctr <= max);

#if DEBUG
            System.Diagnostics.Debug.Assert(ctr < 100_000);
#endif

            // E-Reader have all IVs == 0
            if (EReader)
            {
                for (int i = 0; i < IVs.Count; i++)
                    pk.SetIV(i, 0);
            }
        }
    }
}
