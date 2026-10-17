using ImageMagick;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// Turns an uploaded photo into a JPEG small enough to bind into the claim PDF.
/// </summary>
/// <remarks>
/// <b>Magick.NET rather than the Skia QuestPDF already carries, because of HEIC.</b> An iPhone saves
/// photos as HEIC by default, and the app accepts them because refusing the format a claimant's own
/// phone produced is refusing the claimant. Skia does not decode HEIC on Linux; Magick.NET ships libheif
/// in its native package for every runtime the cluster and the laptops use.
/// <para>
/// <b>The originals are untouched.</b> This is a printable copy for the PDF. The uploaded bytes stay in
/// the object store under their content hash, and the manifest prints that hash, so "is this the file
/// that was uploaded" is still answerable from the original and not from this.
/// </para>
/// </remarks>
public static class EvidenceImage
{
    /// <summary>
    /// Long edge, in pixels. About 240 dpi across an A4 page, which keeps the print on a thermal docket
    /// legible, while a 12-megapixel phone photo comes down from several megabytes to a few hundred KB.
    /// </summary>
    private const uint LongEdge = 2000;

    private const uint Quality = 82;

    public static byte[] ToJpeg(Stream source)
    {
        using MagickImage image = new(source);

        // Phones record "this way up" as an EXIF flag rather than rotating the pixels. Without this a
        // receipt photographed in portrait lands on its side, and Strip() below would then throw away
        // the only record of which way it should have gone.
        image.AutoOrient();

        // Greater: only ever shrink. A small screenshot blown up to 2000 px is bigger and no clearer.
        image.Resize(new MagickGeometry(LongEdge, LongEdge) { Greater = true });

        // Location, device and timestamps. None of it is evidence of the purchase, and a receipt photo
        // taken at home carries the claimant's home coordinates.
        image.Strip();

        // A PNG screenshot can carry transparency, which JPEG cannot, and the default flatten is black.
        image.BackgroundColor = MagickColors.White;
        image.Alpha(AlphaAction.Remove);

        image.Quality = Quality;
        image.Format = MagickFormat.Jpeg;

        return image.ToByteArray();
    }
}
