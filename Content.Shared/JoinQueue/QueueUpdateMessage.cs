// SPDX-FileCopyrightText: 2024 DEATHB4DEFEAT <77995199+DEATHB4DEFEAT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.JoinQueue;

/// <summary>
///     Sent from server to client with queue state for player
///     Also initiates queue state on client
/// </summary>
public sealed class QueueUpdateMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    ///     Total players in queue
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///     Player current position in queue (starts from 1)
    /// </summary>
    public int Position { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Total = buffer.ReadInt32();
        Position = buffer.ReadInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Total);
        buffer.Write(Position);
    }
}
