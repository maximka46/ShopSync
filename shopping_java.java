// shopping_java.java — совместный список покупок на Java (Swing)

import javax.swing.*;
import javax.swing.table.*;
import java.awt.*;
import java.awt.event.*;
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.List;
import com.google.gson.*; // требуется Gson

public class ShoppingJava extends JFrame {
    private static final String DATA_FILE = "shoplist.json";
    private List<Item> items = new ArrayList<>();
    private JTable table;
    private DefaultTableModel tableModel;
    private JTextField searchField;
    private JComboBox<String> catCombo;
    private JLabel statusLabel;

    public ShoppingJava() {
        setTitle("🛒 ShopSync — Java");
        setSize(900, 650);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setLayout(new BorderLayout());
        loadData();
        createUI();
        refreshTable();
    }

    private void createUI() {
        // Панель инструментов
        JPanel toolbar = new JPanel();
        JButton addBtn = new JButton("Добавить");
        JButton editBtn = new JButton("Редактировать");
        JButton delBtn = new JButton("Удалить");
        JButton buyBtn = new JButton("Куплен/Возврат");
        JButton statsBtn = new JButton("Статистика");
        JButton exportBtn = new JButton("Экспорт");
        JButton importBtn = new JButton("Импорт");
        toolbar.add(addBtn);
        toolbar.add(editBtn);
        toolbar.add(delBtn);
        toolbar.add(buyBtn);
        toolbar.add(statsBtn);
        toolbar.add(exportBtn);
        toolbar.add(importBtn);
        add(toolbar, BorderLayout.NORTH);

        // Фильтры
        JPanel filterPanel = new JPanel(new FlowLayout());
        filterPanel.add(new JLabel("Поиск:"));
        searchField = new JTextField(15);
        searchField.getDocument().addDocumentListener(new javax.swing.event.DocumentListener() {
            public void changedUpdate(javax.swing.event.DocumentEvent e) { refreshTable(); }
            public void insertUpdate(javax.swing.event.DocumentEvent e) { refreshTable(); }
            public void removeUpdate(javax.swing.event.DocumentEvent e) { refreshTable(); }
        });
        filterPanel.add(searchField);
        filterPanel.add(new JLabel("Категория:"));
        catCombo = new JComboBox<>();
        catCombo.addItem("");
        catCombo.addActionListener(e -> refreshTable());
        filterPanel.add(catCombo);
        JButton resetBtn = new JButton("Сбросить");
        resetBtn.addActionListener(e -> { searchField.setText(""); catCombo.setSelectedIndex(0); });
        filterPanel.add(resetBtn);
        add(filterPanel, BorderLayout.SOUTH);

        // Таблица
        String[] cols = {"Название", "Кол-во", "Цена", "Категория", "Срок", "Куплено"};
        tableModel = new DefaultTableModel(cols, 0) {
            @Override
            public boolean isCellEditable(int row, int col) { return false; }
        };
        table = new JTable(tableModel);
        table.setRowHeight(25);
        table.getTableHeader().setReorderingAllowed(false);
        table.addMouseListener(new MouseAdapter() {
            public void mouseClicked(MouseEvent e) { if (e.getClickCount() == 2) editItem(); }
        });
        JScrollPane scroll = new JScrollPane(table);
        add(scroll, BorderLayout.CENTER);

        // Статус
        statusLabel = new JLabel("Готов");
        add(statusLabel, BorderLayout.SOUTH);

        // Обработчики
        addBtn.addActionListener(e -> addItem());
        editBtn.addActionListener(e -> editItem());
        delBtn.addActionListener(e -> deleteItem());
        buyBtn.addActionListener(e -> togglePurchased());
        statsBtn.addActionListener(e -> showStats());
        exportBtn.addActionListener(e -> exportData());
        importBtn.addActionListener(e -> importData());
    }

