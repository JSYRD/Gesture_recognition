﻿using System;
using System.Collections.Generic;

namespace PKHeX.Core
{
    /// <summary>
    /// Stat/misc data for individual species or their associated alternate form data.
    /// </summary>
    public abstract class PersonalInfo
    {
        /// <summary>
        /// Raw Data
        /// </summary>
        protected readonly byte[] Data;

        protected PersonalInfo(byte[] data) => Data = data;

        /// <summary>
        /// Writes entry to raw bytes.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] Write();

        /// <summary>
        /// Base HP
        /// </summary>
        public abstract int HP { get; set; }

        /// <summary>
        /// Base Attack
        /// </summary>
        public abstract int ATK { get; set; }

        /// <summary>
        /// Base Defense
        /// </summary>
        public abstract int DEF { get; set; }

        /// <summary>
        /// Base Speed
        /// </summary>
        public abstract int SPE { get; set; }

        /// <summary>
        /// Base Special Attack
        /// </summary>
        public abstract int SPA { get; set; }

        /// <summary>
        /// Base Special Defense
        /// </summary>
        public abstract int SPD { get; set; }

        /// <summary>
        /// Base Stat values
        /// </summary>
        public IReadOnlyList<int> Stats => new[] { HP, ATK, DEF, SPE, SPA, SPD };

        /// <summary>
        /// Amount of HP Effort Values to yield when defeating this entry.
        /// </summary>
        public abstract int EV_HP { get; set; }

        /// <summary>
        /// Amount of Attack Effort Values to yield when defeating this entry.
        /// </summary>
        public abstract int EV_ATK { get; set; }

        /// <summary>
        /// Amount of Defense Effort Values to yield when defeating this entry.
        /// </summary>
        public abstract int EV_DEF { get; set; }

        /// <summary>
        /// Amount of Speed Effort Values to yield when defeating this entry.
        /// </summary>
        public abstract int EV_SPE { get; set; }

        /// <summary>
        /// Amount of Special Attack Effort Values to yield when defeating this entry.
        /// </summary>
        public abstract int EV_SPA { get; set; }

        /// <summary>
        /// Amount of Special Defense Effort Values to yield when defeating this entry.
        /// </summary>
        public abstract int EV_SPD { get; set; }

        /// <summary>
        /// Primary Type
        /// </summary>
        public abstract int Type1 { get; set; }

        /// <summary>
        /// Secondary Type
        /// </summary>
        public abstract int Type2 { get; set; }

        /// <summary>
        /// First Egg Group
        /// </summary>
        public abstract int EggGroup1 { get; set; }

        /// <summary>
        /// Second Egg Group
        /// </summary>
        public abstract int EggGroup2 { get; set; }

        /// <summary>
        /// Catch Rate
        /// </summary>
        public abstract int CatchRate { get; set; }

        /// <summary>
        /// Evolution Stage value (or equivalent for un-evolved).
        /// </summary>
        public virtual int EvoStage { get; set; }

        /// <summary>
        /// Held Items the entry can be randomly encountered with.
        /// </summary>
        public abstract IReadOnlyList<int> Items { get; set; }

        /// <summary>
        /// Gender Ratio value determining if the entry is a fixed gender or bi-gendered.
        /// </summary>
        public abstract int Gender { get; set; }

        /// <summary>
        /// Amount of Hatching Step Cycles required to hatch if in an egg.
        /// </summary>
        public abstract int HatchCycles { get; set; }

        /// <summary>
        /// Initial Friendship when captured or received.
        /// </summary>
        public abstract int BaseFriendship { get; set; }

        /// <summary>
        /// Experience-Level Growth Rate type
        /// </summary>
        public abstract int EXPGrowth { get; set; }

        /// <summary>
        /// Full list of <see cref="PKM.Ability"/> values the entry can have.
        /// </summary>
        public abstract IReadOnlyList<int> Abilities { get; set; }

