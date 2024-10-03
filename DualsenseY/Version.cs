using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace DualSenseY
{
    public class Version
    {
        public double CurrentVersion = 5.3;

        public void Update()
        {
            using (WebClient client = new WebClient())
            {
                string _RemoteVersion = client.DownloadString("https://raw.githubusercontent.com/WujekFoliarz/DualSenseY/master/version").Replace("\n", "");
                double RemoteVersion = Convert.ToDouble(_RemoteVersion);

                string fileName = $"DualSenseY.{_RemoteVersion}v.zip";
                client.DownloadFile($"https://github.com/WujekFoliarz/DualSenseY/releases/latest/download/" + "DualSenseY." + _RemoteVersion + "v.zip", "update.zip");
                if (File.Exists("update.zip"))
                {
                    foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory()))
                    {
                        if (file.Contains(".dll") || file.Contains(".exe") || file.Contains(".wav") || file.Contains(".json") || file.Contains(".pdb"))
                        {
                            if(!file.Contains("DSX"))
                            {
                                File.Move(file, file + ".bak");
                            }
                        }
                    }
                    ZipFile.ExtractToDirectory("update.zip", Directory.GetCurrentDirectory());
                    Environment.Exit(0);
                }
            }
        }

        public bool IsOutdated()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    double RemoteVersion = Convert.ToDouble(client.DownloadString("https://raw.githubusercontent.com/WujekFoliarz/DualSenseY/master/version"), CultureInfo.InvariantCulture);
                    if (RemoteVersion > CurrentVersion)
                        return true;
                    else
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public void RemoveOldFiles()
        {
            foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory()))
            {
                if (file.Contains("update.zip"))
                    File.Delete(file);
                else if (file.Contains(".bak"))
                    File.Delete(file);
            }
        }
    }
}
