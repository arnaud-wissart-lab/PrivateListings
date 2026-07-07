using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;

namespace PrivateListings.Infrastructure.Storage;

public sealed class FileSystemListingImageStorage(
    IOptions<ListingImageOptions> options,
    IHostEnvironment environment,
    ILogger<FileSystemListingImageStorage> logger)
    : IListingImageStorage
{
    private const int SignatureBufferLength = 16;

    public async Task<StoredListingImage> SaveAsync(
        ListingImageUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        var normalizedExtension = NormalizeExtension(upload.OriginalFileName);
        var expectedContentType = GetContentType(normalizedExtension);
        var contentType = NormalizeContentType(upload.ContentType);

        if (!StringComparer.OrdinalIgnoreCase.Equals(contentType, expectedContentType))
        {
            throw new InvalidDataException("Le type du fichier image est invalide.");
        }

        if (upload.SizeInBytes <= 0 || upload.SizeInBytes > options.Value.MaxSizeInBytes)
        {
            throw new InvalidDataException("La taille du fichier image dépasse la limite autorisée.");
        }

        await using var memory = new MemoryStream();
        await CopyWithLimitAsync(upload.Content, memory, options.Value.MaxSizeInBytes, cancellationToken)
            .ConfigureAwait(false);

        if (!HasExpectedSignature(normalizedExtension, memory))
        {
            throw new InvalidDataException("La signature du fichier image est invalide.");
        }

        var storageExtension = normalizedExtension == ".jpeg" ? ".jpg" : normalizedExtension;
        var storageKey = BuildStorageKey(storageExtension);
        var fullPath = ResolveStoragePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var sanitizedContent = StripKnownMetadata(normalizedExtension, memory.ToArray());

        try
        {
            await File.WriteAllBytesAsync(fullPath, sanitizedContent, cancellationToken).ConfigureAwait(false);

            return new StoredListingImage(storageKey, expectedContentType, sanitizedContent.LongLength);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TryDeleteFile(fullPath);
            StorageLog.ImageRejected(logger, exception.GetType().Name);
            throw new InvalidDataException("Le fichier image n'a pas pu être validé.", exception);
        }
    }

    public Task<ListingImageFile?> OpenAsync(
        ListingImageAccess image,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = ResolveStoragePath(image.StorageKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<ListingImageFile?>(null);
        }

        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult<ListingImageFile?>(new ListingImageFile(
            stream,
            image.ContentType,
            image.OriginalFileName,
            image.UploadedAt));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = ResolveStoragePath(storageKey);
        TryDeleteFile(fullPath);

        return Task.CompletedTask;
    }

    private static async Task CopyWithLimitAsync(
        Stream source,
        MemoryStream destination,
        long maxSize,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        long total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maxSize)
            {
                throw new InvalidDataException("La taille du fichier image dépasse la limite autorisée.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        if (total == 0)
        {
            throw new InvalidDataException("Le fichier image est vide.");
        }

        destination.Position = 0;
    }

    private string ResolveStoragePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(storageKey))
        {
            throw new InvalidDataException("La clé de stockage image est invalide.");
        }

        var root = GetStorageRoot();
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(
            fullRoot,
            storageKey.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("La clé de stockage image est invalide.");
        }

        return fullPath;
    }

    private string GetStorageRoot()
        => string.IsNullOrWhiteSpace(options.Value.StorageRoot)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "listing-images")
            : options.Value.StorageRoot;

    private static string BuildStorageKey(string extension)
    {
        var now = DateTimeOffset.UtcNow;
        return string.Join(
            '/',
            now.Year.ToString("0000", CultureInfo.InvariantCulture),
            now.Month.ToString("00", CultureInfo.InvariantCulture),
            $"{Guid.NewGuid():N}{extension}");
    }

    private static string NormalizeExtension(string fileName)
    {
        var extension = Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" => extension,
            ".svg" => throw new InvalidDataException("Les fichiers SVG ne sont pas autorisés pour les annonces."),
            _ => throw new InvalidDataException("L'extension du fichier image est invalide.")
        };
    }

    private static string NormalizeContentType(string contentType)
    {
        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var normalized = separatorIndex >= 0 ? contentType[..separatorIndex] : contentType;
        return normalized.Trim().ToLowerInvariant();
    }

    private static string GetContentType(string extension)
        => extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => throw new InvalidDataException("L'extension du fichier image est invalide.")
        };

    private static bool HasExpectedSignature(string extension, MemoryStream content)
    {
        Span<byte> signature = stackalloc byte[SignatureBufferLength];
        content.Position = 0;
        var read = content.Read(signature);
        content.Position = 0;

        return extension switch
        {
            ".jpg" or ".jpeg" => read >= 3
                && signature[0] == 0xFF
                && signature[1] == 0xD8
                && signature[2] == 0xFF,
            ".png" => read >= 8
                && signature[0] == 0x89
                && signature[1] == 0x50
                && signature[2] == 0x4E
                && signature[3] == 0x47
                && signature[4] == 0x0D
                && signature[5] == 0x0A
                && signature[6] == 0x1A
                && signature[7] == 0x0A,
            ".webp" => read >= 12
                && signature[0] == 0x52
                && signature[1] == 0x49
                && signature[2] == 0x46
                && signature[3] == 0x46
                && signature[8] == 0x57
                && signature[9] == 0x45
                && signature[10] == 0x42
                && signature[11] == 0x50,
            _ => false
        };
    }

    private static byte[] StripKnownMetadata(string extension, byte[] content)
        => extension switch
        {
            ".jpg" or ".jpeg" => StripJpegMetadata(content),
            ".png" => StripPngMetadata(content),
            ".webp" => StripWebpMetadata(content),
            _ => content
        };

    private static byte[] StripJpegMetadata(byte[] content)
    {
        if (content.Length < 4 || content[0] != 0xFF || content[1] != 0xD8)
        {
            return content;
        }

        using var output = new MemoryStream(content.Length);
        output.Write(content, 0, 2);

        var offset = 2;
        while (offset < content.Length)
        {
            var segmentStart = offset;
            if (content[offset] != 0xFF)
            {
                output.Write(content, offset, content.Length - offset);
                break;
            }

            while (offset < content.Length && content[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= content.Length)
            {
                break;
            }

            var marker = content[offset++];
            if (marker == 0xDA)
            {
                output.Write(content, segmentStart, content.Length - segmentStart);
                break;
            }

            if (IsMarkerWithoutLength(marker))
            {
                output.Write(content, segmentStart, offset - segmentStart);
                continue;
            }

            if (offset + 2 > content.Length)
            {
                output.Write(content, segmentStart, content.Length - segmentStart);
                break;
            }

            var segmentLength = (content[offset] << 8) + content[offset + 1];
            var segmentEnd = offset + segmentLength;
            if (segmentLength < 2 || segmentEnd > content.Length)
            {
                output.Write(content, segmentStart, content.Length - segmentStart);
                break;
            }

            if (!IsJpegMetadataMarker(marker))
            {
                output.Write(content, segmentStart, segmentEnd - segmentStart);
            }

            offset = segmentEnd;
        }

        return output.ToArray();
    }

    private static byte[] StripPngMetadata(byte[] content)
    {
        if (content.Length < 8)
        {
            return content;
        }

        using var output = new MemoryStream(content.Length);
        output.Write(content, 0, 8);

        var offset = 8;
        while (offset + 12 <= content.Length)
        {
            var length = ReadBigEndianInt32(content, offset);
            if (length < 0)
            {
                return content;
            }

            var chunkStart = offset;
            var chunkEnd = offset + 12 + length;
            if (chunkEnd > content.Length)
            {
                return content;
            }

            var chunkType = System.Text.Encoding.ASCII.GetString(content, offset + 4, 4);
            if (!IsPngMetadataChunk(chunkType))
            {
                output.Write(content, chunkStart, chunkEnd - chunkStart);
            }

            offset = chunkEnd;
        }

        return output.ToArray();
    }

    private static byte[] StripWebpMetadata(byte[] content)
    {
        if (content.Length < 12)
        {
            return content;
        }

        using var output = new MemoryStream(content.Length);
        output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        output.Write(new byte[4]);
        output.Write(System.Text.Encoding.ASCII.GetBytes("WEBP"));

        var offset = 12;
        while (offset + 8 <= content.Length)
        {
            var chunkStart = offset;
            var chunkType = System.Text.Encoding.ASCII.GetString(content, offset, 4);
            var chunkSize = ReadLittleEndianInt32(content, offset + 4);
            if (chunkSize < 0)
            {
                return content;
            }

            var paddedSize = chunkSize + (chunkSize % 2);
            var chunkEnd = offset + 8 + paddedSize;
            if (chunkEnd > content.Length)
            {
                return content;
            }

            if (!IsWebpMetadataChunk(chunkType))
            {
                output.Write(content, chunkStart, chunkEnd - chunkStart);
            }

            offset = chunkEnd;
        }

        var sanitized = output.ToArray();
        WriteLittleEndianInt32(sanitized, 4, sanitized.Length - 8);
        return sanitized;
    }

    private static bool IsMarkerWithoutLength(byte marker)
        => marker == 0x01 || marker is >= 0xD0 and <= 0xD9;

    private static bool IsJpegMetadataMarker(byte marker)
        => marker is 0xE1 or 0xE2 or 0xED;

    private static bool IsPngMetadataChunk(string chunkType)
        => chunkType is "eXIf" or "iTXt" or "tEXt" or "zTXt" or "iCCP";

    private static bool IsWebpMetadataChunk(string chunkType)
        => chunkType is "EXIF" or "XMP " or "ICCP";

    private static int ReadBigEndianInt32(byte[] content, int offset)
        => (content[offset] << 24)
            | (content[offset + 1] << 16)
            | (content[offset + 2] << 8)
            | content[offset + 3];

    private static int ReadLittleEndianInt32(byte[] content, int offset)
        => content[offset]
            | (content[offset + 1] << 8)
            | (content[offset + 2] << 16)
            | (content[offset + 3] << 24);

    private static void WriteLittleEndianInt32(byte[] content, int offset, int value)
    {
        content[offset] = (byte)value;
        content[offset + 1] = (byte)(value >> 8);
        content[offset + 2] = (byte)(value >> 16);
        content[offset + 3] = (byte)(value >> 24);
    }

    private static void TryDeleteFile(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal static partial class StorageLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Image d'annonce refusée pendant la validation. Type d'erreur : {ExceptionType}.")]
    public static partial void ImageRejected(ILogger logger, string exceptionType);
}
