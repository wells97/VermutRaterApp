using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VermutRaterApp.Helpers;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding($"[{Key}]", source: LocalizationResourceManager.Instance);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
