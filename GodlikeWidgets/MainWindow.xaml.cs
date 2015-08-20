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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HWiNFO64Sensors;
using HWiNFO64Sensors.Structures;




namespace GodlikeWidgets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    

    public partial class MainWindow : Window
    {
        /*
         * CONFIG VALUES
        */

        private bool TEST_MODE = false; // Debug mode = Writing info to file

        private int SLEEP_INTERVAL = 500; // Should match HWiNFO64 Refresh Value!
        private int CHARTS_NODES = 60; //Higher = Higher cpu usage but more values on chart

        private string CONFIG_NETWORK_SENSOR_NAME = "Network: Intel Centrino Advanced-N 6200 AGN 2x2 HMC WiFi Adapter";
        private string CONFIG_GPU1_SENSOR_NAME = "GPU [#1]";
        private string CONFIG_GPU2_SENSOR_NAME = "GPU [#0]";
        private string CONFIG_DISK1_SENSOR_NAME = "SAMSUNG";
        private string CONFIG_DISK2_SENSOR_NAME = "WDC";

        /*
         * END CONFIG VALUES
        */
        #region Initialization of values
        private BackgroundWorker t_oWriteDataBackgroundWorker = null;
        private BackgroundWorker t_oReadDataBackgroundWorker = null;
        private HWiNFOWrapper hwW;
        
        
        private List<KeyValuePair<string, int>> cPUChartValueList;
        private List<KeyValuePair<string, int>> networkChartValueList;
        private List<Struct_SensorElement> sensorElementsDataArray;
        private Dictionary<string, int> processUsageList;
        private DriveInfo[] DriveDataList;

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        const UInt32 SWP_NOZORDER = 0x0004;
        const int WM_ACTIVATEAPP = 0x001C;
        const int WM_ACTIVATE = 0x0006;
        const int WM_SETFOCUS = 0x0007;
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const int WM_WINDOWPOSCHANGING = 0x0046;
        public const Int32 WM_SYSCOMMAND = 0x112;
        public IntPtr SC_MAXIMIZE = new IntPtr(0xF030);
        public IntPtr SC_MINIMIZE = new IntPtr(0xF020);
        const int GWL_HWNDPARENT = -8;
        #endregion

        #region DLLImports
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static extern IntPtr DeferWindowPos(IntPtr hWinPosInfo, IntPtr hWnd,
           IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static extern IntPtr BeginDeferWindowPos(int nNumWindows);
        [DllImport("user32.dll")]
        static extern bool EndDeferWindowPos(IntPtr hWinPosInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);
        #endregion

        #region Win32API Crap
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SETFOCUS)
            {
                IntPtr hWnd2 = new WindowInteropHelper(this).Handle;
                SetWindowPos(hWnd2, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                handled = true;
            }

            if(msg == WM_SYSCOMMAND)
            {
                if(wParam == SC_MAXIMIZE)
                {
                    IntPtr hWnd2 = new WindowInteropHelper(this).Handle;
                    SetWindowPos(hWnd2, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                    handled = true;
                }
                if(wParam == SC_MINIMIZE)
                {
                    IntPtr hWnd2 = new WindowInteropHelper(this).Handle;
                    SetWindowPos(hWnd2, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                    handled = true;
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.Activate();

                }
            }
            return IntPtr.Zero;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IntPtr windowHandle = (new WindowInteropHelper(this)).Handle;
            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src.RemoveHook(new HwndSourceHook(this.WndProc));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); 
        }

        // Additional prevent minimize if WM_SYSCOMMAND fails
        private void Window_StateChanged(object sender, EventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
            this.Activate();

        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Initialized(object sender, EventArgs e)
        {


            IntPtr hWnd = new WindowInteropHelper(this).Handle;


            
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            IntPtr windowHandle = (new WindowInteropHelper(this)).Handle;
            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src.AddHook(new HwndSourceHook(WndProc));


            this.hwW = new HWiNFOWrapper();
            this.sensorElementsDataArray = hwW.GetSensorData();
            this.DriveDataList = DriveInfo.GetDrives();

            // DEBUG PURPOSES ONLY
            if (TEST_MODE)
            {

                if (!System.IO.File.Exists(@"C:\Users\Kama3\Documents\Visual Studio 2013\Projects\GodlikeWidgets\GodlikeWidgets\test2.txt"))
                    System.IO.File.Create(@"C:\Users\Kama3\Documents\Visual Studio 2013\Projects\GodlikeWidgets\GodlikeWidgets\test2.txt");

                FileStream fs = File.Open(@"C:\Users\Kama3\Documents\Visual Studio 2013\Projects\GodlikeWidgets\GodlikeWidgets\test2.txt", FileMode.Truncate, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);

                foreach (Struct_SensorElement sensorElement in sensorElementsDataArray)
                {
                    sw.WriteLine(String.Format("Sensor Name: {0}", sensorElement.szName));
                    sw.WriteLine(String.Format("Sensor ID: {0}", sensorElement.dwSensorID));

                    foreach (Struct_SensorElementReader sensorReader in sensorElement.sensorReaders)
                    {
                        sw.WriteLine(String.Format("\tSensor Name: {0}", sensorReader.szName));
                        sw.WriteLine(String.Format("\tSensor Units: {0}", sensorReader.szUnit));
                        sw.WriteLine(String.Format("\tSensor Type: {0}", Enum.GetName(typeof(SENSOR_READING_TYPE), sensorReader.tReading)));
                        sw.WriteLine(String.Format("\tSensor Value: {0}", sensorReader.Value));
                        sw.Write("\n");
                    }

                    sw.Write("\n");
                }


                sw.Flush();
                fs.Flush();
                sw.Close();
                fs.Close();
            }

            // Initialize lists, just one time
            this.cPUChartValueList = new List<KeyValuePair<string, int>>();
            this.networkChartValueList = new List<KeyValuePair<string, int>>();
            this.processUsageList = new Dictionary<string, int>();

            // Fil charts with zeroes
            for (int i = 0; i < CHARTS_NODES+1; i++)
            {
                cPUChartValueList.Add(new KeyValuePair<string, int>(i.ToString(), 0));
            }

            for (int i = 0; i < CHARTS_NODES+1; i++)
            {
                networkChartValueList.Add(new KeyValuePair<string, int>(i.ToString(), 0));
            }

            // Start ReadData Thread
            t_oReadDataBackgroundWorker = new BackgroundWorker();
            t_oReadDataBackgroundWorker.DoWork += new DoWorkEventHandler(Thread_ReadData);
            t_oReadDataBackgroundWorker.WorkerSupportsCancellation = true;
            t_oReadDataBackgroundWorker.WorkerReportsProgress = false;
            t_oReadDataBackgroundWorker.RunWorkerAsync();

            // Start WriteData Thread
            t_oWriteDataBackgroundWorker = new BackgroundWorker();
            t_oWriteDataBackgroundWorker.DoWork += new DoWorkEventHandler(Thread_WriteData);
            t_oWriteDataBackgroundWorker.WorkerSupportsCancellation = true;
            t_oWriteDataBackgroundWorker.WorkerReportsProgress = false;
            t_oWriteDataBackgroundWorker.RunWorkerAsync();

           
        }

        public void Thread_WriteData(object sender, DoWorkEventArgs e)
        {
            // Work forever
            for(long i=0; ; i++)
            {

                
                    Application.Current.Dispatcher.BeginInvoke
                    (
                        DispatcherPriority.DataBind,
                        new Action(() =>
                        {
                            try
                            {

                                

                                
                                /*
                                 *  Data Read
                                */
                                label_cc_CPUCLOCK.Content = Math.Round(this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Clock").Value).ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Clock").szUnit;
                                label_cc_CPUPOWER.Content = Math.Round(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package Power").Value, 1).ToString() + " " + this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package Power").szUnit;
                                int iCPUcore1_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Thread #0 Usage").Value);
                                int iCPUcore2_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #1 Thread #0 Usage").Value);
                                int iCPUcore3_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #2 Thread #0 Usage").Value);
                                int iCPUcore4_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #3 Thread #0 Usage").Value);
                                int iCPUtotal_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Total CPU Usage").Value);

                                // Memory
                                int memoryUsed = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Used").Value);
                                int memoryAvailable = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Available").Value);
                                int memoryUsage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Load").Value);


                                //Network
                                int networkTotalDL = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total DL").Value);
                                int networkTotalUL = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total UP").Value);
                                double networkCurrentDL = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current DL rate").Value, 1);
                                double networkCurrentUL = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current UP rate").Value, 1);

                                // GPU1
                                double GPU1_Voltage = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Core Voltage").Value, 3);
                                int GPU1_Temp = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Temperature").Value);
                                int GPU1_Core = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Clock").Value);
                                int GPU1_VRAM = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Clock").Value);
                                int GPU1_UsedVRAM = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Allocated").Value);
                                int GPU1_UsedVRAMUsage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Usage").Value);

                                // GPU2
                                double GPU2_Voltage = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Core Voltage").Value, 3);
                                int GPU2_Temp = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Temperature").Value);
                                int GPU2_Core = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Clock").Value);
                                int GPU2_VRAM = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Clock").Value);
                                int GPU2_UsedVRAM = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Allocated").Value);
                                int GPU2_UsedVRAMUsage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Usage").Value);
                                
                                // Disk 1
                                double Disk1_ReadRate = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Read Rate").Value, 1);
                                double Disk1_WriteRate = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Write Rate").Value, 1);
                                double Disk1_DiskUsage = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total Activity").Value, 1);
                                double Disk1_TotalSpace = Math.Round((((double)this.DriveDataList.First(f => f.Name == @"C:\").TotalSize / 1048576) / 1024), 1);
                                double Disk1_FreeSpace = Math.Round((((double)this.DriveDataList.First(f => f.Name == @"C:\").TotalFreeSpace / 1048576) / 1024), 1);

                                // Disk 2
                                double Disk2_ReadRate = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Read Rate").Value, 1);
                                double Disk2_WriteRate = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Write Rate").Value, 1);
                                double Disk2_DiskUsage = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total Activity").Value, 1);
                                double Disk2_TotalSpace = Math.Round((((double)this.DriveDataList.First(f => f.Name == @"F:\").TotalSize / 1048576) / 1024), 1);
                                double Disk2_FreeSpace = Math.Round((((double)this.DriveDataList.First(f => f.Name == @"F:\").TotalFreeSpace / 1048576) / 1024), 1);
                                
                                /*
                                 *  Data Write
                                */
                                

                                // RAM
                                label_cc_FreeRAM.Content = memoryAvailable.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Used").szUnit;
                                label_cc_UsedRAM.Content = memoryUsed.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Used").szUnit;
                                Memory_UsageBar.Value = memoryUsage;
                                // Network
                                label_cc_TotalDL.Content = networkTotalDL.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total DL").szUnit;
                                label_cc_TotalUL.Content = networkTotalUL.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total UP").szUnit;
                                label_cc_CurrentDL.Content = networkCurrentDL.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current DL rate").szUnit;
                                label_cc_CurrentUL.Content = networkCurrentUL.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current UP rate").szUnit;

                                // GPU1
                                label_cc_GPU1_Voltage.Content = GPU1_Voltage.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Core Voltage").szUnit;
                                label_cc_GPU1_Temp.Content = GPU1_Temp.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Temperature").szUnit;
                                label_cc_GPU1_Core.Content = GPU1_Core.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Clock").szUnit;
                                label_cc_GPU1_VRAM.Content = GPU1_VRAM.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Clock").szUnit;
                                label_cc_GPU1_UsedVRAM.Content = GPU1_UsedVRAM.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Allocated").szUnit;
                                GPU1_VRAM_UsageBar.Value = GPU1_UsedVRAMUsage;

                                // GPU2
                                label_cc_GPU2_Voltage.Content = GPU2_Voltage.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Core Voltage").szUnit;
                                label_cc_GPU2_Temp.Content = GPU2_Temp.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Temperature").szUnit;
                                label_cc_GPU2_Core.Content = GPU2_Core.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Clock").szUnit;
                                label_cc_GPU2_VRAM.Content = GPU2_VRAM.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Clock").szUnit;
                                label_cc_GPU2_UsedVRAM.Content = GPU2_UsedVRAM.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_GPU2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "GPU Memory Allocated").szUnit;
                                GPU2_VRAM_UsageBar.Value = GPU2_UsedVRAMUsage;

                                // Disk1
                                label_cc_Disk1_ReadRate.Content = Disk1_ReadRate.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Read Rate").szUnit;
                                label_cc_Disk1_WriteRate.Content = Disk1_WriteRate.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK1_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Write Rate").szUnit;
                                Disk1_UsageBar.Value = Disk1_DiskUsage;
                                label_cc_Disk1_FreeSpace.Content = Disk1_FreeSpace + " GB";
                                label_cc_Disk1_TotalSpace.Content = Disk1_TotalSpace + " GB";
                                Disk1_SpaceUsageBar.Value = Math.Abs(Math.Round((Disk1_FreeSpace/Disk1_TotalSpace) * 100) -100);

                                // Disk2
                                label_cc_Disk2_ReadRate.Content = Disk2_ReadRate.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Read Rate").szUnit;
                                label_cc_Disk2_WriteRate.Content = Disk2_WriteRate.ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_DISK2_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Write Rate").szUnit;
                                Disk2_UsageBar.Value = Disk2_DiskUsage;
                                label_cc_Disk2_FreeSpace.Content = Disk2_FreeSpace + " GB";
                                label_cc_Disk2_TotalSpace.Content = Disk2_TotalSpace + " GB";
                                Disk2_SpaceUsageBar.Value = Math.Abs(Math.Round((Disk2_FreeSpace/Disk2_TotalSpace) * 100) -100);

                                // Cpu chart cleaning
                                if (cPUChartValueList.Count >= CHARTS_NODES)
                                    cPUChartValueList.Remove(cPUChartValueList[0]);
                                cPUChartValueList.Add(new KeyValuePair<string, int>(i.ToString(), iCPUtotal_usage));
                                CPU_UsageBar.Value = iCPUtotal_usage;
                                CPU_Chart.DataContext = cPUChartValueList;
                                CPU_Chart.Refresh();
                                
                                // Network Chart cleaning
                                if (networkChartValueList.Count >= CHARTS_NODES)
                                    networkChartValueList.Remove(networkChartValueList[0]);
                                networkChartValueList.Add(new KeyValuePair<string, int>(i.ToString(), Convert.ToInt32(networkCurrentDL)));
                                Network_Chart.DataContext = networkChartValueList;
                                Network_Chart.Refresh();
                            }
                            catch(Exception )
                            {
                                // WYJEBANEEEEEE
                            }
                            
                        })
                    );
                

                Thread.Sleep(this.SLEEP_INTERVAL);
 
            }
            
        }

        public void Thread_ReadData(object sender, DoWorkEventArgs e)
        {
            // Work forever
            for (long i = 0; ; i++)
            {
                this.sensorElementsDataArray.Clear();
                this.sensorElementsDataArray = hwW.GetSensorData();

                Thread.Sleep(this.SLEEP_INTERVAL);
            }
        }

        


    }
}
