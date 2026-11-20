using System.Buffers.Binary;
using Diyarak.Market.Listing;

namespace Diyarak.Market.Application.Photos;

internal enum ListingPhotoSanitizeStatus
{
    Sanitized = 1,
    UnsupportedFormat = 2,
    Malformed = 3
}

/// <summary>
/// Detects JPEG, PNG and WebP by their signatures and removes embedded metadata (EXIF including
/// GPS location, XMP, IPTC, comments and text chunks) without re-encoding pixels. For JPEG the
/// EXIF orientation is preserved in a minimal EXIF segment so phone photos keep displaying upright.
/// </summary>
internal static class ListingPhotoSanitizer
{
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> ExifHeader => [0x45, 0x78, 0x69, 0x66, 0x00, 0x00];

    internal static ListingPhotoSanitizeStatus Sanitize(
        ReadOnlySpan<byte> input,
        out ListingPhotoFormat format,
        out byte[] output)
    {
        output = [];
        format = default;

        if (input.Length >= 3 && input[0] == 0xFF && input[1] == 0xD8 && input[2] == 0xFF)
        {
            format = ListingPhotoFormat.Jpeg;
            return Result(TrySanitizeJpeg(input, out output));
        }

        if (input.StartsWith(PngSignature))
        {
            format = ListingPhotoFormat.Png;
            return Result(TrySanitizePng(input, out output));
        }

        if (input.Length >= 12 &&
            input[..4].SequenceEqual("RIFF"u8) &&
            input.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            format = ListingPhotoFormat.Webp;
            return Result(TrySanitizeWebp(input, out output));
        }

        return ListingPhotoSanitizeStatus.UnsupportedFormat;
    }

    private static ListingPhotoSanitizeStatus Result(bool sanitized) =>
        sanitized ? ListingPhotoSanitizeStatus.Sanitized : ListingPhotoSanitizeStatus.Malformed;

    // ---------------------------------------------------------------- JPEG

    private static bool TrySanitizeJpeg(ReadOnlySpan<byte> input, out byte[] output)
    {
        output = [];
        using var buffer = new MemoryStream(input.Length);
        buffer.Write(input[..2]); // SOI

        ushort? orientation = null;
        bool orientationWritten = false;
        int offset = 2;

        while (true)
        {
            // Skip fill bytes before a marker.
            while (offset < input.Length && input[offset] == 0xFF && offset + 1 < input.Length && input[offset + 1] == 0xFF)
                offset++;

            if (offset + 4 > input.Length || input[offset] != 0xFF)
                return false;

            byte marker = input[offset + 1];

            if (marker is 0xD8 or 0xD9 or 0x01 or (>= 0xD0 and <= 0xD7))
                return false; // Unexpected standalone marker before SOS.

            int length = BinaryPrimitives.ReadUInt16BigEndian(input.Slice(offset + 2, 2));

            if (length < 2 || offset + 2 + length > input.Length)
                return false;

            ReadOnlySpan<byte> segment = input.Slice(offset, 2 + length);
            ReadOnlySpan<byte> payload = segment[4..];

            if (marker == 0xE1 && payload.StartsWith(ExifHeader))
                orientation ??= ReadExifOrientation(payload[ExifHeader.Length..]);

            bool keep = marker switch
            {
                0xE0 => true,  // APP0 JFIF
                0xE2 => true,  // APP2 ICC colour profile
                0xEE => true,  // APP14 Adobe colour transform
                >= 0xE1 and <= 0xEF => false, // EXIF, XMP, IPTC and other application data
                0xFE => false, // Comment
                _ => true,     // Tables, frame headers, SOS
            };

            if (marker == 0xDA)
            {
                // Start of scan: everything after this header is entropy-coded image data.
                WriteOrientationOnce(buffer, orientation, ref orientationWritten);
                buffer.Write(input[offset..]);
                output = buffer.ToArray();
                return input.Length > offset + 2 + length;
            }

            if (keep)
            {
                if (marker != 0xE0)
                    WriteOrientationOnce(buffer, orientation, ref orientationWritten);

                buffer.Write(segment);
            }

            offset += 2 + length;
        }
    }

