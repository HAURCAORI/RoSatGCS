using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.Files
{
    class FileSizeHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetFileAttributesEx(string lpFileName, int fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);

        public static bool TryGetFileSize(string path, out long size, out bool isDirectory)
        {
            if (GetFileAttributesEx(path, 0, out var data))
            {
                isDirectory = (data.dwFileAttributes & FileAttributes.Directory) != 0;
                size = ((long)data.nFileSizeHigh << 32) + data.nFileSizeLow;
                return true;
            }

            size = 0;
            isDirectory = false;
            return false;
        }
    }

    public class FileConvertHelper
    {
        public static byte[] ToFixedAscii(string s, int k)
        {
            var buf = new byte[k];
            int n = Encoding.ASCII.GetBytes(s.AsSpan(), buf.AsSpan(1, 47));
            buf[0] = (byte)n;
            return buf;
        }

        public enum FManOpResult : byte
        {
            [Description("Succeeded")]
            OK = 0,

            [Description("A hard error occurred in the low level disk I/O layer")]
            DISK_ERR = 1,

            [Description("Assertion failed")]
            INT_ERR = 2,

            [Description("The physical drive cannot work")]
            NOT_READY = 3,

            [Description("Could not find the file")]
            NO_FILE = 4,

            [Description("Could not find the path")]
            NO_PATH = 5,

            [Description("The path name format is invalid")]
            INVALID_NAME = 6,

            [Description("Access denied due to prohibited access or directory full")]
            DENIED = 7,

            [Description("Access denied due to prohibited access")]
            EXIST = 8,

            [Description("The file/directory object is invalid")]
            INVALID_OBJECT = 9,

            [Description("The physical drive is write protected")]
            WRITE_PROTECTED = 10,

            [Description("The logical drive number is invalid")]
            INVALID_DRIVE = 11,

            [Description("The volume has no work area")]
            NOT_ENABLED = 12,

            [Description("There is no valid FAT volume")]
            NO_FILESYSTEM = 13,

            [Description("The f_mkfs() aborted due to any problem")]
            MKFS_ABORTED = 14,

            [Description("Could not get a grant to access the volume within defined period")]
            TIMEOUT = 15,

            [Description("The operation is rejected according to the file sharing policy")]
            LOCKED = 16,

            [Description("LFN working buffer could not be allocated")]
            NOT_ENOUGH_CORE = 17,

            [Description("Number of open files > _FS_LOCK")]
            TOO_MANY_OPEN_FILES = 18,

            [Description("Given parameter is invalid")]
            INVALID_PARAMETER = 19
        }
    }
}
