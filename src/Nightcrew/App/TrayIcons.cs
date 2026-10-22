using System.Drawing;
using Nightcrew.App.Ui;
using Nightcrew.Core;

namespace Nightcrew.App;

/// <summary>
/// Tray icons drawn from <see cref="MarkGeometry"/> at the size Windows asks for, so they follow display scaling.
/// Each one is made once per state, size and taskbar theme.
/// </summary>
public sealed class TrayIcons : IDisposable
{
    private readonly Dictionary<(MarkState, int, bool), Icon> _icons = [];

    /// <summary>UI thread only, because the marks are drawn with WPF.</summary>
    public Icon Get(MarkState state, int pixels, bool lightTaskbar)
    {
        var key = (state, pixels, lightTaskbar);
        if (_icons.TryGetValue(key, out var icon)) return icon;

        var ink = lightTaskbar ? Theme.Night : Theme.Moon;
        var bgra = MarkGeometry.ToBgra(MarkGeometry.Render(state, pixels, ink));
        var ico = IcoWriter.Write([IcoWriter.Frame.Bitmap(pixels, bgra)]);
        icon = new Icon(new MemoryStream(ico), pixels, pixels); // owns its handle, so Dispose frees it
        _icons[key] = icon;
        return icon;
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon.Dispose();
        _icons.Clear();
    }
}
