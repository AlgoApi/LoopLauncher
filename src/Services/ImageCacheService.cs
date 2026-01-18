using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging; // Здесь живет Bitmap

namespace LoopLauncher.Services
{
    public static class ImageCacheService
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly string _cacheDir;
        
        // В Avalonia используем Bitmap вместо BitmapImage
        private static readonly Dictionary<string, Bitmap> _memoryCache = new();
        private static readonly object _lock = new();

        static ImageCacheService()
        {
            _cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LoopLauncher", "cache", "images"
            );
            Directory.CreateDirectory(_cacheDir);
        }

        public static async Task<Bitmap?> GetImageAsync(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            lock (_lock)
            {
                if (_memoryCache.TryGetValue(url, out var cached))
                    return cached;
            }

            try
            {
                var fileName = GetCacheFileName(url);
                var filePath = Path.Combine(_cacheDir, fileName);

                if (File.Exists(filePath))
                {
                    var image = LoadImageFromFile(filePath);
                    if (image != null)
                    {
                        lock (_lock) { _memoryCache[url] = image; }
                        return image;
                    }
                }

                var bytes = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(filePath, bytes);

                var downloadedImage = LoadImageFromBytes(bytes);
                if (downloadedImage != null)
                {
                    lock (_lock) { _memoryCache[url] = downloadedImage; }
                }
                return downloadedImage;
            }
            catch
            {
                return null;
            }
        }

        private static string GetCacheFileName(string url)
        {
            // MD5 по-прежнему работает, но можно использовать SHA256 для современности
            var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(url));
            var hashString = Convert.ToHexString(hashBytes).ToLower();
            
            var uri = new Uri(url);
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            return hashString + ext;
        }

        // В Avalonia загрузка изображения делается в одну строку
        private static Bitmap? LoadImageFromFile(string path)
        {
            try
            {
                // Просто создаем новый Bitmap, передавая путь
                return new Bitmap(path);
            }
            catch { return null; }
        }

        private static Bitmap? LoadImageFromBytes(byte[] bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                // В Avalonia Bitmap принимает поток в конструктор
                return new Bitmap(ms);
            }
            catch { return null; }
        }

        public static void ClearCache()
        {
            lock (_lock) { _memoryCache.Clear(); }
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    foreach (var file in Directory.GetFiles(_cacheDir))
                        File.Delete(file);
                }
            }
            catch { }
        }
    }
}