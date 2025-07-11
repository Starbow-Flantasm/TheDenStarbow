// SPDX-FileCopyrightText: 2024 MendaxxDev <153332064+MendaxxDev@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Simple sound emitter that emits sound on AfterActivatableUIOpenEvent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmitSoundOnUIOpenComponent : BaseEmitSoundComponent
{
    /// <summary>
    /// Blacklist for making the sound not play if certain entities open the UI
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new();
}
