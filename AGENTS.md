# AGENTS.md

This file provides guidance to Codex and other coding agents when working with code in this repository.

## Project Overview

RedStar-14 is a C# remake of SS13 running on the Robust Toolbox engine.

## Build and Run Commands

```bash
# Initial setup (run once after cloning)
python RUN_THIS.py

# Build
dotnet build --configuration DebugOpt

# Run server and client
./runclient.bat   # or runclient.sh on Linux
./runserver.bat   # or runserver.sh on Linux

# Run tests
dotnet test Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj

# Run single test
dotnet test Content.Tests/Content.Tests.csproj --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Database migrations
./Content.Server.Database/add-migration.sh <MigrationName>
```

## Architecture

### Project Structure

- **Content.Shared** - Code running on both server and client (components, shared systems, prototypes)
- **Content.Server** - Server-only logic and systems
- **Content.Client** - Client-only logic, UI, rendering
- **RobustToolbox** - Game engine (git submodule)
- **Resources/Prototypes** - YAML entity and data definitions

### Entity Component System (ECS)

**Components** are data containers:
```csharp
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ExampleComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public int Value = 0;
}
```

Key attributes:
- `[RegisterComponent]` - Required for all components
- `[NetworkedComponent]` - Syncs to clients
- `[AutoGenerateComponentState]` - Generates serialization code
- `[DataField]` - Field loads from YAML prototypes
- `[AutoNetworkedField]` - Individual field syncs

**Systems** process components:
```csharp
public sealed class ExampleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExampleComponent, MapInitEvent>(OnInit);
    }
}
```

Pattern: Create shared system in Content.Shared, extend in Content.Server/Client if needed.

### Prototypes (YAML)

Entity definitions in `Resources/Prototypes/Entities/`:
```yaml
- type: entity
  id: ExampleEntity
  parent: BaseItem
  components:
  - type: Example
    value: 10
```

Data prototypes for configuration:
```yaml
- type: examplePrototype
  id: ExampleId
  name: example-name
```

Prototype C# definition:
```csharp
[Prototype]
public sealed partial class ExamplePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
```

### Dependency Injection

Services register in IoC files:
- `Content.Shared/IoC/SharedContentIoC.cs`
- `Content.Server/IoC/ServerContentIoC.cs`

Usage: `[Dependency] private readonly IService _service = default!;`

### Network Events

```csharp
// Define event
[ByRefEvent]
public record struct ExampleEvent(EntityUid Entity);

// Raise event
RaiseLocalEvent(uid, ref exampleEvent);
```

## Code Conventions

- File-scoped namespaces: `namespace Content.Shared.Example;`
- Sealed partial classes for components
- Use `Dirty(uid, component)` to mark networked components as changed
- Access control: `[Access(typeof(ExampleSystem))]`
- Type-safe prototype references: `ProtoId<ExamplePrototype>`

## Testing

- Unit tests inherit from `ContentUnitTest`
- Integration tests use full game systems
- Test framework: NUnit

---

## Contribution Rules (RedStar)

If you are contributing to RedStar-14, use [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md) and current CI checks as the baseline for quality and branch hygiene.

> ⚠️ **Do not use the GitHub web editor.** Pull Requests created through the web editor may be closed without review.

