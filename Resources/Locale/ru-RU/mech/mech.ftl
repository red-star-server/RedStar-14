mech-verb-enter = Войти
mech-verb-exit = Извлечь пилота
mech-equipment-begin-install = { CAPITALIZE($item) } устанавливается...
mech-equipment-finish-install = Установка { $item } завершена
mech-install-begin-popup = { $user } устанавливает { $item }...
mech-cannot-insert-broken-popup = Нельзя устанавливать детали в сломанный мех.
mech-equipment-slot-full-popup = У меха нет свободных слотов оборудования.
mech-equipment-whitelist-fail-popup = Это оборудование нельзя установить в этого меха.
mech-module-begin-install = { CAPITALIZE($item) } устанавливается...
mech-module-finish-install = Установка { $item } завершена
mech-module-slot-full-popup = У меха нет свободных слотов модулей.
mech-module-whitelist-fail-popup = Этот модуль нельзя установить в этого меха.
mech-cannot-modify-closed-popup = Откройте мех перед модификацией.
mech-duplicate-installed-popup = Такой модуль уже установлен.
mech-equipment-select-popup = Выбрано следующее: { $item }
mech-equipment-select-none-popup = Ничего не выбрано
mech-radial-no-equipment = Без оборудования
mech-ui-open-verb = Открыть панель управления
mech-menu-title = Панель управления меха
mech-integrity-display = { $amount } %
mech-integrity-display-broken = СЛОМАН
mech-energy-display = { $amount } %
mech-energy-missing = ОТСУТСТВУЕТ
mech-slot-display = Доступно слотов: { $amount }
mech-no-enter = Вы не можете пилотировать это.
mech-eject-pilot-alert = { $user } вытаскивает пилота из { $item }!
mech-generator-output-label = Выработка: { $rate } Вт
mech-generator-fuel-label = Топливо: { $amount } ({ $name })
mech-construction-guide-string = Все детали меха должны быть прикреплены к каркасу.
mech-generator-eject-fuel-button = Извлечь топливо
mech-module-slot-display = Доступно слотов модулей: { $amount }
mech-equipment-slot-display-label = Оборудование: { $used }/{ $max }
mech-module-slot-display-label = Модули: { $used }/{ $max }
mech-equipment-size-display = Размер: { $size }
mech-remove-disabled-tooltip = Нельзя снять оборудование без доступа
mech-equipment-section = Оборудование
mech-module-section = Модули
mech-lock-status-locked = Статус замка: заблокирован
mech-lock-status-unlocked = Статус замка: разблокирован
mech-lock-dna-label = ДНК-замок
mech-lock-card-label = ID-замок
mech-lock-register = Зарегистрировать
mech-lock-activate = Активировать
mech-lock-deactivate = Деактивировать
mech-lock-reset = Сбросить
mech-lock-not-registered = Не зарегистрирован
mech-lock-owner-unknown = неизвестно
mech-lock-dna-info = Зарегистрированная ДНК: { $owner }
mech-lock-card-info = Зарегистрированный доступ: { $owner }
mech-lock-access-denied-popup = Доступ запрещён. Этот мех заблокирован.
mech-lock-no-dna-popup = Не удалось зарегистрировать ДНК-замок.
mech-lock-no-card-popup = Нужна ID-карта для регистрации ID-замка.
mech-lock-dna-registered-popup = ДНК-замок зарегистрирован.
mech-lock-card-registered-popup = ID-замок зарегистрирован.
mech-lock-card-no-access-popup = У ID-карты нет доступов для регистрации.
mech-lock-activated-popup = Замок активирован.
mech-lock-deactivated-popup = Замок деактивирован.
mech-lock-reset-success-popup = Замок сброшен.
mech-lock-already-registered-popup = Этот замок уже зарегистрирован.
mech-cabin-section = Жизнеобеспечение
mech-cabin-pressure-label = Воздух кабины:
mech-cabin-temperature-label = Температура:
mech-tank-pressure-label = Баллон:
mech-fan-state-label = Вентилятор:
mech-cabin-pressure-level = { $level } кПа
mech-cabin-temperature-level = { $tempC } C
mech-tank-pressure-level = { $pressure } кПа ({ $liters } л)
mech-tank-missing = баллон отсутствует
mech-no-airtight-status = недоступно
mech-cabin-purge-button = Продуть кабину
mech-airtight-enabled = Герметичность: вкл.
mech-airtight-disabled = Герметичность: выкл.
mech-fan-enabled = Вентилятор: вкл.
mech-fan-disabled = Вентилятор: выкл.
mech-filter-enabled = Фильтр: вкл.
mech-filter-disabled = Фильтр: выкл.
mech-fan-state-off = выкл.
mech-fan-state-on = работает
mech-fan-state-idle = ожидание
mech-fan-state-na = недоступно
# PR #39958 mech control panel layout
mech-integrity-display-label = Целостность
mech-energy-display-label = Энергия
mech-fan-status-label = Статус вентилятора:
mech-settings-no-access-label = Доступ запрещён
mech-air-toggle-button = Переключить
mech-airtight-unavailable-label = кабина негерметична
mech-fan-label = Вентилятор:
mech-filter-enabled-checkbox = Фильтр
mech-fan-missing-label = нет модуля вентилятора
mech-lock-register-button = Зарегистрировать замок
mech-lock-reset-tooltip = Сбросить
mech-equipment-label = Оборудование
mech-modules-label = Модули
mech-cabin-pressure-level-label = { $level } кПа
mech-cabin-temperature-level-label = { $tempC } °C
mech-no-airtight-status-label = недоступно
mech-tank-pressure-level-label =
    { $state ->
        [ok] { $pressure } кПа
       *[na] Н/Д
    }
mech-fan-status-level-label =
    { $state ->
        [on] работает
        [idle] ожидание
        [off] выкл.
       *[na] Н/Д
    }
mech-lock-not-set-label = Не задан
mech-lock-deactivate-button = Деактивировать
mech-lock-activate-button = Активировать
