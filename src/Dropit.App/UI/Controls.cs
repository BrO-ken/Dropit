using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Dropit.Core;

namespace Dropit.App.UI;

/// <summary>
/// Small Windows 11–style building blocks drawn in code (cards, segmented choices, toggles,
/// sliders, pill buttons), so they look the same everywhere without depending on a theme assembly.
/// </summary>
internal static class Controls
{
    /// <summary>
    /// Arabic reads noticeably smaller than latin at the same point size: the script sits lower on the
    /// line and its ascenders and dots are shorter. Nudging the size up is what makes the two
    /// languages feel like the same interface rather than the Arabic one looking like a zoom-out.
    /// </summary>
    public static double Type(double size) => Loc.IsRtl ? Math.Round(size * 1.09, 1) : size;

    /// <summary>
    /// A horizontal row that reads in the interface's direction. WPF lays a horizontal StackPanel out
    /// left to right whatever the FlowDirection, so in Arabic the children have to be reversed by
    /// hand - otherwise the options read backwards: "Windows" would sit on the far left instead of
    /// being the first thing read on the right.
    /// </summary>
    public static StackPanel Row(params UIElement[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var ordered = Loc.IsRtl ? children.Reverse().ToArray() : children;
        foreach (var child in ordered)
            panel.Children.Add(child);
        return panel;
    }

    /// <summary>
    /// Same direction rule as <see cref="Row"/>, but the items wrap to a second line instead of
    /// running off the edge. Five keyboard shortcuts do not fit next to a title on a narrow window.
    /// </summary>
    public static WrapPanel WrappingRow(params UIElement[] children)
    {
        // WPF mirrors a WrapPanel under RTL but not a StackPanel, so without pinning the direction the
        // reading order would depend on which helper happened to build the row. Pin it and let the
        // explicit child order be the single source of truth.
        var panel = new WrapPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };
        var ordered = Loc.IsRtl ? children.Reverse().ToArray() : children;
        foreach (var child in ordered)
            panel.Children.Add(child);
        return panel;
    }

    public static TextBlock Text(string text, double size = 14, FontWeight? weight = null, Brush? brush = null, bool wrap = true)
    {
        size = Type(size);
        return new TextBlock
        {
            Text = text,
            FontSize = size,
            // Arabic puts dots under the baseline; 1.0 leading collides them with the next line.
            LineHeight = size * Theme.BodyLineHeight,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            FontWeight = weight ?? FontWeights.Normal,
            Foreground = brush ?? Theme.Text,
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = wrap ? TextTrimming.None : TextTrimming.CharacterEllipsis,
        };
    }

    public static TextBlock SectionHeader(string text) => new()
    {
        Text = text,
        FontFamily = Theme.DisplayFont,
        FontSize = Type(15),
        LineHeight = Type(15) * Theme.BodyLineHeight,
        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
        FontWeight = FontWeights.SemiBold,
        Foreground = Theme.Muted,
        Margin = new Thickness(2, 26, 0, 8),
    };

    /// <summary>A rounded card with a title/description on the leading edge and a control on the trailing one.</summary>
    public static Border Card(string title, string? description, UIElement? control, UIElement? below = null, string? glyph = null)
    {
        // Dock the sides explicitly instead of leaning on DockPanel's implicit mirroring, so the
        // icon always sits on the reading edge: left in English, right in Arabic.
        var iconSide = Loc.IsRtl ? Dock.Right : Dock.Left;
        var controlSide = Loc.IsRtl ? Dock.Left : Dock.Right;
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(Text(title, 14, FontWeights.Normal));
        if (!string.IsNullOrEmpty(description))
        {
            var d = Text(description, 12, brush: Theme.Muted);
            d.Margin = new Thickness(0, 2, 0, 0);
            left.Children.Add(d);
        }

        var row = new DockPanel { LastChildFill = true };
        // The gap between the label and the control belongs on the label, on whichever side the control
        // is docked. Putting it on the control alone leaves them touching in Arabic, where the control
        // sits on the left and long Arabic labels run right up against it.
        const double Gap = 20;
        if (glyph is not null)
        {
            var icon = new TextBlock
            {
                Text = glyph,
                FontFamily = Theme.Icons,
                FontSize = 18,
                Foreground = Theme.Text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0),
            };
            DockPanel.SetDock(icon, iconSide);
            row.Children.Add(icon);
        }
        if (control is FrameworkElement fe)
        {
            fe.VerticalAlignment = VerticalAlignment.Center;
            fe.Margin = new Thickness(0);
            DockPanel.SetDock(fe, controlSide);
            row.Children.Add(fe);
        }
        left.Margin = Loc.IsRtl ? new Thickness(Gap, 0, 0, 0) : new Thickness(0, 0, Gap, 0);
        row.Children.Add(left);

