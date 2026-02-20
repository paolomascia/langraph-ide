using System;
using System.Windows;

namespace LangraphIDE
{
    public enum AppTheme
    {
        Dark,
        Light
    }

    public static class ThemeManager
    {
        private static AppTheme _currentTheme = AppTheme.Dark;

        public static AppTheme CurrentTheme => _currentTheme;

        public static void Initialize()
        {
            ApplyTheme(_currentTheme);
        }

        public static void ApplyTheme(AppTheme theme)
        {
            _currentTheme = theme;

            var themeUri = theme switch
            {
                AppTheme.Light => new Uri("pack://application:,,,/LangraphIDE;component/Themes/LightTheme.xaml"),
                _ => new Uri("pack://application:,,,/LangraphIDE;component/Themes/DarkTheme.xaml")
            };

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            ResourceDictionary? existingTheme = null;
            foreach (var dict in mergedDictionaries)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("Theme.xaml"))
                {
                    existingTheme = dict;
                    break;
                }
            }

            if (existingTheme != null)
                mergedDictionaries.Remove(existingTheme);

            mergedDictionaries.Insert(0, new ResourceDictionary { Source = themeUri });
        }

        public static void ToggleTheme()
        {
            var newTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(newTheme);
        }
    }
}
