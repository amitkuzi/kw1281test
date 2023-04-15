namespace BitFab.KW1281Test.Cluster
{
    public interface ICluster
    {
        void UnlockForEepromReadWrite();

        string DumpEeprom(uint? address, uint? length, string? dumpFileName);
    }
}
