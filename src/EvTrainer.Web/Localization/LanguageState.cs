namespace EvTrainer.Web.Localization;

/// <summary>Active UI language. Changing it re-renders the UI without reloading.</summary>
public sealed class LanguageState(LocalizationCatalog catalog)
{
    private string _current = LocalizationCatalog.DefaultLanguage;

    public event Action? Changed;

    public IReadOnlyList<string> Available => catalog.Languages;

    /// <summary>Active language; unknown languages are ignored.</summary>
    public string Current
    {
        get => _current;
        set
        {
            if (value == _current || !catalog.Languages.Contains(value))
            {
                return;
            }

            _current = value;
            Changed?.Invoke();
        }
    }
}