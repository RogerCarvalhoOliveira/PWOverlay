using System;
using System.IO;
using System.Text;

namespace PWOverlayMaintenance
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            try
            {
                string directory = Path.GetFullPath(args[0]);
                string log = Path.GetFullPath(args[1]);
                File.WriteAllText(log, "Pasta da instalação: " + directory + Environment.NewLine, Encoding.UTF8);
                return PWOverlayShared.ToolMaintenance.Stop(directory, true,
                    message => File.AppendAllText(log, DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine, Encoding.UTF8)) ? 0 : 1;
            }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
    }
}
