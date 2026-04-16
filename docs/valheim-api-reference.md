# Valheim API Reference (assembly_valheim.dll)

Получено через рефлексию из `assembly_valheim.dll`. Версия Valheim на момент анализа: 2026-04-13.

Практические заметки по фиксам мода и локальному циклу сборки/деплоя:
[dev-notes.md](dev-notes.md)

---

## Character (базовый класс персонажей)

### Посадка/присоединение
```csharp
// Посадить персонажа (лодка, седло, кровать, телега)
void AttachStart(
    Transform attachPoint,      // точка крепления
    GameObject colliderRoot,    // корневой объект с Rigidbody
    bool hideWeapons,           // скрыть оружие
    bool isBed,                 // это кровать?
    bool onShip,                // это корабль?
    string attachAnimation,     // анимация ("", "attach_chair", "attach_bed")
    Vector3 detachOffset,       // смещение при слезании
    Transform cameraPos         // позиция камеры (null = авто)
);

void AttachStop();              // слезть
bool IsAttached();              // сидит ли?
bool IsAttachedToShip();        // на корабле?
bool HaveRider();               // есть ли наездник?
```

### Размеры
```csharp
float GetRadius();              // радиус коллайдера
CapsuleCollider m_collider;     // капсула: .height, .radius
```

### Ключевые поля
```csharp
Rigidbody m_lastAttachBody;     // тело к которому прикреплён
Vector3 m_lastAttachPos;        // позиция крепления
```

---

## BaseAI (базовый AI существ)

### Движение (приватные — доступ через рефлексию)
```csharp
bool FindPath(Vector3 target);
bool MoveTo(float dt, Vector3 point, float dist, bool run);
void MoveTowards(Vector3 dir, bool run);
bool MoveAndAvoid(float dt, Vector3 point, float dist, bool run);
void StopMoving();  // публичный
```

### Рефлексия
```csharp
AccessTools.Method(typeof(BaseAI), "FindPath", new[] { typeof(Vector3) });
AccessTools.Method(typeof(BaseAI), "MoveTo", new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) });
```

---

## MonsterAI (AI монстров, наследует BaseAI)

```csharp
bool UpdateAI(float dt);           // главный цикл AI — можно заблокировать Prefix-патчем
void SetFollowTarget(GameObject go); // следовать за объектом
void SetPatrolPoint();              // установить патрульную точку

// ВНИМАНИЕ: IsEnemy не существует на MonsterAI — Harmony выдаст warning и может
// нарушить порядок применения патчей. Чтобы сделать NPC мирным —
// достаточно заблокировать UpdateAI (Prefix → return false).
// Без UpdateAI NPC не может обрабатывать агрессию физически.
```

---

## Tameable (приручение)

### Методы
```csharp
void Tame();                    // ПРИВАТНЫЙ — приручить мгновенно
void TameAllInArea();           // публичный — приручить всех в области
bool IsTamed();                 // приручён ли?
bool Interact(Humanoid player, bool hold, bool repeat);
void Command(Humanoid player);  // дать команду
bool HaveSaddle();
bool HaveRider();
```

### Рефлексия для Tame()
```csharp
AccessTools.Method(typeof(Tameable), "Tame").Invoke(tameable, null);
```

### Поля
```csharp
float m_tamingTime;             // время приручения
bool m_startsTamed;             // начинает прирученным
bool m_commandable;             // можно давать команды
Sadle m_saddle;                 // седло
Character m_character;          // ПРИВАТНЫЙ
MonsterAI m_monsterAI;          // ПРИВАТНЫЙ
```

---

## Character.AttachStart (крепление к объекту)

```csharp
// Метод на Character (базовый класс). Работает для Player, но для NPC
// НЕ перемещает физически — NPC "прикрепляется" по анимации,
// но transform не следует за mount'ом.
void AttachStart(
    Transform attachPoint,      // точка крепления
    GameObject colliderRoot,    // объект-родитель
    bool hideWeapons,
    bool isBed,
    bool onShip,
    string attachAnimation,     // "" для дефолта
    Vector3 detachOffset,       // смещение при отсоединении
    Transform cameraPos = null  // только для Player
);
void AttachStop();
// ВЫВОД: для NPC-наездника использовать SetParent тоже не работает
// (Rigidbody + ZNetView конфликтуют). Решение: NPC остаётся дома,
// животное бежит отдельно.
```

---

## Procreation (размножение)

