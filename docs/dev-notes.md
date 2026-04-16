# Bygd Dev Notes

## 2026-04-15: Свободное размещение, devmode, круги

### Свободное размещение столов

Убраны проверки при постройке:
- **Стол Старосты** — больше не требует крышу при установке
- **Курьерский Столб** — больше не требует крышу и аванпост уровня 2+ рядом

Проверки перенесены в условия спавна NPC:
- Староста появляется когда: крыша + кровать + огонь
- Курьер появляется когда: крыша + кровать + переданный аванпост уровня 2+ рядом

Идея: стол — якорь, игрок строит дом вокруг него. Это открывает путь
для автостройки из чертежей (Plan → Approve → Build).

### DevMode

`bygd devmode` — тоггл для разработки:
- Поселенцы не уходят при плохих условиях
- Потребление ресурсов отключено (нет деградации/голода/abandoned)

`bygd setlevel <N>` — прямая установка уровня ближайшего аванпоста
без проверок. Если не передан — передаётся автоматически.

### Круги от верстака

**Проблема:** Все наши piece'ы клонируются от `piece_workbench`.
`CraftingStation` удалялся, но дочерние `CircleProjector` и `EffectArea`
оставались → постоянно видимые круги радиуса.

**Решение:** `StripWorkbenchComponents()` в `Plugin.cs` удаляет:
- `CraftingStation`
- `CircleProjector` (дочерний GO)
- `EffectArea` (дочерний GO)
- Обнуляет `Piece.m_craftingStation`

### Чертежи

