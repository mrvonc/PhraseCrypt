using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhraseCryptApp
{
    /// <summary>
    /// Simple LSB (least significant bit) steganography: hides text in the lowest
    /// bits of a carrier image's RGB channels, leaving the alpha channel untouched.
    /// Output is always written losslessly as PNG.
    /// Payload layout: [4-byte length, big-endian] + [UTF-8 text]
    /// </summary>
    public static class SteganographyUtility
    {
        public static void HideText(string carrierImagePath, string outputImagePath, string secretText)
        {
            if (string.IsNullOrEmpty(secretText))
            {
                throw new ArgumentException(Localization.T("ErrorNoSecretText"));
            }

            BitmapImage source = LoadBitmap(carrierImagePath);
            FormatConvertedBitmap converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            List<int> usableIndices = GetUsableByteIndices(pixels.Length);

            byte[] secretBytes = Encoding.UTF8.GetBytes(secretText);
            byte[] lengthPrefix = BitConverter.GetBytes(secretBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthPrefix); // store big-endian inside the image
            }

            byte[] payload = new byte[lengthPrefix.Length + secretBytes.Length];
            Buffer.BlockCopy(lengthPrefix, 0, payload, 0, lengthPrefix.Length);
            Buffer.BlockCopy(secretBytes, 0, payload, lengthPrefix.Length, secretBytes.Length);

            long neededBits = (long)payload.Length * 8;
            if (neededBits > usableIndices.Count)
            {
                double neededKb = neededBits / 8.0 / 1024.0;
                throw new InvalidOperationException(Localization.T("ErrorImageTooSmall", neededKb));
            }

            for (int bitIndex = 0; bitIndex < neededBits; bitIndex++)
            {
                int byteIndex = bitIndex / 8;
                int bitInByte = 7 - (bitIndex % 8);
                int bit = (payload[byteIndex] >> bitInByte) & 1;

                int pixelByteIndex = usableIndices[bitIndex];
                pixels[pixelByteIndex] = (byte)((pixels[pixelByteIndex] & 0xFE) | bit);
            }

            WriteableBitmap output = new WriteableBitmap(width, height, converted.DpiX, converted.DpiY, PixelFormats.Bgra32, null);
            output.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            using FileStream fs = new FileStream(outputImagePath, FileMode.Create);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(output));
            encoder.Save(fs);
        }

        public static string ExtractText(string imagePath)
        {
            BitmapImage source = LoadBitmap(imagePath);
            FormatConvertedBitmap converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            List<int> usableIndices = GetUsableByteIndices(pixels.Length);

            if (usableIndices.Count < 32)
            {
                throw new InvalidDataException(Localization.T("ErrorImageTooSmallForData"));
            }

            byte[] lengthBytes = ExtractBits(pixels, usableIndices, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            int payloadLength = BitConverter.ToInt32(lengthBytes, 0);

            long maxPossibleBytes = (usableIndices.Count - 32) / 8;
            if (payloadLength <= 0 || payloadLength > maxPossibleBytes)
            {
                throw new InvalidDataException(Localization.T("ErrorNoValidPackageFound"));
            }

            byte[] textBytes = ExtractBits(pixels, usableIndices, 32, payloadLength);
            return Encoding.UTF8.GetString(textBytes);
        }

        private static byte[] ExtractBits(byte[] pixels, List<int> usableIndices, int startBit, int byteCount)
        {
            byte[] output = new byte[byteCount];
            for (int bitIndex = 0; bitIndex < byteCount * 8; bitIndex++)
            {
                int pixelByteIndex = usableIndices[startBit + bitIndex];
                int bit = pixels[pixelByteIndex] & 1;

                int outByteIndex = bitIndex / 8;
                int bitInByte = 7 - (bitIndex % 8);
                output[outByteIndex] |= (byte)(bit << bitInByte);
            }
            return output;
        }

        /// <summary>
        /// Returns every byte index of the pixel array usable for LSB embedding
        /// (R, G and B per pixel), skipping the alpha byte (every 4th byte).
        /// </summary>
        private static List<int> GetUsableByteIndices(int pixelArrayLength)
        {
            List<int> indices = new List<int>(pixelArrayLength / 4 * 3);
            for (int i = 0; i < pixelArrayLength; i++)
            {
                if ((i + 1) % 4 != 0)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        private static BitmapImage LoadBitmap(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
