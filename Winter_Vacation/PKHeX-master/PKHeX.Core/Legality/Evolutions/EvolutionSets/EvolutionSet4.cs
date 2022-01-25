using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 4 Evolution Branch Entries
    /// </summary>
    public static class EvolutionSet4
    {
        private static EvolutionMethod GetMethod(ReadOnlySpan<byte> data)
        {
            var method = data[0]; // other byte unnecessary
            int arg = ReadUInt16LittleEndian(data[2..]);
            int species = ReadUInt16LittleEndian(data[4..]);

            if (method == 0)
                throw new ArgumentOutOfRangeException(nameof(method));

            // To have the same structure as gen 6
            // Gen 4 Method 6 is Gen 6 Method 7, G4 7 = G6 8, and so on
            if (method > 6)
                method++;

            var lvl = EvolutionSet6.EvosWithArg.Contains(method) ? 0 : arg;
            return new EvolutionMethod(method, species, argument: arg, level: lvl);
        }

        public static IReadOnlyList<EvolutionMethod[]> GetArray(ReadOnlySpan<byte> data)
        {
            const int bpe = 6; // bytes per evolution entry
            const int entries = 7; // amount of entries per species
            const int size = (entries * bpe) + 2; // bytes per species entry, + 2 alignment bytes

            var evos = new EvolutionMethod[data.Length / size][];
            for (int i = 0; i < evos.Length; i++)
            {
                int offset = i * size;
                int count = 0;
                for (; count < entries; count++)
                {
                    var methodOffset = offset + (count * bpe);
                    var method = data[methodOffset];
                    if (method == 0)
                        break;
                }
                if (count == 0)
                {
                    evos[i] = Array.Empty<EvolutionMethod>();
                    continue;
                }

                var set = new EvolutionMethod[count];
                for (int j = 0; j < set.Length; j++)
                    set[j] = GetMethod(data.Slice(offset + (j * bpe), bpe));
                evos[i] = set;
            }
            return evos;
        }
    }
}
