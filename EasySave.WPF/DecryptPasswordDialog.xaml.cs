using System.Windows;
using System.Windows.Input;

namespace EasySave.WPF
{
    public partial class DecryptPasswordDialog : Window
    {
        public string Password { get; private set; } = string.Empty;

        public DecryptPasswordDialog()
        {
            InitializeComponent();
            ApplyLanguage();
            BtnOk.Click += (_, __) => Ok();
            BtnCancel.Click += (_, __) => { DialogResult = false; Close(); };
            PbPassword.KeyDown += (_, e) => { if (e.Key == Key.Enter) Ok(); };
        }

        void Ok()
        {
            if (string.IsNullOrEmpty(PbPassword.Password))
            {
                var lp = EasySave.Core.LanguageProvider.GetInstance();
                System.Windows.MessageBox.Show(lp.GetText("decrypt.key.missing"), lp.GetText("decrypt.prompt.title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Password = PbPassword.Password;
            DialogResult = true;
            Close();
        }

        void ApplyLanguage()
        {
            var lp = EasySave.Core.LanguageProvider.GetInstance();
            Title = lp.GetText("decrypt.prompt.title");
            TitleText.Text = lp.GetText("decrypt.prompt.title");
            LblPassword.Text = lp.GetText("decrypt.prompt.password");
            BtnOk.Content = lp.GetText("btn.decrypt");
            BtnCancel.Content = lp.GetText("button.cancel");
        }
    }
}
