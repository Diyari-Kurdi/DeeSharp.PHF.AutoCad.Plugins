using System.Collections.Generic;

namespace AcBoltedStorageTankGenereator.Models
{
    public class RowModel
    {
        public ICollection<Panel> Panels { get; set; } = new List<Panel>();
    }
}
