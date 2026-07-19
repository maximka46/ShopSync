// shopping_cpp.cpp — совместный список покупок на C++ (Qt Widgets)

#include <QApplication>
#include <QMainWindow>
#include <QWidget>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QPushButton>
#include <QTableWidget>
#include <QHeaderView>
#include <QLabel>
#include <QLineEdit>
#include <QComboBox>
#include <QMessageBox>
#include <QFileDialog>
#include <QFile>
#include <QJsonDocument>
#include <QJsonArray>
#include <QJsonObject>
#include <QDateTime>
#include <QInputDialog>

struct Item {
    QString name;
    int quantity;
    double price;
    QString category;
    QString expiry;
    bool purchased;
};

class MainWindow : public QMainWindow {
    Q_OBJECT
public:
    MainWindow(QWidget *parent = nullptr) : QMainWindow(parent) {
        setWindowTitle("🛒 ShopSync — C++");
        resize(900, 650);
        loadData();
        createUI();
        refreshTable();
    }

private slots:
    void addItem() {
        QDialog dialog(this);
        dialog.setWindowTitle("Добавить товар");
        QFormLayout form(&dialog);
        QLineEdit *nameEdit = new QLineEdit;
        QLineEdit *qtyEdit = new QLineEdit("1");
        QLineEdit *priceEdit = new QLineEdit("0");
        QLineEdit *catEdit = new QLineEdit;
        QLineEdit *expiryEdit = new QLineEdit;
        form.addRow("Название:", nameEdit);
        form.addRow("Количество:", qtyEdit);
        form.addRow("Цена:", priceEdit);
        form.addRow("Категория:", catEdit);
        form.addRow("Срок годности:", expiryEdit);
        QDialogButtonBox buttons(QDialogButtonBox::Ok | QDialogButtonBox::Cancel);
        connect(&buttons, &QDialogButtonBox::accepted, &dialog, &QDialog::accept);
        connect(&buttons, &QDialogButtonBox::rejected, &dialog, &QDialog::reject);
        form.addRow(&buttons);
        if (dialog.exec() == QDialog::Accepted) {
            Item item;
            item.name = nameEdit->text().trimmed();
            if (item.name.isEmpty()) { QMessageBox::warning(this, "Ошибка", "Введите название"); return; }
            item.quantity = qtyEdit->text().toInt();
            item.price = priceEdit->text().toDouble();
            item.category = catEdit->text().trimmed();
            item.expiry = expiryEdit->text().trimmed();
            item.purchased = false;
            items.append(item);
            saveData();
            refreshTable();
            statusLabel->setText("Добавлен: " + item.name);
        }
    }

    void editItem() {
        int row = table->currentRow();
        if (row < 0) return;
        Item &item = items[row];
        QDialog dialog(this);
        dialog.setWindowTitle("Редактировать товар");
        QFormLayout form(&dialog);
        QLineEdit *nameEdit = new QLineEdit(item.name);
        QLineEdit *qtyEdit = new QLineEdit(QString::number(item.quantity));
        QLineEdit *priceEdit = new QLineEdit(QString::number(item.price));
        QLineEdit *catEdit = new QLineEdit(item.category);
        QLineEdit *expiryEdit = new QLineEdit(item.expiry);
        form.addRow("Название:", nameEdit);
        form.addRow("Количество:", qtyEdit);
        form.addRow("Цена:", priceEdit);
        form.addRow("Категория:", catEdit);
        form.addRow("Срок годности:", expiryEdit);
        QDialogButtonBox buttons(QDialogButtonBox::Ok | QDialogButtonBox::Cancel);
        connect(&buttons, &QDialogButtonBox::accepted, &dialog, &QDialog::accept);
        connect(&buttons, &QDialogButtonBox::rejected, &dialog, &QDialog::reject);
        form.addRow(&buttons);
        if (dialog.exec() == QDialog::Accepted) {
            item.name = nameEdit->text().trimmed();
            item.quantity = qtyEdit->text().toInt();
            item.price = priceEdit->text().toDouble();
            item.category = catEdit->text().trimmed();
            item.expiry = expiryEdit->text().trimmed();
            saveData();
            refreshTable();
            statusLabel->setText("Обновлён: " + item.name);
        }
    }

    void deleteItem() {
        int row = table->currentRow();
        if (row < 0) return;
        if (QMessageBox::question(this, "Удалить", "Удалить товар?") == QMessageBox::Yes) {
            QString name = items[row].name;
            items.removeAt(row);
            saveData();
            refreshTable();
            statusLabel->setText("Удалён: " + name);
        }
    }

