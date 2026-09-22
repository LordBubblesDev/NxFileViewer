namespace Emignatik.NxFileViewer.Utils;

public static class FileSizeHelper
{
    private static readonly UnitRange[] unitRanges =
    [
        new(1, "B"),
        new(2, "KiB"),
        new(3, "MiB"),
        new(4, "GiB"),
        new(5, "TiB"),
        new(6, "PiB")
    ];

    public static string ToFileSize(this long nbBytes) => ((ulong)nbBytes).ToFileSize();

    private static string ToFileSize(this ulong nbBytes)
    {
        foreach (var unitRange in unitRanges) {
            if (nbBytes < unitRange.Threshold) {
                return unitRange.Format(nbBytes);
            }
        }

        return unitRanges[^1].Format(nbBytes);
    }

    private readonly struct UnitRange(int pow, string unit)
    {
        public ulong Threshold { get; } = 1UL << (10 * pow);

        private ulong Divider { get; } = pow == 1 ? 1UL : 1UL << (10 * (pow - 1));

        private string Unit { get; } = unit;

        public string Format(ulong nbBytes)
        {
            var sizeInUnit = nbBytes / (double)Divider;
            return $"{sizeInUnit:0.##} {Unit}";
        }
    }
}
