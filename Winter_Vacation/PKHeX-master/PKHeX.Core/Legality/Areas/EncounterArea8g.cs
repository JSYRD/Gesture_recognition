﻿using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <inheritdoc cref="EncounterArea" />
    /// <summary>
    /// <see cref="GameVersion.GO"/> encounter area for direct-to-HOME transfers.
    /// </summary>
    public sealed record EncounterArea8g : EncounterArea, ISpeciesForm
    {
        /// <summary> Species for the area </summary>
        /// <remarks> Due to how the encounter data is packaged by PKHeX, each species-form is grouped together. </remarks>
        public int Species { get; }
        /// <summary> Form of the Species </summary>
        public int Form { get; }
        public readonly EncounterSlot8GO[] Slots;

        protected override IReadOnlyList<EncounterSlot> Raw => Slots;

        private EncounterArea8g(int species, int form, EncounterSlot8GO[] slots) : base(GameVersion.GO)
        {
            Species = species;
            Form = form;
            Location = Locations.GO8;
            Slots = slots;
        }

        internal static EncounterArea8g[] GetArea(byte[][] data)
        {
            var areas = new EncounterArea8g[data.Length];
            for (int i = 0; i < areas.Length; i++)
                areas[i] = GetArea(data[i]);
            return areas;
        }

        private const int entrySize = (2 * sizeof(int)) + 2;

        private static EncounterArea8g GetArea(ReadOnlySpan<byte> data)
        {
            var sf = ReadUInt16LittleEndian(data);
            int species = sf & 0x7FF;
            int form = sf >> 11;

            var group = GetGroup(species, form);

            var result = new EncounterSlot8GO[(data.Length - 2) / entrySize];
            var area = new EncounterArea8g(species, form, result);
            for (int i = 0; i < result.Length; i++)
            {
                var offset = (i * entrySize) + 2;
                var entry = data.Slice(offset, entrySize);
                result[i] = ReadSlot(entry, area, species, form, group);
            }

            return area;
        }

        private static EncounterSlot8GO ReadSlot(ReadOnlySpan<byte> entry, EncounterArea8g area, int species, int form, GameVersion group)
        {
            int start = ReadInt32LittleEndian(entry);
            int end = ReadInt32LittleEndian(entry[4..]);
            var sg = entry[8];
            var shiny = (Shiny)(sg & 0x3F);
            var gender = (Gender)(sg >> 6);
            var type = (PogoType)entry[9];
            return new EncounterSlot8GO(area, species, form, start, end, shiny, gender, type, group);
        }

        private static GameVersion GetGroup(int species, int form)
        {
            // Transfer Rules:
            // If it can exist in LGP/E, it uses LGP/E's move data for the initial moves.
            // Else, if it can exist in SW/SH, it uses SW/SH's move data for the initial moves.
            // Else, it must exist in US/UM, thus it uses US/UM's moves.

            var pt8 = PersonalTable.SWSH;
            var ptGG = PersonalTable.GG;

            var pi8 = (PersonalInfoSWSH)pt8[species];
            if (pi8.IsPresentInGame)
            {
                bool lgpe = (species is (<= 151 or 808 or 809)) && (form == 0 || ptGG[species].HasForm(form));
                return lgpe ? GameVersion.GG : GameVersion.SWSH;
            }
            if (species <= Legal.MaxSpeciesID_7_USUM)
            {
                bool lgpe = species <= 151 && (form == 0 || ptGG[species].HasForm(form));
                return lgpe ? GameVersion.GG : GameVersion.USUM;
            }

            throw new ArgumentOutOfRangeException(nameof(species));
        }

        public override IEnumerable<EncounterSlot> GetMatchingSlots(PKM pkm, IReadOnlyList<EvoCriteria> chain)
        {
            if (pkm.TSV == 0) // HOME doesn't assign TSV=0 to accounts.
                yield break;

            // Find the first chain that has slots defined.
            // Since it is possible to evolve before transferring, we only need the highest evolution species possible.
            // PoGoEncTool has already extrapolated the evolutions to separate encounters!
            var sf = chain.FirstOrDefault(z => z.Species == Species && (z.Form == Form || FormInfo.IsFormChangeable(Species, Form, z.Form, pkm.Format)));
            if (sf == null)
                yield break;

            var ball = (Ball)pkm.Ball;
            var met = Math.Max(sf.MinLevel, pkm.Met_Level);
            EncounterSlot8GO? deferredIV = null;

            foreach (var slot in Slots)
            {
                if (!slot.IsLevelWithinRange(met))
                    continue;
                if (!slot.IsBallValid(ball))
                    continue;
                if (!slot.Shiny.IsValid(pkm))
                    continue;
                if (slot.Gender != Gender.Random && (int)slot.Gender != pkm.Gender)
                    continue;

                if (!slot.GetIVsValid(pkm))
                {
                    deferredIV ??= slot;
                    continue;
                }

                yield return slot;
            }

            if (deferredIV != null)
                yield return deferredIV;
        }
    }
}
