using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Models
{
    public enum FileDownloadStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
    public class FileDownloadModel : ObservableObject
    {
        private string _name = string.Empty;
        public string Name { get { return _name; } set { SetProperty(ref _name, value); } }
        private string _path = string.Empty;
        public string Path { get { return _path; } set { SetProperty(ref _path, value); } }

        private FileDownloadStatus _status = FileDownloadStatus.Pending;
        public FileDownloadStatus Status { get { return _status; } set { SetProperty(ref _status, value); OnPropertyChanged(nameof(StatusString)); } }

        public string StatusString
        {
            get
            {
                return Status.ToString();
            }
        }

        private string _executedTime = string.Empty;
        public string ExecutedTime { get { return _executedTime; } set { SetProperty(ref _executedTime, value); } }

        private string _info = string.Empty;
        public string Info { get { return _info; } set { SetProperty(ref _info, value); } }


    }
}
