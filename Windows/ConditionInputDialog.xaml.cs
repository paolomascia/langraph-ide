using System.Windows;

namespace LangraphIDE.Windows
{
    public partial class ConditionInputDialog : Window
    {
        public string ConditionValue { get; private set; } = string.Empty;

        public ConditionInputDialog(string? defaultValue = null)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(defaultValue))
            {
                ConditionTextBox.Text = defaultValue;
            }

            ConditionTextBox.Focus();
            ConditionTextBox.SelectAll();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ConditionValue = ConditionTextBox.Text.Trim();

            if (string.IsNullOrEmpty(ConditionValue))
            {
                MessageBox.Show("Please enter a route name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