```csharp
class Procreation : MonoBehaviour
// Компонент на приручаемых животных (Boar, Wolf, Lox).
// Для "служебных" животных — удалять:
//   Object.Destroy(boar.GetComponent<Procreation>());
// Иначе расплодятся.
```

---

## Sadle (седло — наездничество)

```csharp
bool Interact(Humanoid player, bool hold, bool repeat);  // сесть
void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool jump, bool sprint, bool crouch);
bool UpdateRiding(float dt);    // false = нужно слезть
bool HaveValidUser();
Transform m_attachPoint;        // точка посадки
string m_attachAnimation;       // анимация
Vector3 m_detachOffset;         // смещение при слезании
```

### RPC (сеть)
```csharp
void RPC_RequestControl(long saddleID, long playerID);
void RPC_ReleaseControl(long saddleID, long playerID);
void RPC_Controls(long playerID, Vector3 dir, int controls, float stamina);
```

---

## Vagon (телега — именно "Vagon", не "Wagon"!)

### Методы
```csharp
bool Interact(Humanoid player, bool hold, bool repeat);
void AttachTo(GameObject obj);
bool CanAttach(GameObject obj);
bool IsAttached(Character character);
bool IsAttached();
bool InUse();
```

### Публичные поля
```csharp
Transform m_attachPoint;        // точка крепления
Vector3 m_attachOffset;         // смещение крепления
float m_breakForce;             // сила разрыва джойнта
float m_spring;                 // пружина
float m_springDamping;          // демпфер пружины
float m_detachDistance;          // дистанция отсоединения
float m_baseMass;               // базовая масса
float m_itemWeightMassFactor;   // множитель массы от предметов
Container m_container;          // контейнер для груза
Rigidbody[] m_wheels;           // колёса
```

### Приватные поля (доступ через рефлексию)
```csharp
ConfigurableJoint m_attachJoin; // физический джойнт
GameObject m_attachedObject;    // присоединённый объект
Rigidbody m_body;               // тело телеги
Humanoid m_useRequester;        // кто использует
```

### Рефлексия
```csharp
AccessTools.Field(typeof(Vagon), "m_attachJoin");
AccessTools.Field(typeof(Vagon), "m_body");
```

---

## Ship (корабль)

```csharp
bool HasPlayerOnboard();
bool HaveControllingPlayer();
bool IsPlayerInBoat(long playerID);
bool IsPlayerInBoat(Player player);
bool IsPlayerInBoat(ZDOID zdoid);
Rigidbody m_body;
```

---

## ZNetScene (спавн префабов)

```csharp
// Синглтон
ZNetScene.instance

// Получить префаб по имени или хешу
GameObject GetPrefab(string name);
GameObject GetPrefab(int hash);

// ВАЖНО: удаление сетевых объектов
void Destroy(GameObject go);  // удаляет GameObject + ZDO из сейва
// Object.Destroy() удаляет ТОЛЬКО локальный GameObject, ZDO остаётся!
// При следующей загрузке мира объект воссоздаётся из ZDO → дубликаты.
// Правило: Object.Destroy — только для декора. Для NPC/сундуков — ZNetScene.Destroy.
```

### Спавн объекта
```csharp
GameObject prefab = ZNetScene.instance.GetPrefab("Lox");
GameObject obj = Object.Instantiate(prefab, position, rotation);
```

### ZNetView
```csharp
bool IsValid();          // объект реально существует в сети
bool IsOwner();          // мы владеем этим объектом
void ClaimOwnership();   // забрать владение
ZDO GetZDO();            // доступ к сетевым данным
void Destroy();          // альтернатива ZNetScene.Destroy
```

---

## Terminal (консоль)

### Регистрация команд
```csharp
// Через патч Terminal.InitTerminal (Postfix)
new Terminal.ConsoleCommand(
    string command,             // имя команды
    string description,         // описание
    ConsoleEvent action,        // делегат: delegate(Terminal.ConsoleEventArgs args)
    bool isCheat = false,
    bool isNetwork = false,
    bool onlyServer = false,
    bool isSecret = false,
    bool allowInDevBuild = false,
    ConsoleOptionsFetcher optionsFetcher = null,
    bool alwaysRefreshTabOptions = false,
    bool remoteCommand = false,
    bool isAdmin = false
);

// ConsoleEventArgs
args.Length          // количество аргументов
args[i]              // i-й аргумент (string)
args.Context         // Terminal instance
args.Context.AddString(string text)  // вывести текст в консоль
```

---

## Sign (знаки)

```csharp
void SetText(string text);      // установить текст (патчим это)
string GetText();
Transform transform.position;   // позиция знака в мире
```