    private static void WriteOrientationOnce(MemoryStream buffer, ushort? orientation, ref bool written)
    {
        if (written || orientation is not (>= 2 and <= 8))
            return;

        written = true;

        // APP1 "Exif\0\0" + big-endian TIFF header + IFD0 with a single Orientation entry.
        Span<byte> segment = stackalloc byte[2 + 2 + 6 + 8 + 2 + 12 + 4];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment[2..], (ushort)(segment.Length - 2));
        ExifHeader.CopyTo(segment[4..]);
        Span<byte> tiff = segment[10..];
        tiff[0] = (byte)'M';
        tiff[1] = (byte)'M';
        BinaryPrimitives.WriteUInt16BigEndian(tiff[2..], 42);
        BinaryPrimitives.WriteUInt32BigEndian(tiff[4..], 8);
        BinaryPrimitives.WriteUInt16BigEndian(tiff[8..], 1);          // entry count
        BinaryPrimitives.WriteUInt16BigEndian(tiff[10..], 0x0112);    // Orientation
        BinaryPrimitives.WriteUInt16BigEndian(tiff[12..], 3);         // SHORT
        BinaryPrimitives.WriteUInt32BigEndian(tiff[14..], 1);         // count
        BinaryPrimitives.WriteUInt16BigEndian(tiff[18..], orientation.Value);
        BinaryPrimitives.WriteUInt16BigEndian(tiff[20..], 0);
        BinaryPrimitives.WriteUInt32BigEndian(tiff[22..], 0);         // no next IFD
        buffer.Write(segment);
    }

    internal static ushort? ReadExifOrientation(ReadOnlySpan<byte> tiff)
    {
        if (tiff.Length < 8)
            return null;

        bool littleEndian;

        if (tiff[0] == (byte)'I' && tiff[1] == (byte)'I')
            littleEndian = true;
        else if (tiff[0] == (byte)'M' && tiff[1] == (byte)'M')
            littleEndian = false;
        else
            return null;

        if (ReadUInt16(tiff[2..], littleEndian) != 42)
            return null;

        uint ifdOffset = ReadUInt32(tiff[4..], littleEndian);

        if (ifdOffset > (uint)tiff.Length - 2)
            return null;

        int entryCount = ReadUInt16(tiff[(int)ifdOffset..], littleEndian);
        int entriesStart = (int)ifdOffset + 2;

        for (int i = 0; i < entryCount; i++)
        {
            int entry = entriesStart + (i * 12);

            if (entry + 12 > tiff.Length)
                return null;

            if (ReadUInt16(tiff[entry..], littleEndian) == 0x0112 &&
                ReadUInt16(tiff[(entry + 2)..], littleEndian) == 3)
            {
                ushort value = ReadUInt16(tiff[(entry + 8)..], littleEndian);
                return value is >= 1 and <= 8 ? value : null;
            }
        }

        return null;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> span, bool littleEndian) =>
        littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(span) : BinaryPrimitives.ReadUInt16BigEndian(span);

    private static uint ReadUInt32(ReadOnlySpan<byte> span, bool littleEndian) =>
        littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(span) : BinaryPrimitives.ReadUInt32BigEndian(span);

    // ---------------------------------------------------------------- PNG

    private static bool TrySanitizePng(ReadOnlySpan<byte> input, out byte[] output)
    {
        output = [];
        using var buffer = new MemoryStream(input.Length);
        buffer.Write(PngSignature);

        int offset = PngSignature.Length;
        bool sawHeader = false;

        while (offset + 12 <= input.Length)
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(input.Slice(offset, 4));

            if (length > int.MaxValue || offset + 12 + (long)length > input.Length)
                return false;

            ReadOnlySpan<byte> type = input.Slice(offset + 4, 4);
            ReadOnlySpan<byte> chunk = input.Slice(offset, 12 + (int)length);

            if (!sawHeader && !type.SequenceEqual("IHDR"u8))
                return false;

            sawHeader = true;

            bool drop =
                type.SequenceEqual("eXIf"u8) ||
                type.SequenceEqual("tEXt"u8) ||
                type.SequenceEqual("zTXt"u8) ||
                type.SequenceEqual("iTXt"u8) ||
                type.SequenceEqual("tIME"u8);

            if (!drop)
                buffer.Write(chunk);

            offset += chunk.Length;

            if (type.SequenceEqual("IEND"u8))
            {
                output = buffer.ToArray();
                return true;
            }
        }

        return false;
    }

    // ---------------------------------------------------------------- WebP

    private static bool TrySanitizeWebp(ReadOnlySpan<byte> input, out byte[] output)
    {
        output = [];

        uint riffSize = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(4, 4));

        if (riffSize < 4 || 8 + (long)riffSize > input.Length)
            return false;

        ReadOnlySpan<byte> body = input.Slice(12, (int)riffSize - 4);
        using var chunks = new MemoryStream(body.Length);
        int offset = 0;
        bool sawImage = false;

        while (offset < body.Length)
        {
            if (offset + 8 > body.Length)
                return false;

            ReadOnlySpan<byte> fourCc = body.Slice(offset, 4);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(offset + 4, 4));
            long padded = size + (size & 1);

            if (offset + 8 + padded > body.Length && offset + 8 + (long)size != body.Length)
                return false;

            int chunkLength = (int)Math.Min(8 + padded, body.Length - offset);
            ReadOnlySpan<byte> chunk = body.Slice(offset, chunkLength);

            if (fourCc.SequenceEqual("VP8 "u8) || fourCc.SequenceEqual("VP8L"u8) || fourCc.SequenceEqual("ANIM"u8))
                sawImage = true;

            if (fourCc.SequenceEqual("EXIF"u8) || fourCc.SequenceEqual("XMP "u8))
            {
                offset += chunkLength;
                continue;
            }

            if (fourCc.SequenceEqual("VP8X"u8))
            {
                if (size < 10)
                    return false;

                byte[] extended = chunk.ToArray();
                extended[8] &= unchecked((byte)~0x0C); // clear EXIF (0x08) and XMP (0x04) flags
                chunks.Write(extended);
            }
            else
            {
                chunks.Write(chunk);
            }

            offset += chunkLength;
        }

        if (!sawImage)
            return false;

        byte[] content = chunks.ToArray();
        output = new byte[12 + content.Length];
        "RIFF"u8.CopyTo(output);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(4), (uint)(content.Length + 4));
        "WEBP"u8.CopyTo(output.AsSpan(8));
        content.CopyTo(output.AsSpan(12));
        return true;
    }
}
