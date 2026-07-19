// shopping_cs.cs — совместный список покупок на C# (WPF)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ShopSyncWPF
{
    public class Item : INotifyPropertyChanged
    {
        private string _name;
        private int _quantity;
        private double _price;
        private string _category;
        private string _expiry;
        private bool _purchased;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); } }
        public double Price { get => _price; set { _price = value; OnPropertyChanged(); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(); } }
        public string Expiry { get => _expiry; set { _expiry = value; OnPropertyChanged(); } }
        public bool Purchased { get => _purchased; set { _purchased = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<Item> items = new ObservableCollection<Item>();
        private string dataFile = "shoplist.json";
        private ICollectionView view;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            CreateUI();
            RefreshView();
        }

        private void CreateUI()
        {
            Title = "🛒 ShopSync — C#";
            Width = 900;
            Height = 650;
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Панель инструментов
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal };
            var addBtn = new Button { Content = "Добавить", Width = 80 };
            var editBtn = new Button { Content = "Редактировать", Width = 80 };
            var delBtn = new Button { Content = "Удалить", Width = 80 };
            var buyBtn = new Button { Content = "Куплен/Возврат", Width = 100 };
            var statsBtn = new Button { Content = "Статистика", Width = 80 };
            var exportBtn = new Button { Content = "Экспорт", Width = 80 };
            var importBtn = new Button { Content = "Импорт", Width = 80 };
            toolbar.Children.Add(addBtn);
            toolbar.Children.Add(editBtn);
            toolbar.Children.Add(delBtn);
            toolbar.Children.Add(buyBtn);
            toolbar.Children.Add(statsBtn);
            toolbar.Children.Add(exportBtn);
            toolbar.Children.Add(importBtn);
            Grid.SetRow(toolbar, 0);
            grid.Children.Add(toolbar);

            // Фильтры
            var filterPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            filterPanel.Children.Add(new Label { Content = "Поиск:" });
            var searchBox = new TextBox { Width = 150 };
            searchBox.TextChanged += (s, e) => RefreshView();
            filterPanel.Children.Add(searchBox);
            filterPanel.Children.Add(new Label { Content = "Категория:" });
            var catCombo = new ComboBox { Width = 120 };
            catCombo.SelectionChanged += (s, e) => RefreshView();
            filterPanel.Children.Add(catCombo);
            var resetBtn = new Button { Content = "Сбросить", Margin = new Thickness(5,0,0,0) };
            resetBtn.Click += (s, e) => { searchBox.Text = ""; catCombo.SelectedIndex = -1; };
            filterPanel.Children.Add(resetBtn);
            Grid.SetRow(filterPanel, 1);
            grid.Children.Add(filterPanel);

            // Таблица (DataGrid)
            var dg = new DataGrid();
            dg.AutoGenerateColumns = false;
            dg.CanUserAddRows = false;
            dg.IsReadOnly = true;
            dg.SelectionMode = DataGridSelectionMode.Single;
            dg.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Name") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Кол-во", Binding = new Binding("Quantity") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new Binding("Price") { StringFormat = "{0:F2}" } });
            dg.Columns.Add(new DataGridTextColumn { Header = "Категория", Binding = new Binding("Category") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Срок", Binding = new Binding("Expiry") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Куплено", Binding = new Binding("Purchased") { Converter = new BooleanToCheckConverter() } });
            dg.MouseDoubleClick += (s, e) => EditItem();
            Grid.SetRow(dg, 2);
            grid.Children.Add(dg);

            // Статус
            var status = new Label { Content = "Готов" };
            Grid.SetRow(status, 3);
            grid.Children.Add(status);

            Content = grid;

            // Сохранение ссылок
            this.FindName("searchBox", searchBox);
            this.FindName("catCombo", catCombo);
            this.FindName("dg", dg);
            this.FindName("status", status);

            // Обработчики
            addBtn.Click += (s, e) => AddItem();
            editBtn.Click += (s, e) => EditItem();
            delBtn.Click += (s, e) => DeleteItem();
            buyBtn.Click += (s, e) => TogglePurchased();
            statsBtn.Click += (s, e) => ShowStats();
            exportBtn.Click += (s, e) => ExportData();
            importBtn.Click += (s, e) => ImportData();
        }

        private void AddItem()
        {
            var dialog = new ItemDialog();
            if (dialog.ShowDialog() == true)
            {
                items.Add(dialog.Result);
                SaveData();
                RefreshView();
                statusLabel.Content = "Добавлен: " + dialog.Result.Name;
            }
        }

        private void EditItem()
        {
            var item = dg.SelectedItem as Item;
            if (item == null) return;
            var dialog = new ItemDialog(item);
            if (dialog.ShowDialog() == true)
            {
                // Обновляем свойства (уже изменены в диалоге через привязку)
                SaveData();
                RefreshView();
                statusLabel.Content = "Обновлён: " + item.Name;
            }
        }

        private void DeleteItem()
        {
            var item = dg.SelectedItem as Item;
            if (item == null) return;
            if (MessageBox.Show($"Удалить '{item.Name}'?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                items.Remove(item);
                SaveData();
                RefreshView();
                statusLabel.Content = "Удалён: " + item.Name;
            }
        }

        private void TogglePurchased()
        {
            var item = dg.SelectedItem as Item;
            if (item == null) return;
            item.Purchased = !item.Purchased;
            SaveData();
            RefreshView();
            statusLabel.Content = (item.Purchased ? "Куплен" : "Возвращён") + ": " + item.Name;
        }

        private void ShowStats()
        {
            int total = items.Count;
            int bought = items.Count(i => i.Purchased);
            double totalPrice = items.Sum(i => i.Price * i.Quantity);
            double boughtPrice = items.Where(i => i.Purchased).Sum(i => i.Price * i.Quantity);
            string msg = $"Всего товаров: {total}\nКуплено: {bought} ({bought*100.0/total:F1}%)\nОбщая стоимость: {totalPrice:F2} руб.\nКуплено на: {boughtPrice:F2} руб.";
            MessageBox.Show(msg, "Статистика");
        }

        private void ExportData()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "JSON (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json);
                statusLabel.Content = "Экспортировано в " + dialog.FileName;
            }
        }

        private void ImportData()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "JSON (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                string json = File.ReadAllText(dialog.FileName);
                var imported = JsonSerializer.Deserialize<List<Item>>(json);
                if (imported != null)
                {
                    foreach (var it in imported) items.Add(it);
                    SaveData();
                    RefreshView();
                    statusLabel.Content = "Импортировано из " + dialog.FileName;
                }
            }
        }

        private void RefreshView()
        {
            // Применяем фильтры
            string search = searchBox.Text?.Trim().ToLower() ?? "";
            string cat = catCombo.SelectedItem as string;
            var filtered = items.Where(i =>
                (string.IsNullOrEmpty(search) || i.Name.ToLower().Contains(search)) &&
                (string.IsNullOrEmpty(cat) || i.Category == cat)
            ).ToList();
            dg.ItemsSource = filtered;
            UpdateStatus();
            UpdateCategories();
        }

        private void UpdateStatus()
        {
            int total = items.Count;
            int bought = items.Count(i => i.Purchased);
            statusLabel.Content = $"Всего: {total} | Куплено: {bought}";
        }

        private void UpdateCategories()
        {
            string current = catCombo.SelectedItem as string;
            catCombo.Items.Clear();
            catCombo.Items.Add("");
            var cats = items.Select(i => i.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c);
            foreach (var c in cats) catCombo.Items.Add(c);
            if (!string.IsNullOrEmpty(current) && catCombo.Items.Contains(current)) catCombo.SelectedItem = current;
            else catCombo.SelectedIndex = 0;
        }

        private void LoadData()
        {
            if (File.Exists(dataFile))
            {
                string json = File.ReadAllText(dataFile);
                var list = JsonSerializer.Deserialize<List<Item>>(json);
                if (list != null) foreach (var it in list) items.Add(it);
            }
        }

        private void SaveData()
        {
            string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dataFile, json);
        }

        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }

    public class ItemDialog : Window
    {
        public Item Result { get; private set; }

        public ItemDialog(Item editItem = null)
        {
            Title = editItem == null ? "Добавить товар" : "Редактировать товар";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameBox = new TextBox();
            var qtyBox = new TextBox();
            var priceBox = new TextBox();
            var catBox = new TextBox();
            var expiryBox = new TextBox();

            grid.Children.Add(new Label { Content = "Название:", Margin = new Thickness(5) });
            Grid.SetRow(grid.Children[grid.Children.Count-1], 0);
            Grid.SetColumn(grid.Children[grid.Children.Count-1], 0);
            grid.Children.Add(nameBox);
            Grid.SetRow(grid.Children[grid.Children.Count-1], 0);
            Grid.SetColumn(grid.Children[grid.Children.Count-1], 1);

            // ... аналогично для остальных полей
            // Для краткости опущено, но в реальном коде нужно добавить все поля.
            // Вместо этого используем простой диалог с формой.

            // Быстрый вариант: использовать StackPanel
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new Label { Content = "Название:" });
            nameBox = new TextBox();
            panel.Children.Add(nameBox);
            panel.Children.Add(new Label { Content = "Количество:" });
            qtyBox = new TextBox();
            panel.Children.Add(qtyBox);
            panel.Children.Add(new Label { Content = "Цена:" });
            priceBox = new TextBox();
            panel.Children.Add(priceBox);
            panel.Children.Add(new Label { Content = "Категория:" });
            catBox = new TextBox();
            panel.Children.Add(catBox);
            panel.Children.Add(new Label { Content = "Срок годности:" });
            expiryBox = new TextBox();
            panel.Children.Add(expiryBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,10,0,0) };
            var okBtn = new Button { Content = "OK", Width = 80, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            panel.Children.Add(buttons);

            Content = panel;

            if (editItem != null)
            {
                nameBox.Text = editItem.Name;
                qtyBox.Text = editItem.Quantity.ToString();
                priceBox.Text = editItem.Price.ToString();
                catBox.Text = editItem.Category;
                expiryBox.Text = editItem.Expiry;
            }

            okBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("Введите название"); return; }
                var item = new Item();
                item.Name = nameBox.Text.Trim();
                int.TryParse(qtyBox.Text, out item.Quantity);
                double.TryParse(priceBox.Text, out item.Price);
                item.Category = catBox.Text.Trim();
                item.Expiry = expiryBox.Text.Trim();
                item.Purchased = editItem?.Purchased ?? false;
                Result = item;
                DialogResult = true;
                Close();
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        }
    }

    public class BooleanToCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (value is bool b && b) ? "✅" : "❌";
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
