using Avalonia.Controls;
using Avalonia.Platform;

namespace Dropit.Linux.UI;

internal static class AppIcon
{
    public static WindowIcon? Window
    {
        get
        {
            try
            {
                return new WindowIcon(AssetLoader.Open(new Uri("avares://dropit/Assets/dropit.png")));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
