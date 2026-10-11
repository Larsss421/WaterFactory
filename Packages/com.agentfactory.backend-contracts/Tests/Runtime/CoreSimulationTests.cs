#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.Operations;
using AgentFactory.Contracts.Simulation;
using AgentFactory.Contracts.World;
using NUnit.Framework;

namespace AgentFactory.Contracts.Tests
{

public sealed class CoreSimulationTests
{
    [Test]
    public void Economy_IsIdempotent_AndConservesBalance()
    {
        var world = new WorldState { Economy = new EconomyState { InitialBalance = 100, Balance = 100, NextTransactionSequence = 1 } };
        var system = new EconomySystem();
        var operation = new MoneyOperation("tx-1", MoneyCategory.BuildCost, -25, SpendingPolicy.RejectIfInsufficient);
        Assert.That(system.Apply(world, operation).Success, Is.True);
        Assert.That(system.Apply(world, operation).ResultCode, Is.EqualTo("economy.already_applied"));
        Assert.That(world.Economy.Balance, Is.EqualTo(75));
        Assert.That(world.Ledger, Has.Count.EqualTo(1));
    }

    [Test]
    public void Pathfinder_ReturnsDeterministicShortestPath()
    {
        var factory = WalkableFactory(4, 4);
        var result = new GridPathfinder().FindPath(factory, new GridCell(0, 0), new[] { new GridCell(3, 0) });
        Assert.That(result.Reachable, Is.True);
        Assert.That(result.TotalCost, Is.EqualTo(3));
        Assert.That(result.Path.First(), Is.EqualTo(new GridCell(0, 0)));
        Assert.That(result.Path.Last(), Is.EqualTo(new GridCell(3, 0)));
    }

    [Test]
    public void Serializer_RoundTripsWorld()
    {
        var world = new WorldState { ConfigVersion = 7, Economy = new EconomyState { InitialBalance = 12.34m, Balance = 12.34m } };
        var serializer = new WorldSerializer();
        var copy = serializer.Deserialize(serializer.Serialize(world));
        Assert.That(copy.ConfigVersion, Is.EqualTo(7));
        Assert.That(copy.Economy.Balance, Is.EqualTo(12.34m));
    }

    [Test]
    public void Initializer_CreatesPurchasedGridAndAgent()
    {
        var definitions = Definitions();
        var world = new WorldInitializer().Create(definitions);
        var factory = world.Factories["factory-a"];
        Assert.That(factory.Grid.Cells.Values.All(x => x.IsPurchased), Is.True);
        Assert.That(factory.Agents, Has.Count.EqualTo(1));
        Assert.That(world.Economy.Balance, Is.EqualTo(5000));
    }

    private static FactoryState WalkableFactory(int width, int height)
    {
        var factory = new FactoryState { FactoryId = "f", FactoryDefinitionId = "f", IsPurchased = true, Grid = new GridRuntimeState { Size = new GridSize(width, height) } };
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) { var cell = new GridCell(x, y); factory.Grid.Cells[cell] = new GridCellState { Coordinate = cell, IsBuildable = true, IsWalkable = true, IsPurchased = true }; }
        return factory;
    }
    private static GameDefinitions Definitions() => new() { ConfigVersion = 1, Factories = { ["factory-a"] = new FactoryDefinition { FactoryDefinitionId = "factory-a", DisplayNameKey = "factory", GridSize = new GridSize(2, 2), MaxAgentCount = 2, AgentSpawnCells = { new GridCell(0, 0) }, Parcels = { new LandParcelDefinition { ParcelDefinitionId = "p1", InitiallyPurchased = true, Cells = { new GridCell(0, 0), new GridCell(1, 0), new GridCell(0, 1), new GridCell(1, 1) } } } } } };
}
}
