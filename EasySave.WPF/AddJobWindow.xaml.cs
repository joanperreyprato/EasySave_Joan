using System.Windows;

namespace EasySave.WPF
{
    public partial class AddJobWindow : Window
    {
        public EasySave.WPF.JobViewModel Job { get; private set; }

        public AddJobWindow()
        {
            InitializeComponent();
            ApplyLanguage(EasySave_v1._0.LanguageProvider.GetInstance());
            BtnOk.Click += (_,__) => Ok();
            BtnCancel.Click += (_,__) => Cancel();
        }

        void ApplyLanguage(EasySave_v1._0.LanguageProvider lp)
        {
            Title = lp.GetText("addjob.title");
            TitleText.Text = lp.GetText("addjob.title");
            DescriptionText.Text = lp.GetText("addjob.description");
            LabelName.Text = lp.GetText("addjob.name");
            LabelSource.Text = lp.GetText("addjob.source");
            LabelDestination.Text = lp.GetText("addjob.target");
            LabelLog.Text = lp.GetText("addjob.log");
            BtnOk.Content = lp.GetText("button.ok");
            BtnCancel.Content = lp.GetText("button.cancel");
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
