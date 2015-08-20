using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GodlikeWidgets.Structures
{
    class Struct_SensorElement
    {
        public UInt32 dwSensorID;
        public string szName;
        public List<Struct_SensorElementReader> sensorReaders;
        
        public Struct_SensorElement()
        {
            this.sensorReaders = new List<Struct_SensorElementReader>();
        }
    }

    class Struct_SensorElementReader
    {
        public SENSOR_READING_TYPE tReading;
        public string szName;
        public string szUnit;
        public double Value;
        public double ValueMin;
        public double ValueMax;
        public double ValueAvg;
    }

    public enum SENSOR_READING_TYPE
    {
        SENSOR_TYPE_NONE = 0,
        SENSOR_TYPE_TEMP,
        SENSOR_TYPE_VOLT,
        SENSOR_TYPE_FAN,
        SENSOR_TYPE_CURRENT,
        SENSOR_TYPE_POWER,
        SENSOR_TYPE_CLOCK,
        SENSOR_TYPE_USAGE,
        SENSOR_TYPE_OTHER
    };
}
