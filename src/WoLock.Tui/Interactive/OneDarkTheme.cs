using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using WoLock.Tui;

namespace WoLock.Tui.Interactive;

/// <summary>
/// OneDark Pro inspired color theme for the interactive TUI.
/// </summary>
/// <remarks>
/// <para>
/// The palette is based on the official OneDark Pro Atom theme
/// (<c>https://github.com/Binaryify/OneDark-Pro</c>). The reference RGB values
/// and their mapping to the 16 console colors that Terminal.Gui can render:
/// </para>
/// <table>
/// <tr><th>Role</th><th>OneDark Pro</th><th>Terminal.Gui</th></tr>
/// <tr><td>Background</td><td>#282c34</td><td><c>Color.Black</c></td></tr>
/// <tr><td>Foreground</td><td>#abb2bf</td><td><c>Color.Gray</c></td></tr>
/// <tr><td>Comment</td><td>#5c6370</td><td><c>Color.DarkGray</c></td></tr>
/// <tr><td>Blue</td><td>#61afef</td><td><c>Color.Blue</c></td></tr>
/// <tr><td>Green</td><td>#8cc265</td><td><c>Color.Green</c></td></tr>
/// <tr><td>Yellow</td><td>#d18f52</td><td><c>Color.Brown</c> (maps to the yellow console color)</td></tr>
/// <tr><td>Red</td><td>#e05561</td><td><c>Color.Red</c></td></tr>
/// <tr><td>Cyan</td><td>#42b3c2</td><td><c>Color.Cyan</c></td></tr>
/// <tr><td>Magenta</td><td>#c162de</td><td><c>Color.Magenta</c></td></tr>
/// </table>
/// <para>
/// Terminal.Gui 1.17 (<c>CursesDriver</c>) has no truecolor support, so the
/// palette is intentionally limited to these 16 console colors. Any other RGB
/// value would be rejected when the driver builds an <see cref="Terminal.Gui.Attribute"/>.
/// </para>
/// </remarks>
public static class OneDarkTheme
{
    /// <summary>The palette used throughout the TUI.</summary>
    public static class Colors
    {
        /// <summary>Background and base surface (#282c34).</summary>
        public const Color Background = Color.Black;

        /// <summary>Primary text (#abb2bf).</summary>
        public const Color Foreground = Color.Gray;

        /// <summary>Dim text, comments, unselected list rows (#5c6370).</summary>
        public const Color Comment = Color.DarkGray;

        /// <summary>Blue accent, used for titles and the wake action (#61afef).</summary>
        public const Color Blue = Color.Blue;

        /// <summary>Green, used for success (#8cc265).</summary>
        public const Color Green = Color.Green;

        /// <summary>Yellow, used for informational status (#d18f52). Terminal.Gui exposes this as <see cref="Color.Brown"/>.</summary>
        public const Color Yellow = Color.Brown;

        /// <summary>Red, used for errors (#e05561).</summary>
        public const Color Red = Color.Red;

        /// <summary>Cyan, used for secondary details (#42b3c2).</summary>
        public const Color Cyan = Color.Cyan;

        /// <summary>Magenta, used for highlights (#c162de).</summary>
        public const Color Magenta = Color.Magenta;
    }

    private static readonly ColorScheme _windowScheme = WithForeground(Colors.Foreground, Colors.Background);
    private static readonly ColorScheme _listScheme = new()
    {
        Normal = Attribute.Make(Colors.Comment, Colors.Background),
        Focus = Attribute.Make(Colors.Foreground, Colors.Background),
        HotNormal = Attribute.Make(Colors.Foreground, Colors.Background),
        HotFocus = Attribute.Make(Colors.Foreground, Colors.Background),
        Disabled = Attribute.Make(Colors.Comment, Colors.Background),
    };
    private static readonly ColorScheme _buttonScheme = WithForeground(Colors.Foreground, Colors.Blue);
    private static readonly ColorScheme _titleScheme = WithForeground(Colors.Blue, Colors.Background);
    private static readonly ColorScheme _detailScheme = WithForeground(Colors.Cyan, Colors.Background);

    /// <summary>
    /// Color scheme for the root window and general labels.
    /// </summary>
    public static ColorScheme Window => _windowScheme;

    /// <summary>
    /// Color scheme for titles and headers.
    /// </summary>
    public static ColorScheme Title => _titleScheme;

    /// <summary>
    /// Color scheme for secondary detail lines (for example a MAC address).
    /// </summary>
    public static ColorScheme Detail => _detailScheme;

    /// <summary>
    /// Color scheme for the device list. The selected row is drawn brighter than
    /// the unselected rows.
    /// </summary>
    public static ColorScheme List => _listScheme;

    /// <summary>
    /// Color scheme for the primary action button.
    /// </summary>
    public static ColorScheme Button => _buttonScheme;

    /// <summary>
    /// Returns a color scheme whose foreground reflects the given wake outcome,
    /// so the status line can be colored at a glance.
    /// </summary>
    /// <param name="status">The wake outcome to represent.</param>
    public static ColorScheme StatusColor(WakeStatus status) => status switch
    {
        WakeStatus.Responding => WithForeground(Colors.Green, Colors.Background),
        WakeStatus.PacketSent => WithForeground(Colors.Blue, Colors.Background),
        WakeStatus.NoResponse => WithForeground(Colors.Yellow, Colors.Background),
        WakeStatus.Failed => WithForeground(Colors.Red, Colors.Background),
        _ => Window,
    };

    private static ColorScheme WithForeground(Color foreground, Color background) => new()
    {
        Normal = Attribute.Make(foreground, background),
        Focus = Attribute.Make(foreground, background),
        HotNormal = Attribute.Make(foreground, background),
        HotFocus = Attribute.Make(foreground, background),
        Disabled = Attribute.Make(Colors.Comment, background),
    };
}
