// shopping_go.go — совместный список покупок на Go (консоль)

package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"os"
	"strconv"
	"strings"
	"time"
)

type Item struct {
	Name      string  `json:"name"`
	Quantity  int     `json:"quantity"`
	Price     float64 `json:"price"`
	Category  string  `json:"category"`
	Expiry    string  `json:"expiry"`
	Purchased bool    `json:"purchased"`
}

type App struct {
	items []Item
	file  string
}

func NewApp() *App {
	return &App{file: "shoplist.json"}
}

func (a *App) load() {
	data, err := ioutil.ReadFile(a.file)
	if err != nil {
		return
	}
	json.Unmarshal(data, &a.items)
}

func (a *App) save() {
	data, _ := json.MarshalIndent(a.items, "", "  ")
	ioutil.WriteFile(a.file, data, 0644)
}

func (a *App) add() {
	reader := bufio.NewReader(os.Stdin)
	fmt.Print("Название: ")
	name, _ := reader.ReadString('\n')
	name = strings.TrimSpace(name)
	if name == "" { fmt.Println("Название обязательно"); return }
	fmt.Print("Количество: ")
	qtyStr, _ := reader.ReadString('\n')
	qty, _ := strconv.Atoi(strings.TrimSpace(qtyStr))
	fmt.Print("Цена: ")
	priceStr, _ := reader.ReadString('\n')
	price, _ := strconv.ParseFloat(strings.TrimSpace(priceStr), 64)
	fmt.Print("Категория: ")
	cat, _ := reader.ReadString('\n')
	cat = strings.TrimSpace(cat)
	fmt.Print("Срок годности (ГГГГ-ММ-ДД): ")
	exp, _ := reader.ReadString('\n')
	exp = strings.TrimSpace(exp)
	item := Item{Name: name, Quantity: qty, Price: price, Category: cat, Expiry: exp}
	a.items = append(a.items, item)
	a.save()
	fmt.Println("Добавлено!")
}

func (a *App) list() {
	if len(a.items) == 0 {
		fmt.Println("Список пуст")
		return
	}
	fmt.Println("=== Список покупок ===")
	for i, it := range a.items {
		status := " "
		if it.Purchased {
			status = "✅"
		}
		fmt.Printf("[%s] %d. %s (%d шт.) - %.2f руб., кат. %s, до %s\n",
			status, i+1, it.Name, it.Quantity, it.Price, it.Category, it.Expiry)
	}
}

func (a *App) buy() {
	fmt.Print("Номер товара для отметки: ")
	var idx int
	fmt.Scanln(&idx)
	if idx < 1 || idx > len(a.items) {
		fmt.Println("Неверный номер")
		return
	}
	a.items[idx-1].Purchased = !a.items[idx-1].Purchased
	a.save()
	status := "куплен"
	if !a.items[idx-1].Purchased {
		status = "возвращён"
	}
	fmt.Printf("Товар \"%s\" %s!\n", a.items[idx-1].Name, status)
}

func (a *App) remove() {
	fmt.Print("Номер товара для удаления: ")
	var idx int
	fmt.Scanln(&idx)
	if idx < 1 || idx > len(a.items) {
		fmt.Println("Неверный номер")
		return
	}
	a.items = append(a.items[:idx-1], a.items[idx:]...)
	a.save()
	fmt.Println("Удалено")
}

func (a *App) stats() {
	total := len(a.items)
	bought := 0
	var totalPrice, boughtPrice float64
	for _, it := range a.items {
		if it.Purchased {
			bought++
			boughtPrice += it.Price * float64(it.Quantity)
		}
		totalPrice += it.Price * float64(it.Quantity)
	}
	fmt.Printf("Всего товаров: %d\n", total)
	if total > 0 {
		fmt.Printf("Куплено: %d (%.1f%%)\n", bought, float64(bought)/float64(total)*100)
	} else {
		fmt.Println("Куплено: 0 (0%)")
	}
	fmt.Printf("Общая стоимость: %.2f руб.\n", totalPrice)
	fmt.Printf("Куплено на: %.2f руб.\n", boughtPrice)
}

func (a *App) export() {
	fmt.Print("Имя файла для экспорта (JSON): ")
	var fname string
	fmt.Scanln(&fname)
	if fname == "" {
		fname = "export.json"
	}
	data, _ := json.MarshalIndent(a.items, "", "  ")
	ioutil.WriteFile(fname, data, 0644)
	fmt.Println("Экспортировано в", fname)
}

func (a *App) import_() {
	fmt.Print("Имя файла для импорта (JSON): ")
	var fname string
	fmt.Scanln(&fname)
	if fname == "" {
		fmt.Println("Имя файла не указано")
		return
	}
	data, err := ioutil.ReadFile(fname)
	if err != nil {
		fmt.Println("Ошибка чтения:", err)
		return
	}
	var imported []Item
	err = json.Unmarshal(data, &imported)
	if err != nil {
		fmt.Println("Ошибка формата JSON")
		return
	}
	a.items = append(a.items, imported...)
	a.save()
	fmt.Printf("Импортировано %d товаров\n", len(imported))
}

func main() {
	app := NewApp()
	app.load()
	reader := bufio.NewReader(os.Stdin)
	fmt.Println("🛒 ShopSync — Go Edition")
	fmt.Println("Команды: add, list, buy, remove, stats, export, import, exit")
	for {
		fmt.Print("> ")
		cmd, _ := reader.ReadString('\n')
		cmd = strings.TrimSpace(cmd)
		switch cmd {
		case "add":
			app.add()
		case "list":
			app.list()
		case "buy":
			app.buy()
		case "remove":
			app.remove()
		case "stats":
			app.stats()
		case "export":
			app.export()
		case "import":
			app.import_()
		case "exit":
			app.save()
			fmt.Println("До свидания!")
			return
		default:
			fmt.Println("Неизвестная команда")
		}
	}
}
