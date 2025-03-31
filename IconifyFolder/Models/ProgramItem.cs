using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Drawing;

namespace IconifyFolder.Models
{
    public class ProgramItem
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public Icon Icon { get; set; }
        public bool IsSelected { get; set; }
    }
}
