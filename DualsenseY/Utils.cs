using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DualSenseY
{
    public class Utils
    {
        public static void ScreenshotToClipboard(bool SaveToFile)
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(bitmap as System.Drawing.Image);
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            System.Windows.Clipboard.SetDataObject(bitmap);

            if (SaveToFile)
            {
                if (!Directory.Exists(Settings.ScPath))
                {
                    Directory.CreateDirectory(Settings.ScPath);
                }

                bitmap.Save(Settings.ScPath + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss-fff") + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
