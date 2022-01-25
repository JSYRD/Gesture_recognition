using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <summary>
    /// Unpacks <see cref="Learnset"/> data from legality binary inputs.
    /// </summary>
    public static class LearnsetReader
    {
        private static readonly Learnset EMPTY = new(Array.Empty<int>(), Array.Empty<int>());

        /// <summary>
        /// Loads a learnset using the 8-bit-per-move storage structure used by Generation 1 &amp; 2 games.
        /// </summary>
        /// <param name="input">Raw ROM data containing the contiguous moves</param>
        /// <param name="maxSpecies">Highest species ID for the input game.</param>
        public static Learnset[] GetArray(byte[] input, int maxSpecies)
        {
            var data = new Learnset[maxSpecies + 1];

            int offset = 0;
            for (int s = 0; s < data.Length; s++)
                data[s] = ReadLearnset8(input, ref offset);

            return data;
        }

        /// <summary>
        /// Loads a learnset by reading 16-bit move,level pairs.
        /// </summary>
        /// <param name="entries">Entry data</param>
        public static Learnset[] GetArray(byte[][] entries)
        {
            Learnset[] data = new Learnset[entries.Length];
            for (int i = 0; i < data.Length; i++)
                data[i] = ReadLearnset16(entries[i]);
            return data;
        }

        /// <summary>
        /// Reads a Level up move pool definition from a contiguous chunk of GB era ROM data.
        /// </summary>
        /// <remarks>Moves and Levels are 8-bit</remarks>
        private static Learnset ReadLearnset8(ReadOnlySpan<byte> data, ref int offset)
        {
            int end = offset; // scan for count
            if (data[end] == 0)
            {
                ++offset;
                return EMPTY;
            }
            do { end += 2; } while (data[end] != 0);

            var Count = (end - offset) / 2;
            var Moves = new int[Count];
            var Levels = new int[Count];
            for (int i = 0; i < Moves.Length; i++)
            {
                Levels[i] = data[offset++];
                Moves[i] = data[offset++];
            }
            ++offset;
            return new Learnset(Moves, Levels);
        }

        /// <summary>
        /// Reads a Level up move pool definition from a single move pool definition.
        /// </summary>
        /// <remarks>Count of moves, followed by Moves and Levels which are 16-bit</remarks>
        private static Learnset ReadLearnset16(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
                return EMPTY;
            var Count = (data.Length / 4) - 1;
            var Moves = new int[Count];
            var Levels = new int[Count];
            for (int i = 0; i < Count; i++)
            {
                var move = data.Slice(i * 4, 4);
                Moves[i] = ReadInt16LittleEndian(move);
                Levels[i] = ReadInt16LittleEndian(move[2..]);
            }
            return new Learnset(Moves, Levels);
        }
    }
}