- `viking_aframe_small.blueprint` удалён
- `PiNoKi_Hut.blueprint` → `PiNoKi_Longhouse.blueprint` (187 piece'ов, 2 этажа)
- `PiNoKi_SmallHut.blueprint` — 54 piece'а, 1 этаж
- Привязка к уровню: SmallHut для 1-2, Longhouse для 3+

## 2026-04-15: Рефакторинг структуры кода

### Что изменилось

Разделение ответственностей по правилу «1 файл = 1 ответственность».
Plugin.cs: 617 → 118 строк. CourierPostComponent.cs: 507 → 261 строк.

### Новые файлы

| Файл | Описание |
|------|----------|
| `Framework/Localizations.cs` | Все строки UI (русский + английский) |
| `Commands.cs` | Обработчики консольных команд `bygd *` |
| `Courier/CourierDeliveryRunner.cs` | Логика доставки: патруль, маунт (кабан), движение |
| `Patches/SignPatches.cs` | Патч Sign.SetText (станции @, путеточки #) |
| `Patches/DevPatches.cs` | Terminal.InitTerminal, Console.Awake, Player.OnSpawned |
| `Patches/OutpostPiecePatches.cs` | Placement/Awake/SetCreator для OutpostTable |
| `Patches/OutpostEffectPatches.cs` | EffectArea.GetBaseValue, PrivateArea.CheckAccess |
| `Patches/CourierPiecePatches.cs` | Placement/Awake/SetCreator для CourierPost |
| `Patches/MailPiecePatches.cs` | Awake/SetCreator для MailPost |

### Перемещённые файлы

| Было | Стало |
|------|-------|
| `CartHorse.cs` (корень) | `Transport/CartHorse.cs` |
| `RouteGraph.cs` (корень) | `Transport/RouteGraph.cs` |

### Консолидация

- `"BygdCourierBoar"` → `PrefabNames.CourierBoar`
- Inline-рефлексия в OutpostVisuals (`zdo.GetType().GetMethod(...)`) → `Reflect.ZDO_GetFloat`, `Reflect.ZDO_Set_Float`
- Все Harmony-патчи теперь в `Patches/` (кроме `AISuppression.cs` в Framework — фундаментальный)

### Структура модулей (актуальная)

```
Plugin.cs              — инициализация, регистрация piece'ов
Commands.cs            — консольные команды bygd *
Framework/
  Log.cs               — логирование
  PrefabNames.cs       — константы имён префабов
  Reflect.cs           — рефлексия (FieldInfo/MethodInfo)
  Localizations.cs     — строки UI (ru/en)
  AISuppression.cs     — подавление MonsterAI
Outpost/
  OutpostTable.cs      — OutpostRegistry, RoofCheck, OutpostTable_Runtime
  OutpostTableComponent.cs — MonoBehaviour аванпоста
  OutpostResources.cs  — ZDO-ресурсы (дрова/калории/смола)
  OutpostComfort.cs    — расчёт комфорта
  OutpostTransfer.cs   — OutpostTransferState (ZDO read/write)
  OutpostWard.cs       — OutpostCache, OutpostWard
  OutpostSettlerBinding.cs — привязка поселенец ↔ стол
  OutpostSettlerManager.cs — спавн/деспавн поселенцев
  OutpostChestReceiver.cs — сундук подношений
  OutpostVisuals.cs    — визуализация (огонь, еда, дрова)
Courier/
  CourierPost.cs       — CourierPost_Runtime
  CourierPostComponent.cs — MonoBehaviour столба курьера
  CourierDeliveryRunner.cs — логика доставки/маунта
  CourierBinding.cs    — ZDO-связи столб ↔ курьер
  CourierManager.cs    — спавн/деспавн курьеров
Mail/
  MailPost.cs          — MailPost_Runtime
  MailPostComponent.cs — MonoBehaviour почтового пункта
NPC/
  BaseNPC.cs           — базовый класс NPC
  SettlerNPC.cs        — реплики поселенца
  CourierNPC.cs        — реплики курьера
Transport/
  CartHorse.cs         — управление караваном (Lox+телега)
  RouteGraph.cs        — Dijkstra-маршрутизация
  CourierWalker.cs     — движение курьера/кабана
  CourierPatrol.cs     — оркестрация патруля
  CourierDelivery.cs   — утилиты proximity
  MailBag.cs           — инвентарь курьера
  MountConfig.cs       — конфигурация транспорта
Patches/
  VagonPatches.cs      — патчи Vagon (FixedUpdate, InUse, Interact)
  SignPatches.cs       — Sign.SetText
  DevPatches.cs        — Terminal, Console, Player.OnSpawned
  OutpostPiecePatches.cs    — SetCreator, Awake для OutpostTable
  OutpostEffectPatches.cs   — EffectArea.GetBaseValue, PrivateArea.CheckAccess
  CourierPiecePatches.cs    — SetCreator, Awake для CourierPost
  MailPiecePatches.cs       — SetCreator, Awake для MailPost
```

## 2026-04-14: Blueprints — интеграция с PlanBuild

### PlanBuild как опциональный инструмент

PlanBuild (Thunderstore) — мод для создания/сохранения чертежей.
Установка: `BepInEx/plugins/MathiasDecrock-PlanBuild/`.
Blueprints: `BepInEx/config/PlanBuild/blueprints/*.blueprint`.

**Нет публичного API** для спавна из C# — Bygd парсит формат самостоятельно.

### Формат .blueprint

Текстовый. Заголовки (`#Name:`, `#Creator:`, `#Category:`), секции
(`#SnapPoints`, `#Terrain`, `#Pieces`). Каждый piece:
```
name;category;posX;posY;posZ;rotX;rotY;rotZ;rotW;additionalInfo;scaleX;scaleY;scaleZ
```
Позиции relative к центру. Сортировка: Y → X → Z (фундамент → стены → крыша).
Полное описание формата: `docs/valheim-api-reference.md` → PlanBuild.

### План интеграции

- Bygd читает .blueprint файлы из `Bygd/blueprints/` (свои шаблоны деревни)
- Спавнит pieces через `ZNetScene.instance.GetPrefab` + `Instantiate`
- PlanBuild нужен только для создания/редактирования чертежей
- При level-up или по команде `bygd build` — автопостройка

### Готовые чертежи

Комьюнити: https://www.valheimians.com/builds/?share=downloadable
Файлы .vbuild / .blueprint копируются в папку blueprints.

## 2026-04-14: Доставка по запросу, кабан-курьер, визуал-фиксы

### Доставка по запросу вместо автопатруля

Автопатруль создавал бесконечные циклы и кучу багов. Заменён на:
игрок interact с MailPost → вызывает курьера → курьер (или кабан) идёт,
забирает, доставляет, возвращается. Одна доставка за раз.

Файл: `Mail/MailPostComponent.cs` (Interact → `FindNearestCourierPost` →
`RequestDelivery`), `Courier/CourierPostComponent.cs` (`RequestDelivery` →
`TryStartDirect` → `StartMovingToTarget`).

### Кабан-курьер (уровень 3)

Кабан бежит **вместо** курьера, не с ним. Курьер остаётся дома.
`AttachStart` для NPC не работает (NPC не перемещается с mount'ом).
`SetParent` не работает (Rigidbody + ZNetView конфликтуют).

**Решение:** кабан — отдельное существо, `CourierWalker` вешается на него.
- `SpawnBoar()` — один кабан на весь рейс (не на каждый leg!)
- `Procreation` удаляется (без размножения)
- ZDO маркер `bygd_courier_boar=1` для cleanup
- `AISuppressionMarker` подавляет стандартный AI кабана
- Пустой кабан бежит (`ShouldRun=true`), с грузом идёт шагом
- `CleanupBoar()` + `bygd reset` — удаляют всех курьерских кабанов

### Визуализация еды/дров отключена

`OutpostVisuals.UpdateFoodDisplay/UpdateWoodDisplay` — спавнили клоны
(кружки, стопки дров) без контроля, бесконтрольно множились.
Отключены. `bygd cleanup` удаляет оставшийся декор (`bygd_decor`,
`bygd_wood_decor`).

### CourierWalker — опыт

- `BaseAI.MoveTo(dt, target, 0f, run)` — `run=true` для бега, `false` для шага
- Работает с suppressed AI (не нужен UpdateAI)
- Кабан застревает чаще чем Dverger (маленький, цепляется за terrain)
- Таймаут 15 сек → телепорт к цели (защита от бесконечного застревания)
- Valheim выгружает объекты за ~64м → CourierObject=null → fallback на simulated

### Зоны Valheim и NPC

Valheim выгружает GameObjects за ~64м от игрока. Наши NPC/кабаны
уничтожаются при выгрузке зоны. `CourierObject` становится null.
Для MVP: если объект потерян и игрок далеко → simulated mode (по таймеру).
Если игрок подошёл → можно респавнить (но сложно, пока не реализовано
надёжно).

### Console команды (актуальные)

```
bygd debug    — диагностика: комфорт, аванпосты, сундуки, NPC
bygd levelup  — принудительное повышение уровня
bygd patrol   — принудительный запуск патруля
bygd reset    — экстренный сброс: остановить патрули, убить кабанов, респавн курьеров
bygd relink   — сброс привязок сундуков и перепривязка
bygd cleanup  — удалить дубли NPC, призрачные сундуки, декор
bygd list     — станции и путеточки
bygd go A B   — отправить караван (Lox) между станциями
```

## 2026-04-14: Bugfixes — призраки, сундуки, комфорт, Harmony

### Object.Destroy vs ZNetScene.instance.Destroy

**Проблема:** `Object.Destroy(gameObject)` удаляет только локальный GameObject,
но ZDO остаётся в сейве. При следующей загрузке мира Valheim воссоздаёт объект
из сохранённого ZDO → дубликаты NPC после каждого reload.

**Решение:** Для сетевых объектов (NPC, сундуки) использовать
`ZNetScene.instance.Destroy(gameObject)` — удаляет и GameObject, и ZDO.

**Правило:** `Object.Destroy` — только для визуального декора (без ZNetView).
Всё остальное — `ZNetScene.instance.Destroy`.

### Призрачные сундуки

**Проблема:** После уничтожения сундука (гоблинами, игроком) Container объект
может остаться в памяти Unity — `FindObjectsOfType<Container>()` его находит,
он `activeInHierarchy=true`, `IsValid()=true`, но физически не существует.
Маркер привязки `smartcart_chest_table_id` остаётся → мод думает что сундук
привязан и не привязывает новый.

**Решение (проверено, работает):** Проверка `WearNTear` компонента.
Каждый реальный поставленный сундук имеет `WearNTear` (прочность).
Призрак после уничтожения — нет. Работает корректно с погребами
и подземными постройками.

```csharp
// OutpostChestCollector.IsValidChest()
if (container.GetComponent<WearNTear>() == null)
    return false; // призрак
```

### Comfort: HashSet, не List

**Проблема:** `Piece.s_allComfortPieces` — это `HashSet<Piece>`, не `List<Piece>`.
Cast `as List<Piece>` возвращал null → комфорт всегда 0.

**Решение:** Cast как `IEnumerable`, итерация через `foreach (object obj in ...)`.
Обнаружено через `monodis`:
```
.field private static initonly class HashSet`1<class Piece> s_allComfortPieces
```

### Harmony: Container.OnContainerChanged не патчится

**Проблема:** `Container.OnContainerChanged` регистрируется как callback через
`Inventory.m_onChanged` (delegate). Harmony patch на него не срабатывает —
метод вызывается через `Action.Invoke()`, а не напрямую.

`Inventory.Changed()` — приватный (`private hidebysig`), тоже не патчится
надёжно.

**Решение:** Убрали Harmony patch. Вместо этого — polling в `ConditionLoop`
каждые 5 сек: `OutpostChestCollector.TryCollect()` напрямую проверяет
привязанный сундук и собирает ресурсы.

### Console команды (новые)

- `smartcart debug` — полная диагностика: комфорт, аванпосты, сундуки, NPC
- `smartcart levelup` — принудительная проверка/повышение уровня
- `smartcart relink` — сброс всех привязок сундуков и перепривязка ближайших
- `smartcart cleanup` — теперь также чистит призрачные сундуки

## 2026-04-14: Почтовая сеть — MailPost + Патруль курьера

### Что добавлено

**Почтовый Пункт** (`piece_mailpost`) — остановка курьера, ставится где угодно.
Рядом — сундук (посылка) + табличка с `@адрес` (куда везти).
Курьер автоматически патрулирует все пункты, собирает и развозит посылки.

### Поток

1. Игрок ставит MailPost + сундук + табличку `@имя_станции`
2. Курьер автоматически уезжает на патруль
3. На каждом MailPost: выгружает доставки, забирает новые посылки, очищает табличку
4. Возвращается домой → cooldown 5 мин → новый круг

### Оптимизация: simulated mode

Если игрок далеко от маршрута (>64м) — караван не спавнится физически.
Доставка рассчитывается по ETA: `distance / speed`. Проверка прогресса
каждые 5 сек в `CourierPostComponent.ConditionLoop`.

### Авто-регистрация аванпостов как станций

Переданные аванпосты автоматически регистрируются в `SmartCartPlugin.Stations`
с ключом `outpost_<tableKey>`. RouteGraph подхватывает их для маршрутизации.

### Новые файлы

- `Mail/MailPost.cs` — piece + Harmony патчи (OnPlaced, Restore)
- `Mail/MailPostComponent.cs` — MonoBehaviour: link chest/sign, GetDestination, ClearSign
- `Transport/MailBag.cs` — сумка курьера: PickupFrom/DeliverTo
- `Transport/CourierPatrol.cs` — патруль: кольцевой маршрут, simulated mode, nearest-neighbor
- `Transport/MountConfig.cs` — конфигурация животного (prefab, cargo, speed, deliveryFee)
- `Transport/CourierDelivery.cs` — утилиты proximity (IsPlayerNearRoute, IsPlayerNearPoint)

### Изменённые файлы

- `CartHorse.cs` — добавлен `OnArrived` callback, `StartTripDirect`, `GetCart()`
- `Courier/CourierPostComponent.cs` — убран delivery UI, добавлен автопатруль
- `Outpost/OutpostTableComponent.cs` — авто-регистрация/дерегистрация станции

### Доставка в аванпост

Курьер при возвращении и на остановках выгружает посылки в linked chest
аванпоста. `OutpostChestReceiver_Patch` автоматически обрабатывает содержимое
как подношение — та же логика что и ручное снабжение игроком.

## 2026-04-14: Comfort system, Resin, Courier Post

### Система комфорта

Level-up аванпоста теперь зависит от комфорта в точке стола старосты.
Комфорт считается по тому же алгоритму, что и в Valheim: для каждой
`Piece.ComfortGroup` берём max `m_comfort`, суммируем + базовый 1.

| Уровень | Комфорт | Доп. условие |
|---------|---------|--------------|
| 0 → 1  | ≥ 4     | запасы (6 дров, 240 калорий) |
| 1 → 2  | ≥ 7     | + доверие игрока (отношения ≥ 30) |
| 2 → 3  | ≥ 10    | запасы |
| 3 → 4  | ≥ 13    | запасы |

Файл: `Outpost/OutpostComfort.cs`

### Калории вместо штук еды

Еда считается в калориях через `item.m_shared.m_food × stack`.
Потребление: 40 калорий за цикл (≈ 1 средний продукт).
ZDO ключ: `smartcart_res_calories`.

### Смола (resin)

Новый ресурс. Игрок кладёт смолу в сундук подношений.
Факелы автоматически заправляются смолой, костры — дровами.
ZDO ключ: `smartcart_res_resin`.

### Почтовый Столб и NPC-курьер

Новый якорный объект **Почтовый Столб** (`piece_courier_post`) и NPC-курьер.
Доступен при уровне аванпоста ≥ 2 (доверие). Ставится под крышей в радиусе
переданного аванпоста. Для спавна курьера нужна кровать рядом со столбом.
Огонь отдельный не требуется — один на поселение, за ним следит стол старосты.

### ZDO ключи (новые)

| Ключ | Объект | Тип | Описание |
|------|--------|-----|----------|
| `smartcart_res_calories` | Стол Старосты | int | Калории (вместо штук еды) |
| `smartcart_res_resin` | Стол Старосты | int | Запас смолы для факелов |
| `smartcart_courier_post_key` | Почтовый Столб | string | Уникальный ID столба |
| `smartcart_courier_id` | Почтовый Столб | string | ZDO id привязанного курьера |
| `smartcart_courier_parent_table` | Почтовый Столб | string | Ключ родительского стола старосты |
| `smartcart_courier_post_key` | Курьер (NPC) | string | Ключ родительского столба |

### Новые файлы

- `Outpost/OutpostComfort.cs` — расчёт комфорта в точке, пороги для уровней
- `NPC/BaseNPC.cs` — базовый класс NPC (общая логика: mumble, interact, hover, init)
- `Courier/CourierPost.cs` — регистрация piece, Harmony патчи (placement, restore)
- `Courier/CourierPostComponent.cs` — MonoBehaviour, condition loop, spawn/despawn
- `Courier/CourierManager.cs` — spawn/despawn/refresh курьера
- `Courier/CourierBinding.cs` — ZDO-связи столб ↔ курьер ↔ стол
- `NPC/CourierNPC.cs` — компонент NPC: реплики, внешний вид, AI suppression

### Каскадная деградация

При abandoned стола старосты → все привязанные курьеры тоже деспавнятся
(`OutpostTableComponent.DespawnLinkedCouriers`).

## 2026-04-13: Fix for slow world load after Outpost bed check

### Symptom

После добавления логики `Стола Старосты` с проверкой `кровать + огонь`
загрузка мира начала заметно замедляться.

### Root Cause

Проблема была не в самой проверке кровати, а в жизненном цикле поселенца:

- `OutpostTableComponent` вешался на стол только в момент установки через
  `Piece.SetCreator`.
- После перезагрузки мира этот runtime-компонент не восстанавливался.
- Ссылка `_settler` жила только в памяти процесса и терялась после reload.
- При следующем срабатывании условий стол мог создать нового `Dverger`,
  хотя старый уже оставался в мире и в сейве.
- Из-за этого вокруг аванпостов копились лишние NPC, что раздувало мир и
  замедляло загрузку сохранения.

### What We Changed

- Добавили восстановление `OutpostTableComponent` через патч `Piece.Awake`,
  чтобы стол снова поднимал свою runtime-логику после загрузки мира.
- Добавили `OutpostTable_Runtime.EnsureComponent(...)`, чтобы не дублировать
  логику навешивания компонента.
- Перед спавном нового поселенца стол теперь ищет уже существующего
  `Dverger` рядом со своей точкой спавна и переиспользует его.
- Если рядом уже накопились дубли, стол оставляет ближайшего и удаляет
  лишних поселенцев.
- Проверки наличия кровати и огня переведены с `Physics.OverlapSphere(...)`
  на `Physics.OverlapSphereNonAlloc(...)`, чтобы не создавать лишние
  аллокации каждые 5 секунд.
- Добавлена защита от повторной регистрации `piece_outpost_table` в
  `ZNetScene.Awake`.
- Добавлена ZDO-привязка между столом и поселенцем:
  стол хранит свой `outpost_key` и `settler_id`, а поселенец хранит ключ
  родительского стола.
- При загрузке мира стол сначала пытается восстановить именно связанного
  поселенца по сохранённому идентификатору, и только потом использует
  fallback-логику.
- Добавлена команда `smartcart cleanup` для cleanup уже загруженных
  дубликатов и осиротевших поселенцев.

### Expected Result

- После загрузки мира стол продолжает управлять тем же поселенцем.
- Новый `Dverger` не создаётся при каждом reload.
- Уже накопленные дубликаты возле стола постепенно подчищаются.
- Новые сейвы не должны продолжать раздуваться из-за этой ветки.

## Исследование Valheim API через monodis

### DLL

```
$VALHEIM_MANAGED/assembly_valheim.dll
```

### Команды

```sh
# Все типы (классы, enum, struct)
monodis --typedef "$DLL" | grep ИмяКласса

# Все методы
monodis --method "$DLL" | grep ИмяКласса

# Все поля
monodis --field "$DLL" | grep ИмяКласса
```

### Паттерн: добавление нового Valheim API в мод

1. **Найти** поле/метод через `monodis --field` / `monodis --method`
2. **Проверить доступность**: если public → используем напрямую. Если private/internal → нужна reflection
3. **Reflection** → добавить в `Framework/Reflect.cs`:
   ```csharp
   public static readonly FieldInfo Piece_s_allComfortPieces =
       AccessTools.Field(typeof(Piece), "s_allComfortPieces");
   ```
4. **Документировать** в `docs/valheim-api-reference.md`

### Примеры уже исследованных API

| Что искали | Как нашли | Доступность |
|-----------|-----------|-------------|
| `Piece.m_comfort`, `m_comfortGroup` | `monodis --field` | public — напрямую |
| `Piece.s_allComfortPieces` | `monodis --field` | private — через `Reflect.cs` |
| `Piece.ComfortGroup` enum | binary search + monodis | public enum |
| `EnvMan.IsDay()` | binary search | public static |
| `Sign.GetText()`, `SetText()` | binary search | public |

## Local test workflow

Для локального цикла разработки можно использовать `Taskfile.yml`.

### Команды

```sh
task
```

Дефолтная задача делает сразу две вещи:

- `task build` -> `dotnet build -v quiet`
- `task deploy` -> копирует `bin/Debug/net462/myfirstplugin.dll` в локальный
  `Valheim/BepInEx/plugins`

После деплоя можно сразу зайти в игру и при необходимости вызвать:

```sh
smartcart cleanup
```

Команда чистит загруженных поселенцев аванпостов:

- удаляет дубликаты у одного стола,
- удаляет осиротевших поселенцев без загруженного стола,
- перепривязывает выжившего поселенца к столу.

### Когда использовать

- После любого изменения мода для быстрого smoke-test в локальном Valheim.
- Когда нужно не забыть вручную скопировать DLL после сборки.
- `Jotunn.dll` нужно положить в `BepInEx/plugins/` рядом с `myfirstplugin.dll`.
  NuGet пакет JotunnLib используется только для компиляции — runtime DLL не
  копируется автоматически. Без неё BepInEx не загрузит мод
  (`missing dependencies: com.jotunn.jotunn`).

## Good next steps

- Вынести интервалы и радиусы (`CheckInterval`, `SearchRadius`,
  `SettlerAdoptRadius`) в конфиг
  BepInEx.
- При желании заменить базовую модель старосты на отдельный кастомный prefab
  с собственными материалами и анимациями. Сейчас староста использует
  prefab `Dverger` (см. заметку ниже).
- Добавить более агрессивный cleanup для старых миров через обход ZDO, а не
  только загруженных в сцену объектов.
- Пересмотреть спавн сетевых объектов в `CartHorse`, потому что там тоже
  есть риск накопления persistent-объектов в мире.

## 2026-04-13: Settler prefab: Hildir1 → Dverger

Изначально использовался prefab `Hildir1` для NPC-старосты. Это было ошибкой:
Hildir и Haldor — не обычные character prefab'ы, а location-based NPC,
недоступные через `ZNetScene.GetPrefab()`. `GetPrefab("Hildir1")` всегда
возвращал null, поэтому староста никогда не спавнился.

Доступные NPC prefab'ы в ZNetScene (из character-list Jotunn):
`Dverger`, `DvergerMage`, `DvergerMageFire`, `DvergerMageIce`, `DvergerMageSupport`.

Заменили на `Dverger`. Имя теперь в `Framework/PrefabNames.cs` — при смене
достаточно изменить одну константу.

## 2026-04-13: Рефакторинг — Jotunn + Framework/

Подключён Jotunn (JotunnLib 2.29.0) для:
- Регистрации piece через `PieceManager` (убраны 3 ручных Harmony-патча)
- Локализации через `LocalizationManager`

Создан `Framework/` со своими утилитами:
- `Log.cs` — единый логгер (был Debug.Log + OutpostDiag + Logger.LogInfo)
- `Reflect.cs` — все reflected members в одном месте (было 20+ дублей)
- `PrefabNames.cs` — константы имён prefab'ов + валидация при старте
- `AISuppression.cs` — один Harmony-патч вместо двух (IAISuppressed интерфейс)

## 2026-04-13: Передача аванпоста NPC (подавление рейдов)

### Механика Player Base в Valheim

Player base — невидимая зона вокруг определённых структур. Определяется
компонентом `EffectArea` типа `PlayerBase`, **НЕ** полем `m_creator` у Piece.

Эффекты:
- Блокирует спавн мобов (hostile + passive) в радиусе
- Блокирует деспавн предметов на земле
- 3+ структуры с PlayerBase в 40м от игрока → рейд может триггернуться

Структуры с PlayerBase (20м): Bed, Campfire, Iron Fire Pit, Workbench,
Forge, Smelter, Portal, Ward, Stonecutter, и др.

Workbench: 20м + 4м/апгрейд (макс 36м).
Forge: 20м + 3м/апгрейд (макс 38м).
Artisan table: 40м.

### Ключевые методы (assembly_valheim.dll)

```
EffectArea.GetBaseValue(Vector3 p, float radius) → int [static]
  Считает кол-во PlayerBase структур в радиусе. Используется для
  определения "достаточно ли структур для рейда" (порог = 3).

EffectArea.IsPointInsideArea(Vector3 p, Type type, float radius) → EffectArea [static]
  Проверяет, попадает ли точка в зону EffectArea заданного типа.
  Используется для блокировки спавна мобов (Type.PlayerBase).

RandEventSystem.CheckBase(RandomEvent ev, PlayerEventData player) → bool [instance]
  Проверяет, находится ли игрок на базе для триггера рейда.
  PlayerEventData содержит: position, baseValue (int), possibleEvents.

EffectArea.Type enum:
  None, Heat, Fire, PlayerBase, Burning, Teleport, NoMonsters,
  WarmCozyArea, PrivateProperty
```

### Наше решение

Патчим `EffectArea.GetBaseValue` (Postfix): если позиция в радиусе 40м от
переданного аванпоста, возвращаем 0. Рейд не триггерится (нужно ≥ 3),
но PlayerBase эффект (блокировка спавна) остаётся — он проверяется через
`IsPointInsideArea`, не через `GetBaseValue`.

Файлы:
- `Outpost/OutpostTransfer.cs` — Harmony-патч + ZDO-флаг `smartcart_outpost_transferred`
- `Outpost/OutpostTableComponent.cs` — Interactable: кнопка "Передать NPC" / "Вернуть игроку"

### Почему НЕ m_creator

Обнуление `Piece.m_creator` не убирает рейды. Player base определяется
только наличием `EffectArea.PlayerBase` на структурах, а не тем, кто их
построил. `m_creator` используется только для определения, кто может
ремонтировать/сносить постройку (через Ward).

## Концепция: аванпост как живой организм

### Ward API (PrivateArea) — ключевые методы

```
PrivateArea (Ward / Оберег):
  SetEnabled(bool) / IsEnabled()         — вкл/выкл защиту
  AddPermitted(long playerID, string)    — добавить в разрешённые
  RemovePermitted(long playerID)         — убрать из разрешённых
  IsPermitted(long playerID)             — проверить доступ
  CheckAccess(Vector3, float, bool, bool) — [static] проверка доступа в точке
  m_radius: float                        — радиус защиты
  m_ownerFaction: Faction                — фракция владельца (есть Dverger!)
  Setup(string name)                     — настройка имени

Container (Сундук):
  m_checkGuardStone: bool  — если true, проверяет Ward перед открытием
  m_privacy: PrivacySetting (Private / Group / Public)

Character.Faction enum:
  Players, AnimalsVeg, ForestMonsters, Undead, Demon, MountainMonsters,
  SeaMonsters, PlainsMonsters, Boss, MistlandsMonsters, Dverger,
  PlayerSpawned, TrainingDummy
```

### Архитектура передачи аванпоста

**Стол Старосты = Ward.** При передаче на стол добавляется `PrivateArea`
компонент (faction = Dverger, радиус = радиус аванпоста). Не нужен
отдельный guard_stone — стол сам становится точкой защиты.

**Поток игрока:**
1. Построил дом: стол внутри + кровать + костёр + сундук снаружи
2. Нажал "Передать деревне" на столе
3. Стол → Ward (PrivateArea): двери закрыты, сундуки внутри заблокированы
4. Ближайший сундук в радиусе стола → "приёмник" (m_checkGuardStone=false,
   m_privacy=Public). Если сундука нет — староста: "Поставь сундук для подношений"
5. Появляется староста, живёт в доме
6. Игрок на улице: подходит к сундуку, кладёт ресурсы
7. Староста периодически выходит к сундуку, забирает ресурсы

**Сундук-приёмник:** игрок ставит сам (обычный сундук), при передаче
ближайший сундук автоматически становится приёмником. Позволяет игроку
красиво обустроить зону подношений.

### Уровни роста аванпоста

| Уровень | Что нужно | Что открывается |
|---------|-----------|-----------------|
| 0 — Передан | Стол + кровать + костёр + сундук | Двери закрыты, только сундук-приёмник |
| 1 — Выживание | Поставлять дрова + еду | Староста разговаривает, рассказывает о нуждах |
| 2 — Доверие | Накопить отношения | AddPermitted → двери открываются, можно достраивать |
| 3 — Поселение | Построить 2-й дом + кровать | Второй поселенец (курьер/лесник) |
| 4 — Деревня | Мастерская + 3-й дом | Ремесленник, начало самодостаточности |

### Механика ресурсов

Абстрактное потребление: игрок кладёт ресурсы в сундук → закрыл →
ресурсы списываются в ZDO стола (wood_count, food_count). Сундук всегда
пустой при следующем открытии. Каждые N минут аванпост "тратит" единицу
дров и еды. Нет ресурсов → уровень падает, поселенец может уйти.

### Отношения

Счётчик в ZDO стола, привязан к player ID. Растёт от поставок ресурсов.
При достижении порога → AddPermitted на Ward → двери открываются.

### Хождение старосты к сундуку

Варианты (от простого к сложному):
1. **Мгновенно** — при закрытии сундука ресурсы списываются сразу. MVP.
2. **Телепорт** — староста появляется у сундука, "забирает", возвращается.
3. **AI-движение** — waypoint к сундуку и обратно. Атмосферно, но патфайндинг.

MVP: вариант 1. Хождение — позже как отдельная задача.

### Деградация аванпоста (когда ресурсы кончаются)

Деревня не "умирает" бесповоротно — игрок не теряет прогресс.

| Состояние | Что происходит |
|-----------|---------------|
| Ресурсы есть | Всё работает нормально |
| Ресурсы кончились | Костёр гаснет, староста: "Нам нужны припасы!" |
| Нет ресурсов 1 день | Уровень аванпоста падает на 1 |
| Нет ресурсов 3 дня | Староста уходит, Ward отключается, двери открываются |

**Заброшенное состояние:** стол стоит, ZDO хранит прогресс (отношения,
уровень -1). Когда игрок снова положит ресурсы — тот же староста
возвращается, отношения сохранены. Не нужно заново строить доверие.

## 2026-04-13: MVP реализован — Ward + сундук-приёмник + отношения

### Что реализовано

**Передача аванпоста:**
- Кнопка E на столе → "Передать NPC" / "Вернуть игроку"
- При передаче ищет ближайший Container (сундук) в радиусе 20м
- Если сундука нет — сообщение "Поставь сундук снаружи для подношений!"
- Стол получает компонент PrivateArea (Ward): faction=Dverger, radius=20м
- При возврате игроку — Ward удаляется, сундук отвязывается

**Ward через рефлексию:**
Методы `SetEnabled`, `IsPermitted`, `AddPermitted` в PrivateArea — non-public.
Доступ через `Reflect.PrivateArea_SetEnabled` и т.д. Поля `m_radius`,
`m_ownerFaction`, `m_piece`, `m_nview` — тоже через рефлексию.

**Сундук-приёмник:**
- Связь сундук↔стол через ZDO (ключ `smartcart_chest_table_id`)
- При передаче: `m_checkGuardStone=false`, `m_privacy=Public` — доступен вне Ward
- Harmony-патч на `Container.OnContainerChanged` — при изменении инвентаря
  проверяет маркер, собирает ресурсы, очищает сундук

**Определение ресурсов:**
- Дрова: `m_shared.m_name` == `$item_wood` / `$item_finewood` / `$item_roundlog` / `$item_yggdrasilwood`
- Еда: `m_shared.m_food > 0` (любой съедобный предмет)

**Отношения:**
- Счётчик в ZDO стола, ключ `smartcart_rel_{playerID}`
- +1 за каждый предмет (дрова или еда)
- Порог 30 → `AddPermitted(playerID)` → двери открываются
- Порог задан константой `OutpostResources.AccessThreshold = 30`

**Староста (диалоги):**
- wood <= 0 → "Нам нужны дрова!"
- food <= 0 → "Нужна еда!"
- иначе → "Запасы в порядке."
- Если аванпост не передан → случайные фразы как раньше

### Новые файлы

```
Outpost/OutpostWard.cs          — ActivateWard, DeactivateWard, GrantAccess
Outpost/OutpostResources.cs     — CollectFromChest, Get/SetWood/Food/Relation
Outpost/OutpostChestReceiver.cs — Harmony-патч Container + OutpostChestLink
```

### ZDO ключи (persistence)

```
smartcart_outpost_transferred   — "1" если передан NPC
smartcart_chest_table_id        — ZDO ID стола (на сундуке)
smartcart_res_wood              — int, запас дров
smartcart_res_food              — int, запас еды
smartcart_rel_{playerID}        — int, отношения с игроком
smartcart_last_consume          — double (string), время последнего потребления
smartcart_starving_start        — double (string), начало голодания (0 = не голодает)
smartcart_outpost_level         — int, уровень аванпоста
```

### Важные находки

- `Localization` класс — в `assembly_guiutils.dll`, не в `assembly_valheim.dll`.
  Нужна отдельная ссылка в csproj.
- `PrivateArea` (Ward): `SetEnabled`, `IsPermitted`, `AddPermitted` — non-public,
  требуют рефлексии через AccessTools.
- `Container.m_checkGuardStone` — если false, сундук не проверяет Ward.
- `Container.m_privacy` — enum: Private, Group, Public.

## 2026-04-13: Деградация аванпоста

### Потребление

Каждые 20 минут реального времени аванпост тратит 1 дрова + 1 еду.
Время последнего потребления хранится в ZDO (`smartcart_last_consume`)
как double через string (ZDO не имеет типа double).
При потреблении: если ресурсы есть — списываются, счётчик голода сбрасывается.

### Деградация

| Время без ресурсов | Что происходит |
|-------------------|---------------|
| 0 | Счётчик голода стартует (`smartcart_starving_start`) |
| 40 минут | Уровень аванпоста -1 |
| 2 часа | Аванпост заброшен: староста деспавнится, Ward off, сундук отвязан |

При возобновлении поставок — счётчик голода сбрасывается. Заброшенный
аванпост сохраняет ZDO (отношения, уровень). Тот же староста вернётся
когда появятся ресурсы.

### Диалоги старосты (по приоритету)

1. wood=0 и food=0 → "Мы голодаем! Нет ни дров, ни еды!"
2. wood=0 → "Нам нужны дрова!"
3. food=0 → "Нужна еда!"
4. wood≤3 или food≤3 → "Припасы на исходе. Скоро кончатся."
5. Иначе → "Запасы в порядке."

### Константы (OutpostResources)

```
ConsumeIntervalSeconds = 1200f  // 20 минут между потреблениями
DegradeAfterSeconds    = 2400f  // 40 минут голода → уровень -1
AbandonAfterSeconds    = 7200f  // 2 часа голода → заброшен
```

## 2026-04-13: Отладка зависания при загрузке мира

### Симптом
Мир зависал на этапе загрузки после рефакторинга. В логах нет ошибок,
просто бесконечная загрузка.

### Метод отладки — бинарный поиск
Отключили всё (`harmony.PatchAll()`, `AddLocalizations()`,
`PrefabManager.OnVanillaPrefabsAvailable`) — работает. Включили 50%
(Harmony + локализации, без Jotunn piece) — работает. Включили всё —
работает. Проблема была нестабильной (возможно, старая DLL в кеше
или race condition при загрузке).

### Ключевой вывод
При бинарном поиске бага — отключать по 50%, а не по одному.
Архитектура с отдельными системами (Harmony-патчи, Jotunn piece,
локализации) позволяет легко отключать блоки.

### Проблема с "призрачным" столом
После миграции на Jotunn старый стол (созданный ручными Harmony-патчами
на ZNetScene.Awake) стал "призраком" — ZDO в мире есть, но Jotunn
регистрирует prefab с другим timing'ом, и `Piece.Awake` не вызывается
для старого объекта.

**Решение:** `smartcart cleanup` в консоли + поставить новый стол из молотка.

**На будущее:** при миграции системы регистрации prefab'ов — старые
объекты в мире могут стать невалидными. Нужно либо обеспечить
обратную совместимость hash'ей, либо документировать необходимость
пересоздания объектов.

### Другие фиксы в процессе

- `ZNetScene.instance` мог быть null в `OnVanillaPrefabsAvailable` →
  добавлен null-check перед `PrefabNames.ValidateAll`
- `UpdateSettlerState` спамил `Log.Diag` каждые 5 секунд → убран
- `Log.DiagEnabled` переключён в `false` по умолчанию
- Дубликат `Jotunn.dll` в plugins/ (наша копия + Thunderstore) → удалена наша
- `Talker.m_nameOverride` не найден в текущей версии Valheim → warning,
  не критично (поле nullable в Reflect.cs)

## Модульность проекта

### Принцип: один файл — одна ответственность

OutpostTableComponent.cs разделён с 457 строк на три файла:

```
OutpostTableComponent.cs    (178 строк) — координация: lifecycle, hover, interact
OutpostSettlerBinding.cs    (137 строк) — ZDO persistence: привязка стол↔поселенец
OutpostSettlerManager.cs    (142 строк) — settler lifecycle: спавн, деспавн, refresh
```

### Правила (зафиксированы в CLAUDE.md)

- Файл > 200 строк — повод разделить
- Инфраструктура (Harmony, рефлексия, ZDO) отдельно от game logic
- Reflected members — только в `Framework/Reflect.cs`
- Hardcoded prefab имена — только в `Framework/PrefabNames.cs`
- Логирование — только через `Framework/Log.cs`
- UI строки — через Jotunn $-токены в `Plugin.cs AddLocalizations()`
- ZDO ключи — документировать в этом файле при добавлении
- `task` (Taskfile) для build+deploy: `task` = build + copy DLL

## 2026-04-14: Визуализация ресурсов аванпоста

### Еда на обеденном столе

Игрок ставит обеденный стол (`piece_table*`) в радиусе 10м от стола старосты.
При наличии еды в запасах — на столе появляются декоративные предметы.

- 1-3 еды → 1 предмет, 4-10 → 2, 10+ → 3
- Prefab'ы: `Tankard_dvergr`, `Tankard`, `CookedMeat`
- Расстановка по кругу (radius 25см) от центра стола
- Высота через raycast вниз (точная поверхность стола)
- Объекты child обеденного стола — двигаются с ним
- Чисто визуальные клоны (StripComponents: удалены все компоненты
  кроме Transform, MeshFilter, MeshRenderer, LODGroup)
- Нельзя подобрать, разрушить, взаимодействовать

Если обеденного стола нет — староста бормочет: "Стол бы обеденный
поставить... Не на чём трапезничать."

### Стопка дров у костра

При наличии дров в запасах — рядом с костром (2м вправо) появляется
стопка дров. Исчезает когда дрова кончаются.

- Prefab: `wood_core_stack` (fallback: `blackwood_stack`)
- Высота через `ZoneSystem.GetGroundHeight` (корректно на неровном terrain)
- Визуальный клон — аналогично еде

### Автопополнение костра

Каждые 5 секунд проверяет ближайший Fireplace. Если fuel < 2 и
дрова есть → добавляет 4 fuel, списывает дрова из ZDO.

### Декоративные prefab'ы (PrefabNames.cs)

```
WoodCoreStack  = "wood_core_stack"
BlackwoodStack = "blackwood_stack"
TankardDvergr  = "Tankard_dvergr"
Tankard        = "Tankard"
CookedMeat     = "CookedMeat"
```

**Заметка:** Jotunn prefab list (valheim-modding.github.io) обрезается
и не содержит все prefab'ы. Для поиска имён: `dotnet-script` + рефлексия,
или дамп `ZNetScene.m_prefabs` при старте, или `spawn <name>` в консоли.

## 2026-04-14: Ward через патч CheckAccess (не PrivateArea компонент)

Попытка добавить `PrivateArea` компонент через `AddComponent` вызывала
NullReferenceException — `PrivateArea.Awake()` регистрирует RPC через
`m_nview`, а при динамическом добавлении Awake вызывается ДО установки
полей через рефлексию.

**Решение:** патч `PrivateArea.CheckAccess` (static). Если точка в радиусе
20м от переданного аванпоста и отношения игрока < AccessThreshold →
возвращаем false. Vanilla код сам использует CheckAccess для дверей,
сундуков, строительства.

При передаче аванпоста: передавший игрок сразу получает relation =
AccessThreshold (доступ с первого момента). Остальные — через ресурсы.

## 2026-04-14: Баг с Container.OnContainerChanged при загрузке

`OnContainerChanged` вызывается для КАЖДОГО контейнера при загрузке мира.
Fallback `FindNearestTransferredTable` находил стол рядом с обычными
сундуками и забирал их содержимое.

**Решение:** убран fallback. Вместо этого `EnsureLinked` в ConditionLoop
стола — при загрузке мира перепривязывает ближайший сундук если маркер
потерян. Только для переданных аванпостов, только раз.

## Подсказки от старосты (полный приоритет)

1. wood=0 и food=0 → "Мы голодаем!"
2. wood=0 → "Нам нужны дрова!"
3. food=0 → "Нужна еда!"
4. wood≤3 или food≤3 → "Припасы на исходе"
5. Нет обеденного стола → "Стол бы обеденный поставить..."
6. Всё ок → "Запасы в порядке"

Бормотание: каждые 30-90 сек через `Chat.SetNpcText` (облачко над головой,
видно в 20м). Использует те же контекстные фразы.

### Что НЕ реализовано (следующие шаги)

- Хождение старосты к сундуку (AI waypoints)
- Уровни роста 2-4 (новые дома → новые поселенцы)
- Роли поселенцев (курьер/дровосек → курьер на уровне 4)
- Проверка ровности земли (`ZoneSystem.GetGroundHeight` × 12 точек)
- Автовыравнивание земли (через `TerrainOp` — опасно, не рекомендуется)
