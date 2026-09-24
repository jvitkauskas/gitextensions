using Avalonia.Data.Converters;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Avalonia.Controls.FileStatusList;

/// <summary>The converters of the file status list.</summary>
public static class FileStatusListConverters
{
    /// <summary>Whether git grep is searched as the parameter says (the radio items of <c>btnFindInFilesGitGrep</c>).</summary>
    public static IValueConverter IsGitGrepUsing { get; } = new FuncValueConverter<GitGrepUsing, GitGrepUsing, bool>((value, parameter) => value == parameter);

    /// <summary>Bold for <see langword="true"/> (the default item of the menu).</summary>
    public static IValueConverter Bold { get; } = new FuncValueConverter<bool, global::Avalonia.Media.FontWeight>(value => value ? global::Avalonia.Media.FontWeight.Bold : global::Avalonia.Media.FontWeight.Normal);
}