    void togglePurchased() {
        int row = table->currentRow();
        if (row < 0) return;
        items[row].purchased = !items[row].purchased;
        saveData();
        refreshTable();
        statusLabel->setText(QString("%1: %2").arg(items[row].purchased ? "Куплен" : "Возвращён").arg(items[row].name));
    }

    void showStats() {
        int total = items.size();
        int bought = 0;
        double totalPrice = 0, boughtPrice = 0;
        for (const Item &it : items) {
            if (it.purchased) { bought++; boughtPrice += it.price * it.quantity; }
            totalPrice += it.price * it.quantity;
        }
        QString msg = QString("Всего товаров: %1\nКуплено: %2 (%3%)\nОбщая стоимость: %4 руб.\nКуплено на: %5 руб.")
                      .arg(total).arg(bought).arg(bought*100.0/total, 0, 'f', 1)
                      .arg(totalPrice, 0, 'f', 2).arg(boughtPrice, 0, 'f', 2);
        QMessageBox::information(this, "Статистика", msg);
    }

    void exportData() {
        QString filename = QFileDialog::getSaveFileName(this, "Экспорт JSON", "", "JSON (*.json)");
        if (filename.isEmpty()) return;
        QJsonArray arr;
        for (const Item &it : items) {
            QJsonObject obj;
            obj["name"] = it.name;
            obj["quantity"] = it.quantity;
            obj["price"] = it.price;
            obj["category"] = it.category;
            obj["expiry"] = it.expiry;
            obj["purchased"] = it.purchased;
            arr.append(obj);
        }
        QJsonDocument doc(arr);
        QFile file(filename);
        if (file.open(QIODevice::WriteOnly)) {
            file.write(doc.toJson());
            statusLabel->setText("Экспортировано в " + filename);
        }
    }

    void importData() {
        QString filename = QFileDialog::getOpenFileName(this, "Импорт JSON", "", "JSON (*.json)");
        if (filename.isEmpty()) return;
        QFile file(filename);
        if (!file.open(QIODevice::ReadOnly)) return;
        QByteArray data = file.readAll();
        QJsonDocument doc = QJsonDocument::fromJson(data);
        if (!doc.isArray()) { QMessageBox::warning(this, "Ошибка", "Неверный формат"); return; }
        QJsonArray arr = doc.array();
        for (const QJsonValue &v : arr) {
            QJsonObject obj = v.toObject();
            Item it;
            it.name = obj["name"].toString();
            it.quantity = obj["quantity"].toInt();
            it.price = obj["price"].toDouble();
            it.category = obj["category"].toString();
            it.expiry = obj["expiry"].toString();
            it.purchased = obj["purchased"].toBool();
            items.append(it);
        }
        saveData();
        refreshTable();
        statusLabel->setText("Импортировано из " + filename);
    }

private:
    QList<Item> items;
    QTableWidget *table;
    QLineEdit *searchEdit;
    QComboBox *catCombo;
    QLabel *statusLabel;

    void createUI() {
        QWidget *central = new QWidget(this);
        setCentralWidget(central);
        QVBoxLayout *mainLayout = new QVBoxLayout(central);

        // Панель инструментов
        QHBoxLayout *toolbar = new QHBoxLayout();
        QPushButton *addBtn = new QPushButton("Добавить");
        QPushButton *editBtn = new QPushButton("Редактировать");
        QPushButton *delBtn = new QPushButton("Удалить");
        QPushButton *buyBtn = new QPushButton("Куплен/Возврат");
        QPushButton *statsBtn = new QPushButton("Статистика");
        QPushButton *exportBtn = new QPushButton("Экспорт");
        QPushButton *importBtn = new QPushButton("Импорт");
        toolbar->addWidget(addBtn);
        toolbar->addWidget(editBtn);
        toolbar->addWidget(delBtn);
        toolbar->addWidget(buyBtn);
        toolbar->addWidget(statsBtn);
        toolbar->addWidget(exportBtn);
        toolbar->addWidget(importBtn);
        mainLayout->addLayout(toolbar);

        // Фильтры
        QHBoxLayout *filterLayout = new QHBoxLayout();
        filterLayout->addWidget(new QLabel("Поиск:"));
        searchEdit = new QLineEdit;
        connect(searchEdit, &QLineEdit::textChanged, this, &MainWindow::refreshTable);
        filterLayout->addWidget(searchEdit);
        filterLayout->addWidget(new QLabel("Категория:"));
        catCombo = new QComboBox;
        catCombo->addItem("");
        connect(catCombo, QOverload<int>::of(&QComboBox::currentIndexChanged), this, &MainWindow::refreshTable);
        filterLayout->addWidget(catCombo);
        QPushButton *resetBtn = new QPushButton("Сбросить");
        connect(resetBtn, &QPushButton::clicked, this, [=](){ searchEdit->clear(); catCombo->setCurrentIndex(0); });
        filterLayout->addWidget(resetBtn);
        mainLayout->addLayout(filterLayout);

        // Таблица
        table = new QTableWidget(0, 6);
        table->setHorizontalHeaderLabels({"Название", "Кол-во", "Цена", "Категория", "Срок", "Куплено"});
        table->horizontalHeader()->setSectionResizeMode(QHeaderView::Stretch);
        table->setEditTriggers(QTableWidget::NoEditTriggers);
        table->setSelectionBehavior(QTableWidget::SelectRows);
        connect(table, &QTableWidget::doubleClicked, this, &MainWindow::editItem);
        mainLayout->addWidget(table);

        // Статус
        statusLabel = new QLabel("Готов");
        mainLayout->addWidget(statusLabel);

        connect(addBtn, &QPushButton::clicked, this, &MainWindow::addItem);
        connect(editBtn, &QPushButton::clicked, this, &MainWindow::editItem);
        connect(delBtn, &QPushButton::clicked, this, &MainWindow::deleteItem);
        connect(buyBtn, &QPushButton::clicked, this, &MainWindow::togglePurchased);
        connect(statsBtn, &QPushButton::clicked, this, &MainWindow::showStats);
        connect(exportBtn, &QPushButton::clicked, this, &MainWindow::exportData);
        connect(importBtn, &QPushButton::clicked, this, &MainWindow::importData);
    }

