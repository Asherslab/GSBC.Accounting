#if DEBUG
using System.IO.Compression;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Makes a small, genuinely valid PNG to stand in for a photographed receipt.
/// </summary>
/// <remarks>
/// <b>This exists because a purchase without a file is not a purchase.</b> Section 3 is built out of
/// attachments now - a detail is created by attaching a receipt, and <c>Submit</c> refuses one with no
/// evidence against it. So the mock-data button stopped being able to produce a submittable form the
/// moment it could only write rows, and with it went the quickest way to exercise the PDF, the evidence
/// manifest and the per-purchase file mapping.
/// <para>
/// <b>A real PNG, not a byte blob with a PNG header on it.</b> The upload endpoint checks the declared
/// content type against what the bytes actually are, so a fake would be refused - and quite right, since
/// that check is what refuses a renamed executable. It is also what the preview modal renders, and a
/// preview that shows a broken image would send somebody debugging the wrong thing.
/// </para>
/// <para>
/// Every call produces <b>different bytes</b>, which is not cosmetic: the server stores one row per
/// (submission, content hash), so two identical files would collapse into one attachment and two mock
/// purchases would end up sharing a receipt.
/// </para>
/// <para>
/// Inside <c>#if DEBUG</c>, like the rest of the mock-data machinery. A published build has no mock
/// button, so it has no need of a receipt generator either.
/// </para>
/// </remarks>
public static class MockReceipt
{
    private const int Size = 96;

    /// <summary>
    /// A <paramref name="size"/>-square PNG in a colour derived from <paramref name="seed"/>, with a
    /// paler band across it so two mock receipts are tellable apart at a glance in the preview.
    /// </summary>
    public static byte[] Png(int seed)
    {
        byte r = (byte)(70 + seed * 53 % 150);
        byte g = (byte)(90 + seed * 97 % 140);
        byte b = (byte)(110 + seed * 131 % 130);

        // Raw scanlines: each row is a filter byte (0 = None) followed by RGB triples. Filter None
        // throughout, because this is a flat image and the point is a valid file rather than a small one.
        byte[] raw = new byte[Size * (1 + Size * 3)];
        int at = 0;

        for (int y = 0; y < Size; y++)
        {
            raw[at++] = 0;

            // A lighter horizontal band, positioned by the seed. Purely so a person looking at four mock
            // receipts in the preview modal can see they are four different files.
            bool band = y >= 20 + seed * 7 % 40 && y < 32 + seed * 7 % 40;

            for (int x = 0; x < Size; x++)
            {
                raw[at++] = band ? (byte)(r + 60) : r;
                raw[at++] = band ? (byte)(g + 60) : g;
                raw[at++] = band ? (byte)(b + 60) : b;
            }
        }

        using MemoryStream png = new();

        // The 8-byte signature every PNG starts with, and what the server's magic-byte check reads.
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR: width, height, 8 bits per channel, colour type 2 (truecolour), no interlace.
        byte[] ihdr = [
            .. BigEndian(Size), .. BigEndian(Size),
            8, 2, 0, 0, 0
        ];

        WriteChunk(png, "IHDR", ihdr);
        WriteChunk(png, "IDAT", Deflate(raw));
        WriteChunk(png, "IEND", []);

        return png.ToArray();
    }

    /// <summary>A plausible filename, so the evidence manifest does not read as four copies of one file.</summary>
    public static string FileName(int seed) => $"MOCK-receipt-{seed:0000}.png";

    /// <summary>
    /// zlib-wrapped deflate, which is what a PNG's IDAT holds - <b>not</b> bare deflate.
    /// </summary>
    /// <remarks>
    /// <c>ZLibStream</c> rather than <c>DeflateStream</c> for exactly that reason: the latter omits the
    /// two-byte zlib header and the Adler-32 trailer, and the resulting file is a PNG that every decoder
    /// rejects at the first IDAT.
    /// </remarks>
    private static byte[] Deflate(byte[] data)
    {
        using MemoryStream output = new();

        // Disposed before ToArray: the trailer is written on dispose, so reading the buffer first gives
        // a truncated stream.
        using (ZLibStream deflate = new(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data);
        }

        return output.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] typeBytes = [(byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3]];

        stream.Write(BigEndian(data.Length));
        stream.Write(typeBytes);
        stream.Write(data);

        // The CRC covers the type and the data, and NOT the length - a detail that produces a file every
        // decoder rejects if it is got wrong, and nothing else.
        stream.Write(BigEndian(unchecked((int)Crc32([.. typeBytes, .. data]))));
    }

    private static byte[] BigEndian(int value) =>
        [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    /// <summary>
    /// The CRC-32 the PNG specification prescribes: reflected, polynomial 0xEDB88320, pre- and
    /// post-inverted.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than <c>System.IO.Hashing.Crc32</c>, which is a package this project does not
    /// otherwise need and would ship in every build for the sake of a DEBUG-only generator.
    /// </remarks>
    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        return crc ^ 0xFFFFFFFF;
    }
}
#endif
