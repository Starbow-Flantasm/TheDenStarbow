// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2021 tmtmtl30 <53132901+tmtmtl30@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 TekuNut <13456422+TekuNut@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnComponentInit(Entity<DoorComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;
        comp.OpenSpriteStates = new(2);
        comp.ClosedSpriteStates = new(2);

        comp.OpenSpriteStates.Add((DoorVisualLayers.Base, comp.OpenSpriteState));
        comp.ClosedSpriteStates.Add((DoorVisualLayers.Base, comp.ClosedSpriteState));

        comp.OpeningAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.OpeningAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningSpriteState, 0f) }
                }
            },
        };

        comp.ClosingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.ClosingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingSpriteState, 0f) }
                }
            },
        };

        comp.EmaggingAnimation = new Animation ()
        {
            Length = TimeSpan.FromSeconds(comp.EmaggingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.EmaggingSpriteState, 0f) }
                }
            },
        };
    }

    private void OnAppearanceChange(EntityUid uid, DoorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if(!AppearanceSystem.TryGetData<DoorState>(uid, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (AppearanceSystem.TryGetData<string>(uid, DoorVisuals.BaseRSI, out var baseRsi, args.Component))
        {
            if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / baseRsi, out var res))
            {
                Log.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);
            }
            foreach (var layer in args.Sprite.AllLayers)
            {
                layer.Rsi = res?.RSI;
            }
        }

        TryComp<AnimationPlayerComponent>(uid, out var animPlayer);
        if (_animationSystem.HasRunningAnimation(uid, animPlayer, DoorComponent.AnimationKey))
            _animationSystem.Stop(uid, animPlayer, DoorComponent.AnimationKey); // Halt all running anomations.

        args.Sprite.DrawDepth = comp.ClosedDrawDepth;
        switch(state)
        {
            case DoorState.Open:
                args.Sprite.DrawDepth = comp.OpenDrawDepth;
                foreach(var (layer, layerState) in comp.OpenSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }
                break;
            case DoorState.Closed:
                foreach(var (layer, layerState) in comp.ClosedSpriteStates)
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }
                break;
            case DoorState.Opening:
                if (animPlayer != null && comp.OpeningAnimationTime != 0.0)
                    _animationSystem.Play((uid, animPlayer), (Animation)comp.OpeningAnimation, DoorComponent.AnimationKey);
                break;
            case DoorState.Closing:
                if (animPlayer != null && comp.ClosingAnimationTime != 0.0 && comp.CurrentlyCrushing.Count == 0)
                    _animationSystem.Play((uid, animPlayer), (Animation)comp.ClosingAnimation, DoorComponent.AnimationKey);
                break;
            case DoorState.Denying:
                if (animPlayer != null)
                    _animationSystem.Play((uid, animPlayer), (Animation)comp.DenyingAnimation, DoorComponent.AnimationKey);
                break;
            case DoorState.Welded:
                break;
            case DoorState.Emagging:
                if (animPlayer != null)
                    _animationSystem.Play((uid, animPlayer), (Animation)comp.EmaggingAnimation, DoorComponent.AnimationKey);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Invalid door visual state {state}");
        }
    }
}
