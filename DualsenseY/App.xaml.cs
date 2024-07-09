using System.Windows;

namespace DualsenseY
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Error" + Environment.NewLine + e.Exception.Message, "Error");
            e.Handled = true;
        }
    }

}
