using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace GodlikeWidgets
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class Test : Window
    {
        #region DLLImports and Window Move support
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private  IntPtr thisWindowHandle = IntPtr.Zero;



        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ReleaseCapture();
                SendMessage(this.thisWindowHandle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.thisWindowHandle = new WindowInteropHelper(this).Handle;
        }
        #endregion

        public Test()
        {
            InitializeComponent();
            
        }

    }
}
