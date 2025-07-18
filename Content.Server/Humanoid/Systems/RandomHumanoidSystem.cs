// SPDX-FileCopyrightText: 2022 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Flipp Syder <76629141+vulppine@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <drsmugleaf@gmail.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Vordenburg <114301317+Vordenburg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq;
using Content.Server.Humanoid.Components;
using Content.Server.RandomMetadata;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Humanoid.Systems;

/// <summary>
///     This deals with spawning and setting up random humanoids.
/// </summary>
public sealed class RandomHumanoidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    private HashSet<string> _notRoundStartSpecies = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomHumanoidSpawnerComponent, MapInitEvent>(OnMapInit,
            after: new []{ typeof(RandomMetadataSystem) });
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidSpawnerComponent component, MapInitEvent args)
    {
        QueueDel(uid);
        if (component.SettingsPrototypeId != null)
            SpawnRandomHumanoid(component.SettingsPrototypeId, Transform(uid).Coordinates, MetaData(uid).EntityName);

        var speciesList = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>()
            .Where(x => !x.RoundStart)
            .Select(x => x.Prototype.Id)
            .ToHashSet();

        _notRoundStartSpecies = speciesList;
    }

    public EntityUid SpawnRandomHumanoid(string prototypeId, EntityCoordinates coordinates, string name)
    {
        if (!_prototypeManager.TryIndex<RandomHumanoidSettingsPrototype>(prototypeId, out var prototype))
            throw new ArgumentException("Could not get random humanoid settings");

        var blacklist = prototype.SpeciesBlacklist;

        if (!prototype.SpeciesBlacklist.Any())
            blacklist = _notRoundStartSpecies;

        var profile = HumanoidCharacterProfile.Random(blacklist);
        var speciesProto = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var humanoid = EntityManager.CreateEntityUninitialized(speciesProto.Prototype, coordinates);

        _metaData.SetEntityName(humanoid, prototype.RandomizeName ? profile.Name : name);

        _humanoid.LoadProfile(humanoid, profile);

        if (prototype.Components != null)
        {
            foreach (var entry in prototype.Components.Values)
            {
                var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
                comp.Owner = humanoid; // This .owner must survive for now.
                EntityManager.RemoveComponent(humanoid, comp.GetType());
                EntityManager.AddComponent(humanoid, comp);
            }
        }

        EntityManager.InitializeAndStartEntity(humanoid);

        return humanoid;
    }
}
