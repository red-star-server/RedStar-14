# Вклад в разработку RedStar-14

Если вы собираетесь внести вклад в разработку RedStar-14, ориентируйтесь на шаблон PR в [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md) и текущие CI-проверки репозитория. Это хорошая отправная точка по качеству кода и работе с ветками.

> ⚠️ **Не используйте веб-редактор GitHub.** Pull Request'ы, созданные через веб-редактор, могут быть закрыты без рассмотрения.

"Upstream" означает репозиторий [`red-star-server/RedStar-14`](https://github.com/red-star-server/RedStar-14), из которого синхронизируется форк.

---

## Контент, специфичный для RedStar

Всё, что вы создаёте с нуля (в отличие от изменений в существующем upstream-коде), размещайте в подкаталогах с префиксом `_RedStar`.

**Примеры:**
- `Content.Server\RedStar\Systems\FeatureSystem.cs`
- `Resources\Prototypes\_RedStar\feature.yml`
- `Resources\Textures\_RedStar\icon.png`
- `Resources\Locale\ru-RU\_RedStar\strings.ftl`
- `Resources\Maps\_RedStar\map.yml`

---

## Изменения файлов из upstream

Если вы правите существующие upstream-файлы (C#, YAML, FTL и т.д.), оставляйте пометки у изменённых мест. Это снижает стоимость мерджей и апстримов. Если создаёте новый текст локализации, делайте это в новых FTL-файлах/секциях, если нет явной причины править старый ключ.

В репозитории используется префикс `RedStar` (и уточнения вроде `RedStar-FeatureName`). Используйте тот же стиль для новых пометок.

**Рекомендуемые форматы:**
- Точечное изменение: `# RedStar` или `// RedStar`
- Изменение значения: `# RedStar-value: СТАРОЕ -> НОВОЕ`
- Блок изменений: `# RedStar-start` / `# RedStar-end` или `// RedStar-start` / `// RedStar-end`

**Для YAML:**
- Для одиночных правок оставляйте короткие пометки в строке.
- Для многострочных вставок используйте блочные маркеры `RedStar-start/end`.

**Для C#:**
- Для небольших правок достаточно `// RedStar`.
- Для многострочных портов/вставок оборачивайте блок в `RedStar-start/end`.
- Если код портирован из upstream PR, укажите номер PR в описании PR или рядом с блоком.

> ⚠️ В `.ftl` не ставьте комментарий в той же строке, что и ключ перевода. Комментарий должен быть строкой выше.

---

## Примеры комментариев

**Изменение поля в YAML:**
```yml
- type: entity
  id: ExampleEntity
  categories:
  - NewCategory # RedStar
```

**Изменение значения:**
```yml
  - type: Gun
    fireRate: 4 # RedStar-value: 3 -> 4
```

**Блочная вставка в YAML:**
```yml
# RedStar-start
- type: entity
  id: ExampleRedStarEntity
  parent: BaseItem
# RedStar-end
```

**Точечное изменение в C#:**
```cs
using Content.Shared.Damage; // RedStar
```

**Блочная вставка в C#:**
```cs
// RedStar-start: синхронизация цвета штампа
if (TryComp<StampComponent>(uid, out var stamp))
{
    stamp.StampedColor = state.Color;
}
// RedStar-end
```

**Изменение локализации (`.ftl`):**
```fluent
# RedStar-value: "Job Whitelists" -> "Role Whitelists"
player-panel-job-whitelists = Role Whitelists
```

---

## Карты

Для карт форка используйте каталог `Resources\Maps\_RedStar`.

Если добавляете новую карту ротации:
- добавьте файл карты в `Resources\Maps\_RedStar\...`;
- добавьте/обновите `gameMap`-прототип в `Resources\Prototypes\Maps\...`;
- при необходимости обновите map pool в `Resources\Prototypes\Maps\Pools\...`.

Если меняете существующую карту, заранее согласуйте изменения с мейнтейнером карты и не работайте параллельно над одной `.yml`-картой без координации.

---

## Перед отправкой PR

Перед отправкой PR:
- проверьте `git diff` и список файлов на случайные изменения;
- убедитесь, что нет лишних форматных изменений (пробелы, переносы строк, массовый рефакторинг не по задаче);
- запустите хотя бы базовую проверку сборки: `dotnet build SpaceStation14.sln`.

Если PR долго живёт и в него случайно попали изменения в `RobustToolbox`, уберите их отдельным коммитом:

```bash
git fetch upstream
git restore --source upstream/master -- RobustToolbox
```

Если в `upstream` основной бранч не `master`, используйте актуальное имя ветки (например, `main`).

---

## Ченджлоги

Для контента форка используйте соответствующие файлы в `Resources\Changelog\` по текущему формату репозитория.

Следуйте существующему формату записей (`Add`, `Fix`, `Tweak`, `Remove`) и указывайте номер PR, если он есть.

---

## Дополнительные ресурсы

Если вы новичок в разработке SS14:
- [Документация SS14](https://docs.spacestation14.io/)
- [Обширный гайд по разработке SS14](https://wiki.team.ss14.org/development)

---

## Генерированный ИИ-контент

Контент, созданный ИИ (код, спрайты и т.п.), **запрещено** добавлять в репозиторий.

Попытка отправить такой контент может привести к ограничениям на участие в разработке.
