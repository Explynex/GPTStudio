using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.Infrastructure.Models;
internal class LanguageInfo
{
    public string CountryName { get; }
    public string SelectedSpeecher { get; set; }
    public bool Selected { get; set; }
    public LanguageInfo(string countryName)
    {
        CountryName = countryName;
    }

    public override string ToString() => CountryName;
}

internal class SpeecherInfo
{
    public string Gender { get; set; }
    public string ShortName { get; set; }
    public string DisplayName { get; set; }
    public string LocaleName { get; set; }

    public override string ToString() => $"{LocaleName} | {DisplayName} | {Gender}";
}
