using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;

namespace PumpSimulator
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Transaction> unprocessedTransactions;
        private ComboBox pumpComboBox;
        private ComboBox fuelTypeComboBox;
        private TextBox attendantTextBox;
        private CheckBox processedCheckBox;
        private NumericUpDown litersNumericUpDown;
        private Button submitButton;
        private ListBox transactionsListBox;
        private TextBlock totalUnprocessedTextBlock;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            unprocessedTransactions = new ObservableCollection<Transaction>();
            this.Loaded += MainWindow_Loaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            pumpComboBox = this.FindControl<ComboBox>("PumpComboBox");
            fuelTypeComboBox = this.FindControl<ComboBox>("FuelTypeComboBox");
            attendantTextBox = this.FindControl<TextBox>("AttendantTextBox");
            processedCheckBox = this.FindControl<CheckBox>("ProcessedCheckBox");
            litersNumericUpDown = this.FindControl<NumericUpDown>("LitersNumericUpDown");
            submitButton = this.FindControl<Button>("SubmitButton");
            transactionsListBox = this.FindControl<ListBox>("TransactionsListBox");
            totalUnprocessedTextBlock = this.FindControl<TextBlock>("TotalUnprocessedTextBlock");

            if (transactionsListBox != null)
                transactionsListBox.ItemsSource = unprocessedTransactions;

            if (pumpComboBox != null)
                pumpComboBox.ItemsSource = new[] { "1", "2", "3", "4" };

            if (fuelTypeComboBox != null)
                fuelTypeComboBox.ItemsSource = new[] { "Regular", "Premium", "Diesel" };

            if (submitButton != null)
                submitButton.Click += SubmitButton_Click;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var transaction = new Transaction
            {
                Pump = pumpComboBox?.SelectedItem?.ToString(),
                Attendant = attendantTextBox?.Text,
                FuelType = fuelTypeComboBox?.SelectedItem?.ToString(),
                Processed = processedCheckBox?.IsChecked ?? false,
                Liters = litersNumericUpDown?.Value ?? 0
            };

            if (!transaction.Processed)
            {
                unprocessedTransactions.Add(transaction);
                UpdateTotalUnprocessed();
            }

            ClearInputs();
        }

        private void UpdateTotalUnprocessed()
        {
            if (totalUnprocessedTextBlock != null)
                totalUnprocessedTextBlock.Text = unprocessedTransactions.Count.ToString();
        }

        private void ClearInputs()
        {
            if (pumpComboBox != null) pumpComboBox.SelectedIndex = -1;
            if (attendantTextBox != null) attendantTextBox.Text = "";
            if (fuelTypeComboBox != null) fuelTypeComboBox.SelectedIndex = -1;
            if (processedCheckBox != null) processedCheckBox.IsChecked = false;
            if (litersNumericUpDown != null) litersNumericUpDown.Value = 0;
        }
    }

    public class Transaction
    {
        public string Pump { get; set; }
        public string Attendant { get; set; }
        public string FuelType { get; set; }
        public bool Processed { get; set; }
        public decimal Liters { get; set; }

        public override string ToString()
        {
            return $"Pump: {Pump}, Attendant: {Attendant}, Fuel: {FuelType}, Liters: {Liters}, Processed: {Processed}";
        }
    }
}