// SPDX-FileCopyrightText: 2024 SpeltIncorrectyl <66873282+SpeltIncorrectyl@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power.Generator;

namespace Content.Server.Power.Generator;

public sealed class GeneratorSignalControlSystem: EntitySystem
{
    [Dependency] private readonly GeneratorSystem _generator = default!;
    [Dependency] private readonly ActiveGeneratorRevvingSystem _revving = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneratorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    /// <summary>
    /// Change the state of the generator depending on what signal is sent.
    /// </summary>
    private void OnSignalReceived(EntityUid uid, GeneratorSignalControlComponent component, SignalReceivedEvent args)
    {
        if (!TryComp<FuelGeneratorComponent>(uid, out var generator))
            return;

        if (args.Port == component.OnPort)
        {
            _revving.StartAutoRevving(uid);
        }
        else if (args.Port == component.OffPort)
        {
            _generator.SetFuelGeneratorOn(uid, false, generator);
            _revving.StopAutoRevving(uid);
        }
        else if (args.Port == component.TogglePort)
        {
            if (generator.On)
            {
                _generator.SetFuelGeneratorOn(uid, false, generator);
                _revving.StopAutoRevving(uid);
            }
            else
            {
                _revving.StartAutoRevving(uid);
            }
        }
    }
}
