# shopping_python.py — совместный список покупок на Python (Tkinter GUI)

import tkinter as tk
from tkinter import ttk, messagebox, simpledialog, filedialog
import json
import os
import datetime
from collections import defaultdict

class Item:
    def __init__(self, name, quantity=1, price=0.0, category="", expiry="", purchased=False):
        self.name = name
        self.quantity = quantity
        self.price = price
        self.category = category
        self.expiry = expiry
        self.purchased = purchased

    def to_dict(self):
        return {
            "name": self.name,
            "quantity": self.quantity,
            "price": self.price,
            "category": self.category,
            "expiry": self.expiry,
            "purchased": self.purchased
        }

    @classmethod
    def from_dict(cls, data):
        return cls(data["name"], data["quantity"], data["price"], data["category"], data["expiry"], data["purchased"])

class ShoppingApp:
    def __init__(self, root):
        self.root = root
        self.root.title("🛒 ShopSync — Python")
        self.root.geometry("900x650")
        self.items = []
        self.filename = "shoplist.json"
        self.load_data()
        self.create_widgets()
        self.refresh_list()
        self.history = []  # простая история изменений

    def create_widgets(self):
        # Панель инструментов
        toolbar = tk.Frame(self.root)
        toolbar.pack(fill=tk.X, pady=5)
        tk.Button(toolbar, text="Добавить", command=self.add_item).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Редактировать", command=self.edit_item).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Удалить", command=self.delete_item).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Отметить купленным", command=self.toggle_purchased).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Экспорт JSON", command=self.export_json).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Импорт JSON", command=self.import_json).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Статистика", command=self.show_stats).pack(side=tk.LEFT, padx=5)

        # Фильтры
        filter_frame = tk.Frame(self.root)
        filter_frame.pack(fill=tk.X, pady=5)
        tk.Label(filter_frame, text="Поиск:").pack(side=tk.LEFT, padx=5)
        self.search_var = tk.StringVar()
        self.search_var.trace("w", lambda *args: self.refresh_list())
        tk.Entry(filter_frame, textvariable=self.search_var, width=20).pack(side=tk.LEFT, padx=5)
        tk.Label(filter_frame, text="Категория:").pack(side=tk.LEFT, padx=5)
        self.cat_var = tk.StringVar()
        self.cat_combo = ttk.Combobox(filter_frame, textvariable=self.cat_var, width=15)
        self.cat_combo.pack(side=tk.LEFT, padx=5)
        self.cat_combo.bind("<<ComboboxSelected>>", lambda e: self.refresh_list())
        tk.Button(filter_frame, text="Сбросить фильтры", command=self.reset_filters).pack(side=tk.LEFT, padx=5)

        # Таблица
        columns = ("Название", "Кол-во", "Цена", "Категория", "Срок", "Куплено")
        self.tree = ttk.Treeview(self.root, columns=columns, show="headings", height=15)
        for col in columns:
            self.tree.heading(col, text=col)
            self.tree.column(col, width=100)
        self.tree.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        self.tree.bind("<Double-Button-1>", lambda e: self.edit_item())

        # Статус
        self.status = tk.Label(self.root, text="Готов", anchor=tk.W)
        self.status.pack(fill=tk.X, padx=10)

        # Обновление категорий в комбобоксе
        self.update_categories()

    def load_data(self):
        if os.path.exists(self.filename):
            with open(self.filename, 'r', encoding='utf-8') as f:
                data = json.load(f)
                self.items = [Item.from_dict(d) for d in data]

    def save_data(self):
        with open(self.filename, 'w', encoding='utf-8') as f:
            json.dump([it.to_dict() for it in self.items], f, indent=2, ensure_ascii=False)
        self.update_categories()

    def update_categories(self):
        cats = sorted(set(it.category for it in self.items if it.category))
        self.cat_combo['values'] = [""] + cats

    def refresh_list(self):
        for row in self.tree.get_children():
            self.tree.delete(row)
        query = self.search_var.get().strip().lower()
        cat_filter = self.cat_var.get()
        for item in self.items:
            if query and query not in item.name.lower():
                continue
            if cat_filter and item.category != cat_filter:
                continue
            purchased = "✅" if item.purchased else "❌"
            self.tree.insert("", "end", values=(
                item.name, item.quantity, f"{item.price:.2f}", item.category, item.expiry, purchased
            ))
        self.update_status()

    def add_item(self):
        dialog = tk.Toplevel(self.root)
        dialog.title("Добавить товар")
        dialog.geometry("400x300")
        fields = {}
        labels = ["Название", "Количество", "Цена", "Категория", "Срок годности (ГГГГ-ММ-ДД)"]
        for i, lbl in enumerate(labels):
            tk.Label(dialog, text=lbl).grid(row=i, column=0, padx=5, pady=2, sticky="w")
            entry = tk.Entry(dialog, width=30)
            entry.grid(row=i, column=1, padx=5, pady=2)
            fields[lbl] = entry
        # Кнопки
        def save():
            name = fields["Название"].get().strip()
            if not name:
                messagebox.showerror("Ошибка", "Название обязательно")
                return
            try:
                qty = int(fields["Количество"].get() or "1")
            except:
                qty = 1
            try:
                price = float(fields["Цена"].get() or "0")
            except:
                price = 0.0
            category = fields["Категория"].get().strip()
            expiry = fields["Срок годности (ГГГГ-ММ-ДД)"].get().strip()
            item = Item(name, qty, price, category, expiry)
            self.items.append(item)
            self.save_data()
            self.refresh_list()
            self.status.config(text=f"Добавлен: {name}")
            dialog.destroy()
        tk.Button(dialog, text="Сохранить", command=save).grid(row=len(labels), column=0, pady=10)
        tk.Button(dialog, text="Отмена", command=dialog.destroy).grid(row=len(labels), column=1, pady=10)

    def get_selected(self):
        selection = self.tree.selection()
        if not selection:
            return None
        # Находим индекс по имени и другим полям (не идеально, но для демо)
        values = self.tree.item(selection[0])['values']
        name = values[0]
        qty = int(values[1])
        price = float(values[2])
        category = values[3]
        expiry = values[4]
        for i, item in enumerate(self.items):
            if (item.name == name and item.quantity == qty and item.price == price and
                item.category == category and item.expiry == expiry):
                return i
        return None

    def edit_item(self):
        idx = self.get_selected()
        if idx is None:
            return
        item = self.items[idx]
        dialog = tk.Toplevel(self.root)
        dialog.title("Редактировать товар")
        dialog.geometry("400x300")
        fields = {}
        labels = ["Название", "Количество", "Цена", "Категория", "Срок годности"]
        defaults = [item.name, str(item.quantity), str(item.price), item.category, item.expiry]
        for i, lbl in enumerate(labels):
            tk.Label(dialog, text=lbl).grid(row=i, column=0, padx=5, pady=2, sticky="w")
            entry = tk.Entry(dialog, width=30)
            entry.insert(0, defaults[i])
            entry.grid(row=i, column=1, padx=5, pady=2)
            fields[lbl] = entry
        def save():
            name = fields["Название"].get().strip()
            if not name:
                messagebox.showerror("Ошибка", "Название обязательно")
                return
            try:
                qty = int(fields["Количество"].get() or "1")
            except:
                qty = 1
            try:
                price = float(fields["Цена"].get() or "0")
            except:
                price = 0.0
            category = fields["Категория"].get().strip()
            expiry = fields["Срок годности"].get().strip()
            item.name = name
            item.quantity = qty
            item.price = price
            item.category = category
            item.expiry = expiry
            self.save_data()
            self.refresh_list()
            self.status.config(text=f"Обновлён: {name}")
            dialog.destroy()
        tk.Button(dialog, text="Сохранить", command=save).grid(row=len(labels), column=0, pady=10)
        tk.Button(dialog, text="Отмена", command=dialog.destroy).grid(row=len(labels), column=1, pady=10)

    def delete_item(self):
        idx = self.get_selected()
        if idx is None:
            return
        item = self.items[idx]
        if messagebox.askyesno("Удалить", f"Удалить '{item.name}'?"):
            del self.items[idx]
            self.save_data()
            self.refresh_list()
            self.status.config(text=f"Удалён: {item.name}")

    def toggle_purchased(self):
        idx = self.get_selected()
        if idx is None:
            return
        item = self.items[idx]
        item.purchased = not item.purchased
        self.save_data()
        self.refresh_list()
        self.status.config(text=f"{'Куплен' if item.purchased else 'Возвращён'}: {item.name}")

    def reset_filters(self):
        self.search_var.set("")
        self.cat_var.set("")
        self.refresh_list()

    def show_stats(self):
        total = len(self.items)
        bought = sum(1 for it in self.items if it.purchased)
        total_price = sum(it.price * it.quantity for it in self.items)
        bought_price = sum(it.price * it.quantity for it in self.items if it.purchased)
        msg = (f"Всего товаров: {total}\nКуплено: {bought} ({bought/total*100:.1f}%)\n"
               f"Общая стоимость: {total_price:.2f} руб.\nКуплено на: {bought_price:.2f} руб.")
        messagebox.showinfo("Статистика", msg)

    def export_json(self):
        filename = filedialog.asksaveasfilename(defaultextension=".json", filetypes=[("JSON", "*.json")])
        if filename:
            with open(filename, 'w', encoding='utf-8') as f:
                json.dump([it.to_dict() for it in self.items], f, indent=2, ensure_ascii=False)
            self.status.config(text=f"Экспортировано в {filename}")

    def import_json(self):
        filename = filedialog.askopenfilename(filetypes=[("JSON", "*.json")])
        if filename:
            with open(filename, 'r', encoding='utf-8') as f:
                data = json.load(f)
            for d in data:
                self.items.append(Item.from_dict(d))
            self.save_data()
            self.refresh_list()
            self.status.config(text=f"Импортировано из {filename}")

    def update_status(self):
        self.status.config(text=f"Всего: {len(self.items)} | Куплено: {sum(1 for it in self.items if it.purchased)}")

if __name__ == "__main__":
    root = tk.Tk()
    app = ShoppingApp(root)
    root.mainloop()
