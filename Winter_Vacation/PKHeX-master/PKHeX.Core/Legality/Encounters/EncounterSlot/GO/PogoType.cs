namespace PKHeX.Core
{
    /// <summary>
    /// Encounter Type for various <see cref="GameVersion.GO"/> encounters.
    /// </summary>
    public enum PogoType : byte
    {
        None, // Don't use this.

        /// <summary> Wild encounter, no special requirements </summary>
        Wild,

        /// <summary> Pok�mon Egg, requires Lv. 1 and IV = 1 </summary>
        Egg,
        /// <summary> Strange Egg, requires Lv. 8 and IV = 1 </summary>
        EggS,

        /// <summary> Raid Boss, requires Lv. 20 and IV = 1 </summary>
        Raid = 10,
        /// <summary> Raid Boss (Mythical), requires Lv. 20 and IV = 10 </summary>
        RaidM,

        /// <summary> Field Research Reward, requires Lv. 15 and IV = 1 </summary>
        Research = 20,
        /// <summary> Field Research Reward (Mythical), requires Lv. 15 and IV = 10 </summary>
        ResearchM,
        /// <summary> Field Research Reward, requires Lv. 15 and IV = 10 (Pok� Ball only) </summary>
        ResearchP,

        /// <summary> GO Battle League Reward, requires Lv. 20 and IV = 1 </summary>
        GBL,
        /// <summary> GO Battle League Reward (Mythical), requires Lv. 20 and IV = 10 </summary>
        GBLM,
        /// <summary> GO Battle League Reward, requires Lv. 20 and IV = 0 </summary>
        /// <remarks> On GO Battle Day (September 18, 2021), IV floor and ceiling were both temporarily set to 0 for non-Legendary encounters. This was fixed at 14:43 UTC (September 17, 2021). </remarks>
        GBLZero,
        /// <summary> GO Battle League Reward, requires Lv. 20 and IV = 0 </summary>
        /// <remarks> On GO Battle Day (September 18, 2021), IV floor was set to 0 after a mishap that also set the IV ceiling to 0. </remarks>
        GBLDay,

        /// <summary> Purified, requires Lv. 8 and IV = 1 (Premier Ball only) </summary>
        Shadow = 30,
    }

    public static class PogoTypeExtensions
    {
        /// <summary>
        /// Gets the minimum level (relative to GO's 1-<see cref="EncountersGO.MAX_LEVEL"/>) the <see cref="encounterType"/> must have.
        /// </summary>
        /// <param name="encounterType">Descriptor indicating how the Pok�mon was encountered in GO.</param>
        public static int GetMinLevel(this PogoType encounterType) => encounterType switch
        {
            PogoType.EggS => 8,
            PogoType.Raid => 20,
            PogoType.RaidM => 20,
            PogoType.Research => 15,
            PogoType.ResearchM => 15,
            PogoType.ResearchP => 15,
            PogoType.GBL => 20,
            PogoType.GBLM => 20,
            PogoType.GBLZero => 20,
            PogoType.GBLDay => 20,
            PogoType.Shadow => 8,
            _ => 1,
        };

        /// <summary>
        /// Gets the minimum IVs (relative to GO's 0-15) the <see cref="encounterType"/> must have.
        /// </summary>
        /// <param name="encounterType">Descriptor indicating how the Pok�mon was encountered in GO.</param>
        /// <returns>Required minimum IV (0-15)</returns>
        public static int GetMinIV(this PogoType encounterType) => encounterType switch
        {
            PogoType.Wild => 0,
            PogoType.RaidM => 10,
            PogoType.ResearchM => 10,
            PogoType.ResearchP => 10,
            PogoType.GBLM => 10,
            PogoType.GBLZero => 0,
            PogoType.GBLDay => 0,
            _ => 1,
        };

        /// <summary>
        /// Gets the minimum IVs (relative to GO's 0-15) the <see cref="encounterType"/> must have.
        /// </summary>
        /// <param name="encounterType">Descriptor indicating how the Pok�mon was encountered in GO.</param>
        /// <returns>Required minimum IV (0-15)</returns>
        public static int GetMaxIV(this PogoType encounterType) => encounterType switch
        {
            PogoType.GBLZero => 0,
            _ => 15,
        };

        /// <summary>
        /// Checks if the <see cref="ball"/> is valid for the <see cref="encounterType"/>.
        /// </summary>
        /// <param name="encounterType">Descriptor indicating how the Pok�mon was encountered in GO.</param>
        /// <param name="ball">Current <see cref="Ball"/> the Pok�mon is in.</param>
        /// <returns>True if valid, false if invalid.</returns>
        public static bool IsBallValid(this PogoType encounterType, Ball ball)
        {
            var req = encounterType.GetValidBall();
            if (req == Ball.None)
                return (uint)(ball - 2) <= 2; // Poke, Great, Ultra
            return ball == req;
        }

        /// <summary>
        /// Gets a valid ball that the <see cref="encounterType"/> can have based on the type of capture in Pok�mon GO.
        /// </summary>
        /// <param name="encounterType">Descriptor indicating how the Pok�mon was encountered in GO.</param>
        /// <returns><see cref="Ball.None"/> if no specific ball is required, otherwise returns the required ball.</returns>
        public static Ball GetValidBall(this PogoType encounterType) => encounterType switch
        {
            PogoType.Egg => Ball.Poke,
            PogoType.EggS => Ball.Poke,
            PogoType.Raid => Ball.Premier,
            PogoType.RaidM => Ball.Premier,
            PogoType.ResearchP => Ball.Poke,
            PogoType.Shadow => Ball.Premier,
            _ => Ball.None, // Poke, Great, Ultra
        };
    }
}
