﻿using System;
using System.Collections.Generic;

namespace PKHeX.Core
{
    /// <inheritdoc cref="EncounterArea" />
    /// <summary>
    /// <see cref="GameVersion.RBY"/> encounter area
    /// </summary>
    public sealed record EncounterArea1 : EncounterArea
    {
        public readonly int Rate;
        public readonly EncounterSlot1[] Slots;

        protected override IReadOnlyList<EncounterSlot> Raw => Slots;

        public static EncounterArea1[] GetAreas(byte[][] input, GameVersion game)
        {
            var result = new EncounterArea1[input.Length];
            for (int i = 0; i < input.Length; i++)
                result[i] = new EncounterArea1(input[i], game);
            return result;
        }

        private EncounterArea1(ReadOnlySpan<byte> data, GameVersion game) : base(game)
        {
            Location = data[0];
            // 1 byte unused
            Type = (SlotType)data[2];
            Rate = data[3];

            int count = (data.Length - 4) / 4;
            var slots = new EncounterSlot1[count];
            for (int i = 0; i < slots.Length; i++)
            {
                int offset = 4 + (4 * i);
                var entry = data.Slice(offset, 4);
                slots[i] = ReadSlot(entry);
            }
            Slots = slots;
        }

        private EncounterSlot1 ReadSlot(ReadOnlySpan<byte> entry)
        {
            int species = entry[0];
            int slotNum = entry[1];
            int min = entry[2];
            int max = entry[3];
            return new EncounterSlot1(this, species, min, max, slotNum);
        }

        public override IEnumerable<EncounterSlot> GetMatchingSlots(PKM pkm, IReadOnlyList<EvoCriteria> chain)
        {
            int rate = pkm is PK1 pk1 ? pk1.Catch_Rate : -1;
            foreach (var slot in Slots)
            {
                foreach (var evo in chain)
                {
                    if (slot.Species != evo.Species)
                        continue;

                    if (slot.LevelMin > evo.Level)
                        break;
                    if (slot.Form != evo.Form)
                        break;

                    if (rate != -1)
                    {
                        var expect = (slot.Version == GameVersion.YW ? PersonalTable.Y : PersonalTable.RB)[slot.Species].CatchRate;
                        if (expect != rate && !(ParseSettings.AllowGen1Tradeback && GBRestrictions.IsTradebackCatchRate(rate)))
                            break;
                    }
                    yield return slot;
                    break;
                }
            }
        }
    }
}
