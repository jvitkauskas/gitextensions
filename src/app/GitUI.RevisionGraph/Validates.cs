using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GitUI.UserControls.RevisionGrid.Graph;

/// <summary>
///  The <c>Microsoft.Validates</c> of GitUI for the graph sources compiled here. It is declared in their namespace, so that
///  it is found before GitUI's own (linked) <c>Microsoft.Validates</c>, which it would conflict with through
///  <c>InternalsVisibleTo</c>.
/// </summary>
internal static class Validates
{
    [DebuggerStepThrough]
    public static void NotNull<T>([NotNull] T? value)
        where T : class
    {
        if (value == null)
        {
            Fail("Value must not be null.");
        }
    }

    [DoesNotReturn]
    private static void Fail(string message) => throw new(message);
}