    private void addItem() {
        JDialog dialog = new JDialog(this, "Добавить товар", true);
        dialog.setLayout(new GridLayout(0,2));
        JTextField nameField = new JTextField();
        JTextField qtyField = new JTextField("1");
        JTextField priceField = new JTextField("0");
        JTextField catField = new JTextField();
        JTextField expiryField = new JTextField();
        dialog.add(new JLabel("Название:"));
        dialog.add(nameField);
        dialog.add(new JLabel("Количество:"));
        dialog.add(qtyField);
        dialog.add(new JLabel("Цена:"));
        dialog.add(priceField);
        dialog.add(new JLabel("Категория:"));
        dialog.add(catField);
        dialog.add(new JLabel("Срок годности:"));
        dialog.add(expiryField);
        JButton saveBtn = new JButton("Сохранить");
        JButton cancelBtn = new JButton("Отмена");
        dialog.add(saveBtn);
        dialog.add(cancelBtn);
        dialog.setSize(400, 300);
        dialog.setLocationRelativeTo(this);
        saveBtn.addActionListener(e -> {
            String name = nameField.getText().trim();
            if (name.isEmpty()) { JOptionPane.showMessageDialog(dialog, "Введите название"); return; }
            int qty = Integer.parseInt(qtyField.getText().trim());
            double price = Double.parseDouble(priceField.getText().trim());
            String cat = catField.getText().trim();
            String expiry = expiryField.getText().trim();
            items.add(new Item(name, qty, price, cat, expiry));
            saveData();
            refreshTable();
            statusLabel.setText("Добавлен: " + name);
            dialog.dispose();
        });
        cancelBtn.addActionListener(e -> dialog.dispose());
        dialog.setVisible(true);
    }

    private void editItem() {
        int row = table.getSelectedRow();
        if (row < 0) return;
        Item item = items.get(row);
        JDialog dialog = new JDialog(this, "Редактировать товар", true);
        dialog.setLayout(new GridLayout(0,2));
        JTextField nameField = new JTextField(item.name);
        JTextField qtyField = new JTextField(String.valueOf(item.quantity));
        JTextField priceField = new JTextField(String.valueOf(item.price));
        JTextField catField = new JTextField(item.category);
        JTextField expiryField = new JTextField(item.expiry);
        dialog.add(new JLabel("Название:"));
        dialog.add(nameField);
        dialog.add(new JLabel("Количество:"));
        dialog.add(qtyField);
        dialog.add(new JLabel("Цена:"));
        dialog.add(priceField);
        dialog.add(new JLabel("Категория:"));
        dialog.add(catField);
        dialog.add(new JLabel("Срок годности:"));
        dialog.add(expiryField);
        JButton saveBtn = new JButton("Сохранить");
        JButton cancelBtn = new JButton("Отмена");
        dialog.add(saveBtn);
        dialog.add(cancelBtn);
        dialog.setSize(400, 300);
        dialog.setLocationRelativeTo(this);
        saveBtn.addActionListener(e -> {
            item.name = nameField.getText().trim();
            item.quantity = Integer.parseInt(qtyField.getText().trim());
            item.price = Double.parseDouble(priceField.getText().trim());
            item.category = catField.getText().trim();
            item.expiry = expiryField.getText().trim();
            saveData();
            refreshTable();
            statusLabel.setText("Обновлён: " + item.name);
            dialog.dispose();
        });
        cancelBtn.addActionListener(e -> dialog.dispose());
        dialog.setVisible(true);
    }

    private void deleteItem() {
        int row = table.getSelectedRow();
        if (row < 0) return;
        if (JOptionPane.showConfirmDialog(this, "Удалить товар?", "Подтверждение", JOptionPane.YES_NO_OPTION) == JOptionPane.YES_OPTION) {
            String name = items.get(row).name;
            items.remove(row);
            saveData();
            refreshTable();
            statusLabel.setText("Удалён: " + name);
        }
    }

    private void togglePurchased() {
        int row = table.getSelectedRow();
        if (row < 0) return;
        items.get(row).purchased = !items.get(row).purchased;
        saveData();
        refreshTable();
        statusLabel.setText((items.get(row).purchased ? "Куплен" : "Возвращён") + ": " + items.get(row).name);
    }

