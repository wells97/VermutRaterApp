using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace VermutRaterApp.Helpers;

public class LocalizationResourceManager : INotifyPropertyChanged
{
    private static LocalizationResourceManager _instance;
    private ResourceManager _resourceManager;

    public static LocalizationResourceManager Instance => _instance ??= new LocalizationResourceManager();

    public event PropertyChangedEventHandler PropertyChanged;

    private LocalizationResourceManager()
    {
        _resourceManager = CrearResourceManager();
    }

    public string this[string key]
        => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    public void SetCulture(CultureInfo culture)
    {
        if (culture == null) return;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Reinicialitza el ResourceManager per assegurar lectura correcta
        _resourceManager = CrearResourceManager();

        OnPropertyChanged(null);
    }

    private ResourceManager CrearResourceManager()
    {
        return new ResourceManager("VermutRaterApp.Resources.Strings.AppResources", typeof(LocalizationResourceManager).Assembly);
    }

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
