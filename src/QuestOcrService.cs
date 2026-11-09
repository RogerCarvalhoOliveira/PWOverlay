using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PWQuestOverlay
{
    internal sealed class QuestOcrResult
    {
        internal string Text { get; set; }
        internal long ElapsedMilliseconds { get; set; }
        internal string DiagnosticImagePath { get; set; }
        internal int Width { get; set; }
        internal int Height { get; set; }
    }

    internal static class QuestOcrService
    {
        private const int UpscaleFactor = 2;

        internal static bool IsAvailable(out string reason)
        {
            try
            {
                string executable = FindTesseractExecutable();
                if (string.IsNullOrWhiteSpace(executable))
                {
                    reason = "Tesseract não encontrado ao lado do overlay nem em C:\\Program Files\\Tesseract-OCR.";
                    return false;
                }
                string trainedData = Path.Combine(
                    Path.GetDirectoryName(executable),
                    "tessdata",
                    "por.traineddata");
                if (!File.Exists(trainedData))
                {
                    reason = "O idioma português (por.traineddata) não foi encontrado em " + trainedData;
                    return false;
                }
                reason = string.Empty;
                return true;
            }
            catch (Exception error)
            {
                reason = error.Message;
                return false;
            }
        }

        internal static async Task<QuestOcrResult> ScanAsync(
            IntPtr gameWindow,
            Rectangle clientRegion,
            string diagnosticDirectory)
        {
            if (gameWindow == IntPtr.Zero || !NativeMethods.IsWindow(gameWindow))
                throw new InvalidOperationException("O cliente do Perfect World não está mais disponível.");

            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(gameWindow, out client))
                throw new InvalidOperationException("Não foi possível medir a janela do jogo.");

            Rectangle validRegion = Rectangle.Intersect(
                new Rectangle(0, 0, client.Width, client.Height),
                clientRegion);
            if (validRegion.Width < 20 || validRegion.Height < 20)
                throw new InvalidOperationException("A área selecionada para OCR é pequena demais.");

            var clientOrigin = new NativeMethods.POINT { X = 0, Y = 0 };
            if (!NativeMethods.ClientToScreen(gameWindow, ref clientOrigin))
                throw new InvalidOperationException("Não foi possível localizar a área do jogo na tela.");

            var stopwatch = Stopwatch.StartNew();
            using (Bitmap captured = CaptureScreenRegion(
                clientOrigin.X + validRegion.X,
                clientOrigin.Y + validRegion.Y,
                validRegion.Width,
                validRegion.Height))
            using (Bitmap prepared = PrepareForOcr(captured))
            {
                Directory.CreateDirectory(diagnosticDirectory);
                string imagePath = Path.Combine(diagnosticDirectory, "ocr-last.png");
                prepared.Save(imagePath, ImageFormat.Png);

                string text = await RecognizeAsync(imagePath);
                stopwatch.Stop();
                return new QuestOcrResult
                {
                    Text = text == null ? string.Empty : text.Trim(),
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    DiagnosticImagePath = imagePath,
                    Width = prepared.Width,
                    Height = prepared.Height
                };
            }
        }

        private static Bitmap CaptureScreenRegion(int x, int y, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private static Bitmap PrepareForOcr(Bitmap source)
        {
            var prepared = new Bitmap(
                source.Width * UpscaleFactor,
                source.Height * UpscaleFactor,
                PixelFormat.Format32bppArgb);
            prepared.SetResolution(96.0f * UpscaleFactor, 96.0f * UpscaleFactor);
            using (Graphics graphics = Graphics.FromImage(prepared))
            {
                graphics.Clear(Color.Black);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, prepared.Width, prepared.Height));
            }
            return prepared;
        }

        private static Task<string> RecognizeAsync(string imagePath)
        {
            return Task.Run(delegate
            {
                string executable = FindTesseractExecutable();
                if (string.IsNullOrWhiteSpace(executable))
                    throw new FileNotFoundException("Tesseract OCR não encontrado.");

                string tessdata = Path.Combine(Path.GetDirectoryName(executable), "tessdata");
                string arguments = string.Format(
                    "\"{0}\" stdout --tessdata-dir \"{1}\" -l por --psm 6",
                    imagePath,
                    tessdata);
                var startInfo = new ProcessStartInfo(executable, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(executable)
                };
                using (Process process = Process.Start(startInfo))
                {
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(15000))
                    {
                        try { process.Kill(); }
                        catch { }
                        throw new TimeoutException("O Tesseract excedeu 15 segundos.");
                    }
                    Task.WaitAll(new Task[] { outputTask, errorTask }, 2000);
                    string output = outputTask.IsCompleted ? outputTask.Result : string.Empty;
                    string error = errorTask.IsCompleted ? errorTask.Result : string.Empty;
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException(
                            "Tesseract retornou código " + process.ExitCode + ": " + error.Trim());
                    return output;
                }
            });
        }

        private static string FindTesseractExecutable()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ocr", "tesseract.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tesseract.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Tesseract-OCR", "tesseract.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Tesseract-OCR", "tesseract.exe")
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }
}
