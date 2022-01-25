﻿using System.Linq;
using FluentAssertions;
using Xunit;
using static PKHeX.Core.Species;
using static PKHeX.Core.Move;

namespace PKHeX.Core.Tests.Legality;

public static class TempTests
{
    [Theory]
    [InlineData(Taillow, Boomburst)]
    [InlineData(Plusle, TearfulLook)] [InlineData(Minun, TearfulLook)]
    [InlineData(Luvdisc, HealPulse)]
    [InlineData(Starly, Detect)]
    [InlineData(Chatot, Boomburst)] [InlineData(Chatot, Encore)]
    [InlineData(Spiritomb, FoulPlay)]
    public static void CanLearnEggMoveBDSP(Species species, Move move)
    {
        MoveEgg.GetEggMoves(8, (int)species, 0, GameVersion.BD).Contains((int)move).Should().BeFalse();

        var pb8 = new PB8 { Species = (int)species };
        var encs = EncounterMovesetGenerator.GenerateEncounters(pb8, new[] { (int)move }, GameVersion.BD);
        encs.Any().Should().BeFalse("Unavailable until HOME update supports BD/SP.");
    }
}
