using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookCollectionApp
{
    public class QRCodeGeneratorHelper
    {
        public static Bitmap GenerateQRCode(string url, int pixelsPerModule = 4)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(pixelsPerModule); // Размер каждого модуля QR-кода
                }
            }
        }

        public string GenerateSearchUrl(Book selectedBook)
        {
            if (selectedBook == null)
                throw new ArgumentNullException(nameof(selectedBook));

            // Создаем URL для поиска
            string baseUrl = "https://www.google.com/search";
            string query = $"q=Название книги: {Uri.EscapeDataString(selectedBook.Title)}+ Автор: {Uri.EscapeDataString(selectedBook.Author)}+ Год издания: {selectedBook.Year}";
            return $"{baseUrl}?{query}";
        }
    }
}
