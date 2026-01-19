using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Platform;

namespace LoopLauncher.Helpers
{
    public static class FontHelper
    {
        private static bool _initialized = false;
        private static string? _fontDir;
        private static List<string>? _availableFonts;
        
        public static FontFamily? CurrentFont { get; private set; }
        public static FontFamily? CinzelFont { get; private set; }
        
        // Рекомендуемые шрифты (показываются первыми)
        private static readonly string[] RecommendedFonts = { "Inter", "Cinzel", "Segoe UI", "Arial", "Consolas", "Verdana", "Tahoma" };
        
        public static string CurrentFontName { get; private set; } = "Inter";

        /// <summary>
        /// Получает список всех доступных шрифтов (рекомендуемые + системные)
        /// </summary>
        public static List<string> AvailableFonts
        {
            get
            {
                if (_availableFonts == null)
                {
                    _availableFonts = new List<string>();
                    
                    // Сначала добавляем рекомендуемые
                    _availableFonts.AddRange(RecommendedFonts);
                    
                    // Затем все системные шрифты (кроме уже добавленных)
                    var systemFonts = FontManager.Current.SystemFonts
                        .Select(f => f.Name) // В Avalonia используем .Name
                        .Where(name => !RecommendedFonts.Contains(name))
                        .OrderBy(name => name)
                        .ToList();
                    
                    _availableFonts.AddRange(systemFonts);
                }
                return _availableFonts;
            }
        }

        public static void Initialize(string fontName = "Inter")
        {
            if (!_initialized)
            {
                _initialized = true;
                
                try
                {
                    // Папка для шрифтов в AppData
                    _fontDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "LoopLauncher", "fonts"
                    );
                    Directory.CreateDirectory(_fontDir);

                    // Извлекаем шрифты Cinzel
                    ExtractFont("cinzel_regular.ttf");
                    ExtractFont("cinzel_bold.ttf");
                    
                    // Извлекаем Inter
                    ExtractFont("inter_regular.ttf");
                    ExtractFont("inter_bold.ttf");

                    // Создаём FontFamily для Cinzel
                    string cinzelPath = Path.Combine(_fontDir, "cinzel_regular.ttf");
                    if (File.Exists(cinzelPath))
                    {
                        var folderUri = new Uri(cinzelPath);
                        
                        CurrentFont = new FontFamily(folderUri, "Cinzel(RUS BY LYAJKA)");
                    }
                    else
                    {
                        CurrentFont = new FontFamily("Segoe UI");
                    }
                }
                catch
                {
                    CinzelFont = new FontFamily("Segoe UI");
                }
            }
            
            SetFont(fontName);
        }

        public static void SetFont(string fontName)
        {
            CurrentFontName = fontName;
            
            try
            {
                if (fontName == "Inter" && _fontDir != null)
                {
                    string fontPath = Path.Combine(_fontDir, "inter_regular.ttf");
                    if (File.Exists(fontPath))
                    {
                        // Важно: Uri должен указывать на ФАЙЛ, а строка — на ИМЯ семейства
                        var fileUri = new Uri(fontPath);
                        CurrentFont = new FontFamily(fileUri, "Inter");
                    }
                    else { throw new FileNotFoundException(); }
                }
                else if (fontName == "Cinzel")
                {
                    string fontPath = Path.Combine(_fontDir, "cinzel_regular.ttf");
                    if (File.Exists(fontPath))
                    {
                        var fileUri = new Uri(fontPath);
                        // Если имя со скобками не сработает, попробуйте просто "Cinzel"
                        CurrentFont = new FontFamily(fileUri, "Cinzel(RUS BY LYAJKA)");
                    }
                    else { throw new FileNotFoundException(); }
                }
                else
                {
                    CurrentFont = new FontFamily(fontName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка шрифта: {ex.Message}");
                CurrentFont = new FontFamily("Default"); // Системный шрифт по умолчанию
            }
        }

        private static void ExtractFont(string fontName)
        {
            if (_fontDir == null) return;
            
            var fontPath = Path.Combine(_fontDir, fontName);
            
            if (!File.Exists(fontPath))
            {
                try
                {
                    var uri = new Uri($"avares://src/Fonts/{fontName}");
                    using var stream = AssetLoader.Open(uri);
                    using var fileStream = File.Create(fontPath);
                    stream.CopyTo(fileStream);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка извлечения шрифта {fontName}: {ex.Message}");
                }
            }
        }
    }
}
