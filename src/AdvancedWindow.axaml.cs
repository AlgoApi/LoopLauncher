using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LoopLauncher.Helpers;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using LoopLauncher.Services;

namespace LoopLauncher
{
    public partial class AdvancedWindow : Window
    {
        private readonly SettingsManager _settingsManager;
        private readonly LocalizationService _localization;
        private readonly string _gameDir;
        private readonly string _selectedBranch;
        
        private const string DefaultGameArgs = "--app-dir \"{app-dir}\" --java-exec \"{java-exec}\" --user-dir \"{user-dir}\" --auth-mode offline --uuid {uuid} --name {name}";
        private const string DefaultServerArgs = "@echo off\r\nset \"CURRENT_DIR=%~dp0\"\r\ncd /d \"%CURRENT_DIR%\"\r\n\"%CURRENT_DIR%..\\..\\..\\jre\\latest\\bin\\java.exe\" -jar \"HytaleServer.jar\" --assets \"..\\Assets.zip\"\r\n\r\npause";

        public AdvancedWindow(SettingsManager settingsManager, LocalizationService localization, string gameDir, string selectedBranch)
        {
            InitializeComponent();
            
            if (FontHelper.CurrentFont != null)
            {
                FontFamily = FontHelper.CurrentFont;
            }
            
            _settingsManager = settingsManager;
            _localization = localization;
            _gameDir = gameDir;
            _selectedBranch = selectedBranch;
            
            LoadSettings();
            UpdateUI();
        }

        public AdvancedWindow() { InitializeComponent(); }

        private void UpdateUI()
        {
            TitleText.Text = _localization.Get("advanced.title");
            GameArgsLabel.Text = _localization.Get("advanced.game_args");
            GameArgsHintText.Text = _localization.Get("advanced.game_args_hint");
            ResetGameArgsBtnText.Text = _localization.Get("advanced.reset_default");
            ServerArgsLabel.Text = _localization.Get("advanced.server_args");
            ServerArgsHintText.Text = _localization.Get("advanced.server_args_hint");
            ResetServerArgsBtnText.Text = _localization.Get("advanced.reset_default");
            SaveServerArgsBtnText.Text = _localization.Get("advanced.save_to_file");
            FoldersLabel.Text = _localization.Get("advanced.folders");
            OpenGameFolderBtnText.Text = _localization.Get("advanced.folder_game");
            OpenServerFolderBtnText.Text = _localization.Get("advanced.folder_server");
            OpenUserDataFolderBtnText.Text = _localization.Get("advanced.folder_userdata");
            OpenModsFolderBtnText.Text = _localization.Get("advanced.folder_mods");
            CancelBtn.Content = _localization.Get("settings.cancel");
            SaveBtn.Content = _localization.Get("settings.save");
        }

        private void LoadSettings()
        {
            var settings = _settingsManager.Load();
            
            // Game args
            GameArgsTextBox.Text = string.IsNullOrEmpty(settings.CustomGameArgs) 
                ? DefaultGameArgs 
                : settings.CustomGameArgs;
            
            // Server args - читаем из файла если есть
            var serverBatPath = GetServerBatPath();
            if (!string.IsNullOrEmpty(serverBatPath) && File.Exists(serverBatPath))
            {
                ServerArgsTextBox.Text = File.ReadAllText(serverBatPath);
            }
            else
            {
                ServerArgsTextBox.Text = DefaultServerArgs;
            }
        }

        private string? GetServerBatPath()
        {
            var installDir = Path.Combine(_gameDir, "install", _selectedBranch, "package", "game");
            if (!Directory.Exists(installDir))
                return null;

            foreach (var versionDir in Directory.GetDirectories(installDir))
            {
                var serverBat = Path.Combine(versionDir, "Server", "start-server.bat");
                if (File.Exists(serverBat))
                    return serverBat;
            }
            return null;
        }

        private string? GetServerFolder()
        {
            var serverBat = GetServerBatPath();
            return serverBat != null ? Path.GetDirectoryName(serverBat) : null;
        }

        private string GetGameFolder()
        {
            return Path.Combine(_gameDir, "install", _selectedBranch, "package", "game", "latest", "Client");
        }

        private string GetUserDataFolder()
        {
            return Path.Combine(_gameDir, "UserData");
        }

        private string GetModsFolder()
        {
            return Path.Combine(_gameDir, "UserData", "Mods");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = _settingsManager.Load();
            settings.CustomGameArgs = GameArgsTextBox.Text.Trim();
            _settingsManager.Save(settings);
            
            MessageBoxManager.GetMessageBoxStandard(
                _localization.Get("settings.success"),
                _localization.Get("settings.saved"),
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            Close();
        }

        private void ResetGameArgsButton_Click(object sender, RoutedEventArgs e)
        {
            GameArgsTextBox.Text = DefaultGameArgs;
        }

        private void ResetServerArgsButton_Click(object sender, RoutedEventArgs e)
        {
            ServerArgsTextBox.Text = DefaultServerArgs;
        }

        private void SaveServerArgsButton_Click(object sender, RoutedEventArgs e)
        {
            var serverBatPath = GetServerBatPath();
            if (string.IsNullOrEmpty(serverBatPath))
            {
                MessageBoxManager.GetMessageBoxStandard(
                    _localization.Get("advanced.server_not_found"),
                    _localization.Get("error.title"),
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                return;
            }

            try
            {
                File.WriteAllText(serverBatPath, ServerArgsTextBox.Text);
                MessageBoxManager.GetMessageBoxStandard(
                    _localization.Get("advanced.server_saved"),
                    _localization.Get("settings.success"),
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard(
                    $"{_localization.Get("error.title")}: {ex.Message}",
                    _localization.Get("error.title"),
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            }
        }

        private void OpenGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(GetGameFolder());
        }

        private void OpenServerFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetServerFolder();
            if (string.IsNullOrEmpty(folder))
            {
                MessageBoxManager.GetMessageBoxStandard(
                    _localization.Get("advanced.server_not_found"),
                    _localization.Get("error.title"),
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                return;
            }
            OpenFolder(folder);
        }

        private void OpenUserDataFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetUserDataFolder();
            Directory.CreateDirectory(folder);
            OpenFolder(folder);
        }

        private void OpenModsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetModsFolder();
            Directory.CreateDirectory(folder);
            OpenFolder(folder);
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    MessageBoxManager.GetMessageBoxStandard(
                        _localization.Get("advanced.folder_not_exists"),
                        _localization.Get("error.title"),
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                    return;
                }
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard(
                    $"{_localization.Get("error.title")}: {ex.Message}",
                    _localization.Get("error.title"),
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            }
        }
    }
}
