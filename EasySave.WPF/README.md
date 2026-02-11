EasySave.WPF - simple WPF UI for EasySave 2.0

Features implemented in this scaffold:
- WPF application project targeting .NET 8
- Main window allowing adding jobs, starting/stopping all
- Job manager integrating CryptoProxy (loads CryptoSoft.dll if present)
- LogService supporting JSON and XML daily logs and cryptTime field

How to use:
- Build the solution
- Copy CryptoSoft.dll (from CryptoSoft project) next to the WPF exe if you want encryption
- Configure LogService and JobManager in your app bootstrap
