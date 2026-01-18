using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using LoopLauncher.Services;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LoopLauncher
{
    public partial class App : Application
    {
        public IServiceProvider Services => Program.ServiceProvider!;
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        async public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = Services.GetRequiredService<MainWindow>();

                await StartSystemChecksAsync(desktop.MainWindow);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task StartSystemChecksAsync(Window? owner)
        {
            var systemCheck = new SystemCheckService();

            // Run system check on first launch
            if (systemCheck.IsFirstLaunch())
            {
                var result = await systemCheck.RunChecksAsync();

                // Show errors if critical checks failed
                var errors = result.GetErrors();
                if (errors.Count > 0)
                {
                    var errorMsg = new StringBuilder();
                    errorMsg.AppendLine("Critical issues detected:\n");
                    foreach (var error in errors)
                    {
                        errorMsg.AppendLine($"• {error}");
                    }
                    errorMsg.AppendLine("\nThe launcher may not work correctly.");

                    await MessageBoxManager.GetMessageBoxStandard(
                        $"LoopLauncher - System Check",
                        errorMsg.ToString(),
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                }
                else
                {
                    // Show warnings if non-critical issues found
                    var warnings = result.GetWarnings();
                    if (warnings.Count > 0)
                    {
                        var warnMsg = new StringBuilder();
                        warnMsg.AppendLine("Some issues were detected:\n");
                        foreach (var warning in warnings)
                        {
                            warnMsg.AppendLine($"• {warning}");
                        }
                        warnMsg.AppendLine("\nThe launcher will still work, but some features may be limited.");

                        await MessageBoxManager.GetMessageBoxStandard(
                            $"LoopLauncher - System Check",
                            warnMsg.ToString(),
                            ButtonEnum.Ok,
                            MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                    }
                }

                // Mark check as completed
                systemCheck.MarkCheckCompleted();
            }
        }
    }
}
