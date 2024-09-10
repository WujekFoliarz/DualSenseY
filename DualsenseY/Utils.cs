using System.Drawing;
using System.Windows.Forms;

namespace DualSenseY
{
    public class Utils
    {
        public static void ScreenshotToClipboard()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(bitmap as System.Drawing.Image);
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            System.Windows.Clipboard.SetDataObject(bitmap);
        }
    }
}
