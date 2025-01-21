using System.Runtime.InteropServices;
using System;
using System.Text;

namespace BamChecker
{
    internal class DllImports
    {
        // vars
        public const uint SHGFI_ICON = 0x000000100;      // medium
        public const uint SHGFI_LARGEICON = 0x000000000; // large
        public const uint SHGFI_SMALLICON = 0x000000001; // small

        // func
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int GetLogicalDriveStrings(int nBufferLength, char[] lpBuffer);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CryptCATAdminAcquireContext(ref IntPtr phCatAdmin, string pgwszCachedCat, int dwFlags);

        [DllImport("wintrust.dll", SetLastError = true)]
        public static extern bool CryptCATAdminReleaseContext(IntPtr hCatAdmin, int dwFlags);

        [DllImport("wintrust.dll", SetLastError = true)]
        public static extern bool CryptCATAdminCalcHashFromFileHandle(IntPtr hFile, ref uint pdwHashSize, byte[] pbHash, uint dwFlags);

        [DllImport("wintrust.dll", SetLastError = true)]
        public static extern IntPtr CryptCATAdminEnumCatalogFromHash(IntPtr hCatAdmin, byte[] pbHash, uint dwHashSize, uint dwFlags, ref IntPtr phCatalog);

        [DllImport("wintrust.dll", SetLastError = true)]
        public static extern bool CryptCATCatalogInfoFromContext(IntPtr hCatalog, ref CATALOG_INFO catInfo, uint dwFlags);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint WinVerifyTrust(IntPtr hWnd, ref Guid pgActionID, ref WINTRUST_DATA pWinTrustData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, int dwFlags);

        [DllImport("dbghelp.dll")]
        public static extern IntPtr ImageNtHeader(IntPtr module);

        [DllImport("dbghelp.dll")]
        public static extern IntPtr ImageDirectoryEntryToData(IntPtr baseAddress, [MarshalAs(UnmanagedType.Bool)] bool mappedAsImage, int directoryEntry, out uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        // struct
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcwszFilePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct GUID
        {
            public uint Data1;
            public ushort Data2;
            public ushort Data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WintrustData
        {
            public uint cbStruct;
            public IntPtr pFile;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pCatalog;
            public uint dwStateAction;
            public uint dwProvFlags;
            public uint dwUIContext;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WintrustFileInfo
        {
            public uint cbStruct;
            public string pcwszFilePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CATALOG_INFO
        {
            public uint cbStruct;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] // MAX_PATH
            public string wszCatalogFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINTRUST_DATA
        {
            public uint cbStruct;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public uint dwStateAction;
            public WINTRUST_CATALOG_INFO pCatalog;
            public uint dwProvFlags;  // Questo campo deve essere aggiunto come nel codice C++
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINTRUST_CATALOG_INFO
        {
            public uint cbStruct;
            public string pcwszCatalogFilePath;
            public byte[] pbCalculatedFileHash;
            public uint cbCalculatedFileHash;
            public string pcwszMemberFilePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_IMPORT_DESCRIPTOR
        {
            public uint OriginalFirstThunk;
            public uint TimeDateStamp;
            public uint ForwarderChain;
            public uint Name;
            public uint FirstThunk;
        }
    }
}
