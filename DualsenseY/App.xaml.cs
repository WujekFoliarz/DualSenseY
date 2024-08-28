using System.Windows;

namespace DualsenseY
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {

#line 4 "..\..\..\App.xaml"
            this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);

#line default
#line hidden
        }

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            DualsenseY.App app = new DualsenseY.App();
            app.InitializeComponent();
            app.Run();
        }


        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Error" + Environment.NewLine + e.Exception.Message, "Error");
            e.Handled = true;
        }
    }

}
