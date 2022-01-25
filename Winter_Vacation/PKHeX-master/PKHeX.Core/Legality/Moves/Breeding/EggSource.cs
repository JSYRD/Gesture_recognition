﻿using System;
using static PKHeX.Core.LegalityCheckStrings;

namespace PKHeX.Core
{
    /// <summary>
    /// Egg Moveset Building Order for Generation 2
    /// </summary>
    public enum EggSource2 : byte
    {
        None,
        /// <summary> Initial moveset for the egg's level. </summary>
        Base,
        /// <summary> Egg move inherited from the Father. </summary>
        FatherEgg,
        /// <summary> Technical Machine move inherited from the Father. </summary>
        FatherTM,
        /// <summary> Level Up move inherited from a parent. </summary>
        ParentLevelUp,
        /// <summary> Tutor move (Elemental Beam) inherited from a parent. </summary>
        Tutor,

        Max,
    }

    /// <summary>
    /// Egg Moveset Building Order for Generation 3 &amp; 4
    /// </summary>
    public enum EggSource34 : byte
    {
        None,
        /// <summary> Initial moveset for the egg's level. </summary>
        Base,
        /// <summary> Egg move inherited from the Father. </summary>
        FatherEgg,
        /// <summary> Technical Machine move inherited from the Father. </summary>
        FatherTM,
        /// <summary> Level Up move inherited from a parent. </summary>
        ParentLevelUp,

        Max,

        /// <summary> Special Move applied at the end if certain conditions are satisfied. </summary>
        VoltTackle,
    }

    /// <summary>
    /// Egg Moveset Building Order for Generation 5
    /// </summary>
    public enum EggSource5 : byte
    {
        None,
        /// <summary> Initial moveset for the egg's level. </summary>
        Base,
        /// <summary> Egg move inherited from the Father. </summary>
        FatherEgg,
        /// <summary> Technical Machine move inherited from the Father. </summary>
        ParentLevelUp,
        /// <summary> Technical Machine move inherited from the Father. </summary>
        /// <remarks> After level up, unlike Gen3/4! </remarks>
        FatherTM,

        Max,

        /// <summary> Special Move applied at the end if certain conditions are satisfied. </summary>
        VoltTackle,
    }

    /// <summary>
    /// Egg Moveset Building Order for Generation 6+
    /// </summary>
    public enum EggSource6 : byte
    {
        None,
        /// <summary> Initial moveset for the egg's level. </summary>
        Base,
        /// <summary> Level Up move inherited from a parent. </summary>
        ParentLevelUp,
        /// <summary> Egg move inherited from a parent. </summary>
        ParentEgg,

        Max,

        /// <summary> Special Move applied at the end if certain conditions are satisfied. </summary>
        VoltTackle,
    }

    /// <summary>
    /// Utility logic for converting a <see cref="MoveBreed"/> move result into a user friendly string.
    /// </summary>
    public static class EggSourceUtil
    {
        /// <summary>
        /// Unboxes the parse result and returns a user friendly string for the move result.
        /// </summary>
        public static string GetSource(object parse, int generation, int index)
        {
            static string GetLine<T>(T[] arr, Func<T, string> act, int index)
            {
                if ((uint)index >= arr.Length)
                    return LMoveSourceEmpty;
                return act(arr[index]);
            }

            return generation switch
            {
                2      => GetLine((EggSource2[]) parse, GetSource, index),
                3 or 4 => GetLine((EggSource34[])parse, GetSource, index),
                5      => GetLine((EggSource5[]) parse, GetSource, index),
                >= 6   => GetLine((EggSource6[]) parse, GetSource, index),
                _      => LMoveSourceEmpty,
            };
        }

        public static string GetSource(this EggSource2 source) => source switch
        {
            EggSource2.Base => LMoveRelearnEgg,
            EggSource2.FatherEgg => LMoveEggInherited,
            EggSource2.FatherTM => LMoveEggTMHM,
            EggSource2.ParentLevelUp => LMoveEggLevelUp,
            EggSource2.Tutor => LMoveEggInheritedTutor,
            EggSource2.Max => "Any",
            _ => LMoveEggInvalid,
        };

        public static string GetSource(this EggSource34 source) => source switch
        {
            EggSource34.Base => LMoveRelearnEgg,
            EggSource34.FatherEgg => LMoveEggInherited,
            EggSource34.FatherTM => LMoveEggTMHM,
            EggSource34.ParentLevelUp => LMoveEggLevelUp,
            EggSource34.Max => "Any",
            EggSource34.VoltTackle => LMoveSourceSpecial,
            _ => LMoveEggInvalid,
        };

        public static string GetSource(this EggSource5 source) => source switch
        {
            EggSource5.Base => LMoveRelearnEgg,
            EggSource5.FatherEgg => LMoveEggInherited,
            EggSource5.ParentLevelUp => LMoveEggLevelUp,
            EggSource5.FatherTM => LMoveEggTMHM,
            EggSource5.Max => "Any",
            EggSource5.VoltTackle => LMoveSourceSpecial,
            _ => LMoveEggInvalid,
        };

        public static string GetSource(this EggSource6 source) => source switch
        {
            EggSource6.Base => LMoveRelearnEgg,
            EggSource6.ParentLevelUp => LMoveEggLevelUp,
            EggSource6.ParentEgg => LMoveEggInherited,
            EggSource6.Max => "Any",
            EggSource6.VoltTackle => LMoveSourceSpecial,
            _ => LMoveEggInvalid,
        };
    }
}
