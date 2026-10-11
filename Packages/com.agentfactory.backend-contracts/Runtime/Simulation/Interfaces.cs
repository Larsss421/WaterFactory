#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.Operations;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public interface IWorldRequestHandler { OperationResult Handle(OperationRequest request, WorldState world, GameDefinitions definitions); }
public interface IFactorySimulation { void Step(WorldState world, FactoryState factory, GameDefinitions definitions, double deltaTime); }
public interface ITaskDispatcher { void Dispatch(WorldState world, FactoryState factory); }
public interface IPathfinder { PathResult FindPath(FactoryState factory, GridCell start, IReadOnlyList<GridCell> goals); }
public interface INetworkTopologySystem { void RebuildFactoryTopology(FactoryState factory, GameDefinitions definitions); }
public interface IEconomySystem { MoneyOperationResult Apply(WorldState world, MoneyOperation operation); }
public interface IStoryProgressSystem { void ProcessEvent(WorldState world, GameEvent gameEvent, GameDefinitions definitions); }
public interface IWorldInvariantValidator { ValidationReport Validate(WorldState world, GameDefinitions definitions); }

public sealed record PathResult(bool Reachable, IReadOnlyList<GridCell> Path, GridCell? SelectedGoal, float TotalCost, string? FailureReasonCode);
public sealed record MoneyOperationResult(bool Success, string ResultCode, string? TransactionId, decimal RequestedAmount, decimal AppliedAmount, decimal BalanceBefore, decimal BalanceAfter, decimal Shortage);

public sealed class ValidationReport
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
}
