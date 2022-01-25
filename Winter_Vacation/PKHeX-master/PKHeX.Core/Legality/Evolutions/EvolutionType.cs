using static PKHeX.Core.EvolutionType;

namespace PKHeX.Core
{
    public enum EvolutionType : byte
    {
        None = 0,
        LevelUpFriendship = 1,
        LevelUpFriendshipMorning = 2,
        LevelUpFriendshipNight = 3,
        LevelUp = 4,
        Trade = 5,
        TradeHeldItem = 6,
        TradeShelmetKarrablast = 7,
        UseItem = 8,
        LevelUpATK = 9,
        LevelUpAeqD = 10,
        LevelUpDEF = 11,
        LevelUpECl5 = 12,
        LevelUpECgeq5 = 13,
        LevelUpNinjask = 14,
        LevelUpShedinja = 15,
        LevelUpBeauty = 16,
        UseItemMale = 17,
        UseItemFemale = 18,
        LevelUpHeldItemDay = 19,
        LevelUpHeldItemNight = 20,
        LevelUpKnowMove = 21,
        LevelUpWithTeammate = 22,
        LevelUpMale = 23,
        LevelUpFemale = 24,
        LevelUpElectric = 25,
        LevelUpForest = 26,
        LevelUpCold = 27,
        LevelUpInverted = 28,
        LevelUpAffection50MoveType = 29,
        LevelUpMoveType = 30,
        LevelUpWeather = 31,
        LevelUpMorning = 32,
        LevelUpNight = 33,
        LevelUpFormFemale1 = 34,
        UNUSED = 35,
        LevelUpVersion = 36,
        LevelUpVersionDay = 37,
        LevelUpVersionNight = 38,
        LevelUpSummit = 39,
        LevelUpDusk = 40,
        LevelUpWormhole = 41,
        UseItemWormhole = 42,
        CriticalHitsInBattle = 43, // Sirfetch'd
        HitPointsLostInBattle = 44, // Runerigus
        Spin = 45, // Alcremie
        LevelUpNatureAmped = 46, // Toxtricity
        LevelUpNatureLowKey = 47, // Toxtricity
        TowerOfDarkness = 48, // Urshifu
        TowerOfWaters = 49, // Urshifu
    }

    public static class EvolutionTypeExtensions
    {
        public static bool IsTrade(this EvolutionType t) => t is Trade or TradeHeldItem or TradeShelmetKarrablast;
        public static bool IsLevelUpRequired(this EvolutionType t) => t.ToString().StartsWith("LevelUp"); // don't use this
    }
}