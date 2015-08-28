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
using System.Collections.ObjectModel;
using HWiNFO64Sensors;
using HWiNFO64Sensors.Structures;
using GodlikeWidgets.Data;
using GodlikeWidgets.Structures;





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

        /*public ObservableCollection<Disk> Array1 { get {
            StaticCollectionPropertyChanged(null, new CollectionChangeEventArgs(CollectionChangeAction.Refresh, "Array1"));
            return Database.DISKS; } set {
                StaticCollectionPropertyChanged(null, new CollectionChangeEventArgs(CollectionChangeAction.Refresh, "Array1"));
            Database.DISKS = value; } }
        public static event EventHandler<CollectionChangeEventArgs> StaticCollectionPropertyChanged;*/

 

        private bool TEST_MODE = false; // Debug mode = Writing info to file

        private int SLEEP_INTERVAL = 500; // Should match HWiNFO64 Refresh Value! Lower = Higher cpu usage but faster refresh
        private int CHARTS_NODES = 60; //Higher = Higher cpu usage but more values on chart

        private string CONFIG_NETWORK_SENSOR_NAME = "Network: Intel Centrino Advanced-N 6200 AGN 2x2 HMC WiFi Adapter";
        private string CONFIG_GPU1_SENSOR_NAME = "GPU [#1]";
        private string CONFIG_GPU2_SENSOR_NAME = "GPU [#0]";
        private string CONFIG_DISK1_SENSOR_NAME = "SAMSUNG";
        private string CONFIG_DISK2_SENSOR_NAME = "WDC";

        // Reserved for future use
        #pragma warning disable 0414
        private bool CONFIG_USES_CPU_CHART = true;
        private bool CONFIG_USES_NETWORK_CHART = true;
        #pragma warning restore 0414

        /*
         * END CONFIG VALUES
        */
        #region Initialization of values
        private BackgroundWorker t_oWriteDataBackgroundWorker = null;
        private BackgroundWorker t_oReadDataBackgroundWorker = null;
        private HWiNFOWrapper hwW;
        
        
        /*private List<KeyValuePair<string, int>> cPUChartValueList;
        private List<KeyValuePair<string, int>> networkChartValueList;*/
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
            //Application.Current.Shutdown(); 
            Test test = new Test();
            test.Activate();
            test.Visibility = System.Windows.Visibility.Visible;

            test.Left = 250;
            test.Top = 250;
            
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

            #region TEST MODE
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
            #endregion

            // Initialize lists, just one time
            Database.CPU_CHART_VALUES = new ObservableCollection<KeyValuePair<string, int>>();
            Database.NETWORK_CHART_VALUES = new ObservableCollection<KeyValuePair<string, int>>();
            this.processUsageList = new Dictionary<string, int>();
            Database.DISKS = new ObservableCollection<Disk>();
            Database.GPUS = new ObservableCollection<GPU>();


            // Test disk
            Disk disk_C = new Disk("C", CONFIG_DISK1_SENSOR_NAME);
            Disk disk_F = new Disk("F", CONFIG_DISK2_SENSOR_NAME);

            Database.DISKS.Add(disk_C);
            Database.DISKS.Add(disk_F);

            // Test GPU

            GPU gpu1 = new GPU(CONFIG_GPU1_SENSOR_NAME);
            GPU gpu2 = new GPU(CONFIG_GPU2_SENSOR_NAME);

            Database.GPUS.Add(gpu1);
            Database.GPUS.Add(gpu2);


            // Fil charts with zeroes
            for (int i = 0; i < CHARTS_NODES + 1; i++)
            {
                Database.CPU_CHART_VALUES.Add(new KeyValuePair<string, int>(i.ToString(), 0));
            }


            for (int i = 0; i < CHARTS_NODES+1; i++)
            {
                Database.NETWORK_CHART_VALUES.Add(new KeyValuePair<string, int>(i.ToString(), 0));
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
                                #region Read

                                int iCPUcore1_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Thread #0 Usage").Value);
                                int iCPUcore2_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #1 Thread #0 Usage").Value);
                                int iCPUcore3_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #2 Thread #0 Usage").Value);
                                int iCPUcore4_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #3 Thread #0 Usage").Value);
                                int iCPUtotal_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Total CPU Usage").Value);
                                double fCPUCLOCK = Math.Round(this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Clock").Value);
                                double fCPUPOWER = Math.Round(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package Power").Value, 1);
                                int iCPUTEMPERATURE = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package").Value);

                                // Memory
                                int memoryUsed = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Used").Value);
                                int memoryAvailable = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Available").Value);
                                int memoryUsage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Load").Value);


                                //Network
                                int networkTotalDL = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total DL").Value);
                                int networkTotalUL = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total UP").Value);
                                double networkCurrentDL = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current DL rate").Value, 1);
                                double networkCurrentUL = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current UP rate").Value, 1);


                                #region Disks
                                ObservableCollection<Disk> tempDiskColl = new ObservableCollection<Disk>();
                                foreach(Disk disk in Database.DISKS)
                                {
                                    Disk tempdisk = new Disk(disk.driveLetter, disk.sensorName);
                                    tempdisk.readRate = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(disk.sensorName)).sensorReaders.Find(f => f.szName == "Read Rate").Value, 1).ToString() + " MB/s";
                                    tempdisk.writeRate = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(disk.sensorName)).sensorReaders.Find(f => f.szName == "Write Rate").Value, 1).ToString() + " MB/s";
                                    tempdisk.diskUsage = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(disk.sensorName)).sensorReaders.Find(f => f.szName == "Total Activity").Value, 1);
                                    tempdisk.totalSpace = Math.Round((((double)this.DriveDataList.First(f => f.Name == disk.driveLetter + @":\").TotalSize / 1048576) / 1024), 1).ToString() + " GB";
                                    tempdisk.freeSpace = Math.Round((((double)this.DriveDataList.First(f => f.Name == disk.driveLetter + @":\").TotalFreeSpace / 1048576) / 1024), 1).ToString() + " GB";
                                    tempdisk.spaceUsage = Math.Abs(Math.Round((Math.Round((((double)this.DriveDataList.First(f => f.Name == disk.driveLetter + @":\").TotalFreeSpace / 1048576) / 1024), 1) / Math.Round((((double)this.DriveDataList.First(f => f.Name == disk.driveLetter + @":\").TotalSize / 1048576) / 1024), 1)) * 100) - 100);
                                    tempDiskColl.Add(tempdisk);
                                }

                                Database.DISKS.Clear();

                                foreach(Disk disk in tempDiskColl)
                                {
                                    Database.DISKS.Add(disk);
                                }
                                tempDiskColl.Clear();
                                #endregion

                                #region GPUS
                                ObservableCollection<GPU> tempGPUColl = new ObservableCollection<GPU>();

                                foreach (GPU gpu in Database.GPUS)
                                {
                                    GPU tempGPU = new GPU(gpu.sensorName);

                                    tempGPU.voltage = Math.Round(this.sensorElementsDataArray.Find(f => f.szName.Contains(tempGPU.sensorName)).sensorReaders.Find(f => f.szName == "GPU Core Voltage").Value, 3).ToString() + " V";
                                    tempGPU.temp = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(tempGPU.sensorName)).sensorReaders.Find(f => f.szName == "GPU Temperature").Value).ToString() + " °C";
                                    tempGPU.coreClock = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(tempGPU.sensorName)).sensorReaders.Find(f => f.szName == "GPU Clock").Value).ToString() + " MHz";
                                    tempGPU.vramClock = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(tempGPU.sensorName)).sensorReaders.Find(f => f.szName == "GPU Memory Clock").Value).ToString() + " MHz";
                                    tempGPU.usedVRAM = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(tempGPU.sensorName)).sensorReaders.Find(f => f.szName == "GPU Memory Allocated").Value).ToString() + " MB";
                                    tempGPU.usedVRAMUsage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.szName.Contains(tempGPU.sensorName)).sensorReaders.Find(f => f.szName == "GPU Memory Usage").Value);

                                    tempGPUColl.Add(tempGPU);
                                }

                                Database.GPUS.Clear();

                                foreach (GPU gpu in tempGPUColl)
                                {
                                    Database.GPUS.Add(gpu);
                                }

                                tempGPUColl.Clear();

                                #endregion

                                #endregion

                                /*
                                 *  Data Write
                                */
                                #region Write

                                //CPU
                                Database.CPU_CPUCLOCK = fCPUCLOCK.ToString() + " MHz"; // + this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Clock").szUnit;
                                Database.CPU_CPUPOWER = fCPUPOWER.ToString() + " W"; // +this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package Power").szUnit;
                                Database.CPU_CPUTEMPERATURE = iCPUTEMPERATURE.ToString() + " °C";// +this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package").szUnit;
                                
                                // RAM
                                Database.RAM_FREERAM = memoryAvailable.ToString()  + " MB"; // + this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Used").szUnit;
                                Database.RAM_USEDRAM = memoryUsed.ToString() + " MB"; // this.sensorElementsDataArray.Find(f => f.szName == "System").sensorReaders.Find(f => f.szName == "Physical Memory Used").szUnit;
                                Database.RAM_USAGE = (double)memoryUsage;

                                // Network
                                Database.NETWORK_DL_TOTAL = networkTotalDL.ToString() + " MB"; //+ this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total DL").szUnit;
                                Database.NETWORK_UL_TOTAL = networkTotalUL.ToString() + " MB"; // + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Total UP").szUnit;
                                Database.NETWORK_DL_CURRENT = networkCurrentDL.ToString() + " KB/s"; // + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current DL rate").szUnit;
                                Database.NETWORK_UL_CURRENT = networkCurrentUL.ToString() + " KB/s"; // + this.sensorElementsDataArray.Find(f => f.szName.Contains(CONFIG_NETWORK_SENSOR_NAME)).sensorReaders.Find(f => f.szName == "Current UP rate").szUnit;


                                if (CONFIG_USES_CPU_CHART)
                                {
                                    // CPU Chart
                                    if (Database.CPU_CHART_VALUES.Count >= CHARTS_NODES)
                                        Database.CPU_CHART_VALUES.Remove(Database.CPU_CHART_VALUES[0]);
                                    Database.CPU_CHART_VALUES.Add(new KeyValuePair<string, int>(i.ToString(), iCPUtotal_usage));
                                    Database.CPU_CPUUSAGE = iCPUtotal_usage;
                                }

                                if (CONFIG_USES_NETWORK_CHART)
                                {
                                    // Network Chart
                                    if (Database.NETWORK_CHART_VALUES.Count >= CHARTS_NODES)
                                        Database.NETWORK_CHART_VALUES.Remove(Database.NETWORK_CHART_VALUES[0]);
                                    Database.NETWORK_CHART_VALUES.Add(new KeyValuePair<string, int>(i.ToString(), Convert.ToInt32(networkCurrentDL)));
                                }

                                #endregion

                            }
                            catch(Exception )
                            {
                                // Honestly I dont give a shit at the moment :)
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
