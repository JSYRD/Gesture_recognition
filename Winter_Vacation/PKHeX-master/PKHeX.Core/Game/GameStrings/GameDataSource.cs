﻿using System;
using System.Collections.Generic;

namespace PKHeX.Core
{
    /// <summary>
    /// Bundles raw string inputs into lists that can be used in data binding.
    /// </summary>
    public sealed class GameDataSource
    {
        public static readonly IReadOnlyList<ComboItem> Regions = new List<ComboItem>
        {
            new ("Japan (日本)",      0),
            new ("Americas (NA/SA)",  1),
            new ("Europe (EU/AU)",    2),
            new ("China (中国大陆)",   4),
            new ("Korea (한국)",       5),
            new ("Taiwan (香港/台灣)", 6),
        };

        private static readonly List<ComboItem> LanguageList = new()
        {
            new ComboItem("JPN (日本語)",   (int)LanguageID.Japanese),
            new ComboItem("ENG (English)",  (int)LanguageID.English),
            new ComboItem("FRE (Français)", (int)LanguageID.French),
            new ComboItem("ITA (Italiano)", (int)LanguageID.Italian),
            new ComboItem("GER (Deutsch)",  (int)LanguageID.German),
            new ComboItem("ESP (Español)",  (int)LanguageID.Spanish),
            new ComboItem("KOR (한국어)",    (int)LanguageID.Korean),
            new ComboItem("CHS (简体中文)",  (int)LanguageID.ChineseS),
            new ComboItem("CHT (繁體中文)",  (int)LanguageID.ChineseT),
        };

        public GameDataSource(GameStrings s)
        {
            Strings = s;
            BallDataSource = GetBalls(s.itemlist);
            SpeciesDataSource = Util.GetCBList(s.specieslist);
            NatureDataSource = Util.GetCBList(s.natures);
            AbilityDataSource = Util.GetCBList(s.abilitylist);
            GroundTileDataSource = Util.GetUnsortedCBList(s.groundtiletypes, GroundTileTypeExtensions.ValidTileTypes);

            var moves = Util.GetCBList(s.movelist);
            HaXMoveDataSource = moves;
            var legal = new List<ComboItem>(moves);
            legal.RemoveAll(m => Legal.Z_Moves.Contains(m.Value));
            LegalMoveDataSource = legal;

            VersionDataSource = GetVersionList(s);

            Met = new MetDataSource(s);

            Empty = new ComboItem(s.Species[0], 0);
        }

        /// <summary> Strings that this object's lists were generated with. </summary>
        public readonly GameStrings Strings;

        /// <summary> Contains Met Data lists to source lists from. </summary>
        public readonly MetDataSource Met;

        /// <summary> Represents "(None)", localized to this object's language strings. </summary>
        public readonly ComboItem Empty;

        public readonly IReadOnlyList<ComboItem> SpeciesDataSource;
        public readonly IReadOnlyList<ComboItem> BallDataSource;
        public readonly IReadOnlyList<ComboItem> NatureDataSource;
        public readonly IReadOnlyList<ComboItem> AbilityDataSource;
        public readonly IReadOnlyList<ComboItem> VersionDataSource;
        public readonly IReadOnlyList<ComboItem> LegalMoveDataSource;
        public readonly IReadOnlyList<ComboItem> HaXMoveDataSource;
        public readonly IReadOnlyList<ComboItem> GroundTileDataSource;

        private static IReadOnlyList<ComboItem> GetBalls(string[] itemList)
        {
            // ignores Poke/Great/Ultra
            ReadOnlySpan<ushort> ball_nums = stackalloc ushort[] { 007, 576, 013, 492, 497, 014, 495, 493, 496, 494, 011, 498, 008, 006, 012, 015, 009, 005, 499, 010, 001, 016, 851 };
            ReadOnlySpan<byte>   ball_vals = stackalloc   byte[] { 007, 025, 013, 017, 022, 014, 020, 018, 021, 019, 011, 023, 008, 006, 012, 015, 009, 005, 024, 010, 001, 016, 026 };
            return Util.GetVariedCBListBall(itemList, ball_nums, ball_vals);
        }

        private static IReadOnlyList<ComboItem> GetVersionList(GameStrings s)
        {
            var list = s.gamelist;
            ReadOnlySpan<byte> games = stackalloc byte[]
            {
                48, 49, // 8 bdsp
                44, 45, // 8 swsh
                42, 43, // 7 gg
                30, 31, // 7 sm
                32, 33, // 7 usum
                24, 25, // 6 xy
                27, 26, // 6 oras
                21, 20, // 5 bw
                23, 22, // 5 b2w2
                10, 11, 12, // 4 dppt
                07, 08, // 4 hgss
                02, 01, 03, // 3 rse
                04, 05, // 3 frlg
                15,     // 3 cxd

                39, 40, 41, // 7vc2
                35, 36, 37, 38, // 7vc1
                34, // 7go
            };

            return Util.GetUnsortedCBList(list, games);
        }

        public List<ComboItem> GetItemDataSource(GameVersion game, int generation, IReadOnlyList<ushort> allowed, bool HaX = false)
        {
            var items = Strings.GetItemStrings(generation, game);
            return HaX ? Util.GetCBList(items) : Util.GetCBList(items, allowed);
        }

        public static IReadOnlyList<ComboItem> LanguageDataSource(int gen)
        {
            var languages = new List<ComboItem>(LanguageList);
            if (gen == 3)
                languages.RemoveAll(l => l.Value >= (int)LanguageID.Korean);
            else if (gen < 7)
                languages.RemoveAll(l => l.Value > (int)LanguageID.Korean);
            return languages;
        }
    }
}
