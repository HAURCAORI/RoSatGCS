using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Models
{
    public class RemoteFileModel : ObservableObject
    {
        private string _name = "";
        private string _path = "";
        private long _size = 0;
        private long _timestamp = 0;
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Path { get => _path; set => SetProperty(ref _path, value); }
        public long Size { get => _size; set => SetProperty(ref _size, value); }
        public long Timestamp { get => _timestamp; set => SetProperty(ref _timestamp, value); }

        public RemoteFileModel() { }
        public RemoteFileModel(string name, string path, long size, long timestamp)
        {
            _name = name;
            _path = path;
            _size = size;
            _timestamp = timestamp;
        }
    }
}