"Upstream" means [`red-star-server/RedStar-14`](https://github.com/red-star-server/RedStar-14).

### RedStar-specific Content

All net-new content (not edits of existing upstream files) must be placed in directories with `_RedStar` prefix.

Examples:
- `Content.Server/RedStar/Systems/FeatureSystem.cs`
- `Resources/Prototypes/_RedStar/feature.yml`
- `Resources/Textures/_RedStar/icon.png`
- `Resources/Locale/ru-RU/_RedStar/strings.ftl`
- `Resources/Maps/_RedStar/map.yml`

Never modify `RobustToolbox` as part of gameplay/content PRs. Engine changes must go through a dedicated engine workflow.

### Editing Upstream Files

When editing existing upstream files (C#, YAML, FTL, etc.), leave markers near modified lines to reduce merge and upstream sync cost.

Use the `RS14` prefix consistently:
- Point edit: `# RS14` or `// RS14`
- Value change: `# RS14-value: OLD -> NEW`
- Block edit: `# RS14-start` / `# RS14-end` or `// RS14-start` / `// RS14-end`

For `.ftl`: place comments on the line above the translation key, not on the same line.

### Maps

For fork maps, use `Resources/Maps/_RedStar`.

If adding a new rotation map:
- add map file under `Resources/Maps/_RedStar/...`;
- add/update `gameMap` prototype in `Resources/Prototypes/Maps/...`;
- update map pools in `Resources/Prototypes/Maps/Pools/...` when needed.

Coordinate with the map maintainer before parallel edits to the same `.yml` map.

### Before Opening PR

Before submitting PR:
- check `git diff` and changed file list for accidental changes;
- ensure no unrelated formatting noise;
- run at least a baseline build check: `dotnet build SpaceStation14.sln`.

If long-lived PR accidentally includes `RobustToolbox` changes, remove them in a separate commit:

```bash
git fetch upstream
git restore --source upstream/master -- RobustToolbox
```

If upstream default branch is not `master`, use the actual branch name (for example `main`).

### Changelogs

For fork content, use corresponding files in `Resources/Changelog/` according to current repository format.

Use entry types `Add`, `Fix`, `Tweak`, `Remove` and include PR number when available.

### AI-generated Content

AI-generated content (code, sprites, and similar assets) is prohibited for repository contribution.

---

## Вклад в разработку RedStar-14

Если вы собираетесь внести вклад в разработку RedStar-14, ориентируйтесь на шаблон PR в [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md) и текущие CI-проверки репозитория.

> ⚠️ **Не используйте веб-редактор GitHub.** Pull Request'ы, созданные через веб-редактор, могут быть закрыты без рассмотрения.

"Upstream" означает репозиторий [`red-star-server/RedStar-14`](https://github.com/red-star-server/RedStar-14), из которого синхронизируется форк.

### Контент, специфичный для RedStar

Всё, что вы создаёте с нуля (в отличие от изменений в существующем upstream-коде), размещайте в подкаталогах с префиксом `_RedStar`.

**Примеры:**
- `Content.Server\RedStar\Systems\FeatureSystem.cs`
- `Resources\Prototypes\_RedStar\feature.yml`
- `Resources\Textures\_RedStar\icon.png`
- `Resources\Locale\ru-RU\_RedStar\strings.ftl`
- `Resources\Maps\_RedStar\map.yml`

Никогда не изменяйте `RobustToolbox` в рамках контентных PR. Изменения движка должны идти отдельным процессом.

### Изменения файлов из upstream

Если вы правите существующие upstream-файлы (C#, YAML, FTL и т.д.), оставляйте пометки у изменённых мест.

Используйте префикс `RS14`:
- Точечное изменение: `# RS14` или `// RS14`
- Изменение значения: `# RedStar-value: СТАРОЕ -> НОВОЕ`
- Блок изменений: `# RS14-start` / `# RS14-end` или `// RS14-start` / `// RS14-end`

> ⚠️ В `.ftl` не ставьте комментарий в той же строке, что и ключ перевода. Комментарий должен быть строкой выше.

### Карты

Для карт форка используйте каталог `Resources\Maps\_RedStar`.

Если добавляете новую карту ротации:
- добавьте файл карты в `Resources\Maps\_RedStar\...`;
- добавьте/обновите `gameMap`-прототип в `Resources\Prototypes\Maps\...`;
- при необходимости обновите map pool в `Resources\Prototypes\Maps\Pools\...`.

Если меняете существующую карту, заранее согласуйте изменения с мейнтейнером карты и не работайте параллельно над одной `.yml`-картой без координации.

### Перед отправкой PR

Перед отправкой PR:
- проверьте `git diff` и список файлов на случайные изменения;
- убедитесь, что нет лишних форматных изменений;
- запустите хотя бы базовую проверку сборки: `dotnet build SpaceStation14.sln`.

Если PR долго живёт и в него случайно попали изменения в `RobustToolbox`, уберите их отдельным коммитом:

```bash
git fetch upstream
git restore --source upstream/master -- RobustToolbox
```

Если в `upstream` основной бранч не `master`, используйте актуальное имя ветки (например, `main`).

### Ченджлоги

Для контента форка используйте соответствующие файлы в `Resources\Changelog\` по текущему формату репозитория.

Следуйте формату записей (`Add`, `Fix`, `Tweak`, `Remove`) и указывайте номер PR, если он есть.

### Генерированный ИИ-контент

Контент, созданный ИИ (код, спрайты и т.п.), **запрещено** добавлять в репозиторий.
