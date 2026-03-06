using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProPilot.Services;

namespace ProPilot.ViewModels;

/// <summary>
/// ViewModel for the first-run setup ProWindow.
/// </summary>
public partial class SetupWindowViewModel : ObservableObject
{
    private readonly IModelManager _modelManager;
    private CancellationTokenSource? _downloadCts;

    [ObservableProperty] private int _systemRamGb;
    [ObservableProperty] private string _recommendedTier = "Standard";

    public ObservableCollection<ModelProfileViewModel> Profiles { get; } = new();

    [ObservableProperty] private ModelProfileViewModel? _selectedProfile;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private double _downloadPercent;
    [ObservableProperty] private string _downloadStatus = string.Empty;
    [ObservableProperty] private string _bytesDownloadedText = string.Empty;
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public SetupWindowViewModel(IModelManager modelManager)
    {
        _modelManager = modelManager;

        SystemRamGb = ModelManager.GetSystemRamGb();
        RecommendedTier = SystemRamGb >= 16 ? "Standard" : "Light";

        foreach (var profile in _modelManager.GetAvailableProfiles())
        {
            var vm = new ModelProfileViewModel
            {
                Name = profile.Name,
                Description = profile.Description,
                FileSizeText = $"{profile.FileSizeBytes / 1_073_741_824.0:F1} GB download",
                MinRamText = $"Requires {profile.MinimumRamGb}+ GB RAM",
                IsRecommended = profile.Name == RecommendedTier,
                MeetsRamRequirement = SystemRamGb >= profile.MinimumRamGb,
                Profile = profile
            };
            Profiles.Add(vm);
        }

        SelectedProfile = Profiles.Count > 0
            ? Profiles[RecommendedTier == "Standard" && Profiles.Count > 1 ? 1 : 0]
            : null;
    }

    [RelayCommand]
    private async Task DownloadModelAsync()
    {
        if (SelectedProfile?.Profile == null) return;

        IsDownloading = true;
        ErrorMessage = string.Empty;
        DownloadStatus = "Connecting...";
        DownloadPercent = 0;

        _downloadCts = new CancellationTokenSource();

        var totalSize = SelectedProfile.Profile.FileSizeBytes;
        var progress = new Progress<ModelDownloadProgress>(p =>
        {
            DownloadPercent = p.PercentComplete;
            var downloadedGb = p.BytesDownloaded / 1_073_741_824.0;
            var totalGb = p.TotalBytes / 1_073_741_824.0;
            BytesDownloadedText = $"{downloadedGb:F2} GB / {totalGb:F2} GB";
            DownloadStatus = "Downloading...";
        });

        try
        {
            await _modelManager.DownloadModelAsync(
                SelectedProfile.Profile, progress, _downloadCts.Token);

            IsComplete = true;
            DownloadStatus = "Model installed successfully!";
            DownloadPercent = 100;
        }
        catch (OperationCanceledException)
        {
            DownloadStatus = "Download cancelled.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Download failed: {ex.Message}");
            ErrorMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCts?.Cancel();
    }

    [RelayCommand]
    private void BrowseForModel()
    {
        // Use WPF OpenFileDialog for manual GGUF file selection
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select a GGUF Model File",
            Filter = "GGUF Model Files (*.gguf)|*.gguf|All Files (*.*)|*.*",
            DefaultExt = ".gguf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var destDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ProPilot", "models");
                System.IO.Directory.CreateDirectory(destDir);

                var destPath = System.IO.Path.Combine(destDir,
                    System.IO.Path.GetFileName(dialog.FileName));

                System.IO.File.Copy(dialog.FileName, destPath, overwrite: true);

                IsComplete = true;
                DownloadStatus = $"Model installed from: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to copy model: {ex.Message}";
            }
        }
    }
}

/// <summary>
/// ViewModel for a single model profile row in the setup window.
/// </summary>
public partial class ModelProfileViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _fileSizeText = string.Empty;
    [ObservableProperty] private string _minRamText = string.Empty;
    [ObservableProperty] private bool _isRecommended;
    [ObservableProperty] private bool _meetsRamRequirement;

    public ModelProfile? Profile { get; set; }
}
