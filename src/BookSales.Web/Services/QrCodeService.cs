using QRCoder;

namespace BookSales.Web.Services;

public sealed class QrCodeService
{
    public string GenerateQrBase64(string content)
    {
        using var generator = new QRCodeGenerator();

        using var data = generator.CreateQrCode(
            content,
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(10);

        return Convert.ToBase64String(bytes);
    }
}