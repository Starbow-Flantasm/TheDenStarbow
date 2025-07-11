// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 BlitzTheSquishy <73762869+BlitzTheSquishy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Falcon <falcon@zigtag.dev>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <flyingkarii@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Server.Parallax;
using Content.Server.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;


namespace Content.Server.Station.Systems;

public sealed class StationSurfaceSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationSurfaceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StationSurfaceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.MapPath is not {} path)
            return;

        var map = _map.CreateMap(out var mapId);
        if (!_mapLoader.TryLoadGrid(mapId, path, out _))
        {
            Log.Error($"Failed to load surface map {ent.Comp.MapPath}!");
            Del(map);
            return;
        }

        // loading replaced the map entity with a new one so get the latest id
        map = _map.GetMap(mapId);
        _map.SetPaused(map, false);

        // Needs a cherrypick, but this system is unused entirely for now
        //_biome.SetEnabled(map); // generate the terrain after the grids loaded to prevent it getting hidden under it
        ent.Comp.Map = map;
    }
}
