#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public sealed class WorldSimulation
{
    private readonly ITaskDispatcher _dispatcher;
    private readonly DeviceProcessingSystem _devices;
    private readonly PressureSystem _pressure;
    private readonly INetworkTopologySystem _topology;
    private readonly EconomySystem _economy;
    private readonly StoryProgressSystem _story;
    private readonly IWorldInvariantValidator _validator;

    public WorldSimulation(ITaskDispatcher dispatcher, DeviceProcessingSystem devices, PressureSystem pressure,
        INetworkTopologySystem topology, EconomySystem economy, StoryProgressSystem story, IWorldInvariantValidator validator)
    { _dispatcher = dispatcher; _devices = devices; _pressure = pressure; _topology = topology; _economy = economy; _story = story; _validator = validator; }

    public static WorldSimulation CreateDefault()
    {
        var economy = new EconomySystem();
        return new WorldSimulation(new TaskDispatcher(new GridPathfinder()), new DeviceProcessingSystem(new BufferService(), economy),
            new PressureSystem(), new NetworkTopologySystem(), economy, new StoryProgressSystem(), new WorldInvariantValidator());
    }

    public ValidationReport Step(WorldState world, GameDefinitions definitions, double deltaTime)
    {
        if (deltaTime <= 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        if (world.PauseReasons.Count > 0) return _validator.Validate(world, definitions);
        foreach (var factory in world.Factories.Values.OrderBy(x => x.FactoryId, StringComparer.Ordinal))
        {
            _dispatcher.Dispatch(world, factory);
            StepAgents(world, factory, definitions, deltaTime);
            _devices.Step(world, factory, definitions, deltaTime);
            DeliverOrders(world, factory);
            _pressure.Step(world, factory, definitions, deltaTime);
            if (factory.Pipes.Values.Any(x => x.PipeStateValue == PipeRuntimeState.Ruptured && x.NetworkId != null)) _topology.RebuildFactoryTopology(factory, definitions);
        }
        foreach (var gameEvent in world.EventLog.OrderBy(x => x.Sequence).ToArray()) _story.ProcessEvent(world, gameEvent, definitions);
        _economy.EvaluateBankruptcy(world);
        world.SimTimeSeconds += deltaTime;
        world.SimulationStepId++;
        return _validator.Validate(world, definitions);
    }

    private static void StepAgents(WorldState world, FactoryState factory, GameDefinitions definitions, double deltaTime)
    {
        foreach (var agent in factory.Agents.Values.OrderBy(x => x.AgentId, StringComparer.Ordinal))
        {
            if (agent.CurrentTaskId == null || !factory.Tasks.TryGetValue(agent.CurrentTaskId, out var task)) { Reset(agent); continue; }
            if (agent.CurrentPath.Count > 0) { agent.CurrentCell = agent.CurrentPath[0]; agent.CurrentPath.RemoveAt(0); agent.State = agent.CurrentPath.Count == 0 ? AgentWorkState.Working : AgentWorkState.Moving; task.State = agent.State == AgentWorkState.Working ? TaskLifeState.Executing : TaskLifeState.Moving; continue; }
            agent.State = AgentWorkState.Working; task.State = TaskLifeState.Executing; task.CompletedWorkSeconds += deltaTime; task.Progress01 = (float)Math.Clamp(task.CompletedWorkSeconds / Math.Max(0.01, task.RequiredWorkSeconds), 0, 1);
            if (task.Progress01 < 1) continue;
            CompleteTask(world, factory, definitions, task); task.State = TaskLifeState.Completed; Reset(agent);
        }
    }

    private static void CompleteTask(WorldState world, FactoryState factory, GameDefinitions definitions, TaskState task)
    {
        if (factory.Devices.TryGetValue(task.TargetInstanceId, out var device))
        {
            if (task.Type == TaskType.Build) { device.LifeState = PlaceableLifeState.Active; device.WorkState = DeviceWorkState.Idle; device.Construction = null; InitializePorts(device, definitions.Devices[device.DefinitionId]); }
            else if (task.Type == TaskType.Upgrade && device.Upgrade != null) { var upgrade = definitions.Upgrades[device.Upgrade.UpgradeId]; device.Level = upgrade.ToLevel; device.ProcessingSpeedMultiplier = upgrade.ProcessingSpeedMultiplier; device.Upgrade = null; device.OperationLock = new DeviceOperationLock(); device.WorkState = DeviceWorkState.Idle; }
            else if (task.Type == TaskType.Remove) { Release(factory, device); device.LifeState = PlaceableLifeState.Removed; device.OperationLock = new DeviceOperationLock(); }
        }
        else if (factory.Pipes.TryGetValue(task.TargetInstanceId, out var pipe))
        {
            if (task.Type == TaskType.Build) { pipe.LifeState = PlaceableLifeState.Active; pipe.PipeStateValue = PipeRuntimeState.Active; }
            else if (task.Type == TaskType.RepairPipe) { pipe.Health = definitions.Pipes[pipe.PipeTypeId].MaxHealth; pipe.PipeStateValue = PipeRuntimeState.Active; pipe.Damage = new PipeDamageState(); }
            else if (task.Type == TaskType.Remove) { Release(factory, pipe); pipe.LifeState = PlaceableLifeState.Removed; pipe.PipeStateValue = PipeRuntimeState.Removed; }
        }
        world.EventLog.Add(new GameEvent { EventId = $"event:{world.SimulationStepId}:{task.TaskId}", Sequence = world.EventLog.Count + 1, EventType = $"task.{task.Type}.completed", SimTime = world.SimTimeSeconds, FactoryId = factory.FactoryId, RelatedObjectId = task.TargetInstanceId, RelatedTaskId = task.TaskId });
    }

    private static void InitializePorts(DeviceState device, DeviceDefinition definition)
    {
        if (device.Ports.Count > 0) return;
        foreach (var port in definition.Ports) device.Ports.Add(new PortState { PortId = $"{device.InstanceId}:{port.PortDefinitionId}", PortDefinitionId = port.PortDefinitionId, OwnerInstanceId = device.InstanceId, WorldCell = new GridCell(device.Anchor.Cell.X + port.LocalCell.X, device.Anchor.Cell.Y + port.LocalCell.Y), WorldFacing = port.LocalFacing, Direction = port.Direction, Medium = port.Medium, MaxFlowRate = port.MaxFlowRate, Status = PortRuntimeStatus.Disconnected });
    }
    private static void DeliverOrders(WorldState world, FactoryState factory)
    {
        foreach (var order in factory.Orders.Values.Where(x => x.Status == OrderStatus.Active).OrderBy(x => x.AcceptedSequence))
        { if (world.SimTimeSeconds > order.DeadlineSimTime) { order.Status = OrderStatus.Expired; continue; } if (order.DeliveredAmount + 1e-9 >= order.RequiredAmount) order.Status = OrderStatus.Completed; }
    }
    private static void Reset(AgentState agent) { agent.CurrentTaskId = null; agent.CurrentPath.Clear(); agent.State = AgentWorkState.Idle; }
    private static void Release(FactoryState factory, PlaceableState value) { foreach (var cell in value.OccupiedCells) if (factory.Grid.Cells.TryGetValue(cell, out var state) && state.OccupantInstanceId == value.InstanceId) { state.OccupantInstanceId = null; state.IsWalkable = state.IsBuildable; } }
}
}