        /// <summary>
        /// Gets the ability index without creating an array and looking through it.
        /// </summary>
        /// <param name="abilityID">Ability ID</param>
        /// <returns>Ability Index</returns>
        public abstract int GetAbilityIndex(int abilityID);

        /// <summary>
        /// Escape factor used for fleeing the Safari Zone or calling for help in SOS Battles.
        /// </summary>
        public abstract int EscapeRate { get; set; }

        /// <summary>
        /// Count of <see cref="PKM.Form"/> values the entry can have.
        /// </summary>
        public virtual int FormCount { get; set; } = 1;

        /// <summary>
        /// Pointer to the first <see cref="PKM.Form"/> <see cref="PersonalInfo"/> index
        /// </summary>
        protected internal virtual int FormStatsIndex { get; set; }

        /// <summary>
        /// Pointer to the <see cref="PKM.Form"/> sprite index.
        /// </summary>
        public virtual int FormSprite { get; set; }

        /// <summary>
        /// Base Experience Yield factor
        /// </summary>
        public abstract int BaseEXP { get; set; }

        /// <summary>
        /// Main color ID of the entry. The majority of the Pokémon's color is of this color, usually.
        /// </summary>
        public abstract int Color { get; set; }

        /// <summary>
        /// Height of the entry in meters (m).
        /// </summary>
        public virtual int Height { get; set; } = 0;

        /// <summary>
        /// Mass of the entry in kilograms (kg).
        /// </summary>
        public virtual int Weight { get; set; } = 0;

        /// <summary>
        /// TM/HM learn compatibility flags for individual moves.
        /// </summary>
        public bool[] TMHM = Array.Empty<bool>();

        /// <summary>
        /// Grass-Fire-Water-Etc typed learn compatibility flags for individual moves.
        /// </summary>
        public bool[] TypeTutors = Array.Empty<bool>();

        /// <summary>
        /// Special tutor learn compatibility flags for individual moves.
        /// </summary>
        public bool[][] SpecialTutors = Array.Empty<bool[]>();

        protected static bool[] GetBits(ReadOnlySpan<byte> data)
        {
            bool[] result = new bool[data.Length << 3];
            for (int i = result.Length - 1; i >= 0; i--)
                result[i] = (data[i >> 3] >> (i & 7) & 0x1) == 1;
            return result;
        }

        protected static void SetBits(bool[] bits, Span<byte> data)
        {
            for (int i = bits.Length - 1; i >= 0; i--)
                data[i>>3] |= (byte)(bits[i] ? 1 << (i&0x7) : 0);
        }

        /// <summary>
        /// Injects supplementary TM/HM compatibility which is not present in the generation specific <see cref="PersonalInfo"/> format.
        /// </summary>
        /// <param name="data">Data to read from</param>
        internal void AddTMHM(ReadOnlySpan<byte> data) => TMHM = GetBits(data);

        /// <summary>
        /// Injects supplementary Type Tutor compatibility which is not present in the generation specific <see cref="PersonalInfo"/> format.
        /// </summary>
        /// <param name="data">Data to read from</param>
        internal void AddTypeTutors(ReadOnlySpan<byte> data) => TypeTutors = GetBits(data);

        /// <summary>
        /// Gets the <see cref="PersonalTable"/> <see cref="PKM.Form"/> entry index for the input criteria, with fallback for the original species entry.
        /// </summary>
        /// <param name="species"><see cref="PKM.Species"/> to retrieve for</param>
        /// <param name="form"><see cref="PKM.Form"/> to retrieve for</param>
        /// <returns>Index the <see cref="PKM.Form"/> exists as in the <see cref="PersonalTable"/>.</returns>
        public int FormIndex(int species, int form)
        {
            if (!HasForm(form))
                return species;
            return FormStatsIndex + form - 1;
        }

