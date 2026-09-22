# FlyPet 3D — На краю стола

<img width="1182" height="832" alt="Image" src="https://github.com/user-attachments/assets/7700a082-67df-4df7-b1cb-74fb684be8e9" />

**FlyPet 3D** — это Windows-приложение с виртуальным питомцем-мухой, поведение которой управляется упрощённой моделью мозга на основе данных коннектома дрозофилы FlyWire. Муха живёт на столе, исследует его, ест, пьёт, спит, чистится, пугается хлопушки и может делить стол с мухой второго игрока по прямому сетевому соединению.

Проект сочетает:

- WinForms-интерфейс и программный 3D-рендерер;
- LIF-симуляцию нейронной сети;
- загрузку CSV-данных коннектома;
- P2P-мультиплеер через TCP + TLS;
- сохранение профиля и состояния питомца.

---

## Возможности

- **Виртуальная муха-питомец**
  - голод, жажда, энергия, чистота;
  - сон и бодрствование;
  - поиск еды и воды;
  - чистка лапок;
  - реакция на угрозу и хлопок.

- **Нейросимуляция**
  - загрузка данных коннектома FlyWire;
  - LIF-модель нейронов;
  - роли нейронов: зрительные looming-нейроны, giant fiber, DNA, MDN, DNP09, DNG11, ESCW и др.;
  - сигналы мозга влияют на повороты, движение, испуг, груминг и возбуждение.

- **3D-стол**
  - программный рендерер без внешних 3D-библиотек;
  - вращение камеры, зум, сброс;
  - переключение 3D/2D.

- **Инструменты**
  - наблюдение;
  - фрукт;
  - вода;
  - мягкая кисть;
  - хлопушка.

- **Мультиплеер на двоих**
  - прямой TCP + TLS;
  - приглашение `flypet://...`;
  - синхронизация положения мухи и действий на столе;
  - у каждого игрока свой профиль и своя муха.

- **Профили**
  - отдельные сохранения для разных игроков;
  - хранение в `%LocalAppData%\FlyPet`.

---

## Требования

- Windows 10/11 x64;
- .NET Framework 4.8;
- Visual Studio 2022 с workload **.NET desktop development**;
- .NET Framework 4.8 targeting pack;
- для сборки из командной строки — MSBuild или совместимый .NET SDK.

Проект настроен на:

- `TargetFramework`: `net48`
- `PlatformTarget`: `x64`
- `OutputType`: `Exe`
- `UseWindowsForms`: `true`
- точка входа: `FlyPet.Entry`

---

## Сборка и запуск

### Visual Studio

1. Откройте `FlyPet3D.sln`.
2. Выберите конфигурацию `Release` и платформу `x64`.
3. Соберите решение.
4. Запустите `FlyPet3D.exe`.

### Командная строка

```bat
msbuild FlyPet3D.sln /p:Configuration=Release /p:Platform=x64
```

или, если установлен .NET SDK и targeting pack:

```bat
dotnet build FlyPet3D.sln -c Release
```

После сборки исполняемый файл появится в `bin\x64\Release\` или рядом с проектом в зависимости от конфигурации.

---

## Данные коннектома

Для работы нейросимуляции нужны CSV-файлы коннектома, сжатые gzip.

Проект ожидает как минимум:

- `classification.csv.gz`
- `connections_princeton.csv.gz`
- `consolidated_cell_types.csv.gz`

Дополнительно может использоваться:

- `neurons.csv.gz`
- `coordinates.csv.gz` — для координат нейронов в консольном бенчмарке.

В `FlyPet3D.csproj` эти файлы могут быть подключены как встроенные ресурсы:

```xml
<EmbeddedResource Include="FlyPet.Data.classification.csv.gz" LogicalName="FlyPet.Data.classification.csv.gz" />
<EmbeddedResource Include="FlyPet.Data.connections_princeton.csv.gz" LogicalName="FlyPet.Data.connections_princeton.csv.gz" />
<EmbeddedResource Include="FlyPet.Data.consolidated_cell_types.csv.gz" LogicalName="FlyPet.Data.consolidated_cell_types.csv.gz" />
<EmbeddedResource Include="FlyPet.Data.neurons.csv.gz" LogicalName="FlyPet.Data.neurons.csv.gz" />
```

Если файлы не встроены в сборку, положите их рядом с `.exe`. Приложение также ищет `classification.csv.gz` вверх по дереву каталогов от папки запуска.

### Проверка данных

```bat
FlyPet3D.exe --verify-data verify.json
```

Команда запускает загрузку коннектома и записывает JSON с результатом проверки. Ожидаемые значения включают:

- около `139255` нейронов;
- около `3732460` связей;
- `314` looming-нейронов;
- не менее `64` мс биологического времени симуляции;
- наличие спайков.

---

## Управление

| Действие | Управление |
|---|---|
| Выбрать инструмент | `1`–`5` |
| Наблюдение | `1` или правая кнопка мыши |
| Фрукт | `2` |
| Вода | `3` |
| Кисть | `4` |
| Хлопушка | `5` |
| Применить инструмент на столе | левая кнопка мыши |
| Пауза | `Space` |
| Лампа / сон | `L` |
| Сбросить камеру | `R` |
| Переключить 3D/2D | `F2` |
| Вращать камеру в 3D | средняя кнопка мыши + перетаскивание |
| Приблизить/отдалить | колесо мыши |

В 3D-режиме на столе можно вращать камеру и приближать её. В 2D-режиме доступны те же инструменты, но без объёмной камеры.

---

## Мультиплеер

Мультиплеер рассчитан на двух игроков.

### Хост

1. Нажмите кнопку приглашения в главном окне.
2. Укажите внешний IPv4-адрес или адрес VPN.
3. Выберите TCP-порт, по умолчанию `47831`.
4. Нажмите **Открыть стол**.
5. Скопируйте приглашение вида:

```text
flypet://<host>:<port>/<key>#<fingerprint>
```

### Присоединение

1. Вставьте полное приглашение `flypet://...`.
2. Нажмите **Присоединиться**.

