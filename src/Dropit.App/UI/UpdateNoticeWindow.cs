using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Dropit.App.Shell;
using Dropit.Core;
using static Dropit.App.UI.Controls;
using static Dropit.Core.Loc;

namespace Dropit.App.UI;

/// <summary>
/// The window that tells the user a new version is there, and lets them start the install. A balloon from the
/// tray is easy to miss and cannot show progress; this window can do both, so the tray only announces and
/// this window does the work.
/// </summary>
internal sealed class UpdateNoticeWindow : Window
{
    private static UpdateNoticeWindow? _open;

    private readonly Updater _updater;
    private readonly UpdateInfo _update;
    private readonly TextBlock _status;
    private readonly TextBlock _headline;
    private readonly FrameworkElement _bar;
    private readonly Border _action;
    private readonly Border _later;

    public static void Show(Updater updater, UpdateInfo update)
    {
        if (_open is not null)
        {
            if (_open.WindowState == WindowState.Minimized)
                _open.WindowState = WindowState.Normal;
            _open.Show();
            _open.Activate();
            return;
        }
        _open = new UpdateNoticeWindow(updater, update);
        _open.Show();
        _open.Activate();
    }

    private UpdateNoticeWindow(Updater updater, UpdateInfo update)
    {
        _updater = updater;
        _update = update;

        Direction.Apply(this);
        Theme.Refresh();

        Title = T("Update", "تحديث");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = false;
        FontFamily = Theme.UiFont;
        Chrome.Apply(this);

        _headline = new TextBlock
        {
            Text = T($"Dropit {update.Version} is ready to install.", $"Dropit {update.Version} جاهز للتثبيت."),
            FontSize = Type(17),
            FontWeight = FontWeights.SemiBold,
            Foreground = Theme.Text,
            TextWrapping = TextWrapping.Wrap,
        };

        _status = new TextBlock
        {
            Text = T($"You have {Updater.Current}. The installer replaces this copy and keeps your settings.",
                     "لديك الإصدار {Updater.Current}. يحدّث المثبِّت هذه النسخة ويحتفظ بإعداداتك."),
            FontSize = Type(13),
            Foreground = Theme.Muted,
            TextWrapping = TextWrapping.Wrap,
        };

        var track = new Border { Height = 5, CornerRadius = new CornerRadius(3), Background = Theme.Track, Margin = new Thickness(0, 10, 0, 0) };
        var fill = new Border { Height = 5, CornerRadius = new CornerRadius(3), Background = Theme.AccentGradient, HorizontalAlignment = HorizontalAlignment.Left };
        track.Child = fill;
        _bar = track;
        _bar.Visibility = Visibility.Collapsed;

        _action = Button(T("Update now", "حدّث الآن"), () => _ = _updater.InstallAsync(_update), primary: true, glyph: "\uE896");
        _later = Button(T("Later", "لاحقًا"), () => Close());

        var buttons = Row(_action, _later);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;

        var body = new StackPanel { Margin = new Thickness(22, 18, 22, 18) };
        body.Children.Add(_headline);
        body.Children.Add(_status);
        body.Children.Add(_bar);
        body.Children.Add(buttons);

        var root = new Grid { Background = Theme.Page };
        root.Children.Add(body);
        Content = new Border
        {
            CornerRadius = new CornerRadius(Theme.Radius),
            Background = Brushes.Transparent,
            ClipToBounds = true,
            Child = root,
        };

        _updater.Changed += OnUpdaterChanged;
        Closed += (_, _) =>
        {
            _updater.Changed -= OnUpdaterChanged;
            if (ReferenceEquals(_open, this))
                _open = null;
        };
        Paint();
    }

    private void OnUpdaterChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(Paint);
            return;
        }
        Paint();
    }

    /// <summary>Shows the state of the update: waiting, downloading, installing, or over.</summary>
    private void Paint()
    {
        switch (_updater.Step)
        {
            case UpdateStep.Downloading:
                _bar.Visibility = Visibility.Visible;
                _status.Text = T("Downloading the installer…", "جارٍ تنزيل المثبِّت…");
                _action.IsEnabled = false;
                break;
            case UpdateStep.Installing:
                _bar.Visibility = Visibility.Collapsed;
                _status.Text = T("Installing…", "جارٍ التثبيت…");
                _action.IsEnabled = false;
                break;
            case UpdateStep.Failed:
                _bar.Visibility = Visibility.Collapsed;
                _status.Text = T("The update did not work: ", "لم ينجح التحديث: ") + (_updater.LastError ?? "");
                _action.IsEnabled = true;
                break;
            case UpdateStep.Cancelled:
                _bar.Visibility = Visibility.Collapsed;
                _status.Text = T("The update was cancelled.", "تم إلغاء التحديث.");
                _action.IsEnabled = true;
                break;
            default:
                _status.Text = T($"You have {Updater.Current}. The installer replaces this copy and keeps your settings.",
                                  $"لديك الإصدار {Updater.Current}. يحدّث المثبِّت هذه النسخة ويحتفظ بإعداداتك.");
                _action.IsEnabled = true;
                break;
        }
    }
}
