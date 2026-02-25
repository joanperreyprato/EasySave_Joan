using System.Windows;
using System.IO;
using System.Windows.Input;

namespace EasySave.WPF
{
    public partial class AddJobWindow : Window
    {
        public EasySave.WPF.JobViewModel Job { get; private set; }

        public AddJobWindow()
        {
            InitializeComponent();
            ApplyLanguage(EasySave.Core.LanguageProvider.GetInstance());
            BtnOk.Click += (_,__) => Ok();
            BtnCancel.Click += (_,__) => Cancel();
            BtnBrowseSource.Click += (_,__) => BrowseSource();
            BtnBrowseDestination.Click += (_,__) => BrowseDestination();
        }

        void ApplyLanguage(EasySave.Core.LanguageProvider lp)
        {
            Title = lp.GetText("addjob.title");
            TitleText.Text = lp.GetText("addjob.title");
            DescriptionText.Text = lp.GetText("addjob.description");
            LabelName.Text = lp.GetText("addjob.name");
            LabelSource.Text = lp.GetText("addjob.source");
            LabelDestination.Text = lp.GetText("addjob.target");
            LabelLog.Text = lp.GetText("addjob.log");
            LabelType.Text = lp.GetText("addjob.type");
            if (CbItemFull != null) CbItemFull.Content = lp.GetText("backup.type.full");
            if (CbItemDifferential != null) CbItemDifferential.Content = lp.GetText("backup.type.differential");
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
            jobVm.BackupType = CbBackupType.SelectedIndex == 1 ? EasySave.Core.BackupType.Differential : EasySave.Core.BackupType.Full;
            Job = jobVm;
            DialogResult = true;
            Close();
        }

        void Cancel()
        {
            DialogResult = false;
            Close();
        }

        void BrowseSource()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = EasySave.Core.LanguageProvider.GetInstance().GetText("browse.select.source");
            if (Directory.Exists(TxtSource.Text)) dlg.SelectedPath = TxtSource.Text;
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                TxtSource.Text = dlg.SelectedPath;
            }
        }

        void BrowseDestination()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = EasySave.Core.LanguageProvider.GetInstance().GetText("browse.select.target");
            if (Directory.Exists(TxtDestination.Text)) dlg.SelectedPath = TxtDestination.Text;
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                TxtDestination.Text = dlg.SelectedPath;
            }
        }

        private void CbLogFormat_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                cb.Focus();
                if (!cb.IsDropDownOpen)
                    cb.IsDropDownOpen = true;
            }
        }

        private void CbBackupType_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                cb.Focus();
                if (!cb.IsDropDownOpen)
                    cb.IsDropDownOpen = true;
            }
        }
    }
}
