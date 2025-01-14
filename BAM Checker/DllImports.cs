using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAM_Checker
{
    class DllImports
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetLogicalDriveStrings(int nBufferLength, char[] lpBuffer);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int QueryDosDevice(string lpDeviceName, string lpTargetPath, int ucchMax);
    }
}
