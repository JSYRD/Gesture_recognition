﻿using static PKHeX.Core.ContestStatGranting;
using static PKHeX.Core.ContestStatInfo;
using static PKHeX.Core.LegalityCheckStrings;

namespace PKHeX.Core;

/// <summary>
/// Verifies the Contest stat details.
/// </summary>
public sealed class ContestStatVerifier : Verifier
{
    protected override CheckIdentifier Identifier => CheckIdentifier.Memory;
    public override void Verify(LegalityAnalysis data)
    {
        var pkm = data.pkm;
        if (pkm is not IContestStats s)
            return;

        // If no stats have been increased from the initial amount, then we're done here.
        // some encounters have contest stats built in. they're already checked by the initial encounter match.
        if (!s.HasContestStats())
            return;

        // Check the correlation of Stats & Sheen!
        // In generations 3,4 and BDSP, blocks/poffins have a feel(sheen) equal to sheen=sum(stats)/5, with +/- 10% for a favored stat.
        // In generation 6 (ORAS), they don't award any sheen, so any value is legal.

        var correlation = GetContestStatRestriction(pkm, data.Info.Generation);
        if (correlation == None)
        {
            // We're only here because we have contest stat values. We aren't permitted to have any, so flag it.
            data.AddLine(GetInvalid(LContestZero));
        }
        else if (correlation == NoSheen)
        {
            // We can get contest stat values, but we can't get any for Sheen.
            // Any combination of non-sheen is ok, but nonzero sheen is illegal.
            if (s.CNT_Sheen != 0)
                data.AddLine(GetInvalid(LContestZeroSheen));
        }
        else if (correlation == CorrelateSheen)
        {
            bool gen3 = data.Info.Generation == 3;
            bool bdsp = pkm.HasVisitedBDSP(data.Info.EncounterOriginal.Species);
            var method = gen3 ? ContestStatGrantingSheen.Gen3 :
                bdsp ? ContestStatGrantingSheen.Gen8b : ContestStatGrantingSheen.Gen4;

            // Check for stat values that exceed a valid sheen value.
            var initial = GetReferenceTemplate(data.Info.EncounterMatch);
            var minSheen = CalculateMinimumSheen(s, initial, pkm, method);

            if (s.CNT_Sheen < minSheen)
                data.AddLine(GetInvalid(string.Format(LContestSheenTooLow_0, minSheen)));

            // Check for sheen values that are too high.
            var maxSheen = CalculateMaximumSheen(s, pkm.Nature, initial, gen3);
            if (s.CNT_Sheen > maxSheen)
                data.AddLine(GetInvalid(string.Format(LContestSheenTooHigh_0, maxSheen)));
        }
        else if (correlation == Mixed)
        {
            bool gen3 = data.Info.Generation == 3;

            // Check for sheen values that are too high.
            var initial = GetReferenceTemplate(data.Info.EncounterMatch);
            var maxSheen = CalculateMaximumSheen(s, pkm.Nature, initial, gen3);
            if (s.CNT_Sheen > maxSheen)
                data.AddLine(GetInvalid(string.Format(LContestSheenTooHigh_0, maxSheen)));
        }
    }
}
