using System.Collections.Generic;
using System.Linq;

namespace AcBoltedStorageTankGenereator.Models
{
    public class StorageTank
    {
        public List<RowModel> LeftAndRight { get; set; } = new List<RowModel>();
        public List<RowModel> FrontAndBack { get; set; } = new List<RowModel>();
        public double Length
            => LeftAndRight[0].RowWidth;
        public double Width
           => FrontAndBack[0].RowWidth;
        public double Height
           => LeftAndRight[0].RowHeight;
    }
}
