using System.ComponentModel;
using EasyLog;

namespace EasySave.WPF
{
    public class JobViewModel : INotifyPropertyChanged
    {
        string _name = string.Empty;
        string _source = string.Empty;
        string _destination = string.Empty;
        string _state = string.Empty;
        double _progress = 0.0;
        bool _isSelected = false;

        public EasyLog.LogFormat PreferredLogFormat { get; set; } = EasyLog.LogFormat.Json;
        public EasySave.Core.BackupType BackupType { get; set; } = EasySave.Core.BackupType.Full;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public string Source { get => _source; set { _source = value; OnPropertyChanged(nameof(Source)); } }
        public string Destination { get => _destination; set { _destination = value; OnPropertyChanged(nameof(Destination)); } }
        public string State { get => _state; set { _state = value; OnPropertyChanged(nameof(State)); } }
        public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
