// SPDX-FileCopyrightText: 2023 Tim Falken <timfalken@hotmail.com>
// SPDX-FileCopyrightText: 2025 BlitzTheSquishy <73762869+BlitzTheSquishy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CartridgeLoader.Cartridges;

[Prototype("crimeAssistPage")]
public sealed partial class CrimeAssistPage : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = "";

    [DataField("onStart")]
    public string? OnStart { get; private set; }

    [DataField("locKey")]
    public string? LocKey { get; private set; }

    [DataField("onYes")]
    public string? OnYes { get; private set; }

    [DataField("onNo")]
    public string? OnNo { get; private set; }

    [DataField("locKeyTitle")]
    public string? LocKeyTitle { get; private set; }

    [DataField("locKeyDescription")]
    public string? LocKeyDescription { get; private set; }

    [DataField("locKeySeverity")]
    public string? LocKeySeverity { get; private set; }

    [DataField("locKeyPunishment")]
    public string? LocKeyPunishment { get; private set; }
}
