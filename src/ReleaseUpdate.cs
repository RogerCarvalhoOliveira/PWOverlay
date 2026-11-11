using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PWOverlayCentral
{
    internal sealed class ReleaseUpdate
    {
        private const string Repository = "RogerCarvalhoOliveira/PWOverlay";
        private const string Latest = "https://api.github.com/repos/" + Repository + "/releases/latest";
        private static readonly Regex TagFormat = new Regex(@"\Av\d+\.\d+\.\d+\z");
        private static readonly Regex DigestFormat = new Regex(@"\Asha256:[a-fA-F0-9]{64}\z");

        public sealed class Release
        {
            public string tag_name { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
            public List<Asset> assets { get; set; }
        }

        public sealed class Asset
        {
            public string name { get; set; }
            public string browser_download_url { get; set; }
            public string digest { get; set; }
            public long size { get; set; }
        }

        internal Version Version { get; private set; }
        internal string FileName { get; private set; }
        internal string Sha256 { get; private set; }
        internal long Size { get; private set; }
        private string DownloadUrl { get; set; }

        internal static Version CurrentVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return new Version(version.Major, version.Minor, version.Build);
            }
        }

        internal static ReleaseUpdate Check(Version current, CancellationToken cancellation)
        {
            using (var response = Open(Latest, "application/vnd.github+json", cancellation))
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                return ReadRelease(reader.ReadToEnd(), current);
        }

        internal static ReleaseUpdate ReadRelease(string json, Version current)
        {
            Release release = new JavaScriptSerializer().Deserialize<Release>(json);
            Version version;
            if (release == null || !TagFormat.IsMatch(release.tag_name ?? "") ||
                !Version.TryParse(release.tag_name.Substring(1), out version))
                throw new InvalidDataException("O GitHub retornou uma versão inválida.");
            if (release.draft || release.prerelease || version <= current) return null;

            string file = "PWQuestOverlay-Setup-" + release.tag_name + ".exe";
            string url = "https://github.com/" + Repository + "/releases/download/" + release.tag_name + "/" + file;
            Asset asset = (release.assets ?? new List<Asset>()).SingleOrDefault(candidate => candidate.name == file);
            if (asset == null || asset.browser_download_url != url || asset.size <= 0 ||
                !DigestFormat.IsMatch(asset.digest ?? ""))
                throw new InvalidDataException("A versão publicada ainda não tem um instalador verificável.");
            return new ReleaseUpdate
            {
                Version = version, FileName = file, DownloadUrl = url,
                Size = asset.size, Sha256 = asset.digest.Substring("sha256:".Length)
            };
        }

        internal string Download(CancellationToken cancellation)
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PWQuestOverlay", "updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string staged = Path.Combine(directory, FileName + ".part");
            string final = Path.Combine(directory, FileName);
            try
            {
                using (var response = Open(DownloadUrl, "application/octet-stream", cancellation))
                using (var input = response.GetResponseStream())
                using (var output = File.Create(staged))
                {
                    var buffer = new byte[65536];
                    long downloaded = 0;
                    int count;
                    while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        downloaded += count;
                        if (downloaded > Size) throw new InvalidDataException("O instalador baixado excede o tamanho publicado.");
                        output.Write(buffer, 0, count);
                    }
                }
                cancellation.ThrowIfCancellationRequested();
                Verify(staged);
                File.Move(staged, final);
                return final;
            }
            finally
            {
                if (File.Exists(staged)) File.Delete(staged);
            }
        }

        internal void Verify(string path)
        {
            using (var file = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                if (file.Length != Size ||
                    !string.Equals(BitConverter.ToString(sha.ComputeHash(file)).Replace("-", ""), Sha256,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("O instalador baixado diverge do arquivo publicado no GitHub.");
            }
        }

        private static HttpWebResponse Open(string url, string accept, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "PWQuestOverlay-Update";
            request.Accept = accept;
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.MaximumAutomaticRedirections = 5;
            using (cancellation.Register(request.Abort))
            {
                var response = (HttpWebResponse)request.GetResponse();
                Uri actual = response.ResponseUri;
                if (actual.Scheme == Uri.UriSchemeHttps &&
                    (actual.Host == "api.github.com" || actual.Host == "github.com" ||
                     actual.Host == "release-assets.githubusercontent.com" || actual.Host == "objects.githubusercontent.com"))
                    return response;
                response.Dispose();
                throw new InvalidDataException("O GitHub redirecionou o download para um endereço inesperado.");
            }
        }
    }

    internal sealed class UpdateCheckForm : Form
    {
        private readonly Version _current;
        private readonly Label _status;
        private readonly Button _download;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private ReleaseUpdate _release;
        internal string InstallerPath { get; private set; }
        internal string InstallerHash { get { return _release == null ? null : _release.Sha256; } }

        internal UpdateCheckForm(Version current)
        {
            _current = current;
            Text = "Procurar atualizações — PW Quest Overlay";
            Icon = PWOverlayShared.AppBranding.Icon;
            ClientSize = new Size(452, 153);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9f);
            _status = new Label { Location = new Point(16, 15), Size = new Size(420, 80),
                Text = "Consultando a versão publicada no GitHub..." };
            _download = new Button { Text = "Baixar e abrir instalador", Location = new Point(150, 106),
                Size = new Size(184, 31), Enabled = false };
            var close = new Button { Text = "Fechar", Location = new Point(344, 106),
                Size = new Size(92, 31), DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { _status, _download, close });
            CancelButton = close;
            Shown += async delegate { await Check(); };
            _download.Click += async delegate { await Download(); };
            FormClosed += delegate { _cancellation.Cancel(); };
        }

        private async Task Check()
        {
            try
            {
                _release = await Task.Run(() => ReleaseUpdate.Check(_current, _cancellation.Token));
                if (_cancellation.IsCancellationRequested) return;
                _status.Text = _release == null
                    ? "Você já está usando a versão " + _current + " ou uma mais recente que a publicada."
                    : "Versão disponível: " + _release.Version + ".\nSua versão: " + _current + ".";
                _download.Enabled = _release != null;
            }
            catch (Exception error)
            {
                if (!_cancellation.IsCancellationRequested)
                    _status.Text = "Não foi possível consultar as atualizações.\n" + error.Message;
            }
        }

        private async Task Download()
        {
            _download.Enabled = false;
            _status.Text = "Baixando e verificando a versão " + _release.Version + "...";
            try
            {
                string file = await Task.Run(() => _release.Download(_cancellation.Token));
                if (_cancellation.IsCancellationRequested) return;
                InstallerPath = file;
                DialogResult = DialogResult.OK;
            }
            catch (Exception error)
            {
                if (_cancellation.IsCancellationRequested) return;
                _status.Text = "Não foi possível preparar o instalador.\n" + error.Message;
                _download.Enabled = true;
            }
        }

        internal void LaunchInstaller()
        {
            _release.Verify(InstallerPath);
            using (Process setup = Process.Start(new ProcessStartInfo(InstallerPath)
                { WorkingDirectory = Path.GetDirectoryName(InstallerPath), UseShellExecute = true })) { }
        }
    }
}
