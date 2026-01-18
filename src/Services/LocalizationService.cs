using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace LoopLauncher.Services
{
    public class LocalizationService
    {
        private Dictionary<string, string> _translations = new();
        private readonly string _langDir;
        private string _currentLanguage = "en";

        public event Action? LanguageChanged;

        public LocalizationService()
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _langDir = Path.Combine(roaming, "LoopLauncher", "languages");
            Directory.CreateDirectory(_langDir);
            
            CreateDefaultLanguages();
            
            // Определяем язык по умолчанию от системы Windows
            var defaultLang = GetSystemLanguage();
            LoadLanguage(defaultLang);
        }

        public string CurrentLanguage => _currentLanguage;

        private string GetSystemLanguage()
        {
            var culture = CultureInfo.CurrentUICulture;
            var langCode = culture.TwoLetterISOLanguageName.ToLower();
            
            // Проверяем, есть ли такой язык в доступных
            var langFile = Path.Combine(_langDir, $"{langCode}.json");
            if (File.Exists(langFile))
            {
                return langCode;
            }
            
            // Fallback на английский
            return "en";
        }

        public List<string> GetAvailableLanguages()
        {
            var languages = new List<string>();
            if (Directory.Exists(_langDir))
            {
                foreach (var file in Directory.GetFiles(_langDir, "*.json"))
                {
                    languages.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            return languages;
        }

        public void LoadLanguage(string language)
        {
            var langFile = Path.Combine(_langDir, $"{language}.json");
            if (File.Exists(langFile))
            {
                try
                {
                    var json = File.ReadAllText(langFile);
                    _translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) 
                        ?? new Dictionary<string, string>();
                    _currentLanguage = language;
                    LanguageChanged?.Invoke();
                }
                catch
                {
                    _translations = new Dictionary<string, string>();
                }
            }
        }

        public string Get(string key)
        {
            return _translations.TryGetValue(key, out var value) ? value : key;
        }

        private void CreateDefaultLanguages()
        {
            // English - всегда обновляем недостающие ключи
            var enFile = Path.Combine(_langDir, "en.json");
            var enDefaults = GetEnglishDefaults();
            MergeLanguageFile(enFile, enDefaults);

            // Russian - всегда обновляем недостающие ключи
            var ruFile = Path.Combine(_langDir, "ru.json");
            var ruDefaults = GetRussianDefaults();
            MergeLanguageFile(ruFile, ruDefaults);
        }

        private void MergeLanguageFile(string filePath, Dictionary<string, string> defaults)
        {
            Dictionary<string, string> existing = new();
            
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    existing = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new();
                }
                catch { }
            }

            // Добавляем недостающие ключи и обновляем существующие
            bool updated = false;
            foreach (var kvp in defaults)
            {
                if (!existing.ContainsKey(kvp.Key) || existing[kvp.Key] != kvp.Value)
                {
                    existing[kvp.Key] = kvp.Value;
                    updated = true;
                }
            }

            // Сохраняем если были изменения или файл не существовал
            if (updated || !File.Exists(filePath))
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(existing, Formatting.Indented));
            }
        }

        private Dictionary<string, string> GetEnglishDefaults()
        {
            return new Dictionary<string, string>
            {
                ["app.title"] = "LoopLauncher",
                ["main.news"] = "HYTALE NEWS",
                ["main.nickname"] = "NICKNAME",
                ["main.version"] = "VERSION",
                ["main.branch"] = "BRANCH",
                ["main.play"] = "PLAY",
                ["main.settings"] = "⚙ Settings",
                ["main.mods"] = "Mods",
                ["main.preparing"] = "Preparing...",
                ["main.footer"] = "LoopLauncher v1.0.6 • Unofficial launcher",
                ["main.disclaimer"] = "This is a non-commercial fan project. After trying the game, please purchase it at",
                ["main.versions_found"] = "Versions found: {0}",
                ["main.latest"] = "Latest (latest)",
                ["main.version_num"] = "Version {0}",
                
                ["error.title"] = "Error",
                ["error.nickname_empty"] = "Please enter a nickname!",
                ["error.nickname_length"] = "Nickname must be 3-16 characters!",
                ["error.version_select"] = "Please select a version!",
                ["error.launch"] = "Launch error: {0}",
                ["error.corrupted_files"] = "Game files are corrupted. Please use the Reinstall button (🔄) to fix this issue.",
                ["error.corrupted_reinstall"] = "Game files are corrupted or damaged.\n\nThis can happen after applying patches.\n\nReinstall the game now?",
                
                ["status.checking_java"] = "Checking Java...",
                ["status.checking_game"] = "Checking game...",
                ["status.launching"] = "Launching game...",
                ["status.downloading_jre"] = "Downloading Java Runtime...",
                ["status.extracting_java"] = "Extracting Java...",
                ["status.system_java"] = "Using system Java",
                ["status.game_installed"] = "Game already installed",
                ["status.update_available"] = "Update available, downloading...",
                ["status.updating"] = "Updating to {0}...",
                ["status.pwr_cached"] = "PWR file already downloaded",
                ["status.redownloading"] = "File corrupted, re-downloading...",
                ["status.downloading"] = "Downloading {0}...",
                ["status.downloading_patch"] = "Downloading patch {0} -> {1}...",
                ["status.installing"] = "Installing game...",
                ["status.downloading_butler"] = "Downloading Butler...",
                ["status.extracting_butler"] = "Extracting Butler...",
                ["status.applying_patch"] = "Applying patch...",
                ["status.downloading_base"] = "Downloading base version {0}...",
                ["status.installing_base"] = "Installing base version...",
                ["status.applying_patch_version"] = "Applying patch {0}...",
                ["status.game_installed_done"] = "Game installed!",
                ["status.checking_versions"] = "Checking available versions...",
                ["status.reinstalling"] = "Removing old files...",
                ["status.patch_failed_full"] = "Incremental patch failed, downloading full version...",
                ["status.corrupted_retry_full"] = "Files corrupted, retrying with full installation...",
                
                ["main.reinstall"] = "Reinstall",
                ["main.reinstall_confirm"] = "Reinstall the game?\n\nThis will delete the current installation and download the selected version again.\n\nYour saves and mods will NOT be affected.",
                ["main.reinstall_title"] = "Reinstall Game",
                
                ["settings.title"] = "⚙ Settings",
                ["settings.game_folder"] = "GAME FOLDER",
                ["settings.api_key"] = "CURSEFORGE API KEY",
                ["settings.api_key_hint"] = "Get your API key at console.curseforge.com",
                ["settings.info"] = "LoopLauncher v1.0.6",
                ["settings.info_desc"] = "Unofficial launcher for Hytale",
                ["settings.cancel"] = "Cancel",
                ["settings.save"] = "Save",
                ["settings.saved"] = "Settings saved!",
                ["settings.success"] = "Success",
                ["settings.select_folder"] = "Select game folder",
                ["settings.mirror"] = "DOWNLOAD MIRROR",
                ["settings.use_mirror"] = "Use mirror server (if official doesn't work)",
                ["settings.mirror_warning"] = "Only use if official server doesn't work! Speed limited to ~2 MB/s",
                ["settings.mirror_confirm"] = "Warning!\n\nUse mirror ONLY if you cannot download from official servers.\n\nMirror limitations:\n- Speed limited to ~2 MB/s\n- May not have latest versions\n\nTry official server first!",
                ["settings.russifier"] = "RUSSIFIER",
                ["settings.install_russifier"] = "Install Russifier",
                ["settings.russifier_no_game"] = "Game not installed",
                ["settings.russifier_downloading"] = "Downloading...",
                ["settings.russifier_installing"] = "Installing...",
                ["settings.russifier_done"] = "Russifier installed for {0} version(s)!",
                ["settings.russifier_error"] = "Error",
                ["settings.onlinefix"] = "ONLINE FIX",
                ["settings.install_onlinefix"] = "Install Online Fix",
                ["settings.onlinefix_warning"] = "May break some UI elements (missing textures)",
                ["settings.onlinefix_no_game"] = "Game not installed",
                ["settings.onlinefix_downloading"] = "Downloading...",
                ["settings.onlinefix_installing"] = "Installing...",
                ["settings.onlinefix_done"] = "Online fix installed for {0} version(s)!",
                ["settings.onlinefix_error"] = "Error",
                ["settings.restore_title"] = "Restore Backup",
                ["settings.restore_confirm"] = "Restore original files from backup?\n\nThis will remove the installed mod/fix.",
                ["settings.restore_done"] = "Restored for {0} version(s)!",
                
                ["main.start_server"] = "Start Server",
                ["main.server_info_title"] = "Server Information",
                ["main.server_info"] = "To play with friends you need:\n\n1. Install Radmin VPN or similar (for LAN play)\n2. All players must be on the same virtual network\n\nTo connect to yourself:\n- Online Play > Direct Connect > localhost\n\nFor others to connect:\n- Online Play > Direct Connect > Your IP address",
                ["main.vpn_hint"] = "If download is slow or stuck, try using a VPN - this is a common issue with game servers.",
                ["main.website"] = "Website",
                ["main.discord"] = "Discord",
                
                ["update.available"] = "Update available!",
                ["update.message"] = "New version {0} is available.\nCurrent version: {1}\n\nOpen download page?",
                ["update.message_auto"] = "New version {0} is available.\nCurrent version: {1}\n\nYes - Update automatically\nNo - Open download page\nCancel - Skip",
                ["update.checking"] = "Checking for updates...",
                ["update.downloading"] = "Downloading update...",
                ["update.extracting"] = "Extracting update...",
                ["update.preparing"] = "Preparing to restart...",
                ["update.restarting"] = "The launcher will restart automatically.\n\nPlease wait...\n\nIf nothing happens within 10 seconds, download the update manually from GitHub.",
                ["update.restarting_title"] = "Updating...",
                ["update.error"] = "Update error: {0}",
                
                ["mods.title"] = "Mods Manager",
                ["mods.installed"] = "INSTALLED MODS",
                ["mods.browse"] = "CURSEFORGE",
                ["mods.ready"] = "Ready",
                ["mods.loading"] = "Loading mods...",
                ["mods.searching"] = "Searching...",
                ["mods.found"] = "Found: {0} mods",
                ["mods.count"] = "{0} mods installed",
                ["mods.delete_confirm"] = "Delete mod \"{0}\"?",
                ["mods.delete_title"] = "Delete mod",
                ["mods.deleted"] = "Mod \"{0}\" deleted",
                ["mods.delete_selected_confirm"] = "Delete {0} selected mods?",
                ["mods.deleted_count"] = "{0} mods deleted",
                ["mods.no_api_key"] = "CurseForge API key not set. Add it in Settings.",
                ["mods.search_placeholder"] = "Search mods...",
                ["mods.checking_updates"] = "Checking for updates...",
                ["mods.updates_available"] = "{0} updates available",
                ["mods.no_updates"] = "All mods are up to date",
                ["mods.updating"] = "Updating {0}...",
                ["mods.updated"] = "{0} updated!",
                ["mods.update_failed"] = "Update failed",
                
                // Filter labels
                ["mods.filter.all_categories"] = "All Categories",
                ["mods.filter.all_versions"] = "All Versions",
                ["mods.filter.all_types"] = "All Types",
                ["mods.filter.release"] = "Release",
                ["mods.filter.beta"] = "Beta",
                ["mods.filter.alpha"] = "Alpha",
                
                // Sort option labels
                ["mods.sort.popularity"] = "Popularity",
                ["mods.sort.downloads"] = "Downloads",
                ["mods.sort.updated"] = "Updated",
                ["mods.sort.name_az"] = "Name A-Z",
                ["mods.sort.name_za"] = "Name Z-A",
                
                // Tag management strings
                ["mods.tags.all"] = "All",
                ["mods.tags.add"] = "Add Tag",
                ["mods.tags.remove"] = "Remove Tag",
                ["mods.tags.create"] = "Create New Tag",
                ["mods.tags.create_title"] = "Create Tag",
                ["mods.tags.name_placeholder"] = "Tag name",
                ["mods.tags.delete_confirm"] = "Delete tag \"{0}\"? It will be removed from all mods.",
                ["mods.tags.delete_title"] = "Delete Tag",
                
                // Modpack management strings
                ["modpack.label"] = "MODPACK",
                ["modpack.default"] = "Default (No Modpack)",
                ["modpack.create.title"] = "Create Modpack",
                ["modpack.create.name_label"] = "Modpack Name",
                ["modpack.create.select_mods"] = "Select Mods to Include",
                ["modpack.create.selected_count"] = "{0} mods selected",
                ["modpack.create.button"] = "Create",
                ["modpack.create.error_empty_name"] = "Please enter a modpack name",
                ["modpack.create.error_invalid_name"] = "Invalid name. Cannot contain: \\ / : * ? \" < > |",
                ["modpack.created"] = "Modpack \"{0}\" created!",
                ["modpack.edit.title"] = "Edit Modpack",
                ["modpack.edit.current_mods"] = "Mods in Modpack",
                ["modpack.edit.add_mods"] = "Add Mods from Default",
                ["modpack.edit.save"] = "Save",
                ["modpack.renamed"] = "Modpack renamed to \"{0}\"",
                ["modpack.delete_confirm"] = "Delete modpack \"{0}\"?\n\nThis will permanently delete all mods in this modpack.",
                ["modpack.delete_title"] = "Delete Modpack",
                ["modpack.deleted"] = "Modpack \"{0}\" deleted",
                ["modpack.export_title"] = "Export Modpack",
                ["modpack.exporting"] = "Exporting modpack...",
                ["modpack.exported"] = "Modpack \"{0}\" exported!",
                ["modpack.import_title"] = "Import Modpack",
                ["modpack.importing"] = "Importing modpack...",
                ["modpack.imported"] = "Modpack \"{0}\" imported!",
                ["modpack.import_failed"] = "Failed to import modpack. Invalid or corrupted file.",
                
                // Main window modpack strings
                ["main.modpack"] = "MODPACK",
                ["main.modpack_default"] = "Default",
                ["main.manage_modpacks"] = "Manage Modpacks",
                
                // Tag status messages
                ["mods.tags.created"] = "Tag \"{0}\" created",
                ["mods.tags.added_to_mod"] = "Tag added to {0}",
                ["mods.tags.removed_from_mod"] = "Tag removed from {0}",
                
                ["settings.logging"] = "LOGGING",
                ["settings.verbose_logging"] = "Enable verbose logging",
                ["settings.logging_hint"] = "Detailed logs help diagnose issues. Logs are saved to %AppData%\\LoopLauncher\\logs",
                ["settings.open_logs"] = "Open logs folder",
                ["settings.download"] = "DOWNLOAD",
                ["settings.always_full_download"] = "Always download full version",
                ["settings.download_hint"] = "Recommended. Disabling may cause corrupted files when updating.",
                ["settings.font"] = "FONT",
                ["settings.font_hint"] = "Requires restart to apply",
                ["settings.advanced"] = "ADVANCED",
                ["settings.advanced_btn"] = "Professional Mode",
                ["settings.advanced_hint"] = "Custom launch arguments, open folders",
                
                ["advanced.title"] = "🔧 Advanced Settings",
                ["advanced.game_args"] = "GAME LAUNCH ARGUMENTS",
                ["advanced.game_args_hint"] = "Variables: {app-dir}, {java-exec}, {user-dir}, {uuid}, {name}",
                ["advanced.server_args"] = "SERVER LAUNCH ARGUMENTS",
                ["advanced.server_args_hint"] = "Modifies start-server.bat content",
                ["advanced.reset_default"] = "↩ Reset to default",
                ["advanced.save_to_file"] = "💾 Save to file",
                ["advanced.folders"] = "OPEN FOLDERS",
                ["advanced.folder_game"] = "Game",
                ["advanced.folder_server"] = "Server",
                ["advanced.folder_userdata"] = "UserData",
                ["advanced.folder_mods"] = "Mods",
                ["advanced.server_not_found"] = "Server not installed for this branch",
                ["advanced.server_saved"] = "Server configuration saved!",
                ["advanced.folder_not_exists"] = "Folder does not exist"
            };
        }

        private Dictionary<string, string> GetRussianDefaults()
        {
            return new Dictionary<string, string>
            {
                ["app.title"] = "LoopLauncher",
                ["main.news"] = "НОВОСТИ HYTALE",
                ["main.nickname"] = "НИКНЕЙМ",
                ["main.version"] = "ВЕРСИЯ",
                ["main.branch"] = "ВЕТКА",
                ["main.play"] = "ИГРАТЬ",
                ["main.settings"] = "⚙ Настройки",
                ["main.mods"] = "Моды",
                ["main.preparing"] = "Подготовка...",
                ["main.footer"] = "LoopLauncher v1.0.6 • Неофициальный лаунчер",
                ["main.disclaimer"] = "Это некоммерческий фан-проект. После ознакомления приобретите игру на",
                ["main.versions_found"] = "Найдено версий: {0}",
                ["main.latest"] = "Последняя (latest)",
                ["main.version_num"] = "Версия {0}",
                
                ["error.title"] = "Ошибка",
                ["error.nickname_empty"] = "Пожалуйста, введите никнейм!",
                ["error.nickname_length"] = "Никнейм должен быть от 3 до 16 символов!",
                ["error.version_select"] = "Пожалуйста, выберите версию!",
                ["error.launch"] = "Ошибка запуска: {0}",
                ["error.corrupted_files"] = "Файлы игры повреждены. Используйте кнопку Переустановить (🔄) для исправления.",
                ["error.corrupted_reinstall"] = "Файлы игры повреждены или испорчены.\n\nЭто может произойти после применения патчей.\n\nПереустановить игру сейчас?",
                
                ["status.checking_java"] = "Проверка Java...",
                ["status.checking_game"] = "Проверка игры...",
                ["status.launching"] = "Запуск игры...",
                ["status.downloading_jre"] = "Загрузка Java Runtime...",
                ["status.extracting_java"] = "Распаковка Java...",
                ["status.system_java"] = "Используется системная Java",
                ["status.game_installed"] = "Игра уже установлена",
                ["status.update_available"] = "Доступно обновление, загрузка...",
                ["status.updating"] = "Обновление до {0}...",
                ["status.pwr_cached"] = "PWR файл уже скачан",
                ["status.redownloading"] = "Файл повреждён, перекачиваем...",
                ["status.downloading"] = "Загрузка {0}...",
                ["status.downloading_patch"] = "Загрузка патча {0} -> {1}...",
                ["status.installing"] = "Установка игры...",
                ["status.downloading_butler"] = "Загрузка Butler...",
                ["status.extracting_butler"] = "Распаковка Butler...",
                ["status.applying_patch"] = "Применение патча...",
                ["status.downloading_base"] = "Загрузка базовой версии {0}...",
                ["status.installing_base"] = "Установка базовой версии...",
                ["status.applying_patch_version"] = "Применение патча {0}...",
                ["status.game_installed_done"] = "Игра установлена!",
                ["status.checking_versions"] = "Проверка доступных версий...",
                ["status.reinstalling"] = "Удаление старых файлов...",
                ["status.patch_failed_full"] = "Инкрементальный патч не сработал, скачиваем полную версию...",
                ["status.corrupted_retry_full"] = "Файлы повреждены, повторная попытка с полной установкой...",
                
                ["main.reinstall"] = "Переустановить",
                ["main.reinstall_confirm"] = "Переустановить игру?\n\nТекущая установка будет удалена и выбранная версия скачается заново.\n\nВаши сохранения и моды НЕ будут затронуты.",
                ["main.reinstall_title"] = "Переустановка игры",
                
                ["settings.title"] = "⚙ Настройки",
                ["settings.game_folder"] = "ПАПКА ИГРЫ",
                ["settings.api_key"] = "CURSEFORGE API КЛЮЧ",
                ["settings.api_key_hint"] = "Получите API ключ на console.curseforge.com",
                ["settings.info"] = "LoopLauncher v1.0.6",
                ["settings.info_desc"] = "Неофициальный лаунчер для Hytale",
                ["settings.cancel"] = "Отмена",
                ["settings.save"] = "Сохранить",
                ["settings.saved"] = "Настройки сохранены!",
                ["settings.success"] = "Успех",
                ["settings.select_folder"] = "Выберите папку для игры",
                ["settings.mirror"] = "ЗЕРКАЛО ЗАГРУЗКИ",
                ["settings.use_mirror"] = "Использовать зеркало (если официальный не работает)",
                ["settings.mirror_warning"] = "Используйте только если официальный сервер не работает! Скорость ограничена ~2 МБ/с",
                ["settings.mirror_confirm"] = "Внимание!\n\nИспользуйте зеркало ТОЛЬКО если не можете скачать с официальных серверов.\n\nОграничения зеркала:\n- Скорость ограничена ~2 МБ/с\n- Может не иметь последних версий\n\nСначала попробуйте официальный сервер!",
                ["settings.russifier"] = "РУСИФИКАТОР от d1ret",
                ["settings.install_russifier"] = "Установить русификатор",
                ["settings.russifier_no_game"] = "Игра не установлена",
                ["settings.russifier_downloading"] = "Скачивание...",
                ["settings.russifier_installing"] = "Установка...",
                ["settings.russifier_done"] = "Русификатор установлен для {0} версий!",
                ["settings.russifier_error"] = "Ошибка",
                ["settings.onlinefix"] = "ОНЛАЙН ФИКС",
                ["settings.install_onlinefix"] = "Установить онлайн фикс",
                ["settings.onlinefix_warning"] = "Может ломать некоторые интерфейсы (пропадают текстуры)",
                ["settings.onlinefix_no_game"] = "Игра не установлена",
                ["settings.onlinefix_downloading"] = "Скачивание...",
                ["settings.onlinefix_installing"] = "Установка...",
                ["settings.onlinefix_done"] = "Онлайн фикс установлен для {0} версий!",
                ["settings.onlinefix_error"] = "Ошибка",
                ["settings.restore_title"] = "Восстановление",
                ["settings.restore_confirm"] = "Восстановить оригинальные файлы из бэкапа?\n\nЭто удалит установленный мод/фикс.",
                ["settings.restore_done"] = "Восстановлено для {0} версий!",
                
                ["main.start_server"] = "Запустить сервер",
                ["main.server_info_title"] = "Информация о сервере",
                ["main.server_info"] = "Для игры с друзьями нужно:\n\n1. Установить Radmin VPN или аналог (для игры по локальной сети)\n2. Все игроки должны быть в одной виртуальной сети\n\nПодключиться к себе:\n- Онлайн игра > Прямое подключение > localhost\n\nЧтобы другие подключились:\n- Онлайн игра > Прямое подключение > Ваш IP адрес",
                ["main.vpn_hint"] = "Если загрузка медленная или зависла, попробуйте использовать VPN - это частая проблема с серверами игры.",
                ["main.website"] = "Сайт",
                ["main.discord"] = "Discord",
                
                ["update.available"] = "Доступно обновление!",
                ["update.message"] = "Доступна новая версия {0}.\nТекущая версия: {1}\n\nОткрыть страницу загрузки?",
                ["update.message_auto"] = "Доступна новая версия {0}.\nТекущая версия: {1}\n\nДа - Обновить автоматически\nНет - Открыть страницу загрузки\nОтмена - Пропустить",
                ["update.checking"] = "Проверка обновлений...",
                ["update.downloading"] = "Скачивание обновления...",
                ["update.extracting"] = "Распаковка обновления...",
                ["update.preparing"] = "Подготовка к перезапуску...",
                ["update.restarting"] = "Лаунчер автоматически перезапустится.\n\nПожалуйста, подождите...\n\nЕсли ничего не произойдёт в течение 10 секунд, скачайте обновление вручную с GitHub.",
                ["update.restarting_title"] = "Обновление...",
                ["update.error"] = "Ошибка обновления: {0}",
                
                ["mods.title"] = "Менеджер модов",
                ["mods.installed"] = "УСТАНОВЛЕННЫЕ МОДЫ",
                ["mods.browse"] = "CURSEFORGE",
                ["mods.ready"] = "Готово",
                ["mods.loading"] = "Загрузка модов...",
                ["mods.searching"] = "Поиск...",
                ["mods.found"] = "Найдено: {0} модов",
                ["mods.count"] = "{0} модов установлено",
                ["mods.delete_confirm"] = "Удалить мод \"{0}\"?",
                ["mods.delete_title"] = "Удаление мода",
                ["mods.deleted"] = "Мод \"{0}\" удалён",
                ["mods.delete_selected_confirm"] = "Удалить выбранные моды ({0})?",
                ["mods.deleted_count"] = "Удалено модов: {0}",
                ["mods.no_api_key"] = "API ключ CurseForge не указан. Добавьте его в Настройках.",
                ["mods.search_placeholder"] = "Поиск модов...",
                ["mods.checking_updates"] = "Проверка обновлений...",
                ["mods.updates_available"] = "Доступно обновлений: {0}",
                ["mods.no_updates"] = "Все моды актуальны",
                ["mods.updating"] = "Обновление {0}...",
                ["mods.updated"] = "{0} обновлён!",
                ["mods.update_failed"] = "Ошибка обновления",
                
                // Filter labels
                ["mods.filter.all_categories"] = "Все категории",
                ["mods.filter.all_versions"] = "Все версии",
                ["mods.filter.all_types"] = "Все типы",
                ["mods.filter.release"] = "Релиз",
                ["mods.filter.beta"] = "Бета",
                ["mods.filter.alpha"] = "Альфа",
                
                // Sort option labels
                ["mods.sort.popularity"] = "Популярность",
                ["mods.sort.downloads"] = "Загрузки",
                ["mods.sort.updated"] = "Обновлено",
                ["mods.sort.name_az"] = "Имя А-Я",
                ["mods.sort.name_za"] = "Имя Я-А",
                
                // Tag management strings
                ["mods.tags.all"] = "Все",
                ["mods.tags.add"] = "Добавить тег",
                ["mods.tags.remove"] = "Удалить тег",
                ["mods.tags.create"] = "Создать новый тег",
                ["mods.tags.create_title"] = "Создать тег",
                ["mods.tags.name_placeholder"] = "Название тега",
                ["mods.tags.delete_confirm"] = "Удалить тег \"{0}\"? Он будет удалён со всех модов.",
                ["mods.tags.delete_title"] = "Удалить тег",
                
                // Modpack management strings
                ["modpack.label"] = "МОДПАК",
                ["modpack.default"] = "По умолчанию (без модпака)",
                ["modpack.create.title"] = "Создать модпак",
                ["modpack.create.name_label"] = "Название модпака",
                ["modpack.create.select_mods"] = "Выберите моды для включения",
                ["modpack.create.selected_count"] = "Выбрано модов: {0}",
                ["modpack.create.button"] = "Создать",
                ["modpack.create.error_empty_name"] = "Введите название модпака",
                ["modpack.create.error_invalid_name"] = "Недопустимое имя. Нельзя использовать: \\ / : * ? \" < > |",
                ["modpack.created"] = "Модпак \"{0}\" создан!",
                ["modpack.edit.title"] = "Редактировать модпак",
                ["modpack.edit.current_mods"] = "Моды в модпаке",
                ["modpack.edit.add_mods"] = "Добавить моды из основной папки",
                ["modpack.edit.save"] = "Сохранить",
                ["modpack.renamed"] = "Модпак переименован в \"{0}\"",
                ["modpack.delete_confirm"] = "Удалить модпак \"{0}\"?\n\nЭто безвозвратно удалит все моды в этом модпаке.",
                ["modpack.delete_title"] = "Удалить модпак",
                ["modpack.deleted"] = "Модпак \"{0}\" удалён",
                ["modpack.export_title"] = "Экспорт модпака",
                ["modpack.exporting"] = "Экспорт модпака...",
                ["modpack.exported"] = "Модпак \"{0}\" экспортирован!",
                ["modpack.import_title"] = "Импорт модпака",
                ["modpack.importing"] = "Импорт модпака...",
                ["modpack.imported"] = "Модпак \"{0}\" импортирован!",
                ["modpack.import_failed"] = "Не удалось импортировать модпак. Файл повреждён или недействителен.",
                
                // Main window modpack strings
                ["main.modpack"] = "МОДПАК",
                ["main.modpack_default"] = "По умолчанию",
                ["main.manage_modpacks"] = "Управление модпаками",
                
                // Tag status messages
                ["mods.tags.created"] = "Тег \"{0}\" создан",
                ["mods.tags.added_to_mod"] = "Тег добавлен к {0}",
                ["mods.tags.removed_from_mod"] = "Тег удалён с {0}",
                
                ["settings.logging"] = "ЛОГИРОВАНИЕ",
                ["settings.verbose_logging"] = "Включить подробные логи",
                ["settings.logging_hint"] = "Подробные логи помогают диагностировать проблемы. Логи сохраняются в %AppData%\\LoopLauncher\\logs",
                ["settings.open_logs"] = "Открыть папку логов",
                ["settings.download"] = "ЗАГРУЗКА",
                ["settings.always_full_download"] = "Всегда скачивать полную версию",
                ["settings.download_hint"] = "Рекомендуется. Отключение может привести к повреждению файлов при обновлении.",
                ["settings.font"] = "ШРИФТ",
                ["settings.font_hint"] = "Требуется перезапуск для применения",
                ["settings.advanced"] = "ДОПОЛНИТЕЛЬНО",
                ["settings.advanced_btn"] = "Режим профессионала",
                ["settings.advanced_hint"] = "Кастомные параметры запуска, открытие папок",
                
                ["advanced.title"] = "🔧 Расширенные настройки",
                ["advanced.game_args"] = "ПАРАМЕТРЫ ЗАПУСКА ИГРЫ",
                ["advanced.game_args_hint"] = "Переменные: {app-dir}, {java-exec}, {user-dir}, {uuid}, {name}",
                ["advanced.server_args"] = "ПАРАМЕТРЫ ЗАПУСКА СЕРВЕРА",
                ["advanced.server_args_hint"] = "Изменяет содержимое start-server.bat",
                ["advanced.reset_default"] = "↩ Сбросить",
                ["advanced.save_to_file"] = "💾 Сохранить в файл",
                ["advanced.folders"] = "ОТКРЫТЬ ПАПКИ",
                ["advanced.folder_game"] = "Игра",
                ["advanced.folder_server"] = "Сервер",
                ["advanced.folder_userdata"] = "UserData",
                ["advanced.folder_mods"] = "Моды",
                ["advanced.server_not_found"] = "Сервер не установлен для этой ветки",
                ["advanced.server_saved"] = "Конфигурация сервера сохранена!",
                ["advanced.folder_not_exists"] = "Папка не существует"
            };
        }
    }
}
