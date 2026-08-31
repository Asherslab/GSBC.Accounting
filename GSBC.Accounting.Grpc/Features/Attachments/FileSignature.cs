namespace GSBC.Accounting.Grpc.Features.Attachments;

/// <summary>
/// Decides what a file actually is by reading its first bytes, and refuses anything whose content does
/// not match what the upload declared.
/// </summary>
/// <remarks>
/// <b>This is checked on write and again on read.</b> Not because GSBC.ImpactKids does it, but because
/// a receipt that is silently not the file it claims to be is discovered by an auditor in year six,
/// when nobody can reconstruct what happened. Checking on read as well is what catches the object store
/// having mangled something after a successful write - which is exactly the SeaweedFS chunk-encoding
/// failure this app had to rule out: right size, right content type, valid database row, and the object
/// was not the file, with no error anywhere.
/// <para>
/// The allow-list is short on purpose. It is what a receipt legitimately is: a PDF, a photo, or a
/// scan. An upload endpoint that is anonymous - as this one is - and accepts arbitrary types is a file
/// host with somebody else's storage bill.
/// </para>
/// </remarks>
public static class FileSignature
{
    public const string Pdf = "application/pdf";
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Heic = "image/heic";
    public const string Webp = "image/webp";

    /// <summary>
    /// Enough bytes for every signature below. HEIC's brand sits at offset 8, so 12 is the minimum that
    /// can identify everything; 16 leaves room without reading a meaningful part of the file.
    /// </summary>
    public const int BytesNeeded = 16;

    /// <summary>
    /// The content type the bytes say this is, or null if it is not a type this app accepts.
    /// </summary>
    public static string? Detect(ReadOnlySpan<byte> head)
    {
        if (head.Length < 4)
            return null;

        // 25 50 44 46 -- "%PDF"
        if (head is [0x25, 0x50, 0x44, 0x46, ..])
            return Pdf;

        // FF D8 FF -- every JPEG variant, JFIF and EXIF alike
        if (head is [0xFF, 0xD8, 0xFF, ..])
            return Jpeg;

        // 89 50 4E 47 0D 0A 1A 0A -- "\x89PNG\r\n\x1a\n"
        if (head is [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..])
            return Png;

        // ISO base media: a 4-byte big-endian box length, then "ftyp", then a 4-character brand.
        // A phone photo from an iPhone is HEIC, and it is the format people most often do not realise
        // they are uploading.
        if (head.Length >= 12 && head[4..8] is [0x66, 0x74, 0x79, 0x70])
        {
            string brand = System.Text.Encoding.ASCII.GetString(head[8..12]);

            // mif1 and msf1 are the still-image brands; heic/heix/hevc/hevx are the HEVC ones.
            if (brand is "heic" or "heix" or "hevc" or "hevx" or "mif1" or "msf1")
                return Heic;
        }

        // "RIFF" .... "WEBP" - the size field sits between the two, so the second marker is at 8.
        if (head.Length >= 12
            && head[..4] is [0x52, 0x49, 0x46, 0x46]
            && head[8..12] is [0x57, 0x45, 0x42, 0x50])
        {
            return Webp;
        }

        return null;
    }

    /// <summary>
    /// True when the declared type is one this app accepts <b>and</b> the bytes agree with it.
    /// </summary>
    /// <remarks>
    /// JPEG is the one place a declared type is allowed to differ from the detected one in a harmless
    /// direction: browsers send <c>image/jpg</c> and <c>image/pjpeg</c> for the same bytes. Everything
    /// else must match exactly - a <c>.exe</c> renamed to <c>.pdf</c> fails here, which is the point.
    /// </remarks>
    public static bool Matches(string? declaredContentType, string detected)
    {
        string declared = Normalise(declaredContentType);

        return declared == detected;
    }

    /// <summary>Lower-cased, parameters stripped, and the JPEG spellings folded together.</summary>
    public static string Normalise(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return string.Empty;

        // "image/jpeg; charset=binary" - the parameters are noise here.
        string type = contentType.Split(';')[0].Trim().ToLowerInvariant();

        return type switch
        {
            "image/jpg" or "image/pjpeg" => Jpeg,
            "image/heif" or "image/heic-sequence" => Heic,
            _ => type
        };
    }

    /// <summary>The extension a stored object should carry, from its detected type.</summary>
    public static string ExtensionFor(string detectedContentType) => detectedContentType switch
    {
        Pdf => ".pdf",
        Jpeg => ".jpg",
        Png => ".png",
        Heic => ".heic",
        Webp => ".webp",
        // Unreachable while Detect only returns the five above, but a new type added there and
        // forgotten here would otherwise silently produce extensionless keys.
        _ => ".bin"
    };
}
