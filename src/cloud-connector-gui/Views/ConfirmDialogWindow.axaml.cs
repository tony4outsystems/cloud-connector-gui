using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CloudConnectorGui.Views;

public sealed partial class ConfirmDialogWindow : Window
{
    public ConfirmDialogWindow()
    {
        InitializeComponent();
    }

    public ConfirmDialogWindow(string title, string message, string confirmText, string cancelText)
        : this()
    {
        Title = title;
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        var confirmButton = this.FindControl<Button>("ConfirmButton")!;
        confirmButton.Content = confirmText;
        confirmButton.Click += (_, _) => Close(true);
        var cancelButton = this.FindControl<Button>("CancelButton")!;
        cancelButton.Content = cancelText;
        cancelButton.Click += (_, _) => Close(false);
    }

    public static Task<bool> ShowAsync(Window owner, string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        var dialog = new ConfirmDialogWindow(title, message, confirmText, cancelText);
        return dialog.ShowDialog<bool>(owner);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
