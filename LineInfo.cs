using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOEC_Index_Test
{
    public class LineInfo
    {

        public LineInfo(int _lid, string _foldername)
        {
            lid = _lid;
            FolderName = _foldername;
        }
        public int lid { get; set; }
        public string FolderName { get; set; }
    }
}
