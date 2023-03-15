using System.Globalization;

using CsvHelper;

namespace ResxEditor.Service;

public class CustomCultureInfo
{
    public string Name { get; set; }
    public string TwoLetterIsoLanguageName { get; set; }
    public string EnglishName { get; set; }
}

public class CultureInfoService
{
    private readonly HttpClient _httpClient;
    private IReadOnlyList<CustomCultureInfo> _cultureInfos;

    public CultureInfoService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        LoadData();
    }

    private async void LoadData()
    {
        var content = await _httpClient.GetStringAsync("cultureinfo.csv");
        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, CultureInfo.CurrentCulture);

        _cultureInfos = csv.GetRecords<CustomCultureInfo>().ToList();
    }

    /// <summary>
    /// Retrieves a <see cref="CultureInfo"/> instance
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public CustomCultureInfo GetCultureInfo(string name)
    {
        return _cultureInfos.FirstOrDefault(x => x.Name == name) ??
               _cultureInfos.FirstOrDefault(x => x.TwoLetterIsoLanguageName == name);
    }
}