---

## Player

```csharp
static Player m_localPlayer;    // синглтон локального игрока
static bool m_debugMode;        // режим отладки
void Message(MessageHud.MessageType type, string text);
void OnSpawned();               // вызывается при спавне
```

### MessageHud.MessageType
```csharp
MessageType.Center   // по центру экрана
MessageType.TopLeft  // левый верхний угол
```

---

## Console

```csharp
static Console instance;        // синглтон
void TryRunCommand(string cmd); // выполнить команду
bool m_consoleEnabled;          // ПРИВАТНЫЙ — консоль разблокирована
```

---

## ConfigurableJoint (Unity — присоединение телеги)

```csharp
// Нужна ссылка: UnityEngine.PhysicsModule.dll
joint.autoConfigureConnectedAnchor = false;
joint.anchor = localAnchorPoint;
joint.connectedAnchor = offsetOnTarget;
joint.connectedBody = targetRigidbody;
joint.breakForce = force;

joint.xMotion = ConfigurableJointMotion.Limited;
joint.yMotion = ConfigurableJointMotion.Limited;
joint.zMotion = ConfigurableJointMotion.Locked;

SoftJointLimit limit = new SoftJointLimit();
limit.limit = 0.001f;
joint.linearLimit = limit;

SoftJointLimitSpring spring = new SoftJointLimitSpring();
spring.spring = springValue;
spring.damper = damperValue;
joint.linearLimitSpring = spring;
```

---

## Известные имена префабов

| Префаб | Описание |
|--------|----------|
| `Lox` | Локс (большое вьючное животное) |
| `Wolf` | Волк |
| `Boar` | Кабан |
| `Cart` | Телега (компонент Vagon) |
| `Sign` | Знак (компонент Sign) |

---

## Harmony — частые паттерны

### Доступ к приватным полям через аргументы патча
```csharp
// Три подчёркивания + имя поля + ref
static void Postfix(ref bool ___m_consoleEnabled) { ... }
```

### Доступ через AccessTools
```csharp
FieldInfo field = AccessTools.Field(typeof(ClassName), "fieldName");
field.GetValue(instance);
field.SetValue(instance, value);

MethodInfo method = AccessTools.Method(typeof(ClassName), "methodName", new[] { paramTypes });
method.Invoke(instance, new object[] { args });
```

### Prefix (перехват до оригинала)
```csharp
[HarmonyPatch(typeof(Target), "Method")]
class Patch { static bool Prefix() { return false; /* skip original */ } }
```

### Postfix (после оригинала)
```csharp
[HarmonyPatch(typeof(Target), "Method")]
class Patch { static void Postfix(ref bool __result) { __result = true; } }
```

---

## Piece (строительный объект)

```csharp
// Компонент на каждом buildable prefab-е
class Piece : MonoBehaviour
{
    string m_name;                  // отображаемое имя
    string m_description;           // описание в UI
    Piece.Requirement[] m_resources; // рецепт (материалы)
    Sprite m_icon;                  // иконка в меню молотка
    Piece.PieceCategory m_category; // вкладка (Misc, Crafting, Building, Furniture)

    // Комфорт
    int m_comfort;                          // значение комфорта этого piece (0 = не даёт комфорт)
    Piece.ComfortGroup m_comfortGroup;      // группа (max из группы, не суммируется)
    GameObject m_comfortObject;             // если задан и неактивен — комфорт не считается
    static List<Piece> s_allComfortPieces;  // все pieces с m_comfort > 0 (private, нужна reflection)

    // Вызывается когда игрок ставит объект. Хук для детектирования установки.
    void SetCreator(long uid);
}

// Группы комфорта — из каждой группы берётся только max значение
enum Piece.ComfortGroup
{
    None, Bed, Fire, Banner, Chair, Table, Carpet, Torch
}

class Piece.Requirement
{
    ItemDrop m_resItem;  // prefab предмета (GetPrefab("Wood").GetComponent<ItemDrop>())
    int m_amount;        // количество
    bool m_recover;      // возвращать при разрушении?
}
```

---

## PieceTable (список объектов молотка)

```csharp
class PieceTable : MonoBehaviour
{
    List<GameObject> m_pieces;  // все доступные объекты
}

// Получить PieceTable молотка:
var hammerPrefab = ObjectDB.instance.GetItemPrefab("Hammer");
var pieceTable = hammerPrefab
    .GetComponent<ItemDrop>()
    .m_itemData.m_shared.m_buildPieces;  // это и есть PieceTable

pieceTable.m_pieces.Add(myPrefab);
```

