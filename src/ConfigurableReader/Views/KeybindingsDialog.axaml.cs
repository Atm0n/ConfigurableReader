using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ConfigurableReader.Models;
using ConfigurableReader.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ConfigurableReader.Views;

public class KeybindingItemViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ReaderAction Action { get; }
    public string DisplayName { get; }

    private Key _boundKey;
    public Key BoundKey
    {
        get => _boundKey;
        set
        {
            if (_boundKey != value)
            {
                _boundKey = value;
                OnPropertyChanged(nameof(BoundKey));
                OnPropertyChanged(nameof(KeyDisplay));
            }
        }
    }

    private bool _isListening;
    public bool IsListening
    {
        get => _isListening;
        set
        {
            if (_isListening != value)
            {
                _isListening = value;
                OnPropertyChanged(nameof(IsListening));
                OnPropertyChanged(nameof(KeyDisplay));
                OnPropertyChanged(nameof(KeyColor));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonForeground));
            }
        }
    }

    public string KeyDisplay => IsListening
        ? LocalizationService.GetString("PressAnyKey")
        : (_boundKey == Key.None ? "None" : _boundKey.ToString());

    public IBrush KeyColor => IsListening
        ? Brushes.Gold
        : new SolidColorBrush(Color.Parse("#4DACFF"));

    public string ButtonText => IsListening
        ? LocalizationService.GetString("Cancel")
        : LocalizationService.GetString("Rebind");

    public IBrush ButtonForeground => IsListening
        ? Brushes.Gold
        : new SolidColorBrush(Color.Parse("#E0E0E0"));

    public KeybindingItemViewModel(ReaderAction action, Key boundKey)
    {
        Action = action;
        BoundKey = boundKey;
        DisplayName = LocalizationService.GetString($"Action{action}");
        if (string.IsNullOrEmpty(DisplayName) || DisplayName == $"Action{action}")
        {
            DisplayName = action.ToString();
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class KeybindingsDialog : Window
{
    private readonly KeyBindingsConfig _config;
    private readonly ObservableCollection<KeybindingItemViewModel> _items = [];
    private KeybindingItemViewModel? _currentlyListeningItem;

    public KeybindingsDialog() : this(KeyBindingsConfig.GetDefaultBindings())
    {
    }

    public KeybindingsDialog(KeyBindingsConfig config)
    {
        _config = config.Clone();
        InitializeComponent();

        foreach (var action in Enum.GetValues<ReaderAction>())
        {
            _items.Add(new KeybindingItemViewModel(action, _config.GetKey(action)));
        }

        ShortcutsItemsControl.ItemsSource = _items;
        KeyDown += Window_KeyDown;
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_currentlyListeningItem != null)
        {
            if (e.Key == Key.Escape)
            {
                // Cancel listening
                _currentlyListeningItem.IsListening = false;
                _currentlyListeningItem = null;
            }
            else
            {
                // Reassign key
                var newKey = e.Key;
                // If another action was bound to this key, clear it to prevent conflict
                var conflict = _items.FirstOrDefault(i => i != _currentlyListeningItem && i.BoundKey == newKey);
                if (conflict != null)
                {
                    conflict.BoundKey = Key.None;
                }

                _currentlyListeningItem.BoundKey = newKey;
                _currentlyListeningItem.IsListening = false;
                _currentlyListeningItem = null;
            }
            e.Handled = true;
        }
    }

    private void RebindButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is KeybindingItemViewModel item)
        {
            if (item.IsListening)
            {
                item.IsListening = false;
                _currentlyListeningItem = null;
                return;
            }

            if (_currentlyListeningItem != null)
            {
                _currentlyListeningItem.IsListening = false;
            }

            item.IsListening = true;
            _currentlyListeningItem = item;
            Focus();
        }
    }

    private void ResetDefaultsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentlyListeningItem != null)
        {
            _currentlyListeningItem.IsListening = false;
            _currentlyListeningItem = null;
        }

        foreach (var item in _items)
        {
            item.BoundKey = KeyBindingsConfig.GetDefaultKey(item.Action);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
        {
            _config.SetKey(item.Action, item.BoundKey);
        }
        Close(_config);
    }
}
