using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using Soundlet.Models;
using Soundlet.Services;

namespace Soundlet;

public partial class ConnectedPage : ContentPage, IQueryAttributable
{
    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");
    private readonly SemaphoreSlim _arrivalGate = new(1, 1);
    private DateTime _selectedDate = DateTime.Today;
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _leaving;
    private bool _clearing;
    private bool _wifiConnection;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _wifiConnection = query.TryGetValue("connection", out var connection)
            && string.Equals(connection?.ToString(), "wifi", StringComparison.OrdinalIgnoreCase);
        ConnectionLabel.Text = _wifiConnection ? "Conectado via Wi-Fi" : "Conectado via Bluetooth";
        ConnectionSymbol.Text = _wifiConnection ? "≋" : "ᛒ";
    }

    private readonly BluetoothService _bluetooth;

    public ConnectedPage(BluetoothService bluetooth)
    {
        _bluetooth = bluetooth;
        InitializeComponent();
        var names = new[] { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };
        for (var i = 0; i < names.Length; i++)
            Weekdays.Add(new Label { Text = names[i], FontSize = 12, TextColor = Color.FromArgb("#A6B5D1"),
                HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center }, i, 0);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _leaving = false;
        _bluetooth.StateChanged += OnBleStateChanged;
        _bluetooth.AlertReceived += OnBleAlertReceived;
        _bluetooth.Error += OnBleError;
        UpdateConnectionState();
        try
        {
            await AlertHistory.Current.LoadAsync();
            RefreshHistory();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync("Histórico", "Não foi possível ler o histórico salvo. Tente abrir a tela novamente.", "OK");
        }
    }

    private void RefreshHistory(Guid? excluded = null)
    {
        Cards.Children.Clear();
        foreach (var record in AlertHistory.Current.Records
                     .Where(a => a.OccurredAt.Date == _selectedDate && a.Id != excluded)
                     .OrderByDescending(a => a.OccurredAt))
            Cards.Children.Add(CreateCard(record));
        EmptyState.IsVisible = Cards.Children.Count == 0;
        EmptyMessage.Text = AlertHistory.Current.Records.Count == 0
            ? "Nenhum alerta recebido até o momento."
            : "Nenhum alerta registrado nesta data";
        UpdateClearButton();
        UpdateDateButton();
    }

    private void UpdateClearButton()
    {
        ClearButton.IsEnabled = !_clearing && AlertHistory.Current.Records.Count > 0;
        ClearButton.Opacity = ClearButton.IsEnabled ? 1 : 0.45;
    }

    private View CreateCard(AlertRecord record)
    {
        var definition = AlertRecord.Definition(record.Type);
        var color = Color.FromArgb(definition.Color);
        var isClap = record.Type == AlertType.Clap;
        var icon = new Border
        {
            WidthRequest = 46, HeightRequest = 46, Stroke = color,
            StrokeShape = new RoundRectangle { CornerRadius = 11 },
            BackgroundColor = color.WithAlpha(0.08f),
            Shadow = new Shadow { Brush = color, Opacity = isClap ? 0.45f : 0.26f, Radius = 10, Offset = new Point(0, 0) },
            Content = new Image { Source = definition.Icon, Margin = 8 }
        };
        var title = new Label { Text = definition.Title, TextColor = Colors.White, FontSize = 14,
            FontFamily = "OpenSansSemibold", LineBreakMode = LineBreakMode.WordWrap };
        var description = new Label { Text = definition.Description, TextColor = Color.FromArgb("#B8C2D5"),
            FontSize = 11, LineBreakMode = LineBreakMode.WordWrap };
        var copy = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { title, description } };
        var time = new Label { Text = record.OccurredAt.ToString("HH:mm", Brazilian), FontSize = 12,
            TextColor = Color.FromArgb("#B8C2D5"), VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.End, MinimumWidthRequest = 36 };
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
        grid.Add(icon, 0); grid.Add(copy, 1); grid.Add(time, 2);
        var card = new Border
        {
            Stroke = color.WithAlpha(isClap ? 0.9f : 0.45f), StrokeThickness = isClap ? 1 : 0.8,
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
            Background = new LinearGradientBrush(new GradientStopCollection
            {
                new(isClap ? Color.FromArgb("#281239") : color.WithAlpha(0.13f), 0),
                new(Color.FromArgb(isClap ? "#100F24" : "#021624"), 1)
            }, new Point(0, 0), new Point(1, 1)),
            Padding = new Thickness(10, 9), Content = grid, MinimumHeightRequest = 65
        };
        SemanticProperties.SetDescription(card, $"{definition.Title}. {definition.Description}. {time.Text}");
        return new VerticalStackLayout { Padding = new Thickness(0, 0, 0, 7), Children = { card } };
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        if (_clearing || AlertHistory.Current.Records.Count == 0) return;
        _clearing = true;
        UpdateClearButton();
        try
        {
            var confirmed = await DisplayAlertAsync("Limpar histórico",
                "Deseja apagar todas as notificações recebidas?", "Limpar", "Cancelar");
            if (!confirmed || _leaving) return;
            await _arrivalGate.WaitAsync();
            try
            {
                await AlertHistory.Current.ClearAsync();
                if (!_leaving) RefreshHistory();
            }
            finally { _arrivalGate.Release(); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!_leaving)
                await DisplayAlertAsync("Histórico", "Não foi possível limpar o histórico. Tente novamente.", "OK");
        }
        finally
        {
            _clearing = false;
            UpdateClearButton();
        }
    }

    private async Task PresentArrivalAsync(AlertRecord record)
    {
        var occurredAt = record.OccurredAt;
        _selectedDate = occurredAt.Date;
        // Other notifications may have been persisted while the previous card was animating.
        RefreshHistory(record.Id);
        EmptyState.IsVisible = false;
        await HistoryScroll.ScrollToAsync(0, 0, false);
        var card = CreateCard(record);
        var size = card.Measure(Math.Max(200, HistoryScroll.Width), double.PositiveInfinity);
        var target = Math.Max(72, size.Height);
        card.Opacity = 0;
        card.TranslationY = -20;
        card.Scale = 0.98;
        var slot = new Grid { HeightRequest = 0, IsClippedToBounds = true, Children = { card } };
        Cards.Children.Insert(0, slot);
        var completion = new TaskCompletionSource();
        new Animation(progress =>
        {
            slot.HeightRequest = target * progress;
            card.Opacity = progress;
            card.TranslationY = -20 * (1 - progress);
            card.Scale = 0.98 + 0.02 * progress;
        }).Commit(slot, "AlertArrival", 16, 340, Easing.CubicOut,
            (_, _) => completion.TrySetResult());
        await completion.Task;
        slot.HeightRequest = -1;
    }

    private async void OnBleAlertReceived(object? sender, AlertRecord record)
    {
        await _arrivalGate.WaitAsync();
        try
        {
            // An alert queued before clearing may already have been removed from storage.
            if (!_leaving && AlertHistory.Current.Records.Any(r => r.Id == record.Id))
                await PresentArrivalAsync(record);
        }
        finally { _arrivalGate.Release(); }
    }
    private async void OnBleError(object? sender, string message)
    {
        if (!_leaving) await DisplayAlertAsync("Histórico", message, "OK");
    }
    private void OnBleStateChanged(object? sender, EventArgs e) => UpdateConnectionState();
    private void UpdateConnectionState()
    {
        if (_wifiConnection) return;
        ConnectionHeadingState.Text = _bluetooth.IsConnected ? "conectada" : "desconectada";
        ConnectionHeadingState.TextColor = Color.FromArgb(_bluetooth.IsConnected ? "#00E0ED" : "#BCC2D3");
        ConnectionLabel.Text = _bluetooth.IsConnected ? "Conectado via Bluetooth" : "Conexão perdida. Volte para buscar.";
        ConnectionDot.Color = Color.FromArgb(_bluetooth.IsConnected ? "#00EDAE" : "#8299B7");
    }

    private void UpdateDateButton() => DateButton.Text = $"▦  {(_selectedDate == DateTime.Today ? "Hoje" : _selectedDate.ToString("dd/MM/yyyy", Brazilian))}  {(CalendarOverlay.IsVisible ? "⌃" : "﹀")}";

    private void OnDateClicked(object? sender, EventArgs e)
    {
        if (CalendarOverlay.IsVisible) { CloseCalendar(); return; }
        // Open at the current month; selection remains independent of the visible month.
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RenderCalendar();
        CalendarOverlay.IsVisible = true;
        PositionCalendar();
        UpdateDateButton();
    }

    private void RenderCalendar()
    {
        MonthLabel.Text = Brazilian.TextInfo.ToTitleCase(_month.ToString("MMMM yyyy", Brazilian));
        Days.Children.Clear();
        var dates = CalendarDates.ForMonth(_month);
        for (var i = 0; i < 42; i++)
        {
            var date = dates[i];
            if (date is null) continue;
            var day = date.Value;
            var selected = day.Date == _selectedDate;
            var button = new Button
            {
                Text = day.Day.ToString(Brazilian), FontSize = 15,
                TextColor = selected ? Color.FromArgb("#001625") : Color.FromArgb(day.Month == _month.Month ? "#F4F7FF" : "#5979A8"),
                BackgroundColor = selected ? Color.FromArgb("#00CEE9") : Colors.Transparent,
                BorderWidth = 0, Padding = 0, CornerRadius = 22,
                MinimumWidthRequest = 0, MinimumHeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill
            };
            SemanticProperties.SetDescription(button, day.ToString("dd 'de' MMMM 'de' yyyy", Brazilian) + (selected ? ", selecionado" : ""));
            button.Clicked += (_, _) =>
            {
                _selectedDate = day.Date;
                CloseCalendar();
                RefreshHistory();
            };
            Days.Add(button, i % 7, i / 7);
        }
    }

    private void OnPreviousMonth(object? sender, EventArgs e)
    {
        if (_month.Year == 1 && _month.Month == 1) return;
        _month = _month.AddMonths(-1); RenderCalendar();
    }
    private void OnNextMonth(object? sender, EventArgs e)
    {
        if (_month.Year == 9999 && _month.Month == 12) return;
        _month = _month.AddMonths(1); RenderCalendar();
    }
    private void OnOutsideTapped(object? sender, TappedEventArgs e) => CloseCalendar();
    private void CloseCalendar() { CalendarOverlay.IsVisible = false; UpdateDateButton(); }

    private void OnRootSizeChanged(object? sender, EventArgs e)
    {
        Bracelet.HeightRequest = Root.Height < 650 ? 110 : 142;
        Heading.FontSize = Root.Width < 360 ? 22 : 25;
        if (CalendarOverlay.IsVisible) PositionCalendar();
    }

    private void PositionCalendar()
    {
        var desired = PageLayout.Padding.Top + DateButton.Y + 4;
        // DateButton.Y is relative to its own header grid.
        if (DateButton.Parent is Grid header) desired += header.Y + header.Height;
        // Safe-area padding can be applied natively without reducing Grid.Height.
        // The laid-out footer gives us the actual bottom of usable content.
        var viewport = Math.Min(CalendarOverlay.Height > 0 ? CalendarOverlay.Height : Root.Height,
            ClearButton.Y + ClearButton.Height + PageLayout.Padding.Bottom);
        var top = Math.Max(6, Math.Min(desired, viewport - 212));
        var height = Math.Min(374, Math.Max(200, viewport - top - 10));
        CalendarPanel.HeightRequest = height;
        CalendarDaysScroll.HeightRequest = Math.Max(88, height - 112);
        CalendarPanel.Margin = new Thickness(22, top, 22, 0);
        var selectedIndex = CalendarDates.ForMonth(_month).ToList().FindIndex(d => d == _selectedDate);
        var row = selectedIndex < 0 ? 0 : selectedIndex / 7;
        Dispatcher.Dispatch(() => _ = CalendarDaysScroll.ScrollToAsync(0,
            CalendarDaysScroll.HeightRequest < 264 ? Math.Max(0, (row - 1) * 44) : 0, false));
    }

    private void OnCalendarSizeChanged(object? sender, EventArgs e)
    {
        if (CalendarOverlay.IsVisible) PositionCalendar();
    }

    protected override bool OnBackButtonPressed()
    {
        if (CalendarOverlay.IsVisible) { CloseCalendar(); return true; }
        return base.OnBackButtonPressed();
    }

    protected override void OnDisappearing()
    {
        _leaving = true;
        _bluetooth.StateChanged -= OnBleStateChanged;
        _bluetooth.AlertReceived -= OnBleAlertReceived;
        _bluetooth.Error -= OnBleError;
        CloseCalendar();
        base.OnDisappearing();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_leaving) return;
        _leaving = true;
        await Shell.Current.GoToAsync("..");
    }

    private async void OnMoreClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Soundlet · ESP32-S3", _wifiConnection
            ? "A conexão Wi-Fi real será implementada em uma próxima etapa. Os alertas são recebidos pela pulseira via Bluetooth."
            : "A pulseira envia alertas PALMA por BLE. Para conectar novamente após uma desconexão, volte à busca.", "Entendi");
}
