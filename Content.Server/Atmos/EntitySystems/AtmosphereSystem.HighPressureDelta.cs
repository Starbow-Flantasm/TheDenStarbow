// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2022 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2022 Jacob Tong <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Tomeno <Tomeno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Tomeno <tomeno@lulzsec.co.uk>
// SPDX-FileCopyrightText: 2022 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Jezithyr <jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 SimpleStation14 <130339894+SimpleStation14@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <flyingkarii@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Server._DEN.Atmos.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Mobs.Components;


namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private EntProtoId _spaceWindProto = "SpaceWindVisual";
    private readonly HashSet<Entity<MovedByPressureComponent>> _activePressures = new();

    private void UpdateHighPressure(float frameTime)
    {
        foreach (var ent in _activePressures)
        {
            if (!ent.Comp.Throwing || _gameTiming.CurTime < ent.Comp.ThrowingCutoffTarget
                || !TryComp(ent.Owner, out PhysicsComponent? physics))
                continue;

            if (TryComp(ent.Owner, out ThrownItemComponent? thrown))
            {
                _thrown.LandComponent(ent.Owner, thrown, physics, true);
                _thrown.StopThrow(ent.Owner, thrown);
            }

            _physics.SetBodyStatus(ent.Owner, physics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(ent.Owner, physics, true);

            ent.Comp.Throwing = false;
            _activePressures.Remove(ent);
        }
    }

    private void HighPressureMovements(Entity<GridAtmosphereComponent> gridAtmosphere,
        TileAtmosphere tile,
        EntityQuery<PhysicsComponent> bodies,
        EntityQuery<TransformComponent> xforms,
        EntityQuery<MovedByPressureComponent> pressureQuery,
        EntityQuery<MetaDataComponent> metas,
        EntityQuery<ProjectileComponent> projectileQuery,
        double gravity)
    {
        var atmosComp = gridAtmosphere.Comp;
        var oneAtmos = Atmospherics.OneAtmosphere;

        // No atmos yeets, return early.
        if (!SpaceWind
            || !gridAtmosphere.Comp.SpaceWindSimulation // Is the grid marked as exempt from space wind?
            || tile.Air is null || tile.Space // No Air Checks. Pressure differentials can't exist in a hard vacuum.
            || tile.Air.Pressure <= atmosComp.PressureCutoff // Below 5kpa(can't throw a base item)
            || oneAtmos - atmosComp.PressureCutoff <= tile.Air.Pressure
            && tile.Air.Pressure <= oneAtmos + atmosComp.PressureCutoff // Check within 5kpa of default pressure.
            || !TryComp(gridAtmosphere.Owner, out MapGridComponent? mapGrid)
            || !_mapSystem.TryGetTileRef(gridAtmosphere.Owner, mapGrid, tile.GridIndices, out var tileRef))
            return;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];
        if (!tileDef.SimulatedTurf)
            return;

        var partialFrictionComposition = gravity * tileDef.MobFrictionNoInput ?? 0.2f;

        var pressureVector = GetPressureVectorFromTile(gridAtmosphere, tile);
        if (!pressureVector.IsValid())
            return;
        tile.LastPressureDirection = pressureVector;

        // Calculate this HERE so that we aren't running the square root of a whole Newton vector per item.
        var pVecLength = pressureVector.Length();
        if (pVecLength <= 1) // Then guard against extremely small vectors.
            return;

        pressureVector *= SpaceWindStrengthMultiplier;

        if (SpaceWindVisuals && atmosComp.SpaceWindSoundCooldown == 0)
        {
            var location = _mapSystem.GridTileToLocal(gridAtmosphere.Owner, mapGrid, tile.GridIndices);
            var visualEnt = SpawnAtPosition(_spaceWindProto, location);
            _transformSystem.SetLocalRotation(visualEnt, pressureVector.ToAngle() - MathF.PI / 2);
        }

        if (pVecLength > 15 && !tile.Hotspot.Valid && atmosComp.SpaceWindSoundCooldown == 0)
        {
            var coordinates = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);
            var volume = Math.Clamp(pVecLength / atmosComp.SpaceWindSoundDenominator, atmosComp.SpaceWindSoundMinVolume, atmosComp.SpaceWindSoundMaxVolume);
            _audio.PlayPvs(atmosComp.SpaceWindSound, coordinates, AudioParams.Default.WithVariation(0.125f).WithVolume(volume));
        }

        if (atmosComp.SpaceWindSoundCooldown++ > atmosComp.SpaceWindSoundCooldownCycles)
            atmosComp.SpaceWindSoundCooldown = 0;

        // TODO: Deprecated for now, it sucks ass and I'm disassembling monstermos because it sucks. This'll be handled by Space Wind after I'm done whiteboarding better equations for it.
        // - TCJ
        // HandleDecompressionFloorRip(mapGrid, otherTile, otherTile.PressureDifference);

        _entSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(tile.GridIndex, tile.GridIndices, _entSet, 0f);

        foreach (var entity in _entSet)
        {
            // Ideally containers would have their own EntityQuery internally or something given recursively it may need to slam GetComp<T> anyway.
            // Also, don't care about static bodies (but also due to collisionwakestate can't query dynamic directly atm).
            if (!bodies.TryGetComponent(entity, out var body)
                || !pressureQuery.TryGetComponent(entity, out var pressure)
                || !pressure.Enabled
                || _containers.IsEntityInContainer(entity, metas.GetComponent(entity))
                || pressure.LastHighPressureMovementAirCycle >= gridAtmosphere.Comp.UpdateCounter)
                continue;

            // tl;dr YEET
            ExperiencePressureDifference(
                (entity, pressure),
                gridAtmosphere.Comp.UpdateCounter,
                pressureVector,
                pVecLength,
                partialFrictionComposition,
                projectileQuery,
                xforms.GetComponent(entity),
                body);
        }
    }

    // Called from AtmosphereSystem.LINDA.cs with SpaceWind CVar check handled there.
    private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile) => gridAtmosphere.HighPressureDelta.Add(tile);

    public void ExperiencePressureDifference(Entity<MovedByPressureComponent> ent,
        int cycle,
        Vector2 pressureVector,
        float pVecLength,
        double partialFrictionComposition,
        EntityQuery<ProjectileComponent> projectileQuery,
        TransformComponent? xform = null,
        PhysicsComponent? physics = null)
    {
        var (uid, component) = ent;
        if (!Resolve(uid, ref physics, false)
            || !Resolve(uid, ref xform)
            || physics.BodyType == BodyType.Static
            || physics.LinearVelocity.Length() >= SpaceWindMaxForce
            || HasComp<WasMovedByPressureComponent>(ent))
            return;

        var alwaysThrow = partialFrictionComposition == 0 || physics.BodyStatus == BodyStatus.InAir;

        // Coefficient of static friction in Newtons (kg * m/s^2), which might not apply under certain conditions.
        var coefficientOfFriction = partialFrictionComposition * physics.Mass;
        coefficientOfFriction *= _standingSystem.IsDown(uid) ? 3 : 1;

        if (TryComp(ent.Owner, out HumanoidAppearanceComponent? humanoidAppearance))
        {
            pressureVector *= HumanoidThrowMultiplier;

            if (SpaceWindAllowKnockdown)
            {
                // Torque threshold for a humanoid shaped object is 1/3rd mass * height squared. Ignore the 3, it's not a magic number in this context.
                // Same with 1.75f, we're quick and dirty shorthanding for the standard height of a human (in meters).
                var heightSquared = MathF.Pow(humanoidAppearance.Height * 1.75f, 2);
                var knockdownThreshold = heightSquared / 3;
                if (knockdownThreshold <= pVecLength)
                    _sharedStunSystem.TryKnockdown(uid, TimeSpan.FromSeconds(SpaceWindKnockdownTime), true);
            }
        }

        if (!alwaysThrow && pVecLength < coefficientOfFriction)
            return;

        // Yes this technically increases the magnitude by a small amount... I detest having to swap between "World" and "Local" vectors.
        // ThrowingSystem increments linear velocity by a given vector, but we have to do this anyways because reasons.
        var velocity = _transformSystem.GetWorldRotation(uid).ToWorldVec() + pressureVector;

        _throwing.TryThrow(uid, velocity, physics, xform, projectileQuery,
            1, doSpin: physics.AngularVelocity < SpaceWindMaxAngularVelocity);

        component.LastHighPressureMovementAirCycle = cycle;
        component.Throwing = true;
        component.ThrowingCutoffTarget = _gameTiming.CurTime + component.CutoffTime;
        _activePressures.Add(ent);
        EnsureComp<WasMovedByPressureComponent>(ent);
    }
}
