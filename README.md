# Cardinal — Mars Rover Küldetéstervező

> **Vadász Dénes Informatika Verseny 2026 — Programozói Kategória**

---

## Logó

```
![Csapat logó](Assets\logo.png)
```

---

## Csapat

| Név | Szerep |
|-----|--------|
| *Borók Máté* | *Útkereső algoritmus elkészítése* |
| *Tóth Péter* | *3D-s megjelenés* |
| *Kovács Dániel* | *Felhasználói felület* |
| *Hagymási Gyula Levente* | *Felkészítő tanár* |


---

## Fejlesztői környezet

- **Programozási nyelv:** C# (.NET 9.0)
- **UI keretrendszer:** Avalonia UI 11.3.12
- **3D megjelenítés:** Silk.NET (OpenGL)
- **IDE:** Visual Studio / JetBrains Rider

---

## A projektről

A Cardinal egy Mars rover útvonaltervező és vizualizációs alkalmazás. Egy 50×50-es rácsból álló marsi térképen kiszámítja az optimális ásványgyűjtési útvonalat, figyelembe véve az időkorlátot, az energiafelhasználást és az akadályokat, majd az eredményt egy interaktív dashboardon játssza vissza.

---

## Útvonaltervezés és optimalizálás

A Cardinal egy **Mohó Genetikus Algoritmust (GreedyGA)** alkalmaz a rover útvonaltervezési problémájának megoldására, amely az utazóügynök-probléma egy energia- és időkorlátokkal kibővített változata.

**Működési elv:**

1. **Előszámítás** — Az algoritmus futása előtt az A\* útvonalkeresővel előre kiszámítja a legrövidebb járható utat minden ásványklaszter-pár és a kiindulópont között. Az összes klaszterközi távolság, klaszteren belüli útvonal és hazaút el van tárolva, hogy futás közben ne kelljen újraszámolni.

2. **Klaszterezés** — A térképen lévő ásványok Manhattan-távolság alapján csoportokba kerülnek, így a rover egymás közelében lévő ásványokat együtt gyűjti be, ahelyett hogy keresztül-kasul szaladgálna a térképen.

3. **Mohó kezdőmegoldás** — Egy determinisztikus mohó menet állítja elő a kiindulási megoldást: mindig azt a következő klasztert választja, amelynek a legjobb az ásványok száma és az utazási költség aránya, feltéve, hogy a rover még időben visszaér a bázisra.

4. **Genetikus algoritmus** — Az útvonalak sorrendjét kódoló kromoszómák populációja 800 generáción keresztül fejlődik. Minden generációban a legjobb egyedeket turnajválasztással választja ki, azokat rendezési keresztezéssel kombinálja, majd 45%-os valószínűséggel mutációt alkalmaz (csere, megfordítás vagy áthelyezés). A 6 legjobb egyed minden generációban változatlanul megmarad (elitizmus).

5. **Lokális keresés** — Miután a GA konvergál, a legjobb útvonalat **2-opt** és **Or-opt** lokális keresési lépésekkel finomítja tovább, amelyek szegmensek megfordításával és egyes klaszterek áthelyezésével próbálnak még több ásványt kinyerni.

6. **Fizikai szimuláció** — Az egész folyamat során a rover energiáját és idejét lépésről lépésre szimulálja a program. A rover minden félórában megválasztja a sebességét az aktuális energiaszint és a napszak függvényében — nappal gyorsabban halad, mivel a napelemek töltik az akkumulátort, éjszaka óvatosabban.

Az eredmény egy teljes küldetésnapló, amelyet CSV formátumban exportál, és minden egyes lépésnél tartalmazza a pozíciót, a sebességet, az energiaszintet, a begyűjtött ásványok számát és az aktuális tevékenységet.

---

## Felhasználói felület

A dashboard Avalonia UI-jal készült, és két fő panelre tagolódik:

**Bal panel — Térkép és 3D nézet**
Egy görgethető 2D karakterrács jeleníti meg a Mars térképét valós időben: a rover aktuális pozíciója ki van emelve, a megtett útvonal pedig rávetítve látható. A térkép alatt helyezkedik el a 3D izometrikus megjelenítő (lásd lentebb).

**Jobb panel — Füles adatnézetek**
- **Rover fül** — Élő telemetriai widgetek: akkumulátorszint, töltési állapot, jelenlegi és tervezett pozíció, sebességmód, megtett távolság, valamint az ásványok darabszáma típusonként (kék, sárga, zöld).
- **Logika fül** — Az algoritmus állapotának és tervezési adatainak megjelenítésére fenntartott terület.
- **Naplók fül** — Egy görgethető konzol, amely időbélyeggel ellátott, színkódolt küldetési eseményeket jelenít meg.

**Alsó sáv — Médialejátszó vezérlők**
Egy idővonal-csúszka segítségével tetszőleges pillanatra lehet tekerni a küldetésben. A lejátszás/szünet, lépés előre és lépés hátra gombok lehetővé teszik a bármelyik pillanat képkockánkénti vizsgálatát.

---

## 3D megjelenítő

A Cardinal egy egyedi, Silk.NET-re épülő OpenGL izometrikus megjelenítőt tartalmaz, amely a küldetés egy második, háromdimenziós nézetét biztosítja, párhuzamosan a 2D térképpel. A megjelenítő egy teljes 3D modellt jelenít meg a marsi terepről: sziklák, valamint kék, sárga és zöld kristálymodellek a térképen meghatározott koordinátáikon. A rover modellje szinkronban mozog a küldetés lejátszásával, és lépésről lépésre követi a pozíciófrissítéseket. A megjelenítő teljes egészében játékmotor nélkül készült.

> **Megjegyzés:** A 3D modellek és textúrafájlok a csapat által lettek elkészítve.

---

## Futtatás

```
dotnet build
dotnet run -- --greedy-ga <órák>
```

A teljes felhasználói felülettel való indításhoz:
```
dotnet run -- -ui <órák>
```

Az `<órák>` helyére a küldetés időtartamát kell megadni órában (minimum 24). A program kiszámítja az útvonalat, legenerálja a `mission_log.csv` és `route.txt` fájlokat, majd megnyitja a dashboardot.

Alternatívaként a mellékelt batch szkript is használható:
```
rwui.bat
```

---

## Algoritmus összefoglalása (pszeudokód)

```
1.  Térkép betöltése, ásványklaszterek azonosítása
2.  A* útvonalak előszámítása minden klaszterpár között
3.  Mohó kezdőmegoldás generálása
4.  Populáció inicializálása: kezdőmegoldás + mutációk + véletlen permutációk
5.  Minden generációban (összesen 800):
        Összes kromoszóma pontozása szimulált ásványhozam alapján
        Elit egyedek változatlan átvitele
        Maradék helyek feltöltése turnajválasztással + rendezési keresztezéssel + mutációval
6.  2-opt és Or-opt lokális keresés alkalmazása a legjobb megoldásra
7.  Legjobb útvonal visszajátszása teljes fizikai szimulációval → napló exportálása
```
