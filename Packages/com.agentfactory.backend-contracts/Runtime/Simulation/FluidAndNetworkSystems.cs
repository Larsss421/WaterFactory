#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public sealed class BufferService
{
    public double Total(FluidBufferState buffer) => buffer.Contents.Sum(x => x.Amount);

    public double Add(FluidBufferState buffer, string fluidId, double requested, float? temperature = null, Phase? phase = null)
    {
        if (requested <= 0) return 0;
        if (!buffer.AllowMixedInput && buffer.Contents.Any(x => x.Amount > 0 && x.FluidId != fluidId)) return 0;
        var accepted = Math.Min(requested, Math.Max(0, buffer.Capacity - Total(buffer)));
        if (accepted <= 0) return 0;
        var stack = buffer.Contents.FirstOrDefault(x => x.FluidId == fluidId && x.Phase == phase);
        if (stack == null)
        {
            stack = new FluidStack { FluidId = fluidId, Temperature = temperature, Phase = phase };
            buffer.Contents.Add(stack);
        }
        stack.Amount += accepted;
        return accepted;
    }

    public double Remove(FluidBufferState buffer, string fluidId, double requested)
    {
        var remaining = Math.Max(0, requested);
        var removed = 0d;
        foreach (var stack in buffer.Contents.Where(x => x.FluidId == fluidId).ToList())
        {
            var take = Math.Min(stack.Amount, remaining);
            stack.Amount -= take;
            remaining -= take;
            removed += take;
            if (stack.Amount <= 1e-9) buffer.Contents.Remove(stack);
            if (remaining <= 1e-9) break;
        }
        return removed;
    }
}

public sealed class NetworkTopologySystem : INetworkTopologySystem
{
    public void RebuildFactoryTopology(FactoryState factory, GameDefinitions definitions)
    {
        factory.Connections.Clear();
        factory.PipeNetworks.Clear();
        foreach (var pipe in factory.Pipes.Values)
        {
            pipe.ConnectionsByDirection.Clear();
            pipe.NetworkId = null;
        }

        var active = factory.Pipes.Values.Where(IsTransportable).OrderBy(x => x.InstanceId, StringComparer.Ordinal).ToArray();
        var byCell = active.ToDictionary(x => x.Anchor.Cell);
        foreach (var pipe in active)
        foreach (var (direction, adjacent) in Adjacent(pipe.Anchor.Cell))
        {
            if (!byCell.TryGetValue(adjacent, out var other) || StringComparer.Ordinal.Compare(pipe.InstanceId, other.InstanceId) >= 0) continue;
            var connectionId = $"conn:{pipe.InstanceId}:{other.InstanceId}";
            var maxFlow = Math.Min(definitions.Pipes[pipe.PipeTypeId].MaxFlowRate, definitions.Pipes[other.PipeTypeId].MaxFlowRate);
            factory.Connections[connectionId] = new ConnectionState
            {
                ConnectionId = connectionId, FactoryId = factory.FactoryId, Status = ConnectionStatus.Active, MaxFlowRate = maxFlow,
                EndpointA = new ConnectionEndpoint { OwnerInstanceId = pipe.InstanceId, Type = ConnectionEndpointType.PipeSide, Cell = pipe.Anchor.Cell, Facing = direction },
                EndpointB = new ConnectionEndpoint { OwnerInstanceId = other.InstanceId, Type = ConnectionEndpointType.PipeSide, Cell = other.Anchor.Cell, Facing = Opposite(direction) }
            };
            pipe.ConnectionsByDirection[direction] = connectionId;
            other.ConnectionsByDirection[Opposite(direction)] = connectionId;
        }

        var visited = new HashSet<string>();
        var networkIndex = 0;
        foreach (var root in active)
        {
            if (!visited.Add(root.InstanceId)) continue;
            var networkId = $"network:{factory.FactoryId}:{++networkIndex:D4}";
            var network = new PipeNetworkState { NetworkId = networkId, FactoryId = factory.FactoryId, Status = NetworkRuntimeStatus.Ready, TopologyVersion = factory.TopologyVersion + 1 };
            var queue = new Queue<PipeState>(); queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                current.NetworkId = networkId; network.PipeIds.Add(current.InstanceId);
                foreach (var connectionId in current.ConnectionsByDirection.Values)
                {
                    network.ConnectionIds.Add(connectionId);
                    var connection = factory.Connections[connectionId];
                    var nextId = connection.EndpointA.OwnerInstanceId == current.InstanceId ? connection.EndpointB.OwnerInstanceId : connection.EndpointA.OwnerInstanceId;
                    if (visited.Add(nextId)) queue.Enqueue(factory.Pipes[nextId]);
                }
            }
            network.TotalContainedAmount = network.PipeIds.Sum(id => factory.Pipes[id].CurrentContainedAmount);
            factory.PipeNetworks[networkId] = network;
        }
        factory.TopologyVersion++;
    }

    private static bool IsTransportable(PipeState pipe) => pipe.LifeState == PlaceableLifeState.Active && pipe.PipeStateValue is PipeRuntimeState.Active or PipeRuntimeState.Damaged;
    private static IEnumerable<(GridDirection direction, GridCell cell)> Adjacent(GridCell cell)
    {
        yield return (GridDirection.North, new GridCell(cell.X, cell.Y + 1));
        yield return (GridDirection.East, new GridCell(cell.X + 1, cell.Y));
        yield return (GridDirection.South, new GridCell(cell.X, cell.Y - 1));
        yield return (GridDirection.West, new GridCell(cell.X - 1, cell.Y));
    }
    private static GridDirection Opposite(GridDirection value) => value switch
    {
        GridDirection.North => GridDirection.South, GridDirection.East => GridDirection.West,
        GridDirection.South => GridDirection.North, _ => GridDirection.East
    };
}

