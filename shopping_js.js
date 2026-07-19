// shopping_js.js — совместный список покупок на JavaScript (Node.js + readline)

const fs = require('fs');
const readline = require('readline');

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: '> '
});

let items = [];
const dataFile = 'shoplist.json';

function loadData() {
    try {
        if (fs.existsSync(dataFile)) {
            const data = fs.readFileSync(dataFile, 'utf8');
            items = JSON.parse(data);
        }
    } catch (e) { /* ignore */ }
}

function saveData() {
    fs.writeFileSync(dataFile, JSON.stringify(items, null, 2));
}

function askQuestion(query) {
    return new Promise((resolve) => {
        rl.question(query, resolve);
    });
}

async function addItem() {
    const name = (await askQuestion('Название: ')).trim();
    if (!name) { console.log('Название обязательно'); return; }
    const qty = parseInt(await askQuestion('Количество: ')) || 1;
    const price = parseFloat(await askQuestion('Цена: ')) || 0;
    const category = (await askQuestion('Категория: ')).trim();
    const expiry = (await askQuestion('Срок годности (ГГГГ-ММ-ДД): ')).trim();
    items.push({ name, quantity: qty, price, category, expiry, purchased: false });
    saveData();
    console.log('Добавлено!');
}

function listItems() {
    if (items.length === 0) { console.log('Список пуст'); return; }
    console.log('=== Список покупок ===');
    items.forEach((it, i) => {
        const status = it.purchased ? '✅' : ' ';
        console.log(`[${status}] ${i+1}. ${it.name} (${it.quantity} шт.) - ${it.price.toFixed(2)} руб., кат. ${it.category}, до ${it.expiry}`);
    });
}

async function buyItem() {
    const idx = parseInt(await askQuestion('Номер товара для отметки: '));
    if (isNaN(idx) || idx < 1 || idx > items.length) { console.log('Неверный номер'); return; }
    const item = items[idx-1];
    item.purchased = !item.purchased;
    saveData();
    console.log(`Товар "${item.name}" ${item.purchased ? 'куплен' : 'возвращён'}!`);
}

async function removeItem() {
    const idx = parseInt(await askQuestion('Номер товара для удаления: '));
    if (isNaN(idx) || idx < 1 || idx > items.length) { console.log('Неверный номер'); return; }
    items.splice(idx-1, 1);
    saveData();
    console.log('Удалено');
}

function stats() {
    const total = items.length;
    const bought = items.filter(it => it.purchased).length;
    const totalPrice = items.reduce((sum, it) => sum + it.price * it.quantity, 0);
    const boughtPrice = items.filter(it => it.purchased).reduce((sum, it) => sum + it.price * it.quantity, 0);
    console.log(`Всего товаров: ${total}`);
    if (total > 0) console.log(`Куплено: ${bought} (${(bought/total*100).toFixed(1)}%)`);
    else console.log('Куплено: 0 (0%)');
    console.log(`Общая стоимость: ${totalPrice.toFixed(2)} руб.`);
    console.log(`Куплено на: ${boughtPrice.toFixed(2)} руб.`);
}

async function exportData() {
    const fname = (await askQuestion('Имя файла для экспорта (JSON): ')).trim() || 'export.json';
    fs.writeFileSync(fname, JSON.stringify(items, null, 2));
    console.log(`Экспортировано в ${fname}`);
}

async function importData() {
    const fname = (await askQuestion('Имя файла для импорта (JSON): ')).trim();
    if (!fname) { console.log('Имя не указано'); return; }
    try {
        const data = fs.readFileSync(fname, 'utf8');
        const imported = JSON.parse(data);
        items = items.concat(imported);
        saveData();
        console.log(`Импортировано ${imported.length} товаров`);
    } catch (e) {
        console.log('Ошибка импорта:', e.message);
    }
}

function exit() {
    saveData();
    console.log('До свидания!');
    rl.close();
}

loadData();
console.log('🛒 ShopSync — JavaScript Edition');
console.log('Команды: add, list, buy, remove, stats, export, import, exit');
rl.prompt();

rl.on('line', async (line) => {
    const cmd = line.trim();
    switch (cmd) {
        case 'add': await addItem(); break;
        case 'list': listItems(); break;
        case 'buy': await buyItem(); break;
        case 'remove': await removeItem(); break;
        case 'stats': stats(); break;
        case 'export': await exportData(); break;
        case 'import': await importData(); break;
        case 'exit': exit(); break;
        default: console.log('Неизвестная команда');
    }
    rl.prompt();
}).on('close', () => {
    saveData();
    process.exit(0);
});
