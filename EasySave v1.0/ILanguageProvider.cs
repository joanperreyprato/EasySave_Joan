using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave_v1._0
{
    public interface ILanguageProvider
    {
        string GetText(string key);
        void SetLanguage(string languageCode);
    }
}