public sealed class DeviceProcessingSystem
{
    private readonly BufferService _buffers;
    private readonly IEconomySystem _economy;
    public DeviceProcessingSystem(BufferService buffers, IEconomySystem economy) { _buffers = buffers; _economy = economy; }

    public void Step(WorldState world, FactoryState factory, GameDefinitions definitions, double deltaTime)
    {
        foreach (var device in factory.Devices.Values.OrderBy(x => x.InstanceId, StringComparer.Ordinal))
        {
            if (device.LifeState != PlaceableLifeState.Active || device.SelectedRecipeId == null || device.OperationLock.IsLocked) continue;
            if (!definitions.Recipes.TryGetValue(device.SelectedRecipeId, out var recipe)) { device.WorkState = DeviceWorkState.Faulted; device.PrimaryBlockReason = "recipe.unknown"; continue; }
            var inputs = device.Buffers.Values.Where(x => x.Role is BufferRole.Input or BufferRole.Storage).ToArray();
            var outputs = device.Buffers.Values.Where(x => x.Role is BufferRole.Output or BufferRole.Storage).ToArray();
            if (!device.RecipeRuntime.InputsReserved)
            {
                if (recipe.Inputs.Any(item => inputs.Sum(x => x.Contents.Where(y => y.FluidId == item.FluidId).Sum(y => y.Amount)) + 1e-9 < item.Amount))
                { device.WorkState = DeviceWorkState.WaitingForInput; device.PrimaryBlockReason = "recipe.input_missing"; continue; }
                if (recipe.Outputs.Any(item => outputs.Sum(x => Math.Max(0, x.Capacity - _buffers.Total(x))) + 1e-9 < item.Amount))
                { device.WorkState = DeviceWorkState.OutputFull; device.PrimaryBlockReason = "recipe.output_full"; continue; }
                foreach (var item in recipe.Inputs)
                {
                    var remaining = item.Amount;
                    foreach (var buffer in inputs) remaining -= _buffers.Remove(buffer, item.FluidId, remaining);
                    device.RecipeRuntime.ReservedInputs.Add(new FluidStack { FluidId = item.FluidId, Amount = item.Amount });
                }
                device.RecipeRuntime.InputsReserved = true;
            }
            device.WorkState = DeviceWorkState.Running; device.PrimaryBlockReason = null;
            device.RecipeRuntime.CycleProgressSeconds += deltaTime * Math.Max(0.01f, device.ProcessingSpeedMultiplier);
            if (device.RecipeRuntime.CycleProgressSeconds + 1e-9 < recipe.CycleDurationSeconds) continue;
            var chargeId = $"energy:{device.InstanceId}:{world.SimulationStepId}";
            var payment = _economy.Apply(world, new AgentFactory.Contracts.Operations.MoneyOperation(chargeId, MoneyCategory.Electricity, -recipe.EnergyCostPerCycle, SpendingPolicy.RejectIfInsufficient, factory.FactoryId, device.InstanceId));
            if (!payment.Success) { device.WorkState = DeviceWorkState.WaitingForInput; device.PrimaryBlockReason = payment.ResultCode; continue; }
            foreach (var item in recipe.Outputs)
            {
                var remaining = item.Amount;
                foreach (var buffer in outputs) remaining -= _buffers.Add(buffer, item.FluidId, remaining);
            }
            device.RecipeRuntime.CycleProgressSeconds = Math.Max(0, device.RecipeRuntime.CycleProgressSeconds - recipe.CycleDurationSeconds);
            device.RecipeRuntime.InputsReserved = false; device.RecipeRuntime.ReservedInputs.Clear();
        }
    }
}

