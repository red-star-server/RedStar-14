// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.Visuals;

[Serializable, NetSerializable]
public enum VisualLayers : byte
{
    Enabled,
}
