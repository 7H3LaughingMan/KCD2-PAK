namespace KCD2_PAK;

public record ZipEntry(string EntryName, uint CompressedSize)
{
    public uint Length => (uint)(30 + EntryName.Length + CompressedSize + 46 + EntryName.Length + 22);
}
