using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using GitExtensions.Extensibility;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using BoxResult = GitExtensions.Extensibility.DialogResult;
using Path = Avalonia.Controls.Shapes.Path;

namespace GitUI.Avalonia.Hosting;

/// <summary>The icons of the message boxes and task dialogs of Avalonia, drawn (the icons of the system are not available).</summary>
public enum DialogBoxIconKind
{
    None,
    Information,
    Warning,
    Error,
    Question,
    Shield,
}

/// <summary>
///  The parts of the message boxes and task dialogs of Avalonia, which are shown off Windows instead of the native ones
///  (<see cref="IDialogBoxHost"/>, docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
/// </summary>
public static partial class DialogBoxParts
{
    [GeneratedRegex("""<a\s+href\s*=\s*"(?<href>[^"]*)"\s*>(?<text>.*?)</a>""", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex LinkRegex { get; }

    public static DialogBoxIconKind ToIconKind(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Information => DialogBoxIconKind.Information,
        MessageBoxIcon.Warning => DialogBoxIconKind.Warning,
        MessageBoxIcon.Error => DialogBoxIconKind.Error,
        MessageBoxIcon.Question => DialogBoxIconKind.Question,
        _ => DialogBoxIconKind.None,
    };

    public static DialogBoxIconKind ToIconKind(TaskDialogIcon? icon)
        => icon == TaskDialogIcon.Information ? DialogBoxIconKind.Information
            : icon == TaskDialogIcon.Warning ? DialogBoxIconKind.Warning
            : icon == TaskDialogIcon.Error ? DialogBoxIconKind.Error
            : icon == TaskDialogIcon.Shield ? DialogBoxIconKind.Shield
            : DialogBoxIconKind.None;

    /// <summary>A 32 pixel icon (a colored circle with a sign, a warning triangle or a shield).</summary>
    public static Control? CreateIcon(DialogBoxIconKind kind)
    {
        if (kind == DialogBoxIconKind.None)
        {
            return null;
        }

        Grid icon = new() { Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Top, Name = $"icon{kind}" };
        switch (kind)
        {
            case DialogBoxIconKind.Warning:
                icon.Children.Add(new Path { Data = Geometry.Parse("M16,2 L31,29 L1,29 Z"), Fill = new SolidColorBrush(Color.FromRgb(0xF2, 0xC2, 0x00)), Stretch = Stretch.None });
                icon.Children.Add(Sign("!", Brushes.Black, top: 7));
                break;
            case DialogBoxIconKind.Shield:
                icon.Children.Add(new Path { Data = Geometry.Parse("M16,1 L29,6 C29,18 24,26 16,31 C8,26 3,18 3,6 Z"), Fill = new SolidColorBrush(Color.FromRgb(0x1E, 0x73, 0xBE)), Stretch = Stretch.None });
                break;
            default:
                Color color = kind == DialogBoxIconKind.Error ? Color.FromRgb(0xD1, 0x34, 0x38) : Color.FromRgb(0x1E, 0x73, 0xBE);
                icon.Children.Add(new Ellipse { Fill = new SolidColorBrush(color) });
                icon.Children.Add(Sign(kind switch { DialogBoxIconKind.Error => "✕", DialogBoxIconKind.Question => "?", _ => "i" }, Brushes.White, top: 0));
                break;
        }

        return icon;

        static TextBlock Sign(string text, IBrush foreground, double top)
            => new()
            {
                Text = text,
                Foreground = foreground,
                FontWeight = FontWeight.Bold,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, top, 0, 0),
            };
    }

    /// <summary>
    ///  A text block of <paramref name="text"/>; with <paramref name="onLink"/>, its <c>&lt;a href="…"&gt;</c> links are links
    ///  (as the task dialog of Windows with <c>TDF_ENABLE_HYPERLINKS</c>).
    /// </summary>
    public static TextBlock CreateText(string text, Action<string>? onLink = null)
    {
        SelectableTextBlock block = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 520 };
        if (onLink is null || !LinkRegex.IsMatch(text))
        {
            block.Text = text;
            return block;
        }

        InlineCollection inlines = [];
        int position = 0;
        foreach (Match match in LinkRegex.Matches(text))
        {
            if (match.Index > position)
            {
                inlines.Add(new Run(text[position..match.Index]));
            }

            string href = match.Groups["href"].Value;
            HyperlinkButton link = new() { Content = match.Groups["text"].Value, Padding = default, Margin = default };
            link.Click += (_, _) => onLink(href);
            inlines.Add(new InlineUIContainer(link) { BaselineAlignment = BaselineAlignment.TextBottom });
            position = match.Index + match.Length;
        }

        if (position < text.Length)
        {
            inlines.Add(new Run(text[position..]));
        }

        block.Inlines = inlines;
        return block;
    }

