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
using GodlikeWidgets.Structures;
using System.Windows.Threading;


namespace GodlikeWidgets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    

    public partial class MainWindow : Window
    {
        private BackgroundWorker t_oWriteDataBackgroundWorker = null;
        private BackgroundWorker t_oReadDataBackgroundWorker = null;
        private HWiNFOWrapper hwW;
        private int SLEEP_INTERVAL = 500;
        private List<KeyValuePair<string, int>> cPUChartValueList;

        private List<Struct_SensorElement> sensorElementsDataArray;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void OnStart()
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); 
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            
            

            this.hwW = new HWiNFOWrapper();
            this.sensorElementsDataArray = hwW.GetSensorData();

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

            this.cPUChartValueList = new List<KeyValuePair<string, int>>();
            

            t_oWriteDataBackgroundWorker = new BackgroundWorker();
            t_oWriteDataBackgroundWorker.DoWork += new DoWorkEventHandler(Thread_WriteData);
            t_oWriteDataBackgroundWorker.WorkerSupportsCancellation = true;
            t_oWriteDataBackgroundWorker.WorkerReportsProgress = false;
            t_oWriteDataBackgroundWorker.RunWorkerAsync();


            t_oReadDataBackgroundWorker = new BackgroundWorker();
            t_oReadDataBackgroundWorker.DoWork += new DoWorkEventHandler(Thread_ReadData);
            t_oReadDataBackgroundWorker.WorkerSupportsCancellation = true;
            t_oReadDataBackgroundWorker.WorkerReportsProgress = false;
            t_oReadDataBackgroundWorker.RunWorkerAsync();
        }

        public uint GetCPUSpeed()
        {
            return 4002;
        }

        public void Thread_WriteData(object sender, DoWorkEventArgs e)
        {
            for(long i=0; ; i++)
            {

                
                    Application.Current.Dispatcher.BeginInvoke
                    (
                        DispatcherPriority.DataBind,
                        new Action(() =>
                        {
                            try
                            {
                                label_cc_CPUCLOCK.Content = Math.Round(this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Clock").Value).ToString() + " " + this.sensorElementsDataArray.Find(f => f.szName == "CPU [#0]: Intel Core-2400").sensorReaders.Find(f => f.szName == "Core #0 Clock").szUnit;
                                label_cc_CPUPOWER.Content = Math.Round(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package Power").Value, 1).ToString() + " " + this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026542592).sensorReaders.Find(f => f.szName == "CPU Package Power").szUnit;
                                int core1_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #0 Thread #0 Usage").Value);
                                int core2_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #1 Thread #0 Usage").Value);
                                int core3_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #2 Thread #0 Usage").Value);
                                int core4_usage = Convert.ToInt32(this.sensorElementsDataArray.Find(f => f.dwSensorID == 4026532608).sensorReaders.Find(f => f.szName == "Core #3 Thread #0 Usage").Value);

                                cPUChartValueList.Clear();
                                cPUChartValueList.Add(new KeyValuePair<string, int>("#1", core1_usage));
                                cPUChartValueList.Add(new KeyValuePair<string, int>("#2", core2_usage));
                                cPUChartValueList.Add(new KeyValuePair<string, int>("#3", core3_usage));
                                cPUChartValueList.Add(new KeyValuePair<string, int>("#4", core4_usage));


                                /*CPU_Chart.DataContext = cPUChartValueList;
                                CPU_Chart.Refresh();*/
                            }
                            catch(Exception )
                            {

                            }
                            
                        })
                    );
                

                Thread.Sleep(this.SLEEP_INTERVAL);
 
            }
            
        }

        public void Thread_ReadData(object sender, DoWorkEventArgs e)
        {
            for (long i = 0; ; i++)
            {
                this.sensorElementsDataArray.Clear();
                this.sensorElementsDataArray = hwW.GetSensorData();
                Thread.Sleep(this.SLEEP_INTERVAL);
            }
        }


    }
}