    private void showStats() {
        int total = items.size();
        int bought = 0;
        double totalPrice = 0, boughtPrice = 0;
        for (Item it : items) {
            if (it.purchased) { bought++; boughtPrice += it.price * it.quantity; }
            totalPrice += it.price * it.quantity;
        }
        String msg = String.format("Всего товаров: %d\nКуплено: %d (%.1f%%)\nОбщая стоимость: %.2f руб.\nКуплено на: %.2f руб.",
                total, bought, bought*100.0/total, totalPrice, boughtPrice);
        JOptionPane.showMessageDialog(this, msg, "Статистика", JOptionPane.INFORMATION_MESSAGE);
    }

    private void exportData() {
        JFileChooser chooser = new JFileChooser();
        if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
            File file = chooser.getSelectedFile();
            try (PrintWriter pw = new PrintWriter(file)) {
                Gson gson = new GsonBuilder().setPrettyPrinting().create();
                pw.write(gson.toJson(items));
                statusLabel.setText("Экспортировано в " + file.getName());
            } catch (IOException e) { e.printStackTrace(); }
        }
    }

    private void importData() {
        JFileChooser chooser = new JFileChooser();
        if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
            File file = chooser.getSelectedFile();
            try (Reader reader = new FileReader(file)) {
                Gson gson = new Gson();
                Item[] arr = gson.fromJson(reader, Item[].class);
                for (Item it : arr) items.add(it);
                saveData();
                refreshTable();
                statusLabel.setText("Импортировано из " + file.getName());
            } catch (Exception e) { e.printStackTrace(); }
        }
    }

    private void refreshTable() {
        tableModel.setRowCount(0);
        String query = searchField.getText().trim().toLowerCase();
        String catFilter = (String) catCombo.getSelectedItem();
        for (Item it : items) {
            if (!query.isEmpty() && !it.name.toLowerCase().contains(query)) continue;
            if (catFilter != null && !catFilter.isEmpty() && !it.category.equals(catFilter)) continue;
            tableModel.addRow(new Object[]{
                it.name, it.quantity, String.format("%.2f", it.price),
                it.category, it.expiry, it.purchased ? "✅" : "❌"
            });
        }
        updateStatus();
        updateCategories();
    }

    private void updateStatus() {
        int total = items.size();
        int bought = 0;
        for (Item it : items) if (it.purchased) bought++;
        statusLabel.setText("Всего: " + total + " | Куплено: " + bought);
    }

    private void updateCategories() {
        String current = (String) catCombo.getSelectedItem();
        catCombo.removeAllItems();
        catCombo.addItem("");
        Set<String> cats = new HashSet<>();
        for (Item it : items) if (!it.category.isEmpty()) cats.add(it.category);
        for (String c : cats) catCombo.addItem(c);
        catCombo.setSelectedItem(current);
    }

    private void loadData() {
        File file = new File(DATA_FILE);
        if (!file.exists()) return;
        try (Reader reader = new FileReader(file)) {
            Gson gson = new Gson();
            Item[] arr = gson.fromJson(reader, Item[].class);
            for (Item it : arr) items.add(it);
        } catch (Exception e) { /* ignore */ }
    }

    private void saveData() {
        try (PrintWriter pw = new PrintWriter(new File(DATA_FILE))) {
            Gson gson = new GsonBuilder().setPrettyPrinting().create();
            pw.write(gson.toJson(items));
        } catch (IOException e) { /* ignore */ }
    }

    static class Item {
        String name;
        int quantity;
        double price;
        String category;
        String expiry;
        boolean purchased;
        Item(String name, int quantity, double price, String category, String expiry) {
            this.name = name; this.quantity = quantity; this.price = price;
            this.category = category; this.expiry = expiry; this.purchased = false;
        }
    }

    public static void main(String[] args) throws Exception {
        UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
        SwingUtilities.invokeLater(() -> new ShoppingJava().setVisible(true));
    }
}
