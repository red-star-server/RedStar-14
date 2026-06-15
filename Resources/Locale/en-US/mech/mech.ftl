# SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
# SPDX-FileCopyrightText: 2023 brainfood1183 <113240905+brainfood1183@users.noreply.github.com>
# SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 Gorox221 <139872389+Gorox221@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

mech-verb-enter = Enter
mech-verb-exit = Remove pilot

mech-equipment-begin-install = Installing the {THE($item)}...
mech-equipment-finish-install = Finished installing the {THE($item)}
# RS14-start
mech-install-begin-popup = {$user} is installing the {THE($item)}...
mech-cannot-insert-broken-popup = You cannot insert anything while the mech is broken.
mech-equipment-slot-full-popup = The mech has no free equipment slots.
mech-equipment-whitelist-fail-popup = This equipment cannot be installed in this mech.
mech-module-begin-install = Installing the {THE($item)}...
mech-module-finish-install = Finished installing the {THE($item)}
mech-module-slot-full-popup = The mech has no free module slots.
mech-module-whitelist-fail-popup = This module cannot be installed in this mech.
mech-cannot-modify-closed-popup = Open the mech before modifying it.
mech-duplicate-installed-popup = That module is already installed.
# RS14-end

mech-equipment-select-popup = {$item} selected
mech-equipment-select-none-popup = Nothing selected
mech-radial-no-equipment = No equipment

mech-ui-open-verb = Open control panel

mech-menu-title = mech control panel

mech-integrity-display = {$amount} %
mech-integrity-display-broken = BROKEN
mech-energy-display = {$amount} %
mech-energy-missing = MISSING
mech-slot-display = Open Slots: {$amount}

mech-no-enter = You cannot pilot this.

mech-eject-pilot-alert = {$user} is pulling the pilot out of the {$item}!

mech-construction-guide-string = All mech parts must be attached to the harness.

# RS14
mech-generator-output-label = Output: {$rate} W
mech-generator-fuel-label = Fuel: {$amount} ({$name})
mech-generator-eject-fuel-button = Eject fuel

# RS14-start
mech-module-slot-display = Open Module Slots: {$amount}
mech-equipment-slot-display-label = Equipment: {$used}/{$max}
mech-module-slot-display-label = Modules: {$used}/{$max}
mech-equipment-size-display = Size: {$size}
mech-remove-disabled-tooltip = Cannot remove equipment while access is denied
mech-equipment-section = Equipment
mech-module-section = Modules
mech-lock-status-locked = Lock status: locked
mech-lock-status-unlocked = Lock status: unlocked
mech-lock-dna-label = DNA lock
mech-lock-card-label = ID lock
mech-lock-register = Register
mech-lock-activate = Activate
mech-lock-deactivate = Deactivate
mech-lock-reset = Reset
mech-lock-not-registered = Not registered
mech-lock-owner-unknown = unknown
mech-lock-dna-info = Registered DNA: {$owner}
mech-lock-card-info = Registered access: {$owner}
mech-lock-access-denied-popup = Access denied. This mech is locked.
mech-lock-no-dna-popup = DNA lock cannot be registered.
mech-lock-no-card-popup = ID lock cannot be registered without an ID card.
mech-lock-dna-registered-popup = DNA lock registered.
mech-lock-card-registered-popup = ID lock registered.
mech-lock-card-no-access-popup = ID card has no access tags to register.
mech-lock-activated-popup = Lock activated.
mech-lock-deactivated-popup = Lock deactivated.
mech-lock-reset-success-popup = Lock reset.
mech-lock-already-registered-popup = This lock is already registered.
mech-cabin-section = Life support
mech-cabin-pressure-label = Cabin air:
mech-cabin-temperature-label = Temperature:
mech-tank-pressure-label = Tank:
mech-fan-state-label = Fan:
mech-cabin-pressure-level = { $level } kPa
mech-cabin-temperature-level = { $tempC } C
mech-tank-pressure-level = { $pressure } kPa ({ $liters } L)
mech-tank-missing = no tank
mech-no-airtight-status = unavailable
mech-cabin-purge-button = Purge cabin
mech-airtight-enabled = Airtight: on
mech-airtight-disabled = Airtight: off
mech-fan-enabled = Fan: on
mech-fan-disabled = Fan: off
mech-filter-enabled = Filter: on
mech-filter-disabled = Filter: off
mech-fan-state-off = off
mech-fan-state-on = running
mech-fan-state-idle = idle
mech-fan-state-na = unavailable

# PR #39958 mech control panel layout
mech-integrity-display-label = Integrity
mech-energy-display-label = Energy
mech-fan-status-label = Fan Status:
mech-settings-no-access-label = Access denied
mech-air-toggle-button = Toggle
mech-airtight-unavailable-label = Not airtight cabin
mech-fan-label = Fan:
mech-filter-enabled-checkbox = Filter
mech-fan-missing-label = No fan module
mech-lock-register-button = Register Lock
mech-lock-reset-tooltip = Reset
mech-equipment-label = Equipment
mech-modules-label = Modules
mech-cabin-pressure-level-label = { $level } kPa
mech-cabin-temperature-level-label = { $tempC } °C
mech-no-airtight-status-label = unavailable
mech-tank-pressure-level-label =
    { $state ->
        [ok] { $pressure } kPa
       *[na] N/A
    }
mech-fan-status-level-label =
    { $state ->
        [on] Running
        [idle] Idle
        [off] Off
       *[na] N/A
    }
mech-lock-not-set-label = Not set
mech-lock-deactivate-button = Deactivate
mech-lock-activate-button = Activate
# RS14-end