    void refreshTable() {
        QString query = searchEdit->text().trimmed().toLower();
        QString catFilter = catCombo->currentText();
        table->setRowCount(0);
        for (const Item &it : items) {
            if (!query.isEmpty() && !it.name.toLower().contains(query)) continue;
            if (!catFilter.isEmpty() && it.category != catFilter) continue;
            int row = table->rowCount();
            table->insertRow(row);
            table->setItem(row, 0, new QTableWidgetItem(it.name));
            table->setItem(row, 1, new QTableWidgetItem(QString::number(it.quantity)));
            table->setItem(row, 2, new QTableWidgetItem(QString::number(it.price, 'f', 2)));
            table->setItem(row, 3, new QTableWidgetItem(it.category));
            table->setItem(row, 4, new QTableWidgetItem(it.expiry));
            table->setItem(row, 5, new QTableWidgetItem(it.purchased ? "✅" : "❌"));
        }
        updateStatus();
        updateCategories();
    }

    void updateStatus() {
        int total = items.size();
        int bought = 0;
        for (const Item &it : items) if (it.purchased) bought++;
        statusLabel->setText(QString("Всего: %1 | Куплено: %2").arg(total).arg(bought));
    }

    void updateCategories() {
        QString current = catCombo->currentText();
        catCombo->clear();
        catCombo->addItem("");
        QStringList cats;
        for (const Item &it : items) if (!it.category.isEmpty() && !cats.contains(it.category)) cats.append(it.category);
        cats.sort();
        catCombo->addItems(cats);
        int idx = catCombo->findText(current);
        if (idx >= 0) catCombo->setCurrentIndex(idx);
    }

    void loadData() {
        QFile file("shoplist.json");
        if (!file.open(QIODevice::ReadOnly)) return;
        QByteArray data = file.readAll();
        QJsonDocument doc = QJsonDocument::fromJson(data);
        if (!doc.isArray()) return;
        QJsonArray arr = doc.array();
        for (const QJsonValue &v : arr) {
            QJsonObject obj = v.toObject();
            Item it;
            it.name = obj["name"].toString();
            it.quantity = obj["quantity"].toInt();
            it.price = obj["price"].toDouble();
            it.category = obj["category"].toString();
            it.expiry = obj["expiry"].toString();
            it.purchased = obj["purchased"].toBool();
            items.append(it);
        }
    }

    void saveData() {
        QJsonArray arr;
        for (const Item &it : items) {
            QJsonObject obj;
            obj["name"] = it.name;
            obj["quantity"] = it.quantity;
            obj["price"] = it.price;
            obj["category"] = it.category;
            obj["expiry"] = it.expiry;
            obj["purchased"] = it.purchased;
            arr.append(obj);
        }
        QJsonDocument doc(arr);
        QFile file("shoplist.json");
        if (file.open(QIODevice::WriteOnly)) {
            file.write(doc.toJson());
        }
    }
};

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);
    MainWindow w;
    w.show();
    return app.exec();
}

#include "shopping_cpp.moc"
