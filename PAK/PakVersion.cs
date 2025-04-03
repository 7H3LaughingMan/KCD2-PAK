namespace KCD2.PAK;

internal enum PakVersionNeededValues : ushort
{
    Default = 10,
    ExplicitDirectory = 20,
    Deflate = 20,
    Deflate64 = 21,
    Zip64 = 45
}

internal enum PakVersionMadeByPlatform : byte
{
    Windows = 0,
    Unix = 3
}
