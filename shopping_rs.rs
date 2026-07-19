// shopping_rs.rs — совместный список покупок на Rust (консоль + termion)

use serde::{Deserialize, Serialize};
use std::fs;
use std::io::{self, Write, BufRead};
use std::str::FromStr;
use termion::{color, style};

#[derive(Serialize, Deserialize, Clone)]
struct Item {
    name: String,
    quantity: u32,
    price: f64,
    category: String,
    expiry: String,
    purchased: bool,
}

struct App {
    items: Vec<Item>,
    filename: String,
}

impl App {
    fn new() -> Self {
        let mut app = App { items: Vec::new(), filename: "shoplist.json".to_string() };
        app.load();
        app
    }

    fn load(&mut self) {
        if let Ok(data) = fs::read_to_string(&self.filename) {
            if let Ok(items) = serde_json::from_str(&data) {
                self.items = items;
            }
        }
    }

    fn save(&self) {
        let data = serde_json::to_string_pretty(&self.items).unwrap();
        fs::write(&self.filename, data).unwrap();
    }

    fn add(&mut self) {
        let reader = io::stdin();
        let mut reader = reader.lock();
        print!("Название: ");
        io::stdout().flush().unwrap();
        let mut name = String::new();
        reader.read_line(&mut name).unwrap();
        let name = name.trim();
        if name.is_empty() { println!("Название обязательно"); return; }
        print!("Количество: ");
        io::stdout().flush().unwrap();
        let mut qty_str = String::new();
        reader.read_line(&mut qty_str).unwrap();
        let qty = qty_str.trim().parse().unwrap_or(1);
        print!("Цена: ");
        io::stdout().flush().unwrap();
        let mut price_str = String::new();
        reader.read_line(&mut price_str).unwrap();
        let price = price_str.trim().parse().unwrap_or(0.0);
        print!("Категория: ");
        io::stdout().flush().unwrap();
        let mut cat = String::new();
        reader.read_line(&mut cat).unwrap();
        let cat = cat.trim().to_string();
        print!("Срок годности (ГГГГ-ММ-ДД): ");
        io::stdout().flush().unwrap();
        let mut expiry = String::new();
        reader.read_line(&mut expiry).unwrap();
        let expiry = expiry.trim().to_string();
        let item = Item { name: name.to_string(), quantity: qty, price, category: cat, expiry, purchased: false };
        self.items.push(item);
        self.save();
        println!("Добавлено!");
    }

    fn list(&self) {
        if self.items.is_empty() {
            println!("Список пуст");
            return;
        }
        println!("=== Список покупок ===");
        for (i, it) in self.items.iter().enumerate() {
            let status = if it.purchased { "✅" } else { " " };
            println!("[{}] {}. {} ({} шт.) - {:.2} руб., кат. {}, до {}",
                status, i+1, it.name, it.quantity, it.price, it.category, it.expiry);
        }
    }

    fn buy(&mut self) {
        print!("Номер товара для отметки: ");
        io::stdout().flush().unwrap();
        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();
        let idx: usize = input.trim().parse().unwrap_or(0);
        if idx == 0 || idx > self.items.len() {
            println!("Неверный номер");
            return;
        }
        let item = &mut self.items[idx-1];
        item.purchased = !item.purchased;
        self.save();
        let status = if item.purchased { "куплен" } else { "возвращён" };
        println!("Товар \"{}\" {}!", item.name, status);
    }

    fn remove(&mut self) {
        print!("Номер товара для удаления: ");
        io::stdout().flush().unwrap();
        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();
        let idx: usize = input.trim().parse().unwrap_or(0);
        if idx == 0 || idx > self.items.len() {
            println!("Неверный номер");
            return;
        }
        self.items.remove(idx-1);
        self.save();
        println!("Удалено");
    }

    fn stats(&self) {
        let total = self.items.len();
        let bought = self.items.iter().filter(|i| i.purchased).count();
        let total_price: f64 = self.items.iter().map(|i| i.price * i.quantity as f64).sum();
        let bought_price: f64 = self.items.iter().filter(|i| i.purchased).map(|i| i.price * i.quantity as f64).sum();
        println!("Всего товаров: {}", total);
        if total > 0 {
            println!("Куплено: {} ({:.1}%)", bought, bought as f64 / total as f64 * 100.0);
        } else {
            println!("Куплено: 0 (0%)");
        }
        println!("Общая стоимость: {:.2} руб.", total_price);
        println!("Куплено на: {:.2} руб.", bought_price);
    }

    fn export_(&self) {
        print!("Имя файла для экспорта (JSON): ");
        io::stdout().flush().unwrap();
        let mut fname = String::new();
        io::stdin().read_line(&mut fname).unwrap();
        let fname = fname.trim();
        if fname.is_empty() { println!("Имя не указано"); return; }
        let data = serde_json::to_string_pretty(&self.items).unwrap();
        fs::write(fname, data).unwrap();
        println!("Экспортировано в {}", fname);
    }

    fn import_(&mut self) {
        print!("Имя файла для импорта (JSON): ");
        io::stdout().flush().unwrap();
        let mut fname = String::new();
        io::stdin().read_line(&mut fname).unwrap();
        let fname = fname.trim();
        if fname.is_empty() { println!("Имя не указано"); return; }
        let data = match fs::read_to_string(fname) {
            Ok(d) => d,
            Err(e) => { println!("Ошибка чтения: {}", e); return; }
        };
        let imported: Vec<Item> = match serde_json::from_str(&data) {
            Ok(v) => v,
            Err(_) => { println!("Ошибка формата JSON"); return; }
        };
        self.items.extend(imported);
        self.save();
        println!("Импортировано {} товаров", self.items.len());
    }
}

fn main() {
    let mut app = App::new();
    let stdin = io::stdin();
    let mut reader = stdin.lock();
    println!("{}🛒 ShopSync — Rust Edition{}", color::Fg(color::Cyan), style::Reset);
    println!("Команды: add, list, buy, remove, stats, export, import, exit");
    loop {
        print!("{}> {}", color::Fg(color::Yellow), style::Reset);
        io::stdout().flush().unwrap();
        let mut cmd = String::new();
        if reader.read_line(&mut cmd).is_err() { break; }
        let cmd = cmd.trim();
        match cmd {
            "add" => app.add(),
            "list" => app.list(),
            "buy" => app.buy(),
            "remove" => app.remove(),
            "stats" => app.stats(),
            "export" => app.export_(),
            "import" => app.import_(),
            "exit" => {
                app.save();
                println!("До свидания!");
                break;
            }
            _ => println!("Неизвестная команда"),
        }
    }
}
