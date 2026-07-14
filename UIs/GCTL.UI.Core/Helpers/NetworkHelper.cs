using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace GCTL.UI.Core.Helpers
{
    public static class NetworkHelper
    {
        public static string GetMacAddress(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    ping.Send(ip, 1000);
                }

                Process process = new Process();
                process.StartInfo.FileName = "arp";
                process.StartInfo.Arguments = "-a";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                var match = Regex.Match(output,
                    $"({Regex.Escape(ip)})\\s+([0-9a-fA-F-]{{17}})");

                if (match.Success)
                    return match.Groups[2].Value;

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}