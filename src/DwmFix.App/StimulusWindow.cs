using System.Drawing.Drawing2D;
using DwmFix.Core;

namespace DwmFix.App;

internal sealed class StimulusWindow : Form
{
    private readonly System.Windows.Forms.Timer _renderTimer;
    private Screen _screen;
    private bool _boostMode;
    private int _hue;

    public StimulusWindow(Screen screen, AppSettings settings)
    {
        _screen = screen;
        _renderTimer = new System.Windows.Forms.Timer();
        _renderTimer.Tick += (_, _) => AdvanceFrame();

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0.01;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        UpdateFor(screen, settings);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= NativeMethods.WsExLayered
                | NativeMethods.WsExNoActivate
                | NativeMethods.WsExToolWindow
                | NativeMethods.WsExTransparent;
            return createParams;
        }
    }

    public void UpdateFor(Screen screen, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _screen = screen;
        _boostMode = settings.BoostMode;

        var size = settings.BoostMode ? new Size(420, 420) : new Size(220, 14);
        Size = size;
        Location = new Point(
            _screen.Bounds.Left + (_screen.Bounds.Width - size.Width) / 2,
            _screen.Bounds.Top + (_screen.Bounds.Height - size.Height) / 2);

        _renderTimer.Interval = Math.Max(1, 1000 / Math.Clamp(settings.RenderFps, AppSettings.MinRenderFps, AppSettings.MaxRenderFps));
        if (!_renderTimer.Enabled)
        {
            _renderTimer.Start();
        }

        if (Visible)
        {
            ReassertTopmost();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ReassertTopmost();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
        {
            return;
        }

        e.Graphics.CompositingMode = CompositingMode.SourceCopy;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

        if (_boostMode)
        {
            PaintBoostFrame(e.Graphics);
        }
        else
        {
            using var brush = new LinearGradientBrush(
                ClientRectangle,
                ColorFromHsv(_hue, saturation: 0.85, value: 1.0),
                ColorFromHsv((_hue + 180) % 360, saturation: 0.85, value: 1.0),
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmMouseActivate)
        {
            m.Result = NativeMethods.MaNoActivate;
            return;
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTimer.Stop();
            _renderTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AdvanceFrame()
    {
        _hue = (_hue + (_boostMode ? 7 : 3)) % 360;
        Invalidate(invalidateChildren: false);
    }

    private void PaintBoostFrame(Graphics graphics)
    {
        const int slices = 24;
        var center = new PointF(ClientRectangle.Width / 2f, ClientRectangle.Height / 2f);
        var radius = MathF.Max(ClientRectangle.Width, ClientRectangle.Height);

        for (var i = 0; i < slices; i++)
        {
            var hue = (_hue + i * (360 / slices)) % 360;
            using var brush = new SolidBrush(ColorFromHsv(hue, saturation: 0.9, value: 1.0));
            graphics.FillPie(
                brush,
                center.X - radius,
                center.Y - radius,
                radius * 2,
                radius * 2,
                i * (360f / slices),
                360f / slices + 1f);
        }
    }

    private void ReassertTopmost()
    {
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }

    private static Color ColorFromHsv(int hue, double saturation, double value)
    {
        var chroma = value * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60.0 % 2 - 1));
        var m = value - chroma;

        var (r, g, b) = hue switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x),
        };

        return Color.FromArgb(
            alpha: 255,
            red: (int)Math.Round((r + m) * 255),
            green: (int)Math.Round((g + m) * 255),
            blue: (int)Math.Round((b + m) * 255));
    }
}
