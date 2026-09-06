using System;
using BepInEx.Configuration;

// BepInEx build compatibility layer.
// House Access was written against the MelonLoader preferences API. In the
// shipped MelonLoader assembly those public types live directly in the
// "MelonLoader" namespace. This file re-provides that surface on top of a
// BepInEx ConfigFile so the rest of the mod source needed no churn.
// Nothing here touches MelonLoader itself.

namespace MelonLoader
{
    public static class MelonPreferences
    {
        private static ConfigFile _file;

        public static void Bind(ConfigFile file)
        {
            _file = file;
        }

        public static MelonPreferences_Category CreateCategory(string id, string displayName)
        {
            if (_file == null)
            {
                throw new InvalidOperationException("MelonPreferences.Bind must be called before CreateCategory.");
            }
            return new MelonPreferences_Category(_file, id);
        }

        public static void Save()
        {
            try
            {
                if (_file != null)
                {
                    _file.Save();
                }
            }
            catch
            {
            }
        }
    }

    public sealed class MelonPreferences_Category
    {
        private readonly ConfigFile _file;
        private readonly string _section;

        internal MelonPreferences_Category(ConfigFile file, string section)
        {
            _file = file;
            _section = section;
        }

        public MelonPreferences_Entry<T> CreateEntry<T>(string id, T def, string displayName = null, string description = null, bool isHidden = false, bool dontSave = false, ValueValidator validator = null, string defaultArray = null)
        {
            ConfigEntry<T> entry = _file.Bind<T>(_section, id, def, description);
            return new MelonPreferences_Entry<T>(entry);
        }
    }

    public sealed class MelonPreferences_Entry<T>
    {
        private readonly ConfigEntry<T> _entry;

        internal MelonPreferences_Entry(ConfigEntry<T> entry)
        {
            _entry = entry;
        }

        public T Value
        {
            get => _entry.Value;
            set => _entry.Value = value;
        }

        public T DefaultValue => (T)_entry.DefaultValue;

        public string DisplayName => _entry.Definition?.Key;
    }

    public class ValueValidator
    {
    }
}

// Some mod files carry "using MelonLoader.Preferences;" from older MelonLoader
// builds; keep the namespace addressable.
namespace MelonLoader.Preferences
{
}
