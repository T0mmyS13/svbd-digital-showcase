# Drone War — Interaktivní balistický simulátor 

2D fyzikální simulátor vytvořený v jazyce C# s využitím frameworků **Avalonia .NET** a **SkiaSharp**. Aplikace vizualizuje balistické trajektorie s realistickou fyzikou, dynamickými kolizemi s terénem a interaktivním zaměřováním pro zneškodnění blížícího se dronu.

## Hlavní technické vlastnosti

### 1. Fyzikální engine a balistické trajektorie
* **Numerická integrace:** Jádro simulace počítá trajektorii střely pomocí numerické integrace s pevným časovým krokem.
* **Vlivy prostředí:** Vektor rychlosti se dynamicky aktualizuje na základě gravitačního zrychlení a proměnlivého 3D vektoru větru, přičemž se uplatňuje koeficient odporu vzduchu.
* **Predikce trajektorie:** Aplikace obsahuje algoritmus pro rychlou predikci teoretické trajektorie bez vlivu větru, která slouží jako vizuální pomůcka pro míření, aniž by zatěžovala hlavní smyčku.

### 2. Terén a interpolace
* **Bilineární interpolace:** Pro plynulé vykreslování a vysoce přesnou detekci kolizí se výška terénu počítá pomocí bilineární interpolace. Místo prostého zaokrouhlování na nejbližší bod algoritmus počítá vážený průměr čtyř okolních bodů mřížky.
* **Dynamické krátery:** Při dopadu střely do terénu algoritmus modifikuje výškovou mapu a vytvoří trvalý kráter pomocí parabolického profilu hloubky v závislosti na vzdálenosti od epicentra dopadu.
* **Barevná topografická mapa:** 2D pohled shora převádí číselná výšková data na barevnou paletu pomocí lineární interpolace (od světle modré pro vodu, přes zelené nížiny až po bílé vrcholky).

### 3. Vykreslování a vizualizace
* **Korekce vizuálního úhlu:** Spodní panel zobrazuje řez terénem a let střely. Kvůli obrovskému rozdílu v měřítku osy X a osy Y nelze hlaveň děla vykreslit pod jejím skutečným fyzikálním úhlem. Systém proto vypočítává korekční vizuální úhel, aby hlaveň SVG modelu v grafu vždy plynule navazovala na křivku trajektorie.
* **SVG Rasterizace:** Herní entity (tělo tanku, dynamická hlaveň, dron a projektil) jsou vykreslovány pomocí vektorových obrázků (SVG).
* **Částicové efekty:** Animace exploze využívá radiální gradienty, které dynamicky mění barvy v čase (od bílo-žluté iniciace přes oranžový oheň až po šedý kouř).

## Architektura systému

Kód je rozdělen do modulů oddělujících logiku simulace od vizualizace:
* `Models/`: Obsahuje logiku simulace a fyzikální výpočty (`Cannon.cs`, `Projectile.cs`, `Wind.cs`, `Drone.cs`).
* `UI & Vykreslování`: Zajišťováno přes Avalonia XAML okna a události na plátně SkiaSharp, které čtou stavy z datových modelů.

## Ovládání

Hra je plně interaktivní pomocí klávesnice a myši:
* **Změna azimutu:** `A` / `D` nebo **Tažení levým tlačítkem** myši v mapě.
* **Změna zenitu (náklonu):** `W` / `S` nebo **Kolečko myši** / Tažení ve spodním grafu.
* **Změna síly výstřelu:** `+` / `=` nebo **Pravé tlačítko myši + Kolečko**.
* **Výstřel:** `Mezerník` nebo **Dvojklik levým tlačítkem** na dělo.

## Jak projekt spustit

### Požadavky
* .NET SDK (s podporou Avalonia UI)
* Windows / Linux / macOS

### Spuštění
Aplikace načítá data terénu ze souborů `.ter` uložených ve složce `data`. K rychlému spuštění z příkazové řádky slouží připravený skript `Run.cmd`, kterému lze předat číslo scénáře (0, 1, 2 nebo 3).
```bash
Run.cmd "Cislo_scenare"