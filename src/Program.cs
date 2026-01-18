using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using LoopLauncher.Services; // Замените на ваши пространства имен
using LoopLauncher.Helpers;

namespace LoopLauncher
{
    internal class Program
    {
        // Статическое поле для доступа к сервисам из App.xaml.cs
        public static IServiceProvider? ServiceProvider { get; private set; }

        // Точка входа в приложение
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // 1. Настраиваем DI-контейнер
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                ServiceProvider = serviceCollection.BuildServiceProvider();

                // 2. Запускаем Avalonia
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                // Здесь можно добавить логирование критических ошибок запуска
                Console.WriteLine(ex.Message);
            }
        }

        // Настройка AppBuilder
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        // Метод для регистрации ваших сервисов
        private static void ConfigureServices(IServiceCollection services)
        {
            // Регистрация сервисов (Singleton или Transient)
            services.AddSingleton<GameLauncher>();
            services.AddSingleton<LocalizationService>();
            services.AddSingleton<SearchFilters>();
            services.AddSingleton<ModManifest>();
            services.AddSingleton<ModAuthor>();
            services.AddSingleton<InstalledMod>();
            services.AddSingleton<CurseForgeSearchResult>();
            services.AddSingleton<CurseForgeLogo>();
            services.AddSingleton<CurseForgeAuthor>();
            services.AddSingleton<CurseForgeFile>();
            services.AddSingleton<ModCategory>();
            services.AddSingleton<ModpackMod>();
            services.AddSingleton<Modpack>();
            services.AddSingleton<ModpackConfig>();
            services.AddSingleton<ModpackManifest>();
            services.AddSingleton<ModpackService>(sp =>
            {
                var settings = sp.GetRequiredService<SettingsManager>().Load();
                return new ModpackService(settings.GameDirectory);
            });
            services.AddTransient<ModService>(sp =>
            {
                var settings = sp.GetRequiredService<SettingsManager>().Load();
                return new ModService(settings.GameDirectory);
            });
            services.AddSingleton<NewsFeedService>();
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<SystemCheckService>();
            services.AddSingleton<UpdateService>();
            

            
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<ModsWindow>();
            services.AddTransient<EditModpackDialog>();
            services.AddTransient<CreateModpackDialog>();
            services.AddTransient<AdvancedWindow>();
        }
    }
}