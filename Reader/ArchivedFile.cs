public class ArchivedFile
{
    public string Name { get; set; }

    public int Size { get; set; }

    public long Offset { get; set; }

    public uint Key { get; set; }
    public byte[] Data { get; set; }
    public byte[] BName { get; set; }
}