---

## ObjectDB

```csharp
// Синглтон. Хук: [HarmonyPatch(typeof(ObjectDB), "Awake")]
ObjectDB.instance

GameObject GetItemPrefab(string name);  // получить prefab предмета по имени
```

---

## ZNetScene (регистрация prefab-ов)

```csharp
// Синглтон. Хук: [HarmonyPatch(typeof(ZNetScene), "Awake")]
ZNetScene.instance

List<GameObject> m_prefabs;  // публичный список всех prefab-ов

// m_namedPrefabs — приватный Dictionary<int, GameObject>, ключ — хеш имени.
// Доступ только через рефлексию:
FieldInfo s_namedPrefabs = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs");
var namedPrefabs = (Dictionary<int, GameObject>)s_namedPrefabs.GetValue(zNetSceneInstance);
namedPrefabs[hash] = prefab;

// ВАЖНО: два правила при клонировании prefab для регистрации в ZNetScene:
//
// 1. НЕ деактивировать источник (source.SetActive(false)) —
//    ломает OnDisable цепочку оригинального prefab и вешает загрузку мира.
//
// 2. После Instantiate УНИЧТОЖИТЬ ZNetView на клоне через DestroyImmediate —
//    ZNetView.Awake() успевает зарегистрировать клон как сетевой объект с битым ZDO,
//    что вешает ZNetScene при загрузке мира. При размещении через молоток
//    Valheim создаёт свежий ZNetView через SpawnObject, поэтому разрушение
//    ZNetView на шаблоне не ломает поставленные объекты.
//
//   var clone = Object.Instantiate(source);
//   clone.SetActive(false);
//   Object.DestroyImmediate(clone.GetComponent<ZNetView>());

// Хеш-алгоритм Valheim (аналог GetStableHashCode — метод расширения в сборке,
// но недоступен через компилятор напрямую; это точная реализация — двойной djb2 с XOR):
int StableHash(string str) {
    int num = 5381, num2 = num, num3 = 0;
    while (num3 < str.Length && str[num3] != '\0') {
        num = ((num << 5) + num) ^ str[num3];
        if (num3 == str.Length - 1 || str[num3 + 1] == '\0') break;
        num2 = ((num2 << 5) + num2) ^ str[num3 + 1];
        num3 += 2;
    }
    return num + num2 * 1566083941;
}
```

---

## Добавление кастомного buildable объекта (без Jotunn)

Три патча в правильном порядке:

```csharp
// 1. ZNetScene.Awake — создать и зарегистрировать prefab
[HarmonyPatch(typeof(ZNetScene), "Awake")]
static void Postfix(ZNetScene __instance) {
    var clone = Object.Instantiate(__instance.GetPrefab("piece_workbench"));
    clone.name = "my_piece";
    Object.DontDestroyOnLoad(clone);
    // настроить Piece компонент...
    __instance.m_prefabs.Add(clone);
    var named = (Dictionary<int, GameObject>)s_namedPrefabs.GetValue(__instance);
    named[StableHash(clone.name)] = clone;
}

// 2. ObjectDB.Awake — добавить в PieceTable молотка
[HarmonyPatch(typeof(ObjectDB), "Awake")]
static void Postfix(ObjectDB __instance) {
    var table = __instance.GetItemPrefab("Hammer")
        .GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
    table.m_pieces.Add(ZNetScene.instance.GetPrefab("my_piece"));
}

// 3. Piece.SetCreator — детектировать установку объекта игроком
[HarmonyPatch(typeof(Piece), "SetCreator")]
static void Postfix(Piece __instance) {
    if (!__instance.name.StartsWith("my_piece")) return;
    // логика при установке...
}
```

---

## EnvMan (окружение, время суток)

```csharp
static EnvMan instance;          // синглтон
static bool IsDay();             // сейчас день?
static bool IsNight();           // сейчас ночь?
float GetDayFraction();          // 0.0-1.0, прогресс дня
bool IsAfternoon();              // вторая половина дня
```

Используется для: патруль курьера только днём (`EnvMan.IsDay()`).

---

## WearNTear (прочность построек)

```csharp
class WearNTear : MonoBehaviour
// Есть на всех поставленных игроком объектах (стены, мебель, сундуки).
// Отсутствует на "призрачных" объектах после уничтожения.
// Используется как маркер "объект реально существует":
//   container.GetComponent<WearNTear>() != null → живой сундук
//   container.GetComponent<WearNTear>() == null → призрак
```

---

