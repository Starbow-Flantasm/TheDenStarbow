// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// a device that allows you to translate anomaly activity into multitool signals.
/// </summary>
[RegisterComponent, Access(typeof(AnomalySynchronizerSystem))]
public sealed partial class AnomalySynchronizerComponent : Component
{
    /// <summary>
    /// The uid of the anomaly to which the synchronizer is connected.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedAnomaly;

    /// <summary>
    /// Should the anomaly pulse when connected to the synchronizer?
    /// </summary>
    [DataField]
    public bool PulseOnConnect = true;

    /// <summary>
    /// Should the anomaly pulse when disconnected from synchronizer?
    /// </summary>
    [DataField]
    public bool PulseOnDisconnect = false;

    /// <summary>
    /// minimum distance from the synchronizer to the anomaly to be attached
    /// </summary>
    [DataField]
    public float AttachRange = 0.4f;

    [DataField]
    public ProtoId<SourcePortPrototype> DecayingPort = "Decaying";

    [DataField]
    public ProtoId<SourcePortPrototype> StabilizePort = "Stabilize";

    [DataField]
    public ProtoId<SourcePortPrototype> GrowingPort = "Growing";

    [DataField]
    public ProtoId<SourcePortPrototype> PulsePort = "Pulse";

    [DataField]
    public ProtoId<SourcePortPrototype> SupercritPort = "Supercritical";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ConnectedSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier DisconnectedSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");
}
