namespace PKHeX.Core
{
    public sealed record SlotInfoFile(string Path) : ISlotInfo
    {
        public SlotOrigin Origin => SlotOrigin.Party;
        public int Slot => 0;

        public bool CanWriteTo(SaveFile sav) => false;
        public WriteBlockedMessage CanWriteTo(SaveFile sav, PKM pkm) => WriteBlockedMessage.InvalidDestination;
        public bool WriteTo(SaveFile sav, PKM pkm, PKMImportSetting setting = PKMImportSetting.UseDefault) => false;
        public PKM Read(SaveFile sav) => sav.BlankPKM;
    }
}
