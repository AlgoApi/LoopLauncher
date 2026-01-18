// SettingsWindow.axaml.cs — Avalonia version
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LoopLauncher.Helpers;
using Microsoft.Extensions.DependencyInjection;
using LoopLauncher.Services;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace LoopLauncher
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsManager _settingsManager;
        private readonly LocalizationService _localization;
        private string RussifierUrl => Config.RussifierUrl;
        private string OnlineFixUrl => Config.OnlineFixUrl;

        public SettingsWindow(SettingsManager settingsManager, LocalizationService localization)
        {
            InitializeComponent();

            if (FontHelper.CurrentFont != null)
            {
                FontFamily = FontHelper.CurrentFont;
            }

            _settingsManager = settingsManager;
            _localization = localization;

            LoadSettings();
            UpdateUI();
            CheckGameInstalled();
        }

        public SettingsWindow() { InitializeComponent(); }

        private void UpdateUI()
        {
            Title = _localization.Get("settings.title");
            GameDirLabel.Text = _localization.Get("settings.game_folder");
            CancelBtn.Content = _localization.Get("settings.cancel");
            SaveBtn.Content = _localization.Get("settings.save");
            MirrorLabel.Text = _localization.Get("settings.mirror");
            UseMirrorText.Text = _localization.Get("settings.use_mirror");
            MirrorWarningText.Text = _localization.Get("settings.mirror_warning");
            RussifierLabel.Text = _localization.Get("settings.russifier");
            RussifierBtnText.Text = _localization.Get("settings.install_russifier");
            OnlineFixLabel.Text = _localization.Get("settings.onlinefix");
            OnlineFixBtnText.Text = _localization.Get("settings.install_onlinefix");
            LoggingLabel.Text = _localization.Get("settings.logging");
            VerboseLoggingText.Text = _localization.Get("settings.verbose_logging");
            LoggingHintText.Text = _localization.Get("settings.logging_hint");
            OpenLogsBtnText.Text = _localization.Get("settings.open_logs");
            DownloadLabel.Text = _localization.Get("settings.download");
            AlwaysFullDownloadText.Text = _localization.Get("settings.always_full_download");
            DownloadHintText.Text = _localization.Get("settings.download_hint");
            FontLabel.Text = _localization.Get("settings.font");
            FontHintText.Text = _localization.Get("settings.font_hint");
            AdvancedLabel.Text = _localization.Get("settings.advanced");
            AdvancedBtnText.Text = _localization.Get("settings.advanced_btn");
            AdvancedHintText.Text = _localization.Get("settings.advanced_hint");
        }

        private void CheckGameInstalled()
        {
            var gameDir = GetGameDirectory();
            var isInstalled = IsGameInstalled(gameDir);

            RussifierBtn.IsEnabled = isInstalled;

            var isOnlineFixSupported = Services.PlatformHelper.IsOnlinefixSupported();
            OnlineFixBtn.IsEnabled = isInstalled && isOnlineFixSupported;

            RussifierStatusText.Text = isInstalled ? "" : _localization.Get("settings.russifier_no_game");

            OnlineFixStatusText.Text = !isOnlineFixSupported
                ? _localization.Get("settings.onlinefix_not_supported")
                : (isInstalled ? "" : _localization.Get("settings.onlinefix_no_game"));

            CheckBackupsAvailable();
        }

        private void CheckBackupsAvailable()
        {
            var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoopLauncher", "backups");

            RussifierRestoreBtn.IsVisible = Directory.Exists(Path.Combine(backupDir, "russifier"));
            OnlineFixRestoreBtn.IsVisible = Directory.Exists(Path.Combine(backupDir, "onlinefix"));
        }

        private string GetGameDirectory()
        {
            var settings = _settingsManager.Load();
            return string.IsNullOrEmpty(settings.GameDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hytale")
                : settings.GameDirectory;
        }

        private bool IsGameInstalled(string gameDir)
        {
            var installBase = Path.Combine(gameDir, "install");
            if (!Directory.Exists(installBase))
                return false;

            foreach (var branch in GameLauncher.AvailableBranches)
            {
                var branchDir = Path.Combine(installBase, branch, "package", "game");
                if (!Directory.Exists(branchDir))
                    continue;

                foreach (var dir in Directory.GetDirectories(branchDir))
                {
                    var clientPath = Path.Combine(dir, "Client", "HytaleClient.exe");
                    if (File.Exists(clientPath))
                        return true;
                }
            }
            return false;
        }

        private List<string> GetAllGameVersionDirs(string gameDir)
        {
            var versionDirs = new List<string>();
            var installBase = Path.Combine(gameDir, "install");
            if (!Directory.Exists(installBase)) return versionDirs;

            foreach (var branch in GameLauncher.AvailableBranches)
            {
                var branchDir = Path.Combine(installBase, branch, "package", "game");
                if (!Directory.Exists(branchDir)) continue;

                foreach (var dir in Directory.GetDirectories(branchDir))
                {
                    var clientPath = Path.Combine(dir, "Client", "HytaleClient.exe");
                    if (File.Exists(clientPath))
                        versionDirs.Add(dir);
                }
            }
            return versionDirs;
        }

        private void LoadSettings()
        {
            var settings = _settingsManager.Load();

            var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hytale");
            GameDirTextBox.Text = string.IsNullOrEmpty(settings.GameDirectory) ? defaultDir : settings.GameDirectory;

            UseMirrorCheckBox.IsChecked = settings.UseMirror;
            MirrorWarningText.IsVisible = settings.UseMirror;
            VerboseLoggingCheckBox.IsChecked = settings.VerboseLogging;
            AlwaysFullDownloadCheckBox.IsChecked = settings.AlwaysFullDownload;

            FontComboBox.ItemsSource = FontHelper.AvailableFonts;
            var savedFont = string.IsNullOrEmpty(settings.FontName) ? "Inter" : settings.FontName;
            FontComboBox.SelectedItem = savedFont;
        }

        private async void UseMirrorCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = UseMirrorCheckBox.IsChecked == true;
            MirrorWarningText.IsVisible = isChecked;

            if (isChecked)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    _localization.Get("settings.mirror_confirm"),
                    _localization.Get("settings.mirror"),
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Warning);

                await box.ShowAsync();
            }
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var props = e.GetCurrentPoint(this).Properties;
                if (props.IsLeftButtonPressed)
                    this.BeginMoveDrag(e);
            }
            catch { /* ignore */ }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = _localization.Get("settings.select_folder")
            };

            var path = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(path))
            {
                GameDirTextBox.Text = path;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private void FontComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // selection updated; saved on Save
        }

        private string GetSelectedFont()
        {
            return FontComboBox.SelectedItem as string ?? "Inter";
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = _settingsManager.Load();
            settings.GameDirectory = GameDirTextBox.Text;
            settings.UseMirror = UseMirrorCheckBox.IsChecked == true;
            settings.VerboseLogging = VerboseLoggingCheckBox.IsChecked == true;
            settings.AlwaysFullDownload = AlwaysFullDownloadCheckBox.IsChecked == true;
            settings.FontName = GetSelectedFont();
            _settingsManager.Save(settings);

            LogService.VerboseLogging = settings.VerboseLogging;

            var box = MessageBoxManager.GetMessageBoxStandard(
                _localization.Get("settings.saved"),
                _localization.Get("settings.success"),
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Info);

            await box.ShowAsync();
            Close();
        }

        private async void RussifierButton_Click(object sender, RoutedEventArgs e)
        {
            RussifierBtn.IsEnabled = false;
            RussifierStatusText.Text = _localization.Get("settings.russifier_downloading");
            RussifierStatusText.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));

            try
            {
                var gameDir = GetGameDirectory();
                var versionDirs = GetAllGameVersionDirs(gameDir);
                if (versionDirs.Count == 0) throw new Exception("No game versions found");

                var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoopLauncher", "cache");
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoopLauncher", "backups", "russifier");
                Directory.CreateDirectory(cacheDir);

                var zipPath = Path.Combine(cacheDir, "ru.zip");
                var extractPath = Path.Combine(cacheDir, "ru_temp");

                bool downloadSuccess = false;
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "LoopLauncher");

                    var response = await httpClient.GetAsync(RussifierUrl);
                    response.EnsureSuccessStatusCode();

                    await using (var fs = new FileStream(zipPath, FileMode.Create))
                        await response.Content.CopyToAsync(fs);

                    downloadSuccess = true;
                }
                catch
                {
                    var addonsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Addons", "ru.zip");
                    if (File.Exists(addonsPath))
                    {
                        RussifierStatusText.Text = _localization.Get("settings.russifier_from_local");
                        File.Copy(addonsPath, zipPath, true);
                        downloadSuccess = true;
                    }
                }

                if (!downloadSuccess) throw new Exception("Download failed and no local file found in Addons folder");

                RussifierStatusText.Text = _localization.Get("settings.russifier_installing");

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                var clientSourceDir = Path.Combine(extractPath, "Client");
                if (!Directory.Exists(clientSourceDir))
                {
                    var dirs = Directory.GetDirectories(extractPath);
                    if (dirs.Length > 0) clientSourceDir = Path.Combine(dirs[0], "Client");
                }

                if (!Directory.Exists(clientSourceDir)) throw new Exception("Client folder not found in archive");

                var filesToBackup = GetFilesRecursive(clientSourceDir)
                    .Select(f => f.Substring(clientSourceDir.Length + 1).TrimStart(Path.DirectorySeparatorChar))
                    .ToList();

                int installedCount = 0;
                foreach (var versionDir in versionDirs)
                {
                    var clientDestDir = Path.Combine(versionDir, "Client");
                    if (Directory.Exists(clientDestDir))
                    {
                        if (installedCount == 0) BackupFiles(clientDestDir, backupDir, filesToBackup);
                        CopyDirectory(clientSourceDir, clientDestDir);
                        installedCount++;
                    }
                }

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                if (File.Exists(zipPath)) File.Delete(zipPath);

                RussifierStatusText.Text = string.Format(_localization.Get("settings.russifier_done"), installedCount);
                RussifierStatusText.Foreground = new SolidColorBrush(Color.Parse("#2ea043"));

                CheckBackupsAvailable();
            }
            catch (Exception ex)
            {
                RussifierStatusText.Text = $"{_localization.Get("settings.russifier_error")}: {ex.Message}";
                RussifierStatusText.Foreground = new SolidColorBrush(Color.Parse("#cc3333"));
            }
            finally
            {
                RussifierBtn.IsEnabled = true;
            }
        }

        private List<string> GetFilesRecursive(string dir)
        {
            var files = new List<string>();
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                files.Add(f);
            return files;
        }

        private void BackupFiles(string sourceDir, string backupDir, List<string> relativePaths)
        {
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);
            Directory.CreateDirectory(backupDir);

            foreach (var relativePath in relativePaths)
            {
                var sourceFile = Path.Combine(sourceDir, relativePath);
                var backupFile = Path.Combine(backupDir, relativePath);
                if (File.Exists(sourceFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupFile) ?? backupDir);
                    File.Copy(sourceFile, backupFile, true);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private async void OnlineFixButton_Click(object sender, RoutedEventArgs e)
        {
            OnlineFixBtn.IsEnabled = false;
            OnlineFixStatusText.Text = _localization.Get("settings.onlinefix_downloading");
            OnlineFixStatusText.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));

            try
            {
                var gameDir = GetGameDirectory();
                var versionDirs = GetAllGameVersionDirs(gameDir);
                if (versionDirs.Count == 0) throw new Exception("No game versions found");

                var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoopLauncher", "cache");
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoopLauncher", "backups", "onlinefix");
                Directory.CreateDirectory(cacheDir);

                var zipPath = Path.Combine(cacheDir, "online.zip");
                var extractPath = Path.Combine(cacheDir, "online_temp");

                bool downloadSuccess = false;
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "LoopLauncher");

                    var response = await httpClient.GetAsync(OnlineFixUrl);
                    response.EnsureSuccessStatusCode();

                    await using (var fs = new FileStream(zipPath, FileMode.Create))
                        await response.Content.CopyToAsync(fs);

                    downloadSuccess = true;
                }
                catch
                {
                    var addonsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Addons", "online.zip");
                    if (File.Exists(addonsPath))
                    {
                        OnlineFixStatusText.Text = _localization.Get("settings.onlinefix_from_local");
                        File.Copy(addonsPath, zipPath, true);
                        downloadSuccess = true;
                    }
                }

                if (!downloadSuccess) throw new Exception("Download failed and no local file found in Addons folder");

                OnlineFixStatusText.Text = _localization.Get("settings.onlinefix_installing");

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                var filesToBackup = GetFilesRecursive(extractPath)
                    .Select(f => f.Substring(extractPath.Length + 1).TrimStart(Path.DirectorySeparatorChar))
                    .ToList();

                int installedCount = 0;
                foreach (var versionDir in versionDirs)
                {
                    if (installedCount == 0) BackupFiles(versionDir, backupDir, filesToBackup);
                    CopyDirectory(extractPath, versionDir);
                    installedCount++;
                }

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                if (File.Exists(zipPath)) File.Delete(zipPath);

                OnlineFixStatusText.Text = string.Format(_localization.Get("settings.onlinefix_done"), installedCount);
                OnlineFixStatusText.Foreground = new SolidColorBrush(Color.Parse("#2ea043"));

                CheckBackupsAvailable();
            }
            catch (Exception ex)
            {
                OnlineFixStatusText.Text = $"{_localization.Get("settings.onlinefix_error")}: {ex.Message}";
                OnlineFixStatusText.Foreground = new SolidColorBrush(Color.Parse("#cc3333"));
            }
            finally
            {
                OnlineFixBtn.IsEnabled = true;
            }
        }

        private void RussifierRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreBackup("russifier", RussifierStatusText);
        }

        private void OnlineFixRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreBackup("onlinefix", OnlineFixStatusText);
        }

        private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logsFolder = LogService.GetLogsFolder();
                Process.Start(new ProcessStartInfo
                {
                    FileName = logsFolder,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private async void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            var gameDir = GetGameDirectory();
            var branch = "pre-release";

            var advancedWindow = (Application.Current as App)?.Services.GetRequiredService<AdvancedWindow>();

            if (advancedWindow != null)
            {
                await advancedWindow.ShowDialog(this);
            }
        }

        private void RestoreBackup(string backupName, Avalonia.Controls.TextBlock statusText)
        {
            try
            {
                var gameDir = GetGameDirectory();
                var versionDirs = GetAllGameVersionDirs(gameDir);
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoopLauncher", "backups", backupName);

                if (!Directory.Exists(backupDir))
                {
                    statusText.Text = "No backup found";
                    return;
                }

                var box = MessageBoxManager.GetMessageBoxStandard(
                    _localization.Get("settings.restore_confirm"),
                    _localization.Get("settings.restore_title"),
                    ButtonEnum.YesNo,
                    MsBox.Avalonia.Enums.Icon.Question);

                var resultTask = box.ShowAsync();
                resultTask.Wait();
                var result = resultTask.Result;

                if (result != ButtonResult.Yes) return;

                int restoredCount = 0;
                foreach (var versionDir in versionDirs)
                {
                    CopyDirectory(backupDir, versionDir);
                    restoredCount++;
                }

                Directory.Delete(backupDir, true);

                statusText.Text = string.Format(_localization.Get("settings.restore_done"), restoredCount);
                statusText.Foreground = new SolidColorBrush(Color.Parse("#2ea043"));

                CheckBackupsAvailable();
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error: {ex.Message}";
                statusText.Foreground = new SolidColorBrush(Color.Parse("#cc3333"));
            }
        }
    }
}
