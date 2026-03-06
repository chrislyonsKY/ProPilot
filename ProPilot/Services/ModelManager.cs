using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProPilot.Services;

/// <summary>
/// Manages GGUF model files — download from HuggingFace, discover local files,
/// and track the active model. Models are stored in %APPDATA%\ProPilot\models\.
/// </summary>
public class ModelManager : IModelManager
{
    private static readonly string ModelsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProPilot", "models");

    private static readonly List<ModelProfile> _profiles = new()
    {
        new ModelProfile
        {
            Name = "Light",
            ModelId = "phi-3-mini",
            HuggingFaceRepo = "microsoft/Phi-3-mini-4k-instruct-gguf",
            FileName = "Phi-3-mini-4k-instruct-q4.gguf",
            FileSizeBytes = 1_500_000_000L, // ~1.5 GB
            Description = "Faster responses, good for most commands. Recommended for 8-16 GB RAM.",
            MinimumRamGb = 8
        },
        new ModelProfile
        {
            Name = "Standard",
            ModelId = "mistral-7b",
            HuggingFaceRepo = "TheBloke/Mistral-7B-Instruct-v0.2-GGUF",
            FileName = "mistral-7b-instruct-v0.2.Q4_K_M.gguf",
            FileSizeBytes = 4_000_000_000L, // ~4 GB
            Description = "Best accuracy for complex commands. Recommended for 16+ GB RAM.",
            MinimumRamGb = 16
        }
    };

    private readonly HttpClient _httpClient;

    public ModelManager()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ProPilot/1.0");
        Directory.CreateDirectory(ModelsDirectory);
    }

    /// <inheritdoc />
    public bool HasLocalModel()
    {
        return Directory.Exists(ModelsDirectory) &&
               Directory.GetFiles(ModelsDirectory, "*.gguf").Length > 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelProfile> GetAvailableProfiles() => _profiles;

    /// <inheritdoc />
    public async Task DownloadModelAsync(
        ModelProfile profile,
        IProgress<ModelDownloadProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var downloadUrl = $"https://huggingface.co/{profile.HuggingFaceRepo}/resolve/main/{profile.FileName}";
        var finalPath = Path.Combine(ModelsDirectory, profile.FileName);
        var tempPath = finalPath + ".tmp";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? profile.FileSizeBytes;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(tempPath);

            var buffer = new byte[81920]; // 80KB buffer
            long bytesRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;
                progress.Report(new ModelDownloadProgress
                {
                    BytesDownloaded = bytesRead,
                    TotalBytes = totalBytes,
                    PercentComplete = (double)bytesRead / totalBytes * 100
                });
            }

            fileStream.Close();

            // Move temp file to final path
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(tempPath, finalPath);

            Debug.WriteLine($"[ProPilot] Model downloaded: {finalPath} ({bytesRead:N0} bytes)");
        }
        catch (OperationCanceledException)
        {
            // Clean up temp file on cancellation
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
        catch (Exception)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <inheritdoc />
    public string? GetActiveModelPath()
    {
        if (!Directory.Exists(ModelsDirectory)) return null;

        // Return the first .gguf file found (or the one matching settings)
        var ggufFiles = Directory.GetFiles(ModelsDirectory, "*.gguf");
        return ggufFiles.FirstOrDefault();
    }

    /// <inheritdoc />
    public IReadOnlyList<LocalModelInfo> GetLocalModels()
    {
        if (!Directory.Exists(ModelsDirectory))
            return Array.Empty<LocalModelInfo>();

        return Directory.GetFiles(ModelsDirectory, "*.gguf")
            .Select(f => new LocalModelInfo
            {
                FilePath = f,
                FileName = Path.GetFileName(f),
                FileSizeBytes = new FileInfo(f).Length,
                Profile = _profiles.FirstOrDefault(p => f.Contains(p.ModelId))
            })
            .ToList();
    }

    /// <summary>
    /// Gets total system RAM in GB for model recommendation.
    /// </summary>
    public static int GetSystemRamGb()
    {
        try
        {
            var memInfo = GC.GetGCMemoryInfo();
            return (int)(memInfo.TotalAvailableMemoryBytes / 1_073_741_824);
        }
        catch
        {
            return 16; // safe fallback
        }
    }
}

// ─── Supporting types ─────────────────────────────────────────

public interface IModelManager
{
    bool HasLocalModel();
    IReadOnlyList<ModelProfile> GetAvailableProfiles();
    Task DownloadModelAsync(ModelProfile profile, IProgress<ModelDownloadProgress> progress, CancellationToken cancellationToken = default);
    string? GetActiveModelPath();
    IReadOnlyList<LocalModelInfo> GetLocalModels();
}

public class ModelProfile
{
    public string Name { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string HuggingFaceRepo { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Description { get; set; } = string.Empty;
    public int MinimumRamGb { get; set; }
}

public class ModelDownloadProgress
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete { get; set; }
}

public class LocalModelInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public ModelProfile? Profile { get; set; }
}
