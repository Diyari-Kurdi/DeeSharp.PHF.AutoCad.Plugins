using System.Collections.Generic;

namespace AcBoltedStorageTankGenereator.Models
{
    public class StorageTank
    {
        public List<RowModel> LeftAndRight { get; set; } = new List<RowModel>();
        public List<RowModel> FrontAndBack { get; set; } = new List<RowModel>();
    }
}
