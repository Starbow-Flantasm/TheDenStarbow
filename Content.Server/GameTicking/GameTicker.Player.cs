// SPDX-FileCopyrightText: 2021 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2021 Galactic Chimp <63882831+GalacticChimp@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Javier Guardia Fernández <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Moony <moonheart08@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Radosvik <65792927+Radosvik@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Veritius <veritiusgaming@gmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Debug <49997488+DebugOk@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 DEATHB4DEFEAT <77995199+DEATHB4DEFEAT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 DoutorWhite <68350815+DoutorWhite@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 LordCarve <27449516+LordCarve@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Repo <47093363+Titian3@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 SimpleStation14 <130339894+SimpleStation14@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 sleepyyapril <flyingkarii@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Players;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    [UsedImplicitly]
    public sealed partial class GameTicker
    {
        private void InitializePlayer() =>
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;

        private async void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            if (_mind.TryGetMind(session.UserId, out var mindId, out var mind))
            {
                if (args.NewStatus != SessionStatus.Disconnected)
                {
                    mind.Session = session;
                    _pvsOverride.AddSessionOverride(GetNetEntity(mindId.Value), session);
                }

                DebugTools.Assert(mind.Session == session);
            }

            DebugTools.Assert(session.GetMind() == mindId);

            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                {
                    AddPlayerToDb(args.Session.UserId.UserId);

                    // Always make sure the client has player data.
                    if (session.Data.ContentDataUncast == null)
                    {
                        var data = new ContentPlayerData(session.UserId, args.Session.Name);
                        data.Mind = mindId;
                        data.Whitelisted = await _db.GetWhitelistStatusAsync(session.UserId); // Nyanotrasen - Whitelist
                        session.Data.ContentDataUncast = data;
                    }

                    var record = await _db.GetPlayerRecordByUserId(args.Session.UserId);
                    var firstConnection = record != null &&
                                          Math.Abs((record.FirstSeenTime - record.LastSeenTime).TotalMinutes) < 1;

                    _chatManager.SendAdminAnnouncement(firstConnection
                        ? Loc.GetString("player-first-join-message", ("name", args.Session.Name))
                        : Loc.GetString("player-join-message", ("name", args.Session.Name)));

                    RaiseNetworkEvent(GetConnectionStatusMsg(), session.Channel);

                    if (firstConnection && _configurationManager.GetCVar(CCVars.AdminNewPlayerJoinSound))
                        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg"),
                            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false,
                            audioParams: new AudioParams { Volume = -5f });

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTime = _gameTiming.CurTime + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    _userDb.ClientConnected(session);

                    if (mind == null)
                    {
                        if (LobbyEnabled)
                            PlayerJoinLobby(session);
                        else
                            SpawnWaitDb();

                        break;
                    }

                    if (mind.CurrentEntity == null || Deleted(mind.CurrentEntity))
                    {
                        DebugTools.Assert(mind.CurrentEntity == null, "a mind's current entity was deleted without updating the mind");

                        // This player is joining the game with an existing mind, but the mind has no entity.
                        // Their entity was probably deleted sometime while they were disconnected, or they were an observer.
                        // Instead of allowing them to spawn in, we will dump and their existing mind in an observer ghost.
                        SpawnObserverWaitDb();
                    }
                    else
                    {
                        if (_playerManager.SetAttachedEntity(session, mind.CurrentEntity))
                        {
                            PlayerJoinGame(session);
                        }
                        else
                        {
                            Log.Error(
                                $"Failed to attach player {session} with mind {ToPrettyString(mindId)} to its current entity {ToPrettyString(mind.CurrentEntity)}");
                            SpawnObserverWaitDb();
                        }
                    }

                    break;
                }

                case SessionStatus.Disconnected:
                {
                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-leave-message", ("name", args.Session.Name)));
                    if (mind != null)
                    {
                        _pvsOverride.ClearOverride(GetNetEntity(mindId!.Value));
                        mind.Session = null;
                    }

                    if (_playerGameStatuses.ContainsKey(session.UserId))
                        _userDb.ClientDisconnected(session);
                    break;
                }
            }
            //When the status of a player changes, update the server info text
            UpdateInfoText();

            async void SpawnWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                SpawnPlayer(session, EntityUid.Invalid);
            }

            async void SpawnObserverWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                JoinAsObserver(session);
            }

            async void AddPlayerToDb(Guid id)
            {
                if (RoundId != 0 && _runLevel != GameRunLevel.PreRoundLobby)
                {
                    await _db.AddRoundPlayers(RoundId, id);
                }
            }
        }

        public HumanoidCharacterProfile GetPlayerProfile(ICommonSession p)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(p.UserId).SelectedCharacter;
        }

        public void PlayerJoinGame(ICommonSession session, bool silent = false)
        {
            if (!silent)
                _chatManager.DispatchServerMessage(session, Loc.GetString("game-ticker-player-join-game-message"));

            _playerGameStatuses[session.UserId] = PlayerGameStatus.JoinedGame;
            _db.AddRoundPlayers(RoundId, session.UserId);

            if (_adminManager.HasAdminFlag(session, AdminFlags.Admin))
            {
                if (_allPreviousGameRules.Count > 0)
                {
                    var rulesMessage = GetGameRulesListMessage(true);
                    _chatManager.SendAdminAnnouncementMessage(session, Loc.GetString("starting-rule-selected-preset", ("preset", rulesMessage)));
                }
            }

            RaiseNetworkEvent(new TickerJoinGameEvent(), session.Channel);
        }

        private void PlayerJoinLobby(ICommonSession session)
        {
            _playerGameStatuses[session.UserId] = LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
            _db.AddRoundPlayers(RoundId, session.UserId);

            var client = session.Channel;
            RaiseNetworkEvent(new TickerJoinLobbyEvent(), client);
            RaiseNetworkEvent(GetStatusMsg(session), client);
            RaiseNetworkEvent(GetInfoMsg(), client);
            RaiseLocalEvent(new PlayerJoinedLobbyEvent(session));
        }

        private void ReqWindowAttentionAll()
        {
            RaiseNetworkEvent(new RequestWindowAttentionEvent());
        }
    }

    public sealed class PlayerJoinedLobbyEvent : EntityEventArgs
    {
        public readonly ICommonSession PlayerSession;

        public PlayerJoinedLobbyEvent(ICommonSession playerSession)
        {
            PlayerSession = playerSession;
        }
    }
}
