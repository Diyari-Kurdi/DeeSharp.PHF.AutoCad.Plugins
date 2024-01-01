using System.Collections.Generic;
using System.Linq;

namespace AcBoltedStorageTankGenerator.Models
{
    public class RowModel
    {
        public ICollection<Panel> Panels { get; set; } = new List<Panel>();

        public double RowWidth
        {
            get
            {
                var x = Panels.Sum(p => p.Width);
                return x;
            }
        }


        public double RowHeight
            => Panels.FirstOrDefault().Height;
    }
}