    /// <summary>A button of the footer with an access key (<c>&amp;</c> in the text).</summary>
    public static Button CreateButton(string text, string name)
        => new() { Content = TranslatedText.ToAccessKeyText(text), Name = name };
}

/// <summary>The message box of Avalonia (as the message box of Windows: the buttons, icon and default button of the call).</summary>
public sealed class MessageBoxWindow : DialogWindow
{
    private readonly BoxResult? _cancelResult;
    private bool _closedByButton;

    public MessageBoxWindow(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, DialogBoxStrings strings)
    {
        Title = caption;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        MinWidth = 280;
        Text = text;

        (BoxResult Result, TranslatedText Caption)[] choices = buttons switch
        {
            MessageBoxButtons.OKCancel => [(BoxResult.OK, strings.Ok), (BoxResult.Cancel, strings.Cancel)],
            MessageBoxButtons.AbortRetryIgnore => [(BoxResult.Abort, strings.Abort), (BoxResult.Retry, strings.Retry), (BoxResult.Ignore, strings.Ignore)],
            MessageBoxButtons.YesNoCancel => [(BoxResult.Yes, strings.Yes), (BoxResult.No, strings.No), (BoxResult.Cancel, strings.Cancel)],
            MessageBoxButtons.YesNo => [(BoxResult.Yes, strings.Yes), (BoxResult.No, strings.No)],
            MessageBoxButtons.RetryCancel => [(BoxResult.Retry, strings.Retry), (BoxResult.Cancel, strings.Cancel)],
            MessageBoxButtons.CancelTryContinue => [(BoxResult.Cancel, strings.Cancel), (BoxResult.TryAgain, strings.TryAgain), (BoxResult.Continue, strings.Continue)],
            _ => [(BoxResult.OK, strings.Ok)],
        };

        // As the message box of Windows: Escape and the close button give Cancel, or OK when it is the only button; they do
        // nothing when there is no such button (e.g. Yes / No).
        _cancelResult = choices.Any(c => c.Result == BoxResult.Cancel) ? BoxResult.Cancel
            : choices.Length == 1 ? BoxResult.OK
            : null;

        int defaultIndex = Math.Min(choices.Length - 1, ((int)defaultButton) >> 8);
        StackPanel buttonPanel = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6 };
        for (int i = 0; i < choices.Length; i++)
        {
            (BoxResult result, TranslatedText choiceCaption) = choices[i];
            Button button = DialogBoxParts.CreateButton(choiceCaption.Text, $"button{result}");
            button.Click += (_, _) => CloseWith(result);
            if (i == defaultIndex)
            {
                button.IsDefault = true;
                button.Classes.Add("accent");
                DefaultButton = button;
            }

            buttonPanel.Children.Add(button);
        }

        Buttons = buttonPanel.Children.OfType<Button>().ToList();

        DockPanel content = new();
        Border footer = new() { Child = buttonPanel };
        footer.Classes.Add("dialogFooter");
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);

        StackPanel body = new() { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(16, 20, 20, 20) };
        if (DialogBoxParts.CreateIcon(DialogBoxParts.ToIconKind(icon)) is { } iconControl)
        {
            body.Children.Add(iconControl);
        }

        body.Children.Add(DialogBoxParts.CreateText(text));
        content.Children.Add(body);
        Content = content;

        Opened += (_, _) => DefaultButton?.Focus();
    }

    /// <summary>The text of the message.</summary>
    public string Text { get; }

    /// <summary>The buttons, in order.</summary>
    public IReadOnlyList<Button> Buttons { get; }

    public Button? DefaultButton { get; }

    /// <summary>The button that closed the message box.</summary>
    public BoxResult Result { get; private set; }

    /// <summary>The text of the message box, as Ctrl+C copies it from the message box of Windows.</summary>
    public string CopyText
    {
        get
        {
            const string separator = "---------------------------";
            string buttons = string.Join("   ", Buttons.Select(b => (b.Content as string ?? "").Replace("_", "")));
            return $"{separator}{Environment.NewLine}{Title}{Environment.NewLine}{separator}{Environment.NewLine}{Text}{Environment.NewLine}{separator}{Environment.NewLine}{buttons}{Environment.NewLine}{separator}{Environment.NewLine}";
        }
    }

    protected override void OnEscapePressed()
    {
        if (_cancelResult is { } result)
        {
            CloseWith(result);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_closedByButton)
        {
            if (_cancelResult is { } result)
            {
                Result = result;
            }
            else
            {
                e.Cancel = true;
            }
        }

        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control && Clipboard is { } clipboard)
        {
            e.Handled = true;
            _ = clipboard.SetTextAsync(CopyText);
            return;
        }

        base.OnKeyDown(e);
    }

    private void CloseWith(BoxResult result)
    {
        Result = result;
        _closedByButton = true;
        Close();
    }
}

