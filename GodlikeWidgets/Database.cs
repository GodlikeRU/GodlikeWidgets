using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using GodlikeWidgets.Structures;

namespace GodlikeWidgets.Data
{
    public static class Database
    {

        #region Event firing
        // Workaround for updating Static Properties
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        private static void NotifyStaticPropertyChanged(string propertyName)
        {
            if (StaticPropertyChanged != null)
                StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
        }

      

        // Workaround for Updating Collections NOT USED
        public static event EventHandler<CollectionChangeEventArgs> StaticCollectionPropertyChanged;
        private static void NotifyStaticCollectionPropertyChanged(string collectionName)
        {
            if (StaticCollectionPropertyChanged != null)
                StaticCollectionPropertyChanged(null, new CollectionChangeEventArgs( CollectionChangeAction.Refresh,collectionName));
        }
        #endregion


        #region CPU
        //CPU
        private static string _CPU_CPUCLOCK;
        public static string CPU_CPUCLOCK
        {
            get { return _CPU_CPUCLOCK; }
            set
            {
                if (value != _CPU_CPUCLOCK)
                {
                    _CPU_CPUCLOCK = value;

                    NotifyStaticPropertyChanged("CPU_CPUCLOCK");
                }
            }
        }

        private static string _CPU_CPUPOWER;
        public static string CPU_CPUPOWER
        {
            get { return _CPU_CPUPOWER; }
            set
            {
                if (value != _CPU_CPUPOWER)
                {
                    _CPU_CPUPOWER = value;

                    NotifyStaticPropertyChanged("CPU_CPUPOWER");
                }
            }
        }

        private static string _CPU_CPUTEMPERATURE;
        public static string CPU_CPUTEMPERATURE
        {
            get { return _CPU_CPUTEMPERATURE; }
            set
            {
                if (value != _CPU_CPUTEMPERATURE)
                {
                    _CPU_CPUTEMPERATURE = value;

                    NotifyStaticPropertyChanged("CPU_CPUTEMPERATURE");
                }
            }
        }

        private static double _CPU_CPUUSAGE;
        public static double CPU_CPUUSAGE
        {
            get { return _CPU_CPUUSAGE; }
            set
            {
                if (value != _CPU_CPUUSAGE)
                {
                    _CPU_CPUUSAGE = value;

                    NotifyStaticPropertyChanged("CPU_CPUUSAGE");
                }
            }
        }

        private static ObservableCollection<KeyValuePair<string, int>> _CPU_CHART_VALUES;
        public static ObservableCollection<KeyValuePair<string, int>> CPU_CHART_VALUES
        {
            get
            { return _CPU_CHART_VALUES; }
            set
            {
                if (value != _CPU_CHART_VALUES)
                {
                    _CPU_CHART_VALUES = value;

                    NotifyStaticPropertyChanged("CPU_CHART_VALUES");
                }
            }
        }
        #endregion

        #region RAM
        //RAM
        private static string _RAM_FREERAM;
        public static string RAM_FREERAM
        {
            get { return _RAM_FREERAM; }
            set
            {
                if (value != _RAM_FREERAM)
                {
                    _RAM_FREERAM = value;

                    NotifyStaticPropertyChanged("RAM_FREERAM");
                }
            }
        }

        private static string _RAM_USEDRAM;
        public static string RAM_USEDRAM
        {
            get { return _RAM_USEDRAM; }
            set
            {
                if (value != _RAM_USEDRAM)
                {
                    _RAM_USEDRAM = value;

                    NotifyStaticPropertyChanged("RAM_USEDRAM");
                }
            }
        }

        private static double _RAM_USAGE;
        public static double RAM_USAGE
        {
            get { return _RAM_USAGE; }
            set
            {
                if (value != _RAM_USAGE)
                {
                    _RAM_USAGE = value;

                    NotifyStaticPropertyChanged("RAM_USAGE");
                }
            }
        }
        #endregion

        #region Network
        //Network 
        private static string _NETWORK_DL_TOTAL;
        public static string NETWORK_DL_TOTAL
        {
            get { return _NETWORK_DL_TOTAL; }
            set
            {
                if (value != _NETWORK_DL_TOTAL)
                {
                    _NETWORK_DL_TOTAL = value;

                    NotifyStaticPropertyChanged("NETWORK_DL_TOTAL");
                }
            }
        }

        private static string _NETWORK_UL_TOTAL;
        public static string NETWORK_UL_TOTAL
        {
            get { return _NETWORK_UL_TOTAL; }
            set
            {
                if (value != _NETWORK_UL_TOTAL)
                {
                    _NETWORK_UL_TOTAL = value;

                    NotifyStaticPropertyChanged("NETWORK_UL_TOTAL");
                }
            }
        }

        private static string _NETWORK_DL_CURRENT;
        public static string NETWORK_DL_CURRENT
        {
            get { return _NETWORK_DL_CURRENT; }
            set
            {
                if (value != _NETWORK_DL_CURRENT)
                {
                    _NETWORK_DL_CURRENT = value;

                    NotifyStaticPropertyChanged("NETWORK_DL_CURRENT");
                }
            }
        }

        private static string _NETWORK_UL_CURRENT;
        public static string NETWORK_UL_CURRENT
        {
            get { return _NETWORK_UL_CURRENT; }
            set
            {
                if (value != _NETWORK_UL_CURRENT)
                {
                    _NETWORK_UL_CURRENT = value;

                    NotifyStaticPropertyChanged("NETWORK_UL_CURRENT");
                }
            }
        }

        private static ObservableCollection<KeyValuePair<string, int>> _NETWORK_CHART_VALUES;
        public static ObservableCollection<KeyValuePair<string, int>> NETWORK_CHART_VALUES
        {
            get
            { return _NETWORK_CHART_VALUES; }
            set
            {
                if (value != _NETWORK_CHART_VALUES)
                {
                    _NETWORK_CHART_VALUES = value;

                    NotifyStaticPropertyChanged("NETWORK_CHART_VALUES");
                }
            }
        }
        #endregion

        #region Disks 
 
        private static ObservableCollection<Disk> _DISKS;
        public static ObservableCollection<Disk> DISKS
        {
            get
            { return _DISKS; }
            set
            {
                if (value != _DISKS)
                {
                    _DISKS = value;

                    NotifyStaticPropertyChanged("DISKS");
                }
            }
        }

        #endregion

        #region GPUS

        private static ObservableCollection<GPU> _GPUS;
        public static ObservableCollection<GPU> GPUS
        {
            get
            { return _GPUS; }
            set
            {
                if (value != _GPUS)
                {
                    _GPUS = value;

                    NotifyStaticPropertyChanged("GPUS");
                }
            }
        }
        #endregion



    }

}