        var content = new StackPanel();
        content.Children.Add(row);
        if (below is not null)
            content.Children.Add(below);

        // Depth, not a highlight line: a 1px rim on the top edge reads as a stray rule once the panel
        // is taller than a couple of lines, which is worse than no effect at all.
        return new Border
        {
            Background = Theme.Card,
            BorderBrush = Theme.CardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 14, 18, 14),
            Margin = new Thickness(0, 0, 0, 4),
            SnapsToDevicePixels = true,
            Effect = Theme.Shadow(14),
            Child = content,
        };
    }

    /// <summary>A row of mutually exclusive pills; the active one is filled with the accent gradient.</summary>
    public static Border Segmented<T>(IReadOnlyList<(string Label, T Value)> options, T selected, Action<T> changed)
    {
        var pills = new List<(Border Pill, TextBlock Text, T Value)>();
        void Select(T value)
        {
            foreach (var (pill, text, v) in pills)
            {
                var on = EqualityComparer<T>.Default.Equals(v, value);
                pill.Background = on ? Theme.AccentGradient : Brushes.Transparent;
                text.Foreground = on ? Theme.TextOnAccent : Theme.Text;
                text.FontWeight = on ? FontWeights.SemiBold : FontWeights.Normal;
            }
        }
        foreach (var (label, value) in options)
        {
            var text = new TextBlock { Text = label, FontSize = Type(12.5), VerticalAlignment = VerticalAlignment.Center };
            var pill = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 5, 10, 6),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = text,
            };
            var v = value;
            pill.MouseLeftButtonUp += (_, _) =>
            {
                Select(v);
                changed(v);
            };
            pills.Add((pill, text, value));
        }
        Select(selected);
        return new Border
        {
            Background = Theme.Track,
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(2),
            Child = WrappingRow([.. pills.Select(p => (UIElement)p.Pill)]),
        };
    }

    /// <summary>Windows 11 toggle switch; in Arabic the knob travels right to left.</summary>
    public static FrameworkElement Toggle(bool value, Action<bool> changed)
    {
        var rtl = Loc.IsRtl;
        var knob = new Ellipse { Width = 12, Height = 12, HorizontalAlignment = rtl ? HorizontalAlignment.Right : HorizontalAlignment.Left };
        var track = new Border
        {
            Width = 40,
            Height = 20,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = knob,
        };
        var translate = new TranslateTransform();
        knob.RenderTransform = translate;
        var on = value;
        void Paint(bool animate)
        {
            track.Background = on ? Theme.AccentGradient : Brushes.Transparent;
            track.BorderBrush = on ? Brushes.Transparent : Theme.Muted;
            knob.Fill = on ? Brushes.White : Theme.Muted;
            // Right-aligned knob already sits at the far end, so travel is negative.
            var x = rtl
                ? (on ? -23.0 : -3.0)
                : (on ? 23.0 : 3.0);
            if (animate)
                translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, TimeSpan.FromMilliseconds(120)) { EasingFunction = new CubicEase() });
            else
                translate.X = x;
        }
        track.MouseLeftButtonUp += (_, _) =>
        {
            on = !on;
            Paint(true);
            changed(on);
        };
        Paint(false);
        return track;
    }

    /// <summary>A slim slider with an accent-filled track and a round thumb; fills from the right in Arabic.</summary>
    public static FrameworkElement Slider(int min, int max, int value, Action<int> changed, Action committed)
    {
        const double width = 200;
        var rtl = Loc.IsRtl;
        var label = new TextBlock { Width = 40, FontSize = 13, Foreground = Theme.Text, TextAlignment = rtl ? TextAlignment.Left : TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var fill = new Border { Height = 4, CornerRadius = new CornerRadius(2), Background = Theme.AccentGradient, HorizontalAlignment = rtl ? HorizontalAlignment.Right : HorizontalAlignment.Left };
        var thumb = new Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = Brushes.White,
            Stroke = Theme.PanelBorder,
            StrokeThickness = 1,
            HorizontalAlignment = rtl ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Effect = Theme.Shadow(6),
        };
        var surface = new Grid { Width = width, Height = 24, Background = Brushes.Transparent, Cursor = Cursors.Hand };
        surface.Children.Add(new Border { Height = 4, CornerRadius = new CornerRadius(2), Background = Theme.Track });
        surface.Children.Add(fill);
        surface.Children.Add(thumb);

        var current = value;
        void Show()
        {
            var t = (current - min) / (double)(max - min);
            fill.Width = Math.Max(0, t * width);
            var travel = t * (width - 18);
            thumb.Margin = rtl ? new Thickness(0, 0, travel, 0) : new Thickness(travel, 0, 0, 0);
            label.Text = current.ToString();
        }
        void SetFrom(Point p)
        {
            var raw = (p.X - 9) / (width - 18);
            var t = Math.Clamp(rtl ? 1 - raw : raw, 0, 1);
            var v = (int)Math.Round(min + t * (max - min));
            if (v == current)
                return;
            current = v;
            Show();
            changed(v);
        }
        surface.MouseLeftButtonDown += (_, e) =>
        {
            surface.CaptureMouse();
            SetFrom(e.GetPosition(surface));
        };
        surface.MouseMove += (_, e) =>
        {
            if (surface.IsMouseCaptured)
                SetFrom(e.GetPosition(surface));
        };
        surface.MouseLeftButtonUp += (_, _) =>
        {
            surface.ReleaseMouseCapture();
            committed();
        };
        Show();

        return Row(surface, label);
    }

    /// <summary>A rounded button; <paramref name="primary"/> ones use the accent gradient.</summary>
    public static Border Button(string text, Action click, bool primary = false, string? glyph = null)
    {
        var content = Row();
        content.HorizontalAlignment = HorizontalAlignment.Center;
        var foreground = primary ? Theme.TextOnAccent : Theme.Text;
        if (glyph is not null)
        {
            content.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = Theme.Icons,
                FontSize = 12,
                Foreground = foreground,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 7, 0),
            });
        }
        content.Children.Add(new TextBlock { Text = text, FontSize = Type(13), Foreground = foreground, FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center });
        var normal = primary ? Theme.AccentGradient : Theme.Card;
        var button = new Border
        {
            Background = normal,
            BorderBrush = primary ? Brushes.Transparent : Theme.PanelBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 6, 14, 7),
            Cursor = Cursors.Hand,
            Child = content,
            HorizontalAlignment = Loc.IsRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        };
        button.MouseEnter += (_, _) => button.Opacity = 0.88;
        button.MouseLeave += (_, _) => button.Opacity = 1;
        button.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            click();
        };
        return button;
    }

    /// <summary>A small coloured status label, e.g. "found" / "missing".</summary>
    public static Border Badge(string text, bool good) => new()
    {
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(8, 2, 8, 3),
        Background = new SolidColorBrush(Color.FromArgb(0x26, good ? (byte)0x3C : (byte)0xF0, good ? (byte)0xC3 : (byte)0x5A, good ? (byte)0x6E : (byte)0x5A)),
        Child = new TextBlock { Text = text, FontSize = Type(11.5), FontWeight = FontWeights.SemiBold, Foreground = good ? Theme.Success : Theme.Error },
    };
}
