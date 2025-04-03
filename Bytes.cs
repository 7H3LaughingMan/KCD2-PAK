namespace KCD2;

public static class Bytes
{
    private enum Kinds
    {
        Bytes = 0,
        Kilobytes = 1,
        Megabytes = 2,
        Gigabytes = 3,
        Terabytes = 4,
    }

    public static double FromKilobytes(double kilobytes) => From(kilobytes, Kinds.Kilobytes);

    public static double FromMegabytes(double megabytes) => From(megabytes, Kinds.Megabytes);

    public static double FromGigabytes(double gigabytes) => From(gigabytes, Kinds.Gigabytes);

    public static double FromTerabytes(double terabytes) => From(terabytes, Kinds.Terabytes);

    public static double ToKilobytes(double bytes) => To(bytes, Kinds.Kilobytes);

    public static double ToMegabytes(double bytes) => To(bytes, Kinds.Megabytes);

    public static double ToGigabytes(double bytes) => To(bytes, Kinds.Gigabytes);

    public static double ToTerabytes(double bytes) => To(bytes, Kinds.Terabytes);

    private static double To(double value, Kinds kind) => value / Math.Pow(1024, (int)kind);

    private static double From(double value, Kinds kind) => value * Math.Pow(1024, (int)kind);
}
