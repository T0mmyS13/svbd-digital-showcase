# Celočíselná kalkulačka s neomezenou přesností 

Konzolová aplikace v jazyce **ANSI C**, která funguje jako interpret aritmetických výrazů zadaných v infixové formě. Výpočty nejsou omezeny rozsahy standardních hardwarových typů, ale pouze dostupnou pamětí RAM.

## Hlavní technické vlastnosti

### 1. Interní reprezentace (Báze 256)
* **Dynamické pole:** Čísla jsou uložena jako struktura s dynamicky alokovaným polem bajtů (`unsigned char`) spravovaným pomocí funkcí `malloc` a `realloc`.
* **Efektivita:** Využívá se celá báze 256, tedy celý rozsah jednoho bajtu, což šetří operační paměť a usnadňuje bitové operace.
* **Řazení Little-Endian:** Nejméně významný bajt je na indexu 0, což zjednodušuje přidávání nových bajtů při přenosech do vyšších řádů.

### 2. Dvojkový doplňkový kód na vstupu a výstupu
* Interně kalkulačka používá oddělené uložení absolutní hodnoty a znaménka (`is_negative`), což je výhodnější pro násobení a dělení.
* **Vstup/Výstup:** Při načítání a výpisu binárních a šestnáctkových čísel aplikace pro kódování záporných čísel výhradně využívá dvojkový doplňkový kód. Šířka doplňkového kódu se dynamicky odvozuje z délky vstupního řetězce.

### 3. Parsování výrazů a algoritmus Shunting-yard
* **Tokenizace:** Textový vstup je rozdělen na tokeny (čísla, operátory, závorky) za použití paměťově úsporné struktury `union`.
* **Převod na RPN:** Pomocí Dijkstrova algoritmu *Shunting-yard* se infixový zápis převede do postfixové notace (Reverzní polské notace), čímž se vyřeší priority operátorů bez nutnosti závorek.
* **Vyhodnocení:** Výsledný postfixový řetězec se lineárně zpracuje zleva doprava pomocí jednoho generického zásobníku, který přes parametr `item_size` ukládá prvky libovolné velikosti.

### 4. Implementované algoritmy
* **Sčítání a odčítání:** Algoritmus prochází čísla od nejnižšího bajtu k nejvyššímu a počítá "pod sebe" s přenosem (carry) pro hodnoty nad 255.
* **Násobení:** Implementováno jako klasické školní násobení se složitostí $O(N^2)$, kde se každým bajtem jednoho čísla vynásobí celé druhé číslo s posunem.
* **Dělení a Modulo:** Využívá metodu dělení po bitech (*Bitwise Long Division*), kde se bit po bitu posouvá aktuální zbytek a odečítá dělitel.
* **Binární umocňování:** Rychlé umocňování metodou *Square and Multiply*, které díky rozepsání exponentu ve dvojkové soustavě výrazně snižuje počet kroků.

## Architektura programu

Kód je striktně rozdělen do 4 modulů:
* `main`: Vstupní bod, řízení toku programu, komunikace s uživatelem a volba režimu.
* `parser`: Tokenizace textu a převod výrazu do RPN.
* `bignum`: Datové struktury velkých čísel a matematické operace.
* `stack`: Generický, typově nezávislý zásobník využívaný pro operátory i čísla.

## Jak program spustit

### Překlad
Kompilace probíhá standardně přes přiložený `Makefile` a nevyžaduje žádné speciální knihovny.
* **Linux:** Příkazem `make` se vytvoří spustitelný soubor `calc`.
* **Windows:** Příkazem `make -f Makefile.win` se zkompiluje soubor `calc.exe`.

### Režimy běhu
1. **Interaktivní režim:** Spuštěním bez parametrů se otevře řádka `>` pro přímé zadávání výrazů, kterou lze ukončit napsáním `quit`.
2. **Souborový režim:** Předáním názvu souboru jako argumentu kalkulačka sekvenčně načte a vyhodnotí všechny příklady ze souboru.

### Podporované formáty a operátory
* **Soustavy:** Desítková, Binární s prefixem `0b`, Šestnáctková s prefixem `0x`.
* **Přepínání výstupu:** Pomocí textových příkazů `dec`, `bin` a `hex` lze měnit číselnou soustavu, ve které se výsledky tisknou.
* **Operátory:** `+`, `-`, `*`, `/`, `%` (modulo), `^` (mocnina), `!` (faktoriál) a unární mínus.

---
*Tento projekt byl vytvořen jako semestrální práce z předmětu Programování v jazyce C na Fakultě aplikovaných věd Západočeské univerzity.*
