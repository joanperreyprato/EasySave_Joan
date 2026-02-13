using System.Windows;

namespace EasySave.WPF
{
    public partial class AddJobWindow : Window
    {
        public EasySave.WPF.JobViewModel Job { get; private set; }

        public AddJobWindow()
        {
            InitializeComponent();
            BtnOk.Click += (_,__) => Ok();
            BtnCancel.Click += (_,__) => Cancel();
        }

        void Ok()
        {
            var jobVm = new EasySave.WPF.JobViewModel
            {
                Name = TxtName.Text,
                Source = TxtSource.Text,
                Destination = TxtDestination.Text,
                State = "Stopped"
            };

            // read log preference
            var format = EasyLog.LogFormat.Json;
            if (CbLogFormat.SelectedItem is System.Windows.Controls.ComboBoxItem it && it.Content.ToString() == "XML")
                format = EasyLog.LogFormat.Xml;

            jobVm.PreferredLogFormat = format;
            Job = jobVm;
            DialogResult = true;
            Close();
        }

        void Cancel()
        {
            DialogResult = false;
            Close();
        }
    }
}