### Важно

- Для игры через интернет порт должен быть открыт в брандмауэре и на роутере.
- Автоматического обхода NAT нет.
- Альтернатива — общий VPN.
- Соединение защищено TLS с самоподписанным сертификатом; отпечаток передаётся в приглашении.
- Если запускаете два окна на одной машине, используйте разные профили:

```bat
FlyPet3D.exe --profile Alice
FlyPet3D.exe --profile Bob
```

---

## Профили и сохранения

По умолчанию данные хранятся в:

```text
%LocalAppData%\FlyPet\
```

Для профиля:

```text
%LocalAppData%\FlyPet\profiles\<profile>\profile.json
```

Сохраняются:

- `Id` игрока;
- имя мухи;
- состояние питомца: сытость, вода, энергия, чистота, возраст, количество приёмов пищи.

Сохранение выполняется периодически и при закрытии окна.

---

## Командная строка

```bat
FlyPet3D.exe
FlyPet3D.exe --profile Alice
FlyPet3D.exe --preview
FlyPet3D.exe --render shot.png --scene table
FlyPet3D.exe --render shot.png --scene swatter
FlyPet3D.exe --render shot.png --scene peer
FlyPet3D.exe --render shot.png --scene orbit
FlyPet3D.exe --render shot.png --scene 2d
FlyPet3D.exe --verify-data verify.json
```

Параметры:

| Параметр | Значение |
|---|---|
| `--profile <name>` | имя профиля |
| `--preview` | режим предпросмотра |
| `--render <file>` | сохранить кадр в PNG |
| `--scene <name>` | сцена для `--render`: `table`, `swatter`, `peer`, `orbit`, `2d` |
| `--verify-data <file>` | проверить загрузку данных и записать JSON |

---

## Структура проекта

```text
FlyPet3D.sln
FlyPet3D.csproj
app.config
app.ico
FlyPet/
  Art.cs                 — 2D-графика и иконки
  Brain.cs               — фоновый поток нейросимуляции
  Entry.cs               — точка входа, разбор аргументов
  FlyFrame.cs            — сетевой кадр состояния мухи
  Habitat.cs             — главное окно
  PeerSession.cs         — P2P TCP/TLS-сессия
  Pet.cs                 — состояние питомца
  PlayerProfile.cs       — профиль игрока
  RoomDialog.cs          — диалог мультиплеера
  SceneEffect.cs         — эффекты сцены
  TableAction.cs         — сетевое действие
  TableCamera.cs         — камера 3D
  TableItem.cs           — предмет на столе
  TableScene.cs          — программный 3D-рендерер
  TableTool.cs           — инструменты
  World.cs               — логика мира и поведения
NeuroFlyEngine/
  BrainSignals.cs        — выходные сигналы мозга
  Edge.cs                — синапс
  FlyBrainEngine.cs      — обёртка над симуляцией
  LIFSim.cs              — LIF-симуляция
  Neuron.cs              — нейрон
  Program.cs             — консольный бенчмарк
  RawConnectomeLoader.cs — загрузка CSV.GZ
  SignalBuilder.cs       — преобразование активности в сигналы
System*/                 — совместимость с .NET Framework 4.8
```

---

## Как это работает

1. `RawConnectomeLoader` читает `classification.csv.gz`, `connections_princeton.csv.gz` и `consolidated_cell_types.csv.gz`.
2. `LIFSim` строит разреженную матрицу связей и моделирует leaky integrate-and-fire нейроны.
3. `SignalBuilder` превращает частоты спайков в `BrainSignals`.
4. `World` использует сигналы мозга для движения, поворотов, испуга, груминга и поиска еды.
5. `Habitat` отображает мир и отправляет состояние мухи второму игроку через `PeerSession`.
6. `TableScene` рисует 3D-стол программным растеризатором.

---

## Ограничения

- Только Windows и .NET Framework 4.8.
- Только x64.
- Мультиплеер рассчитан на двух игроков.
- Нет автоматического NAT traversal.
- 3D-рендерер программный, поэтому производительность зависит от CPU.
- Данные коннектома не являются частью исходного кода, если не встроены в сборку.
- Проект не является медицинской или биологически точной моделью; это игровой питомец на упрощённой нейросимуляции.

---

## Лицензия

Проект лицензрован под GPLv3. 
