using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PWQuestOverlay
{
    internal sealed class QuestOcrResult
    {
        internal string Text { get; set; }
        internal string RawText { get; set; }
        internal string SourceText { get; set; }
        internal long ElapsedMilliseconds { get; set; }
        internal string DiagnosticImagePath { get; set; }
        internal int Width { get; set; }
        internal int Height { get; set; }
        internal int CaptureFrameCount { get; set; }
        internal int CaptureIntervalMilliseconds { get; set; }
        internal bool StateConfirmed { get; set; }
        internal int StabilityCount { get; set; }
    }

    internal static class QuestOcrService
    {
        private const int UpscaleFactor = 2;
        private const int TemporalFrameCount = 3;
        private const int TemporalFrameIntervalMilliseconds = 80;

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
            Bitmap[] frames = new Bitmap[TemporalFrameCount];
            try
            {
                for (int index = 0; index < TemporalFrameCount; index++)
                {
                    frames[index] = CaptureScreenRegion(
                        clientOrigin.X + validRegion.X,
                        clientOrigin.Y + validRegion.Y,
                        validRegion.Width,
                        validRegion.Height);
                    if (index + 1 < TemporalFrameCount)
                        await Task.Delay(TemporalFrameIntervalMilliseconds);
                }

                using (Bitmap captured = CreateTemporalMedian(frames))
                using (Bitmap prepared = PrepareForOcr(frames))
                {
                    Directory.CreateDirectory(diagnosticDirectory);
                    string sourceImagePath = Path.Combine(diagnosticDirectory, "ocr-last-source.png");
                    string imagePath = Path.Combine(diagnosticDirectory, "ocr-last.png");
                    captured.Save(sourceImagePath, ImageFormat.Png);
                    prepared.Save(imagePath, ImageFormat.Png);

                    Task<string> filteredTask = RecognizeAsync(imagePath, 6);
                    Task<string> sourceTask = RecognizeAsync(sourceImagePath, 11);
                    await Task.WhenAll(filteredTask, sourceTask);
                    string rawText = filteredTask.Result == null ? string.Empty : filteredTask.Result.Trim();
                    string sourceText = sourceTask.Result == null ? string.Empty : sourceTask.Result.Trim();
                    string text = MergeReliableCounters(rawText, sourceText);
                    text = MergeNumberedQuestTitles(text, sourceText);
                    File.WriteAllText(
                        Path.Combine(diagnosticDirectory, "ocr-last-filtered.txt"),
                        rawText,
                        new UTF8Encoding(false));
                    File.WriteAllText(
                        Path.Combine(diagnosticDirectory, "ocr-last-source.txt"),
                        sourceText,
                        new UTF8Encoding(false));
                    stopwatch.Stop();
                    return new QuestOcrResult
                    {
                        Text = text,
                        RawText = rawText,
                        SourceText = sourceText,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                        DiagnosticImagePath = imagePath,
                        Width = prepared.Width,
                        Height = prepared.Height,
                        CaptureFrameCount = TemporalFrameCount,
                        CaptureIntervalMilliseconds = TemporalFrameIntervalMilliseconds
                    };
                }
            }
            finally
            {
                foreach (Bitmap frame in frames)
                    if (frame != null) frame.Dispose();
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

        private static Bitmap PrepareForOcr(Bitmap[] frames)
        {
            Bitmap[] masks = frames.Select(CreateQuestTextMask).ToArray();
            try
            {
                using (Bitmap stableMask = CreateStableTextMask(masks))
                {
                    var prepared = new Bitmap(
                        stableMask.Width * UpscaleFactor,
                        stableMask.Height * UpscaleFactor,
                        PixelFormat.Format32bppArgb);
                    prepared.SetResolution(96.0f * UpscaleFactor, 96.0f * UpscaleFactor);
                    using (Graphics graphics = Graphics.FromImage(prepared))
                    {
                        graphics.Clear(Color.White);
                        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                        graphics.PixelOffsetMode = PixelOffsetMode.Half;
                        graphics.DrawImage(stableMask,
                            new Rectangle(0, 0, prepared.Width, prepared.Height));
                    }
                    return prepared;
                }
            }
            finally
            {
                foreach (Bitmap mask in masks) mask.Dispose();
            }
        }

        private static Bitmap CreateTemporalMedian(Bitmap[] frames)
        {
            if (frames == null || frames.Length != TemporalFrameCount)
                throw new ArgumentException("São necessários três quadros para a composição temporal.");
            int width = frames[0].Width;
            int height = frames[0].Height;
            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Rectangle bounds = new Rectangle(0, 0, width, height);
            BitmapData[] inputs = frames.Select(frame => frame.LockBits(
                bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)).ToArray();
            BitmapData output = result.LockBits(
                bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = width * 4;
                byte[][] rows = { new byte[rowBytes], new byte[rowBytes], new byte[rowBytes] };
                byte[] outputRow = new byte[rowBytes];
                for (int y = 0; y < height; y++)
                {
                    for (int frame = 0; frame < TemporalFrameCount; frame++)
                        Marshal.Copy(IntPtr.Add(inputs[frame].Scan0, y * inputs[frame].Stride),
                            rows[frame], 0, rowBytes);
                    for (int index = 0; index < rowBytes; index += 4)
                    {
                        outputRow[index] = Median(rows[0][index], rows[1][index], rows[2][index]);
                        outputRow[index + 1] = Median(rows[0][index + 1], rows[1][index + 1], rows[2][index + 1]);
                        outputRow[index + 2] = Median(rows[0][index + 2], rows[1][index + 2], rows[2][index + 2]);
                        outputRow[index + 3] = 255;
                    }
                    Marshal.Copy(outputRow, 0, IntPtr.Add(output.Scan0, y * output.Stride), rowBytes);
                }
            }
            finally
            {
                for (int index = 0; index < TemporalFrameCount; index++)
                    frames[index].UnlockBits(inputs[index]);
                result.UnlockBits(output);
            }
            return result;
        }

        private static Bitmap CreateStableTextMask(Bitmap[] masks)
        {
            int width = masks[0].Width;
            int height = masks[0].Height;
            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Rectangle bounds = new Rectangle(0, 0, width, height);
            BitmapData[] inputs = masks.Select(mask => mask.LockBits(
                bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)).ToArray();
            BitmapData output = result.LockBits(
                bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = width * 4;
                byte[][] rows = { new byte[rowBytes], new byte[rowBytes], new byte[rowBytes] };
                byte[] outputRow = new byte[rowBytes];
                for (int y = 0; y < height; y++)
                {
                    for (int frame = 0; frame < TemporalFrameCount; frame++)
                        Marshal.Copy(IntPtr.Add(inputs[frame].Scan0, y * inputs[frame].Stride),
                            rows[frame], 0, rowBytes);
                    for (int index = 0; index < rowBytes; index += 4)
                    {
                        int stableVotes = (rows[0][index] < 128 ? 1 : 0) +
                            (rows[1][index] < 128 ? 1 : 0) +
                            (rows[2][index] < 128 ? 1 : 0);
                        byte value = stableVotes >= 2 ? (byte)0 : (byte)255;
                        outputRow[index] = value;
                        outputRow[index + 1] = value;
                        outputRow[index + 2] = value;
                        outputRow[index + 3] = 255;
                    }
                    Marshal.Copy(outputRow, 0, IntPtr.Add(output.Scan0, y * output.Stride), rowBytes);
                }
            }
            finally
            {
                for (int index = 0; index < TemporalFrameCount; index++)
                    masks[index].UnlockBits(inputs[index]);
                result.UnlockBits(output);
            }
            return result;
        }

        private static byte Median(byte first, byte second, byte third)
        {
            if (first > second) { byte swap = first; first = second; second = swap; }
            if (second > third) { byte swap = second; second = third; third = swap; }
            if (first > second) second = first;
            return second;
        }

        private static Bitmap CreateQuestTextMask(Bitmap source)
        {
            var mask = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            Rectangle bounds = new Rectangle(0, 0, source.Width, source.Height);
            BitmapData sourceData = source.LockBits(
                bounds,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData maskData = mask.LockBits(
                bounds,
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = source.Width * 4;
                byte[] sourceRow = new byte[rowBytes];
                byte[] maskRow = new byte[rowBytes];
                for (int y = 0; y < source.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(sourceData.Scan0, y * sourceData.Stride), sourceRow, 0, rowBytes);
                    for (int x = 0; x < source.Width; x++)
                    {
                        int index = x * 4;
                        int blue = sourceRow[index];
                        int green = sourceRow[index + 1];
                        int red = sourceRow[index + 2];
                        int maximum = Math.Max(red, Math.Max(green, blue));
                        int minimum = Math.Min(red, Math.Min(green, blue));

                        bool lightNeutral = red >= 150 && green >= 150 && blue >= 135 &&
                            maximum - minimum <= 90;
                        bool golden = red >= 145 && green >= 100 && blue <= 150 &&
                            red - blue >= 45 && green - blue >= 20;
                        bool completedGreen = green >= 140 && green - red >= 30 &&
                            green - blue >= 15;
                        byte value = lightNeutral || golden || completedGreen
                            ? (byte)0
                            : (byte)255;
                        maskRow[index] = value;
                        maskRow[index + 1] = value;
                        maskRow[index + 2] = value;
                        maskRow[index + 3] = 255;
                    }
                    Marshal.Copy(maskRow, 0, IntPtr.Add(maskData.Scan0, y * maskData.Stride), rowBytes);
                }
            }
            finally
            {
                source.UnlockBits(sourceData);
                mask.UnlockBits(maskData);
            }
            return mask;
        }

        private static string MergeReliableCounters(string filteredText, string sourceText)
        {
            MatchCollection sourceCounters = Regex.Matches(
                sourceText ?? string.Empty,
                @"\(\s*(?<current>\d{1,4})\s*/\s*(?<total>\d{1,4})\s*\)");
            if (sourceCounters.Count == 0) return filteredText ?? string.Empty;

            int counterIndex = 0;
            string merged = Regex.Replace(
                filteredText ?? string.Empty,
                @"\((?![^\r\n)]*,)[^\r\n)]*\d[^\r\n)]*\)",
                delegate(Match match)
                {
                    if (counterIndex >= sourceCounters.Count) return match.Value;
                    Match counter = sourceCounters[counterIndex++];
                    return "(" + counter.Groups["current"].Value + "/" +
                        counter.Groups["total"].Value + ")";
                });
            return merged;
        }

        private static string MergeNumberedQuestTitles(string filteredText, string sourceText)
        {
            string[] filteredLines = (filteredText ?? string.Empty)
                .Replace("\r\n", "\n")
                .Split('\n');
            string[] sourceLines = (sourceText ?? string.Empty)
                .Replace("\r\n", "\n")
                .Split('\n');
            for (int index = 0; index < filteredLines.Length; index++)
            {
                Match filteredTitle = Regex.Match(
                    filteredLines[index],
                    @"^\s*(?<number>\d{1,2})\s+(?<title>\S.+?)\s*$");
                if (!filteredTitle.Success) continue;

                string bestSourceLine = null;
                double bestSimilarity = 0.0;
                foreach (string sourceLine in sourceLines)
                {
                    Match sourceTitle = Regex.Match(
                        sourceLine,
                        @"^\s*(?<number>\d{1,2})\s+(?<title>\S.+?)\s*$");
                    if (!sourceTitle.Success || sourceTitle.Groups["number"].Value !=
                        filteredTitle.Groups["number"].Value)
                        continue;
                    string filteredNormalized = NormalizeForComparison(
                        filteredTitle.Groups["title"].Value);
                    string sourceNormalized = NormalizeForComparison(
                        sourceTitle.Groups["title"].Value);
                    double similarity = CalculateSimilarity(filteredNormalized, sourceNormalized);
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestSourceLine = sourceTitle.Groups["number"].Value + " " +
                            sourceTitle.Groups["title"].Value.Trim();
                    }
                }

                if (bestSourceLine != null && bestSimilarity >= 0.65 &&
                    bestSourceLine.Length <= filteredLines[index].Trim().Length)
                    filteredLines[index] = bestSourceLine;
            }
            return string.Join(Environment.NewLine, filteredLines);
        }

        private static string NormalizeForComparison(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string decomposed = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (char character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                    UnicodeCategory.NonSpacingMark)
                    continue;
                builder.Append(char.IsLetterOrDigit(character)
                    ? char.ToLowerInvariant(character)
                    : ' ');
            }
            return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        }

        private static double CalculateSimilarity(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal)) return 1.0;
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right)) return 0.0;
            int[] previous = new int[right.Length + 1];
            int[] current = new int[right.Length + 1];
            for (int index = 0; index <= right.Length; index++) previous[index] = index;
            for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
            {
                current[0] = leftIndex;
                for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
                {
                    int substitution = previous[rightIndex - 1] +
                        (left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1);
                    current[rightIndex] = Math.Min(
                        Math.Min(previous[rightIndex] + 1, current[rightIndex - 1] + 1),
                        substitution);
                }
                int[] swap = previous;
                previous = current;
                current = swap;
            }
            return 1.0 - (double)previous[right.Length] /
                Math.Max(left.Length, right.Length);
        }

        private static Task<string> RecognizeAsync(string imagePath, int pageSegmentationMode)
        {
            return Task.Run(delegate
            {
                string executable = FindTesseractExecutable();
                if (string.IsNullOrWhiteSpace(executable))
                    throw new FileNotFoundException("Tesseract OCR não encontrado.");

                string tessdata = Path.Combine(Path.GetDirectoryName(executable), "tessdata");
                string arguments = string.Format(
                    "\"{0}\" stdout --tessdata-dir \"{1}\" -l por --psm {2}",
                    imagePath,
                    tessdata,
                    pageSegmentationMode);
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