## Inventory (инвентарь)

```csharp
class Inventory
{
    Action m_onChanged;            // callback при изменении (delegate)
    void Changed();                // private! вызывает m_onChanged.Invoke()
    List<ItemDrop.ItemData> GetAllItems();
    int NrOfItems();
    bool AddItem(ItemDrop.ItemData item);
    bool RemoveItem(ItemDrop.ItemData item);
    void RemoveAll();
}
// ВАЖНО: Harmony patch на Container.OnContainerChanged НЕ РАБОТАЕТ —
// метод вызывается через delegate, не напрямую.
// Inventory.Changed() — private, тоже ненадёжно.
// Решение: polling через ConditionLoop вместо Harmony patch.
```

---

## Fireplace (костёр, факелы)

```csharp
class Fireplace : MonoBehaviour
// Топливо хранится в ZDO:
//   zdo.GetFloat("fuel", 0f)  — текущий уровень
//   zdo.Set("fuel", value)    — установить

// Факелы vs костры: различаем по имени объекта
//   gameObject.name.Contains("torch") → факел (заправляется смолой)
//   остальные → костёр (заправляется дровами)
```

---

## PlanBuild — формат Blueprint файлов

PlanBuild (опциональный мод) — система чертежей для Valheim.

### Расположение файлов

```
BepInEx/config/PlanBuild/blueprints/*.blueprint
BepInEx/config/PlanBuild/blueprints/*.vbuild     (формат BuildShare, тоже поддерживается)
```

### Формат .blueprint

Текстовый файл. Заголовки + секции:

```
#Name:Small House
#Creator:Bygd
#Description:Маленький дом для поселенца
#Category:Village
#SnapPoints
#Terrain
#Pieces
piece_wood_wall;Building;0;0;0;0;0;0;1;;1;1;1
piece_wood_floor;Building;2;0;0;0;0;0.7071;0;0.7071;;1;1;1
piece_wood_roof45;Building;1;3;1;0;0;0;1;;1;1;1
```

### Формат строки Piece

```
name;category;posX;posY;posZ;rotX;rotY;rotZ;rotW;additionalInfo;scaleX;scaleY;scaleZ
```

- `name` — имя prefab'а (piece_wood_wall, piece_bed, etc.)
- `category` — Building, Furniture, Misc, Crafting
- `posX;posY;posZ` — позиция относительно центра blueprint'а (float, InvariantCulture)
- `rotX;rotY;rotZ;rotW` — кватернион поворота
- `additionalInfo` — доп. данные (текст знаков, содержимое сундуков, состояние дверей)
- `scaleX;scaleY;scaleZ` — масштаб (обычно 1;1;1)

### Секции

- `#SnapPoints` — snap-точки для стыковки (опционально)
- `#Terrain` — модификации terrain'а (выравнивание, опционально)
- `#Pieces` — основная секция с деталями постройки

### Pieces отсортированы по Y, затем X, затем Z

Это обеспечивает правильный порядок строительства (фундамент → стены → крыша).

### PlanBuild API

**Нет публичного C# API для спавна blueprint'ов из внешних модов.**
Утилитные методы (только чтение):
```csharp
BlueprintManager.GetPiecesInRadius(Vector3 position, float radius, bool onlyPlanned)
BlueprintManager.GetGameObject(ZDOID zdoid, bool required)
```

### Консольные команды

```
bp.local                            — список локальных blueprint'ов
bp.remove [id]                      — удалить
bp.push [id] / bp.pull [id]         — загрузить/скачать с сервера
bp.thumbnail [id] [rotation]        — пересоздать превью
bp.undo / bp.redo                   — отменить/повторить
bp.clearclipboard                   — очистить буфер
```

### Интеграция с Bygd

PlanBuild — опциональный мод для создания/редактирования чертежей.
Bygd сам парсит .blueprint файлы и спавнит pieces через ZNetScene.
Игрок создаёт blueprint через PlanBuild → кладёт в `Bygd/blueprints/` →
Bygd строит автоматически при level-up или по команде.

---

## Проверка наличия крыши

`Cover` — класс существует в assembly_valheim.dll, но недоступен через компилятор напрямую (нет публичного namespace). Использовать `Physics.Raycast` как замену:

```csharp
bool hasRoof = Physics.Raycast(
    pos + Vector3.up * 0.5f,
    Vector3.up,
    out RaycastHit hit,
    30f,
    LayerMask.GetMask("piece", "piece_nonsolid")
);
// hit.collider.name — имя найденной крыши (для отладки)
```
