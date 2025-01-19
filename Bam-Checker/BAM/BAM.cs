using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BamChecker.UI;
using Microsoft.Win32;

namespace BamChecker.BAM
{
    internal class BAM
    {
        public BAM() { }

        public static BamEntry[] getBamEntries()
        {
            string subkey = @"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings";
            using (RegistryKey localMachine64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                try
                {
                    using (RegistryKey key = localMachine64.OpenSubKey(subkey))
                    {
                        if (key == null)
                        {
                            Pages.Error("Registry key not found.");
                            return new BamEntry[] { };
                        }

                        string[] subKeys = key.GetSubKeyNames();
                        string bamKeyName = subKeys.OrderByDescending(k => k.Length).First();
                        if (string.IsNullOrEmpty(bamKeyName))
                        {
                            Pages.Error("No sub keys found.");
                            return new BamEntry[] { };
                        }

                        RegistryKey bamKey = key.OpenSubKey(bamKeyName);
                        string[] values = bamKey.GetValueNames();

                        List<BamEntry> entries = new List<BamEntry>();
                        foreach (string value in values)
                        {
                            if (string.IsNullOrEmpty(value)) continue;

                            string localTime = "";
                            string utcTime = "";
                            DateTime localTimeDate = DateTime.Now;
                            DateTime utcTimeDate = DateTime.Now;

                            if (bamKey.GetValue(value) is byte[] binaryValue)
                            {
                                try
                                {
                                    DllImports.FILETIME fileTime = BytesToFileTime(binaryValue);
                                    utcTimeDate = DateTime.FromFileTimeUtc(BitConverter.ToInt64(new byte[] {
                                    (byte)(fileTime.dwLowDateTime & 0xFF),
                                    (byte)((fileTime.dwLowDateTime >> 8) & 0xFF),
                                    (byte)((fileTime.dwLowDateTime >> 16) & 0xFF),
                                    (byte)((fileTime.dwLowDateTime >> 24) & 0xFF),
                                    (byte)(fileTime.dwHighDateTime & 0xFF),
                                    (byte)((fileTime.dwHighDateTime >> 8) & 0xFF),
                                    (byte)((fileTime.dwHighDateTime >> 16) & 0xFF),
                                    (byte)((fileTime.dwHighDateTime >> 24) & 0xFF)
                                }, 0));
                                    localTimeDate = DateTime.FromFileTime(BitConverter.ToInt64(new byte[] {
                                    (byte)(fileTime.dwLowDateTime & 0xFF),
                                    (byte)((fileTime.dwLowDateTime >> 8) & 0xFF),
                                    (byte)((fileTime.dwLowDateTime >> 16) & 0xFF),
                                    (byte)((fileTime.dwLowDateTime >> 24) & 0xFF),
                                    (byte)(fileTime.dwHighDateTime & 0xFF),
                                    (byte)((fileTime.dwHighDateTime >> 8) & 0xFF),
                                    (byte)((fileTime.dwHighDateTime >> 16) & 0xFF),
                                    (byte)((fileTime.dwHighDateTime >> 24) & 0xFF)
                                }, 0));

                                    utcTime = $"{utcTimeDate:yyyy-MM-dd HH:mm:ss}";
                                    localTime = $"{localTimeDate:yyyy-MM-dd HH:mm:ss}";
                                }
                                catch
                                {
                                    utcTime = "";
                                    localTime = "";
                                    localTimeDate = DateTime.Now;
                                    utcTimeDate = DateTime.Now;
                                }
                            }

                            string path = value;

                            if (path.StartsWith(@"\Device\"))
                            {
                                string[] pathNoLetterFull = value.Split('\\').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                string[] pathNoLetter = new string[pathNoLetterFull.Length - 2];
                                Array.Copy(pathNoLetterFull, 2, pathNoLetter, 0, pathNoLetter.Length);

                                path = $@"{ConvertHardDiskVolumeToLetter(value)}\{string.Join(@"\", pathNoLetter)}";

                            }

                            BamEntry entry = new BamEntry(path, value, utcTime, localTime, utcTimeDate, localTimeDate);
                            entries.Add(entry);
                        }

                        return entries.ToArray();
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Pages.Error($"Access error: {ex.Message}");
                    return new BamEntry[] { };
                }
                catch (Exception ex)
                {
                    Pages.Error($"Access error: {ex.Message}");
                    return new BamEntry[] { };
                }
            }
        }

        public static string ConvertHardDiskVolumeToLetter(string path)
        {
            const int MAX_PATH = 260;
            char[] drives = new char[MAX_PATH];

            // Recupera i drive logici
            if (DllImports.GetLogicalDriveStrings(MAX_PATH, drives) > 0)
            {
                StringBuilder volumeName = new StringBuilder(MAX_PATH);
                char[] driveLetter = new char[] { ' ', ':' };

                // Scorre i drive
                for (int i = 0; i < drives.Length; i += 4)
                {
                    driveLetter[0] = drives[i];

                    // Query sul volume per ottenere il nome del volume
                    if (DllImports.QueryDosDevice(new string(driveLetter), volumeName, MAX_PATH) > 0)
                    {
                        string volPath = path;
                        string volName = volumeName.ToString();

                        // Controlla se il percorso del volume corrisponde a quello cercato
                        if (volPath.StartsWith(volName))
                        {
                            return driveLetter[0] + ":";
                        }

                        string globalRootPrefix = @"\\?\GLOBALROOT";
                        if (volPath.StartsWith(globalRootPrefix))
                        {
                            volPath = volPath.Substring(globalRootPrefix.Length);
                            if (volPath.StartsWith(volName))
                            {
                                return driveLetter[0] + ":";
                            }
                        }
                    }
                }
            }

            return "?:"; // Restituisce un valore di default se non trova il volume
        }

        public static bool CheckIfFile(string value)
        {
            return value.StartsWith("\\Device\\HarddiskVolume");
        }
        public static DllImports.FILETIME BytesToFileTime(byte[] data)
        {


            DllImports.FILETIME fileTime = new DllImports.FILETIME
            {
                dwLowDateTime = BitConverter.ToUInt32(data, 0),
                dwHighDateTime = BitConverter.ToUInt32(data, 4)
            };
            return fileTime;
        }
    }

    public class BamEntry
    {
        public string Name { get; set; }
        public string BAM_Path { get; set; }
        public string UTC_Time { get; set; }
        public string Local_Time { get; set; }
        public DateTime UTC_Time_Date { get; set; }
        public DateTime Local_Time_Date { get; set; }
        public bool Is_In_Session { get; set; }
        public string Session_Text { get; set; }
        public string Signature { get; set; }

        public BamEntry(string name, string bam_path, string utc_time, string local_time, DateTime utc_time_date, DateTime local_time_date)
        {
            this.Name = name;
            this.BAM_Path = bam_path;
            this.UTC_Time = utc_time;
            this.Local_Time = local_time;
            this.UTC_Time_Date = utc_time_date;
            this.Local_Time_Date = local_time_date;

            this.Is_In_Session = this.Local_Time_Date >= MainWindow.sessionDate && this.Local_Time_Date <= DateTime.Now;
            this.Session_Text = this.Is_In_Session ? "In Session" : "Not In Session";

            this.Signature = GetSignatureStatus(this.Name);
        }

        // signature
        public string GetSignatureStatus(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "Deleted";
            }

            X509Certificate2 cert = GetCertificateFromFile(filePath);
            if (cert == null)
            {
                if (CheckCatalogSignatures(filePath)) return "Signed";

                return "Not signed";
            }

            string subject = cert.Subject.ToLower();
            if (subject.Contains("manthe industries, llc") || subject.Contains("slinkware"))
            {
                return "Not signed";
            }

            return "Signed";
        }

        private X509Certificate2 GetCertificateFromFile(string filePath)
        {
            try
            {
                var signer = new X509Certificate2(filePath);
                return signer;
            }
            catch
            {
                return null;
            }
        }

        public bool CheckCatalogSignatures(string filePath)
        {
            IntPtr hCatAdmin = IntPtr.Zero;
            if (DllImports.CryptCATAdminAcquireContext(ref hCatAdmin, null, 0) == IntPtr.Zero)
                return false;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                uint dwHashSize = 0;
                if (!DllImports.CryptCATAdminCalcHashFromFileHandle(fileStream.SafeFileHandle.DangerousGetHandle(), ref dwHashSize, null, 0))
                {
                    DllImports.CryptCATAdminReleaseContext(hCatAdmin, 0);
                    return false;
                }

                byte[] pbHash = new byte[dwHashSize];
                if (!DllImports.CryptCATAdminCalcHashFromFileHandle(fileStream.SafeFileHandle.DangerousGetHandle(), ref dwHashSize, pbHash, 0))
                {
                    DllImports.CryptCATAdminReleaseContext(hCatAdmin, 0);
                    return false;
                }

                DllImports.CATALOG_INFO catInfo = new DllImports.CATALOG_INFO();
                catInfo.cbStruct = (uint)Marshal.SizeOf(typeof(DllImports.CATALOG_INFO));

                IntPtr hCatInfo = IntPtr.Zero;
                bool isCatalogSigned = false;

                hCatInfo = DllImports.CryptCATAdminEnumCatalogFromHash(hCatAdmin, pbHash, dwHashSize, 0, ref hCatInfo);
                while (hCatInfo != IntPtr.Zero)
                {
                    DllImports.CryptCATCatalogInfoFromContext(hCatInfo, ref catInfo, 0);

                    Guid action = new Guid("6fdd6a3a-bce0-4cd5-bc8c-0ff659ac5506");
                    DllImports.WINTRUST_DATA wtd = new DllImports.WINTRUST_DATA
                    {
                        cbStruct = (uint)Marshal.SizeOf(typeof(DllImports.WINTRUST_DATA)),
                        dwUIChoice = 2,
                        fdwRevocationChecks = 0, 
                        dwUnionChoice = 1,
                        dwStateAction = 0,
                        pCatalog = new DllImports.WINTRUST_CATALOG_INFO
                        {
                            cbStruct = (uint)Marshal.SizeOf(typeof(DllImports.WINTRUST_CATALOG_INFO)),
                            pcwszCatalogFilePath = catInfo.wszCatalogFile,
                            pbCalculatedFileHash = pbHash,
                            cbCalculatedFileHash = dwHashSize,
                            pcwszMemberFilePath = filePath
                        },
                        dwProvFlags = 0
                    };

                    uint res = DllImports.WinVerifyTrust(IntPtr.Zero, ref action, ref wtd);
                    if (res == 0)
                    {
                        isCatalogSigned = true;
                        break;
                    }
                    else
                    {
                        isCatalogSigned = true;
                        break;
                    }

                    // hCatInfo = CryptCATAdminEnumCatalogFromHash(hCatAdmin, pbHash, dwHashSize, 0, ref hCatInfo);
                }

                DllImports.CryptCATAdminReleaseContext(hCatAdmin, 0);
                return isCatalogSigned;
            }
        }
    }
}
