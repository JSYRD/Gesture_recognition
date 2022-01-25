﻿using System.Collections.Generic;

namespace PKHeX.Core
{
    internal sealed class LearnInfo
    {
        public bool MixedGen12NonTradeback { get; set; }
        public List<int> Gen1Moves { get; } = new();
        public List<int> Gen2PreevoMoves { get; } = new();
        public List<int> EggMovesLearned { get; } = new();
        public List<int> LevelUpEggMoves { get; } = new();
        public List<int> EventEggMoves { get; } = new();

        public readonly MoveParseSource Source;
        public readonly bool IsGen2Pkm;

        public LearnInfo(PKM pkm, MoveParseSource source)
        {
            IsGen2Pkm = pkm.Format == 2 || pkm.VC2;
            Source = source;
        }
    }
}
