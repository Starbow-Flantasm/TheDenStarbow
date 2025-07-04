// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.FootPrint;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Random;

namespace Content.Client.FootPrint;

public sealed class FootPrintsVisualizerSystem : VisualizerSystem<FootPrintComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootPrintComponent, ComponentInit>(OnInitialized);
        SubscribeLocalEvent<FootPrintComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FootPrintComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<FootPrintComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not FootPrintState state || !TryGetEntity(state.PrintOwner, out var entity))
            return;

        ent.Comp.PrintOwner = entity.Value;
    }

    private void OnInitialized(EntityUid uid, FootPrintComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(FootPrintVisualLayers.Print);
        UpdateAppearance(uid, comp, sprite);
    }

    private void OnShutdown(EntityUid uid, FootPrintComponent comp, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite)
            && sprite.LayerMapTryGet(FootPrintVisualLayers.Print, out var layer))
            sprite.RemoveLayer(layer);
    }

    private void UpdateAppearance(EntityUid uid, FootPrintComponent component, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(FootPrintVisualLayers.Print, out var layer)
            || !TryComp<FootPrintsComponent>(component.PrintOwner, out var printsComponent)
            || !TryComp<AppearanceComponent>(uid, out var appearance)
            || !_appearance.TryGetData<FootPrintVisuals>(uid, FootPrintVisualState.State, out var printVisuals, appearance))
            return;

        sprite.LayerSetState(layer, new RSI.StateId(printVisuals switch
        {
            FootPrintVisuals.BareFootPrint => printsComponent.RightStep ? printsComponent.RightBarePrint : printsComponent.LeftBarePrint,
            FootPrintVisuals.ShoesPrint => printsComponent.ShoesPrint,
            FootPrintVisuals.SuitPrint => printsComponent.SuitPrint,
            FootPrintVisuals.Dragging => _random.Pick(printsComponent.DraggingPrint),
            _ => throw new ArgumentOutOfRangeException($"Unknown {printVisuals} parameter.")
        }), printsComponent.RsiPath);

        if (_appearance.TryGetData<Color>(uid, FootPrintVisualState.Color, out var printColor, appearance))
            sprite.LayerSetColor(layer, printColor);
    }

    protected override void OnAppearanceChange (EntityUid uid, FootPrintComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        UpdateAppearance(uid, component, sprite);
    }
}