public sealed class PressureSystem
{
    public void Step(WorldState world, FactoryState factory, GameDefinitions definitions, double deltaTime)
    {
        foreach (var pipe in factory.Pipes.Values.OrderBy(x => x.InstanceId, StringComparer.Ordinal))
        {
            if (!definitions.Pipes.TryGetValue(pipe.PipeTypeId, out var definition) || pipe.PipeStateValue is PipeRuntimeState.Ruptured or PipeRuntimeState.Removed) continue;
            pipe.CurrentContainedAmount = Math.Clamp(pipe.CurrentContainedAmount + (pipe.CurrentInflowRate - pipe.CurrentOutflowRate) * deltaTime, 0, definition.Capacity);
            pipe.CurrentPressure = definition.Capacity <= 0 ? 0 : (float)(pipe.CurrentContainedAmount / definition.Capacity * definition.SafePressure);
            pipe.Damage.OverSafePressure = pipe.CurrentPressure > definition.SafePressure;
            pipe.Damage.OverCriticalPressure = pipe.CurrentPressure > definition.CriticalPressure;
            if (!pipe.Damage.OverSafePressure) { pipe.Damage.ContinuousOverpressureTime = 0; continue; }
            pipe.Damage.ContinuousOverpressureTime += deltaTime;
            var multiplier = pipe.Damage.OverCriticalPressure ? definition.RuptureDamageMultiplier : 1f;
            var damage = definition.BaseDamageRate * multiplier * (float)deltaTime;
            pipe.Health = Math.Max(0, pipe.Health - damage); pipe.Damage.LastDamageAmount = damage; pipe.Damage.LastDamageReasonCode = "pressure.over_safe";
            if (pipe.Health <= definition.PreventiveRepairThreshold && pipe.PipeStateValue == PipeRuntimeState.Active)
            { pipe.PipeStateValue = PipeRuntimeState.Damaged; pipe.Damage.DamagedAtSimTime ??= world.SimTimeSeconds; }
            if (pipe.Health > 0) continue;
            pipe.PipeStateValue = PipeRuntimeState.Ruptured; pipe.Damage.RupturedAtSimTime ??= world.SimTimeSeconds;
            foreach (var packetId in pipe.PacketIds)
                if (factory.FluidPackets.TryGetValue(packetId, out var packet)) { packet.State = PacketRuntimeState.Removed; packet.LocationType = FluidLocationType.Leaked; }
            pipe.PacketIds.Clear(); pipe.CurrentContainedAmount = 0;
            EnsureRepairTask(factory, pipe, definition);
        }
    }

    private static void EnsureRepairTask(FactoryState factory, PipeState pipe, PipeDefinition definition)
    {
        if (pipe.Damage.ActiveRepairTaskId != null && factory.Tasks.ContainsKey(pipe.Damage.ActiveRepairTaskId)) return;
        var id = $"task:repair:{pipe.InstanceId}";
        factory.Tasks[id] = new TaskState { TaskId = id, FactoryId = factory.FactoryId, TargetInstanceId = pipe.InstanceId, Type = TaskType.RepairPipe, Priority = 100, State = TaskLifeState.Queued, RequiredWorkSeconds = definition.RepairDurationSeconds, CandidateWorkCells = pipe.OccupiedCells.ToList() };
        pipe.Damage.ActiveRepairTaskId = id;
    }
}
}
