using Nightcrew.App;
using Nightcrew.App.Ui;
using Nightcrew.Core;
using Xunit;

namespace Nightcrew.Tests;

public sealed class TrayIconsTests
{
    [Fact]
    public void Tray_icons_are_made_once_per_state_size_and_theme()
    {
        Sta.Run(() =>
        {
            using var icons = new TrayIcons();
            var idle = icons.Get(MarkState.Idle, 16, lightTaskbar: false);
            Assert.Same(idle, icons.Get(MarkState.Idle, 16, lightTaskbar: false));
            Assert.NotSame(idle, icons.Get(MarkState.Live, 16, lightTaskbar: false));
            Assert.NotSame(idle, icons.Get(MarkState.Idle, 16, lightTaskbar: true));
            Assert.Equal(16, idle.Width);
            Assert.Equal(20, icons.Get(MarkState.Idle, 20, lightTaskbar: false).Width);
        });
    }
}
