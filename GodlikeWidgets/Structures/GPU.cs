using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodlikeWidgets.Structures
{
    public class GPU
    {
        
        public string sensorName { get; set; }
        public string voltage { get; set; }
        public string coreClock { get; set; }
        public string vramClock { get; set; }
        public string usedVRAM { get; set; }
        public double usedVRAMUsage { get; set; }
        public string temp { get; set; }

        public GPU( string sensorName)
        {
            this.sensorName = sensorName;
        }
    }
}
