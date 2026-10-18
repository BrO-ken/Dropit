using System.Windows;
using Dropit.Core;

namespace Dropit.App.UI;

/// <summary>
/// Right-to-left plumbing for the Arabic UI. Every window in this app is built in code, so each
/// one sets its <see cref="FrameworkElement.FlowDirection"/> explicitly rather than relying on an
/// implicit style; <see cref="ApplyToOpenWindows"/> re-flows them when the language changes.
/// </summary>
internal static class Direction
{
    public static FlowDirection For(Language language) =>
        language == Language.Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public static FlowDirection Current => For(Loc.Current);

    /// <summary>Call at the top of a window constructor, before any content is added.</summary>
    public static void Apply(Window window) => window.FlowDirection = Current;

    /// <summary>Re-flows every open window after the user picks a different language.</summary>
    public static void ApplyToOpenWindows()
    {
        var app = Application.Current;
        if (app is null)
            return;
        foreach (Window window in app.Windows)
            window.FlowDirection = Current;
    }
}