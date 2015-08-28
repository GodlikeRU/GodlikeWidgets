using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodlikeWidgets.Structures
{
    public class Disk 
    {
        public string driveLetter { get; set; }
        public string sensorName { get; set; }
        public string readRate { get; set; }
        public string writeRate { get; set; }
        public string totalSpace { get; set; }
        public string freeSpace { get; set; }
        public double diskUsage { get; set; }
        public double spaceUsage { get; set; }

        public Disk(string driveLetter, string sensorName)
        {
            this.driveLetter = driveLetter;
            this.sensorName = sensorName;
        }
    }
}
