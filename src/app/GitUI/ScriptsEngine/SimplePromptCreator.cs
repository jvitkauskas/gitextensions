namespace GitUI.ScriptsEngine;

internal interface ISimplePromptCreator
{
    IUserInputPrompt Create(string? title, string? label, string? defaultValue);
}

internal sealed class SimplePromptCreator : ISimplePromptCreator
{
    public IUserInputPrompt Create(string? title, string? label, string? defaultValue)
    {
        return AvaloniaHosting.AvaloniaDialogs.CreateSimplePrompt(title, label, defaultValue) ?? new SimplePrompt(title, label, defaultValue);
    }
}
