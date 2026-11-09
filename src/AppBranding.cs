using System;
using System.Drawing;
using System.IO;

namespace PWOverlayShared
{
    internal static class AppBranding
    {
        // One shared embedded icon per application; no file dependency at runtime.
        private static readonly Icon _icon = LoadIcon();
        internal static Icon Icon { get { return _icon; } }

        private static Icon LoadIcon()
        {
            using (Stream stream = typeof(AppBranding).Assembly.GetManifestResourceStream("PWOverlay.App.ico"))
            {
                if (stream == null) throw new InvalidOperationException("Ícone incorporado não encontrado. Recompile pelo scripts/build.ps1.");
                using (var icon = new Icon(stream, new Size(32, 32)))
                    return (Icon)icon.Clone();
            }
        }
    }
}
