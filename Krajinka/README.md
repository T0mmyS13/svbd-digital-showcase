# Krajinka — 3D Terrain Renderer

Aplikace **Krajinka** je pokročilý 3D renderer krajiny v reálném čase implementovaný v jazyce C# s využitím moderního .NET 8 a grafického rozhraní OpenTK (OpenGL 3.3 Core Profile).

Projekt demonstruje integraci procedurálního generování terénu z rastrových dat, vlastní fyzikální simulaci pohybu a optimalizovaný systém prostorových kolizí.

---

# Klíčové technické vlastnosti

## 1. Generování terénu a 16bitová elevace

- **Mapování z RGBA PNG:**  
  Geometrie terénu, rozmístění statických 3D objektů a materiálové pokrytí se generují dynamicky analýzou barevných kanálů načtené PNG mapy pomocí knihovny SkiaSharp.

- **16bitová přesnost výšky:**  
  Výška terénu je zakódována kombinací kanálů R (dolní bajt) a B (horní bajt), což poskytuje jemný rozsah 0–65535 výškových úrovní.

  Světové měřítko je definováno konstantou `HeightScale = 0.05`, kde 1 jednotka odpovídá 5 cm.

- **Bilineární interpolace:**  
  Pro plynulý pohyb pozorovatele v libovolných souřadnicích se přesná výška nad terénem vypočítává v reálném čase bilineární interpolací čtyř nejbližších vzorků mřížky.

---

## 2. Shader Pipeline a procedurální atmosféra

Vykreslovací smyčka využívá specializované GLSL shadery pro různé komponenty scény.

### Základní shader (`basic.vert` / `basic.frag`)

- Počítá osvětlení scény pomocí Lambertova difuzního modelu doplněného o ambientní složku (`0.15`).
- Na základě sklonu a výšky terénu plynule prolíná textury trávy, skály a hlíny pomocí funkce `smoothstep`.

### Shader pro vodu (`water.frag`)

- Vykresluje animovanou vodní hladinu na pevné výšce `WaterSurfaceLevel = 1.081 m`.
- Animace cykluje mezi 40 texturovanými snímky s intervalem `0.08 s`.
- Fragment shader kombinuje Lambertův difuzní model s Phongovým zrcadlovým odleskem (`shininess = 64`) pro realistické odlesky světla.

### Shader pro oblohu (`sky.vert` / `sky.frag`)

- Obloha je renderována jako fullscreen trojúhelník generovaný přímo z `gl_VertexID`.
- Směr pohledu je rekonstruován pomocí inverzních projekčních a pohledových matic.
- Slunce je simulováno jako měkký disk pomocí funkce `smoothstep`.
- Barva oblohy i slunce se dynamicky interpoluje mezi denním a nočním cyklem podle výšky slunce nad horizontem.

---

## 3. Fyzika a retro-inspirovaný pohyb

- **Simulace gravitace:**  
  Vertikální pohyb (skoky a pády) je simulován Eulerovou integrací s fyzikálními konstantami:

  - gravitace: `9.81 m/s²`
  - počáteční rychlost výskoku: `3.70 m/s`
  - tolerance kontaktu se zemí: `GroundSnapEpsilon = 8 cm`

  Výška očí hráče je fixována na `1.8 m` nad terénem.

- **Doom (1993) Movement:**  
  Horizontální rychlost pohybu hráče (`MovementSpeed`) vychází z referenční rychlosti původního enginu hry Doom. Po přepočtu na reálné měřítko (`32 jednotek = 1 metr`) odpovídá rychlosti `9.11 m/s`.

- **Pohyb ve vodě:**  
  Při průchodu vodním sloupcem se rychlost automaticky násobí koeficientem `0.6`, zároveň se přehrávají odpovídající audio efekty.

---

## 4. Optimalizovaný kolizní systém a prostorová mřížka

- **AABB hitboxy:**  
  Detekce kolizí s 3D objekty (stromy, kameny) využívá prostorově zarovnané obdélníkové hitboxy v rovině XZ.

- **Spatial Grid:**  
  Pro eliminaci náročného prohledávání všech objektů (`O(N)`) jsou instance rozděleny do prostorové mřížky odpovídající mřížce terénu.

  Kolizní systém kontroluje pouze objekty v okolí budoucí pozice hráče v poloměru tří buněk (~1.5 m).

- **Tierovaný pohyb po objektech:**  
  Systém detekuje výšku chodidel hráče vůči vrškům kamenů.

  Pokud je rozdíl menší než `RockTopStepEpsilon = 8 cm`, pohyb není zablokován a hráč může plynule vystoupit na objekt.

- **Interaktivní flóra:**  
  Květiny generované na volných místech reagují na kolizi, přehrají zvukový efekt a následně jsou odstraněny ze scény.

---

# Architektura systému

Projekt je striktně objektově orientovaný a rozdělený do specializovaných tříd se separací odpovědností.

| Třída | Odpovědnost |
|---|---|
| `Program` / `Window` | Inicializace aplikace, správa hlavní smyčky, render a zpracování vstupů |
| `Terrain` | Analýza PNG mapy, generování meshů, výpočet normál a interpolace výšek |
| `Camera` | Správa pohledové matice, yaw/pitch ovládání a fyzikální simulace pohybu |
| `Model` / `ObjLoader` | Načítání `.obj` modelů, `.mtl` materiálů a textur |
| `Shader` | Kompilace GLSL shaderů a správa uniform proměnných |
| `CollisionSystem` | Detekce AABB kolizí a prostorové dělení scény |
| `AudioManager` | Asynchronní přehrávání zvukových efektů a ambientních stop |

---

# 🛠️ Spuštění projektu

## Požadavky

- .NET 8 SDK
- IDE podporující C#
  - Visual Studio 2022
  - JetBrains Rider
  - VS Code
- Grafická karta s podporou OpenGL 3.3 Core Profile

---

## Postup spuštění

1. Naklonujte repozitář nebo stáhněte složku projektu.
2. V kořenovém adresáři spusťte:

   ```bash
   dotnet run
   ```

3. Po spuštění se v konzoli zobrazí seznam dostupných map.
4. Zadejte index mapy a potvrďte klávesou Enter.
5. Pokud je dostupná pouze jedna mapa, načte se automaticky.

---

# Informace o projektu

Tato aplikace byla vytvořena jako semestrální práce z předmětu **Základy počítačové grafiky** na Fakultě aplikovaných věd Západočeské univerzity v Plzni.

Veškeré externí assety (modely, textury a audio) jsou použity v souladu s licencemi Creative Commons a OpenGameArt.