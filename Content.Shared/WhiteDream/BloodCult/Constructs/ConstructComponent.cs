// SPDX-FileCopyrightText: 2025 Falcon <falcon@zigtag.dev>
// SPDX-FileCopyrightText: 2025 Remuchi <72476615+Remuchi@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <flyingkarii@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Antag;
using Content.Shared.Language;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.Constructs;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    [DataField]
    public List<EntProtoId> Actions = new();

    /// <summary>
    ///     Used by the client to determine how long the transform animation should be played.
    /// </summary>
    [DataField]
    public float TransformDelay = 1;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultMember";

    [DataField]
    public ProtoId<LanguagePrototype> CultLanguageId { get; set; } = "Eldritch";

    [DataField]
    public EntProtoId SpawnOnDeathPrototype { get; set; } = "Ectoplasm";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    public bool Transforming = false;
    public float TransformAccumulator = 0;

    public List<EntityUid?> ActionEntities = new();
}