/// <summary>
///  The task dialog of Avalonia (as the task dialog of Windows: the heading, text, icon, buttons and command links, the
///  verification check box, the expander and the footnote of a <see cref="TaskDialogPage"/>).
/// </summary>
public sealed class TaskDialogWindow : DialogWindow
{
    private readonly TaskDialogPage _page;
    private readonly TaskDialogButton? _cancelResult;
    private bool _closedByButton;

    public TaskDialogWindow(TaskDialogPage page, DialogBoxStrings strings)
    {
        _page = page;
        Title = page.Caption ?? "";
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        CanMinimize = page.AllowMinimize;
        MinWidth = 360;
        Action<string>? onLink = page.EnableLinks ? page.PerformLinkClick : null;

        // Escape and the close button give the Cancel button (the one of the page, or the standard one with AllowCancel).
        _cancelResult = page.Buttons.FirstOrDefault(b => b == TaskDialogButton.Cancel) ?? (page.AllowCancel ? TaskDialogButton.Cancel : null);

        List<Button> buttons = [];
        StackPanel commandLinks = new() { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
        StackPanel footerButtons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6 };
        foreach (TaskDialogButton taskButton in page.Buttons)
        {
            Button button;
            if (taskButton is TaskDialogCommandLinkButton commandLink)
            {
                StackPanel linkContent = new() { Spacing = 2 };
                linkContent.Children.Add(new TextBlock { Text = TranslatedText.ToAccessKeyText(commandLink.Text ?? "").Replace("_", ""), FontWeight = FontWeight.SemiBold, FontSize = 14 });
                if (!string.IsNullOrEmpty(commandLink.DescriptionText))
                {
                    linkContent.Children.Add(new TextBlock { Text = commandLink.DescriptionText, TextWrapping = TextWrapping.Wrap, MaxWidth = 460, Opacity = 0.8 });
                }

                // As the command links of Windows: flat, with an arrow, highlighted under the mouse.
                DockPanel linkRow = new();
                TextBlock arrow = new() { Text = "➜", FontSize = 16, Margin = new Thickness(0, 0, 10, 0), Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x8C, 0x3C)) };
                DockPanel.SetDock(arrow, Dock.Left);
                linkRow.Children.Add(arrow);
                linkRow.Children.Add(linkContent);
                button = new Button
                {
                    Content = linkRow,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 8),
                    Name = $"commandLink{commandLinks.Children.Count}",
                };
                commandLinks.Children.Add(button);
            }
            else
            {
                button = DialogBoxParts.CreateButton(StandardText(taskButton, strings) ?? taskButton.Text ?? "", $"button{footerButtons.Children.Count}");
                footerButtons.Children.Add(button);
            }

            button.IsEnabled = taskButton.Enabled;
            button.Tag = taskButton;
            button.Click += (_, _) => OnButtonClicked(taskButton);
            if (page.DefaultButton is { } defaultButton && defaultButton == taskButton)
            {
                button.IsDefault = true;
                button.Classes.Add("accent");
                DefaultButton = button;
            }

            buttons.Add(button);
        }

        Buttons = buttons;

        // The main area: icon, heading, text, the expanded text after it, the command links.
        StackPanel main = new() { Spacing = 8 };
        if (!string.IsNullOrEmpty(page.Heading))
        {
            main.Children.Add(new TextBlock
            {
                Text = page.Heading,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 520,
                Foreground = new SolidColorBrush(Application.Current?.ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark
                    ? Color.FromRgb(0x6C, 0xB4, 0xF0)
                    : Color.FromRgb(0x1E, 0x5A, 0xA8)),
                Name = "heading",
            });
        }

        if (!string.IsNullOrEmpty(page.Text))
        {
            main.Children.Add(DialogBoxParts.CreateText(page.Text, onLink));
        }

        Control? expandedText = null;
        if (page.Expander is { Text.Length: > 0 } expander)
        {
            expandedText = DialogBoxParts.CreateText(expander.Text, onLink);
            expandedText.IsVisible = expander.Expanded;
            expandedText.Name = "expandedText";
            if (expander.Position == TaskDialogExpanderPosition.AfterText)
            {
                main.Children.Add(expandedText);
            }
        }

        if (commandLinks.Children.Count > 0)
        {
            main.Children.Add(commandLinks);
        }

        StackPanel top = new() { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(16, 16, 20, 16) };
        if (DialogBoxParts.CreateIcon(DialogBoxParts.ToIconKind(page.Icon)) is { } icon)
        {
            top.Children.Add(icon);
        }

        top.Children.Add(main);

        // The footer: the expander button and the check box on the left, the buttons on the right, then the footnote and the
        // expanded text after it.
        DockPanel footerRow = new() { LastChildFill = false };
        StackPanel footerLeft = new() { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
        if (page.Expander is { } footerExpander && expandedText is not null)
        {
            Button expanderButton = new() { Background = Brushes.Transparent, Name = "expanderButton" };
            void UpdateExpanderText()
                => expanderButton.Content = footerExpander.Expanded
                    ? $"▴  {footerExpander.ExpandedButtonText ?? strings.HideDetails.Text}"
                    : $"▾  {footerExpander.CollapsedButtonText ?? strings.ShowDetails.Text}";
            UpdateExpanderText();
            expanderButton.Click += (_, _) =>
            {
                footerExpander.Expanded = !footerExpander.Expanded;
                expandedText.IsVisible = footerExpander.Expanded;
                UpdateExpanderText();
            };
            footerLeft.Children.Add(expanderButton);
        }

        if (page.Verification is { } verification)
        {
            CheckBox checkBox = new() { Content = TranslatedText.ToAccessKeyText(verification.Text ?? ""), IsChecked = verification.Checked, Name = "verification" };
            checkBox.IsCheckedChanged += (_, _) => verification.Checked = checkBox.IsChecked == true;
            footerLeft.Children.Add(checkBox);
        }

        DockPanel.SetDock(footerLeft, Dock.Left);
        DockPanel.SetDock(footerButtons, Dock.Right);
        footerRow.Children.Add(footerLeft);
        footerRow.Children.Add(footerButtons);

        StackPanel footerContent = new() { Spacing = 8 };
        footerContent.Children.Add(footerRow);
        if (!string.IsNullOrEmpty(page.Footnote))
        {
            TextBlock footnote = DialogBoxParts.CreateText(page.Footnote, onLink);
            footnote.Name = "footnote";
            footerContent.Children.Add(footnote);
        }

        if (expandedText is not null && page.Expander?.Position == TaskDialogExpanderPosition.AfterFootnote)
        {
            footerContent.Children.Add(expandedText);
        }

        Border footer = new() { Child = footerContent };
        footer.Classes.Add("dialogFooter");
        DockPanel content = new();
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);
        content.Children.Add(top);
        Content = content;

        Opened += (_, _) => (DefaultButton ?? Buttons.FirstOrDefault(b => b.IsEnabled))?.Focus();
    }

    /// <summary>The buttons (command links first in their order, then the others), each with its <see cref="TaskDialogButton"/> as tag.</summary>
    public IReadOnlyList<Button> Buttons { get; }

    public Button? DefaultButton { get; }

    /// <summary>The button that closed the dialog.</summary>
    public TaskDialogButton? Result { get; private set; }

    protected override void OnEscapePressed()
    {
        if (_cancelResult is not null)
        {
            CloseWith(_cancelResult);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_closedByButton)
        {
            if (_cancelResult is not null)
            {
                Result = _cancelResult;
            }
            else
            {
                e.Cancel = true;
            }
        }

        base.OnClosing(e);
    }

    private static string? StandardText(TaskDialogButton button, DialogBoxStrings strings)
        => button == TaskDialogButton.OK ? strings.Ok.Text
            : button == TaskDialogButton.Cancel ? strings.Cancel.Text
            : button == TaskDialogButton.Abort ? strings.Abort.Text
            : button == TaskDialogButton.Retry ? strings.Retry.Text
            : button == TaskDialogButton.Ignore ? strings.Ignore.Text
            : button == TaskDialogButton.Yes ? strings.Yes.Text
            : button == TaskDialogButton.No ? strings.No.Text
            : button == TaskDialogButton.Close ? strings.Close.Text
            : button == TaskDialogButton.Help ? strings.Help.Text
            : button == TaskDialogButton.TryAgain ? strings.TryAgain.Text
            : button == TaskDialogButton.Continue ? strings.Continue.Text
            : null;

    private void OnButtonClicked(TaskDialogButton button)
    {
        // As TDN_BUTTON_CLICKED: the handlers of the button run, and it closes the dialog unless AllowCloseDialog is false.
        if (button.PerformClick())
        {
            CloseWith(button);
        }
    }

    private void CloseWith(TaskDialogButton button)
    {
        Result = button;
        _closedByButton = true;
        Close();
    }
}
