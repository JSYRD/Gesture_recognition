﻿namespace PKHeX.Core
{
    public enum PokeSize
    {
        XS,
        S,
        AV,
        L,
        XL,
    }

    public static class PokeSizeUtil
    {
        /// <summary>
        /// Compares the sizing scalar to different thresholds to determine the size rating.
        /// </summary>
        /// <param name="scalar">Sizing scalar (0-255)</param>
        /// <returns>0-4 rating</returns>
        public static PokeSize GetSizeRating(int scalar) => scalar switch
        {
            < 0x10 => PokeSize.XS, // 1/16 = XS
            < 0x30 => PokeSize.S,  // 2/16 = S
            < 0xD0 => PokeSize.AV, // average (10/16)
            < 0xF0 => PokeSize.L,  // 2/16 = L
                 _ => PokeSize.XL, // 1/16 = XL
        };

        public static int GetRandomScalar(this PokeSize size) => size switch
        {
            PokeSize.XS => Util.Rand.Next(0x10),
            PokeSize.S  => Util.Rand.Next(0x20) + 0x10,
            PokeSize.AV => Util.Rand.Next(0xA0) + 0x30,
            PokeSize.L  => Util.Rand.Next(0x20) + 0xD0,
            PokeSize.XL => Util.Rand.Next(0x10) + 0xF0,
            _ => GetRandomScalar(),
        };

        /// <summary>
        /// Gets a random size scalar with a triangular distribution (copying official implementation).
        /// </summary>
        public static int GetRandomScalar()
        {
            var rnd = Util.Rand;
            return rnd.Next(0x81) + rnd.Next(0x80);
        }
    }
}