        /// <summary>
        /// Checks if the <see cref="PersonalInfo"/> has the requested <see cref="PKM.Form"/> entry index available.
        /// </summary>
        /// <param name="form"><see cref="PKM.Form"/> to retrieve for</param>
        public bool HasForm(int form)
        {
            if (form <= 0) // no form requested
                return false;
            if (FormStatsIndex <= 0) // no forms present
                return false;
            if (form >= FormCount) // beyond range of species' forms
                return false;
            return true;
        }

        /// <summary>
        /// Gets a random valid gender for the entry.
        /// </summary>
        public int RandomGender()
        {
            var fix = FixedGender;
            return fix >= 0 ? fix : Util.Rand.Next(2);
        }

        /// <summary>
        /// Gets a gender value. Returns -1 if the entry <see cref="IsDualGender"/>.
        /// </summary>
        public int FixedGender
        {
            get
            {
                if (Genderless)
                    return 2;
                if (OnlyFemale)
                    return 1;
                if (OnlyMale)
                    return 0;
                return -1;
            }
        }

        public const int RatioMagicGenderless = 255;
        public const int RatioMagicFemale = 254;
        public const int RatioMagicMale = 0;

        public static bool IsSingleGender(int gt) => (uint)(gt - 1) >= 253;

        /// <summary>
        /// Indicates that the entry has two genders.
        /// </summary>
        public bool IsDualGender => (uint)(Gender - 1) < 253;

        /// <summary>
        /// Indicates that the entry is exclusively Genderless.
        /// </summary>
        public bool Genderless => Gender == RatioMagicGenderless;

        /// <summary>
        /// Indicates that the entry is exclusively Female gendered.
        /// </summary>
        public bool OnlyFemale => Gender == RatioMagicFemale;

        /// <summary>
        /// Indicates that the entry is exclusively Male gendered.
        /// </summary>
        public bool OnlyMale => Gender == RatioMagicMale;

        /// <summary>
        /// Indicates if the entry has forms or not.
        ///  </summary>
        public bool HasForms => FormCount > 1;

        /// <summary>
        /// Base Stat Total sum of all stats.
        /// </summary>
        public int BST => HP + ATK + DEF + SPE + SPA + SPD;

        /// <summary>
        /// Checks to see if the <see cref="PKM.Form"/> is valid within the <see cref="FormCount"/>
        /// </summary>
        /// <param name="form"></param>
        public bool IsFormWithinRange(int form)
        {
            if (form == 0)
                return true;
            return form < FormCount;
        }

        /// <summary>
        /// Checks to see if the provided Types match the entry's types.
        /// </summary>
        /// <remarks>Input order matters! If input order does not matter, use <see cref="IsType(int, int)"/> instead.</remarks>
        /// <param name="type1">First type</param>
        /// <param name="type2">Second type</param>
        /// <returns>Typing is an exact match</returns>
        public bool IsValidTypeCombination(int type1, int type2) => Type1 == type1 && Type2 == type2;

        /// <summary>
        /// Checks if the entry has either type equal to the input type.
        /// </summary>
        /// <param name="type1">Type</param>
        /// <returns>Typing is present in entry</returns>
        public bool IsType(int type1) => Type1 == type1 || Type2 == type1;

        /// <summary>
        /// Checks if the entry has either type equal to both input types.
        /// </summary>
        /// <remarks>Input order does not matter.</remarks>
        /// <param name="type1">Type 1</param>
        /// <param name="type2">Type 2</param>
        /// <returns>Typing is present in entry</returns>
        public bool IsType(int type1, int type2) => (Type1 == type1 || Type2 == type1) && (Type1 == type2 || Type2 == type2);

        /// <summary>
        /// Checks if the entry has either egg group equal to the input type.
        /// </summary>
        /// <param name="group">Egg group</param>
        /// <returns>Egg is present in entry</returns>
        public bool IsEggGroup(int group) => EggGroup1 == group || EggGroup2 == group;
    }
}
