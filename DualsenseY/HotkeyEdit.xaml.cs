using System.Windows;
using System.Windows.Controls;

namespace DualSenseY
{
    /// <summary>
    /// Interaction logic for HotkeyEdit.xaml
    /// </summary>
    public partial class HotkeyEdit : Window
    {
        private List<string> keylist = new List<string>();

        public HotkeyEdit(int index1, int index2, int index3, int index4)
        {
            keylist.Add("[EMPTY]");
            keylist.Add("A");
            keylist.Add("B");
            keylist.Add("C");
            keylist.Add("D");
            keylist.Add("E");
            keylist.Add("F");
            keylist.Add("G");
            keylist.Add("H");
            keylist.Add("I");
            keylist.Add("J");
            keylist.Add("K");
            keylist.Add("L");
            keylist.Add("M");
            keylist.Add("N");
            keylist.Add("O");
            keylist.Add("P");
            keylist.Add("Q");
            keylist.Add("R");
            keylist.Add("S");
            keylist.Add("T");
            keylist.Add("U");
            keylist.Add("V");
            keylist.Add("W");
            keylist.Add("X");
            keylist.Add("Y");
            keylist.Add("Z");

            keylist.Add("0");
            keylist.Add("1");
            keylist.Add("2");
            keylist.Add("3");
            keylist.Add("4");
            keylist.Add("5");
            keylist.Add("6");
            keylist.Add("7");
            keylist.Add("8");
            keylist.Add("9");

            keylist.Add("Enter");
            keylist.Add("Tab");
            keylist.Add("Shift");
            keylist.Add("Control");
            keylist.Add("Alt");
            keylist.Add("Escape");
            keylist.Add("Backspace");
            keylist.Add("Delete");
            keylist.Add("Insert");
            keylist.Add("Home");
            keylist.Add("End");
            keylist.Add("PGUP");
            keylist.Add("PGDN");
            keylist.Add("Up");
            keylist.Add("Down");
            keylist.Add("Left");
            keylist.Add("Right");

            keylist.Add("F1");
            keylist.Add("F2");
            keylist.Add("F3");
            keylist.Add("F4");
            keylist.Add("F5");
            keylist.Add("F6");
            keylist.Add("F7");
            keylist.Add("F8");
            keylist.Add("F9");
            keylist.Add("F10");
            keylist.Add("F11");
            keylist.Add("F12");

            keylist.Add("PRTSC");

            InitializeComponent();

            foreach (string key in keylist)
            {
                one.Items.Add(key);
                two.Items.Add(key);
                three.Items.Add(key);
                four.Items.Add(key);
            }

            one.SelectedIndex = index1;
            two.SelectedIndex = index2;
            three.SelectedIndex = index3;
            four.SelectedIndex = index4;

            if (one.SelectedItem == "[EMPTY]")
                two.IsEnabled = false;

            if (two.SelectedItem == "[EMPTY]")
                three.IsEnabled = false;

            if (three.SelectedItem == "[EMPTY]")
                four.IsEnabled = false;
        }

        public string firstKey
        {
            get { return one.SelectedValue.ToString(); }
        }

        public string secondKey
        {
            get { return two.SelectedValue.ToString(); }
        }

        public string thirdKey
        {
            get { return three.SelectedValue.ToString(); }
        }

        public string fourthKey
        {
            get { return four.SelectedValue.ToString(); }
        }

        public int firstIndex
        {
            get { return one.SelectedIndex; }
        }
        public int secondIndex
        {
            get { return two.SelectedIndex; }
        }
        public int thirdIndex
        {
            get { return three.SelectedIndex; }
        }
        public int fourthIndex
        {
            get { return four.SelectedIndex; }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        private void one_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                if (one.SelectedItem != "[EMPTY]")
                    two.IsEnabled = true;
                else
                {
                    two.SelectedIndex = 0;
                    three.SelectedIndex = 0;
                    four.SelectedIndex = 0;

                    two.IsEnabled = false;
                    three.IsEnabled = false;
                    four.IsEnabled = false;
                }
            }
        }

        private void two_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                if (two.SelectedItem != "[EMPTY]")
                    three.IsEnabled = true;
                else
                {
                    three.SelectedIndex = 0;
                    four.SelectedIndex = 0;

                    three.IsEnabled = false;
                    four.IsEnabled = false;
                }
            }
        }

        private void three_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                if (three.SelectedItem != "[EMPTY]")
                    four.IsEnabled = true;
                else
                {
                    four.SelectedIndex = 0;

                    four.IsEnabled = false;
                }
            }
        }

        private void four_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
