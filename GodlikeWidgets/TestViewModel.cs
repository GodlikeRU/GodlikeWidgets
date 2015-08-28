using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GodlikeWidgets.Structures;
using System.Collections.ObjectModel;

namespace GodlikeWidgets
{
    public class TestViewModel
    {
        public ObservableCollection<Disk> VM_GetDisks
        {
            get { return GodlikeWidgets.Data.Database.DISKS; }



        }
    }
}
