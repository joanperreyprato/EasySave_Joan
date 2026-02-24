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

        public EasyLog.LogFormat PreferredLogFormat { get; set; } = EasyLog.LogFormat.Json;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public string Source { get => _source; set { _source = value; OnPropertyChanged(nameof(Source)); } }
        public string Destination { get => _destination; set { _destination = value; OnPropertyChanged(nameof(Destination)); } }
        public string State { get => _state; set { _state = value; OnPropertyChanged(nameof(State)); } }
        public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
