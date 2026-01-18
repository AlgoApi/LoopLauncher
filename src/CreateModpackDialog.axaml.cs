using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LoopLauncher.Helpers;
using LoopLauncher.Services;

namespace LoopLauncher
{
    public partial class CreateModpackDialog : Window
    {
        private readonly LocalizationService _localization;
        private readonly ObservableCollection<SelectableMod> _selectableMods;
        
        public string? ModpackName { get; private set; }
        public List<string> SelectedModPaths { get; private set; } = new();
        public new bool DialogResult { get; private set; }

        public CreateModpackDialog(LocalizationService localization, List<InstalledMod> availableMods)
        {
            InitializeComponent();
            
            if (FontHelper.CurrentFont != null)
            {
                FontFamily = FontHelper.CurrentFont;
            }

            _localization = localization;
            
            // Create selectable mod items
            _selectableMods = new ObservableCollection<SelectableMod>(
                availableMods.Select(m => new SelectableMod(m))
            );
            
            foreach (var mod in _selectableMods)
            {
                mod.PropertyChanged += OnModSelectionChanged;
            }
            
            ModsListBox.ItemsSource = _selectableMods;
            
            UpdateUI();
            UpdateSelectionSummary();
        }

        public CreateModpackDialog() { InitializeComponent(); }

        private void UpdateUI()
        {
            TitleText.Text = _localization.Get("modpack.create.title");
            NameLabel.Text = _localization.Get("modpack.create.name_label");
            SelectModsLabel.Text = _localization.Get("modpack.create.select_mods");
            CancelBtn.Content = _localization.Get("settings.cancel");
            CreateBtn.Content = _localization.Get("modpack.create.button");
        }

        private void OnModSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableMod.IsSelected))
            {
                UpdateSelectionSummary();
            }
        }

        private void UpdateSelectionSummary()
        {
            var count = _selectableMods.Count(m => m.IsSelected);
            SelectionSummary.Text = string.Format(_localization.Get("modpack.create.selected_count"), count);
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var props = e.GetCurrentPoint(this).Properties;
                if (props.IsLeftButtonPressed)
                {
                    // BeginMoveDrag доступен у Window
                    this.BeginMoveDrag(e);
                }
            }
            catch
            {
                // Игнорируем ошибки перетаскивания
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private IList<object> GetSelectedMods()
        {
            var list = new List<object>();

            var items = ModsListBox.Items;
            if (items == null) return list;

            // Если Items — IEnumerable of view models with 'IsSelected' property
            foreach (var item in items.Cast<object>())
            {
                // try to find property IsSelected via dynamic / reflection
                var type = item.GetType();
                var prop = type.GetProperty("IsSelected");
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    var val = (bool)prop.GetValue(item)!;
                    if (val) list.Add(item);
                }
            }

            return list;
        }

        private async void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            var name = ModpackNameBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                NameErrorText.Text = "Please enter a modpack name.";
                NameErrorText.IsVisible = true;
                return;
            }
            
            ModpackName = name;
            SelectedModPaths = _selectableMods
                .Where(m => m.IsSelected)
                .Select(m => m.Mod.FilePath)
                .ToList();
            
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Wrapper class for mods with selection state
    /// </summary>
    public class SelectableMod : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public InstalledMod Mod { get; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
        
        public SelectableMod(InstalledMod mod)
        {
            Mod = mod;
            _isSelected = false;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
