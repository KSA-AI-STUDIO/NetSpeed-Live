using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QRCoder;

namespace NetPulseOverlay.Views;

/// <summary>
/// UPI donation dialog. The QR code is generated locally (QRCoder, offline —
/// no network use and no external service) from a properly URL-encoded UPI
/// deep-link payload. Donations are optional and processed through UPI, not
/// through Microsoft.
/// </summary>
public partial class DonationWindow : Window
{
    private const string UpiId = "ashutosh.praharaj1@ybl";
    private const string PayeeName = "KSA AI STUDIO";
    private const string PaymentNote = "Support NetSpeed Live";

    private DispatcherTimer? _feedbackTimer;

    public DonationWindow()
    {
        InitializeComponent();
        UpiIdText.Text = UpiId;
        CopyButton.Click += (_, _) => CopyUpiId();
        Loaded += (_, _) => RenderQrCode();
    }

    /// <summary>Builds the UPI deep-link payload with proper URL encoding.</summary>
    public static string BuildUpiUri()
    {
        return "upi://pay?pa=" + Uri.EscapeDataString(UpiId)
             + "&pn=" + Uri.EscapeDataString(PayeeName)
             + "&tn=" + Uri.EscapeDataString(PaymentNote);
    }

    /// <summary>
    /// Renders the QR code locally. Generated once per dialog open (never per
    /// network tick) and frozen for cheap rendering.
    /// </summary>
    private void RenderQrCode()
    {
        try
        {
            using QRCodeGenerator generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(BuildUpiUri(), QRCodeGenerator.ECCLevel.M);
            using PngByteQRCode qr = new PngByteQRCode(data);
            byte[] png = qr.GetGraphic(pixelsPerModule: 6);

            BitmapImage image = new BitmapImage();
            using MemoryStream stream = new MemoryStream(png);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            QrImage.Source = image;
        }
        catch (Exception)
        {
            // QR is informational only; a failure must never crash the app.
        }
    }

    private void CopyUpiId()
    {
        try
        {
            System.Windows.Clipboard.SetText(UpiId);
        }
        catch (Exception)
        {
            return; // clipboard can be locked by another process; ignore
        }

        CopyButton.Content = "Copied!";
        CopyButton.IsEnabled = false;
        _feedbackTimer?.Stop();
        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer!.Stop();
            CopyButton.Content = "Copy UPI ID";
            CopyButton.IsEnabled = true;
        };
        _feedbackTimer.Start();
    }
}