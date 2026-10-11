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

public sealed class WorldRequestHandler : IWorldRequestHandler
{
    private readonly IEconomySystem _economy;
    private readonly WorldInitializer _initializer;
    public WorldRequestHandler(IEconomySystem economy, WorldInitializer initializer) { _economy = economy; _initializer = initializer; }

    public OperationResult Handle(OperationRequest request, WorldState world, GameDefinitions definitions)
    {
        if (world.ProcessedRequests.TryGetValue(request.RequestId, out var processed))
            return Result(request, processed.Success, processed.ResultCode, world.WorldVersion, world.WorldVersion);
        var previous = world.WorldVersion;
        if (request.ExpectedWorldVersion != previous) return Complete(request, world, false, "world.version_conflict", previous);
        try
        {
            var result = request switch
            {
                ConfirmBlueprintRequest value => ConfirmBlueprint(value, world, definitions),
                CancelConstructionRequest value => CancelConstruction(value, world, definitions),
                ConfirmDemolitionRequest value => ConfirmDemolition(value, world, definitions),
                ConfirmUpgradeRequest value => ConfirmUpgrade(value, world, definitions),
                ChangeRecipeRequest value => ChangeRecipe(value, world, definitions),
                PurchaseLandParcelRequest value => PurchaseLand(value, world, definitions),
                PurchaseFactoryRequest value => PurchaseFactory(value, world, definitions),
                HireAgentRequest value => HireAgent(value, world, definitions),
                AcceptOrderRequest value => AcceptOrder(value, world),
                _ => Result(request, false, "request.unsupported", previous, previous)
            };
            return Complete(request, world, result.Success, result.ResultCode, previous, result);
        }
        catch (InvalidOperationException exception)
        {
            return Complete(request, world, false, exception.Message, previous);
        }
    }

    private OperationResult ConfirmBlueprint(ConfirmBlueprintRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId);
        if (definitions.Devices.TryGetValue(request.DefinitionId, out var deviceDefinition))
        {
            var cells = Footprint(request.Anchor, deviceDefinition.Footprint).ToList();
            ValidatePlacement(world, factory, cells, deviceDefinition.RequiredUnlockId);
            if (deviceDefinition.MaxPerFactory > 0 && factory.Devices.Values.Count(x => x.DefinitionId == request.DefinitionId && x.LifeState != PlaceableLifeState.Removed) >= deviceDefinition.MaxPerFactory)
                throw new InvalidOperationException("build.limit_reached");
            var id = NextId(factory.FactoryId, "device", factory.Devices.Keys);
            Charge(world, request.RequestId, MoneyCategory.BuildCost, -deviceDefinition.BuildCost, factory.FactoryId, id);
            var device = new DeviceState { InstanceId = id, DefinitionId = request.DefinitionId, FactoryId = factory.FactoryId, Anchor = request.Anchor, OccupiedCells = cells, LifeState = PlaceableLifeState.UnderConstruction, WorkState = DeviceWorkState.Idle, CreatedByRequestId = request.RequestId, CreatedAtWorldVersion = world.WorldVersion + 1, Construction = new ConstructionState { BuildCost = deviceDefinition.BuildCost, PaidAmount = deviceDefinition.BuildCost, RequiredWorkSeconds = deviceDefinition.BuildDurationSeconds } };
            foreach (var definition in deviceDefinition.Buffers) device.Buffers[definition.BufferId] = new FluidBufferState { BufferId = definition.BufferId, OwnerInstanceId = id, Role = definition.Role, Capacity = definition.Capacity, AllowMixedInput = definition.AllowMixedInput };
            factory.Devices[id] = device; Occupy(factory, id, cells);
            var task = CreateTask(factory, request, id, TaskType.Build, deviceDefinition.BuildDurationSeconds, cells); factory.Tasks[task.TaskId] = task;
            return Success(request, world, id, task.TaskId, -deviceDefinition.BuildCost);
        }
        if (!definitions.Pipes.TryGetValue(request.DefinitionId, out var pipeDefinition)) throw new InvalidOperationException("build.definition_unknown");
        var pipeCells = new[] { request.Anchor.Cell }; ValidatePlacement(world, factory, pipeCells, null);
        var pipeId = NextId(factory.FactoryId, "pipe", factory.Pipes.Keys);
        Charge(world, request.RequestId, MoneyCategory.BuildCost, -pipeDefinition.BuildCost, factory.FactoryId, pipeId);
        factory.Pipes[pipeId] = new PipeState { InstanceId = pipeId, DefinitionId = request.DefinitionId, PipeTypeId = request.DefinitionId, FactoryId = factory.FactoryId, Anchor = request.Anchor, OccupiedCells = pipeCells.ToList(), LifeState = PlaceableLifeState.UnderConstruction, PipeStateValue = PipeRuntimeState.UnderConstruction, Health = pipeDefinition.MaxHealth, CreatedByRequestId = request.RequestId, CreatedAtWorldVersion = world.WorldVersion + 1 };
        Occupy(factory, pipeId, pipeCells);
        var pipeTask = CreateTask(factory, request, pipeId, TaskType.Build, 1, pipeCells); factory.Tasks[pipeTask.TaskId] = pipeTask;
        return Success(request, world, pipeId, pipeTask.TaskId, -pipeDefinition.BuildCost);
    }

    private OperationResult CancelConstruction(CancelConstructionRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId);
        if (!TryPlaceable(factory, request.TargetInstanceId, out var placeable) || placeable.LifeState != PlaceableLifeState.UnderConstruction) throw new InvalidOperationException("construction.not_found");
        var task = factory.Tasks.Values.FirstOrDefault(x => x.TargetInstanceId == placeable.InstanceId && x.Type == TaskType.Build && x.State != TaskLifeState.Completed);
        var progress = task?.Progress01 ?? 0;
        var paid = placeable is DeviceState device ? device.Construction?.PaidAmount ?? 0 : definitions.Pipes[((PipeState)placeable).PipeTypeId].BuildCost;
        var refund = decimal.Round(paid * (decimal)Math.Clamp(1 - progress, 0, 1), 2);
        if (refund > 0) _economy.Apply(world, new MoneyOperation($"refund:{request.RequestId}", MoneyCategory.ConstructionCancelRefund, refund, SpendingPolicy.AllowNegativeBalance, factory.FactoryId, placeable.InstanceId, RelatedRequestId: request.RequestId));
        Release(factory, placeable); Remove(factory, placeable); if (task != null) { task.State = TaskLifeState.Cancelled; Unassign(factory, task); }
        return Success(request, world, placeable.InstanceId, null, refund);
    }

    private OperationResult ConfirmDemolition(ConfirmDemolitionRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId); if (!TryPlaceable(factory, request.TargetInstanceId, out var placeable)) throw new InvalidOperationException("demolition.not_found");
        placeable.LifeState = PlaceableLifeState.Removing;
        if (placeable is DeviceState device) { device.WorkState = DeviceWorkState.Disabled; device.OperationLock = Lock(DeviceOperationType.Demolition, request, world); }
        var duration = placeable is DeviceState d ? definitions.Devices[d.DefinitionId].DemolitionDurationSeconds : 1;
        var task = CreateTask(factory, request, placeable.InstanceId, TaskType.Remove, duration, placeable.OccupiedCells); factory.Tasks[task.TaskId] = task;
        return Success(request, world, placeable.InstanceId, task.TaskId, 0);
    }

    private OperationResult ConfirmUpgrade(ConfirmUpgradeRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId); if (!factory.Devices.TryGetValue(request.TargetInstanceId, out var device)) throw new InvalidOperationException("upgrade.device_not_found");
        if (!definitions.Upgrades.TryGetValue(request.UpgradeId, out var upgrade) || upgrade.DeviceDefinitionId != device.DefinitionId || upgrade.FromLevel != device.Level) throw new InvalidOperationException("upgrade.invalid");
        Charge(world, request.RequestId, MoneyCategory.UpgradeCost, -upgrade.UpgradeCost, factory.FactoryId, device.InstanceId);
        var task = CreateTask(factory, request, device.InstanceId, TaskType.Upgrade, upgrade.UpgradeDurationSeconds, device.OccupiedCells); factory.Tasks[task.TaskId] = task;
        device.Upgrade = new UpgradeRuntimeState { UpgradeId = upgrade.UpgradeId, OriginalLevel = upgrade.FromLevel, TargetLevel = upgrade.ToLevel, PaidAmount = upgrade.UpgradeCost, RequiredWorkSeconds = upgrade.UpgradeDurationSeconds, TaskId = task.TaskId };
        device.OperationLock = Lock(DeviceOperationType.Upgrade, request, world); device.WorkState = DeviceWorkState.Upgrading;
        return Success(request, world, device.InstanceId, task.TaskId, -upgrade.UpgradeCost);
    }

    private static OperationResult ChangeRecipe(ChangeRecipeRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId); if (!factory.Devices.TryGetValue(request.TargetInstanceId, out var device)) throw new InvalidOperationException("recipe.device_not_found");
        if (!definitions.Recipes.TryGetValue(request.NewRecipeId, out var recipe) || !recipe.CompatibleDeviceDefinitionIds.Contains(device.DefinitionId)) throw new InvalidOperationException("recipe.incompatible");
        if (request.InventoryHandlingMode == InventoryHandlingMode.RequireEmpty && device.Buffers.Values.Any(x => x.Contents.Any(y => y.Amount > 0))) throw new InvalidOperationException("recipe.inventory_not_empty");
        device.SelectedRecipeId = request.NewRecipeId; device.RecipeRuntime = new RecipeRuntimeState();
        return Success(request, world, device.InstanceId, null, 0);
    }

    private OperationResult PurchaseLand(PurchaseLandParcelRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId); if (!factory.Parcels.TryGetValue(request.ParcelId, out var parcel) || parcel.IsPurchased) throw new InvalidOperationException("land.invalid");
        var definition = definitions.Factories[factory.FactoryDefinitionId].Parcels.First(x => x.ParcelDefinitionId == parcel.ParcelDefinitionId);
        if (definition.RequiredUnlockId != null && !world.Story.UnlockedIds.Contains(definition.RequiredUnlockId)) throw new InvalidOperationException("land.locked");
        if (definition.RequiredPreviousParcelId != null && (!factory.Parcels.TryGetValue(definition.RequiredPreviousParcelId, out var previous) || !previous.IsPurchased)) throw new InvalidOperationException("land.previous_required");
        Charge(world, request.RequestId, MoneyCategory.LandPurchase, -definition.PurchasePrice, factory.FactoryId, parcel.ParcelId); parcel.IsPurchased = true; parcel.PurchasedAtSimTime = world.SimTimeSeconds; parcel.ActualPurchasePrice = definition.PurchasePrice;
        foreach (var cell in parcel.Cells) factory.Grid.Cells[cell].IsPurchased = true;
        return Success(request, world, parcel.ParcelId, null, -definition.PurchasePrice);
    }

    private OperationResult PurchaseFactory(PurchaseFactoryRequest request, WorldState world, GameDefinitions definitions)
    {
        if (!definitions.Factories.TryGetValue(request.FactoryDefinitionId, out var definition) || world.Factories.Values.Any(x => x.FactoryDefinitionId == definition.FactoryDefinitionId)) throw new InvalidOperationException("factory.invalid");
        if (definition.RequiredUnlockId != null && !world.Story.UnlockedIds.Contains(definition.RequiredUnlockId)) throw new InvalidOperationException("factory.locked");
        Charge(world, request.RequestId, MoneyCategory.FactoryPurchase, -definition.PurchasePrice, null, definition.FactoryDefinitionId);
        var factory = _initializer.CreateFactory(definition, true); factory.PurchasedAtSimTime = world.SimTimeSeconds; world.Factories[factory.FactoryId] = factory;
        return Success(request, world, factory.FactoryId, null, -definition.PurchasePrice);
    }

    private static OperationResult HireAgent(HireAgentRequest request, WorldState world, GameDefinitions definitions)
    {
        var factory = Factory(world, request.FactoryId); var definition = definitions.Factories[factory.FactoryDefinitionId]; if (factory.Agents.Count >= definition.MaxAgentCount) throw new InvalidOperationException("agent.limit_reached");
        var id = NextId(factory.FactoryId, "agent", factory.Agents.Keys); factory.Agents[id] = new AgentState { AgentId = id, FactoryId = factory.FactoryId, State = AgentWorkState.Idle, CurrentCell = definition.AgentSpawnCells.FirstOrDefault() };
        return Success(request, world, id, null, 0);
    }

    private static OperationResult AcceptOrder(AcceptOrderRequest request, WorldState world)
    { var factory = Factory(world, request.FactoryId); if (!factory.Orders.TryGetValue(request.OrderId, out var order) || order.Status != OrderStatus.Available) throw new InvalidOperationException("order.unavailable"); order.Status = OrderStatus.Active; order.AcceptedAtSimTime = world.SimTimeSeconds; order.AcceptedSequence = world.SimulationStepId; return Success(request, world, order.OrderId, null, 0); }

    private void Charge(WorldState world, string requestId, MoneyCategory category, decimal amount, string? factoryId, string objectId)
    { var payment = _economy.Apply(world, new MoneyOperation($"request:{requestId}", category, amount, SpendingPolicy.RejectIfInsufficient, factoryId, objectId, RelatedRequestId: requestId)); if (!payment.Success) throw new InvalidOperationException(payment.ResultCode); }
    private static FactoryState Factory(WorldState world, string id) => world.Factories.TryGetValue(id, out var value) ? value : throw new InvalidOperationException("factory.not_found");
    private static void ValidatePlacement(WorldState world, FactoryState factory, IEnumerable<GridCell> cells, string? unlock) { if (unlock != null && !world.Story.UnlockedIds.Contains(unlock)) throw new InvalidOperationException("build.locked"); foreach (var cell in cells) if (!factory.Grid.Cells.TryGetValue(cell, out var state) || !state.IsPurchased || !state.IsBuildable || state.OccupantInstanceId != null) throw new InvalidOperationException("build.cell_unavailable"); }
    private static IEnumerable<GridCell> Footprint(GridAnchor anchor, GridSize size) { for (var y = 0; y < size.Height; y++) for (var x = 0; x < size.Width; x++) yield return new GridCell(anchor.Cell.X + x, anchor.Cell.Y + y); }
    private static void Occupy(FactoryState factory, string id, IEnumerable<GridCell> cells) { foreach (var cell in cells) { factory.Grid.Cells[cell].OccupantInstanceId = id; factory.Grid.Cells[cell].IsWalkable = false; } }
    private static void Release(FactoryState factory, PlaceableState value) { foreach (var cell in value.OccupiedCells) if (factory.Grid.Cells.TryGetValue(cell, out var state) && state.OccupantInstanceId == value.InstanceId) { state.OccupantInstanceId = null; state.IsWalkable = state.IsBuildable; } }
    private static void Remove(FactoryState factory, PlaceableState value) { if (value is DeviceState) factory.Devices.Remove(value.InstanceId); else factory.Pipes.Remove(value.InstanceId); }
    private static bool TryPlaceable(FactoryState factory, string id, out PlaceableState value) { if (factory.Devices.TryGetValue(id, out var d)) { value = d; return true; } if (factory.Pipes.TryGetValue(id, out var p)) { value = p; return true; } value = null!; return false; }
    private static void Unassign(FactoryState factory, TaskState task) { if (task.AssignedAgentId != null && factory.Agents.TryGetValue(task.AssignedAgentId, out var agent)) { agent.CurrentTaskId = null; agent.CurrentPath.Clear(); agent.State = AgentWorkState.Idle; } task.AssignedAgentId = null; }
    private static string NextId(string factory, string kind, IEnumerable<string> existing) { var index = 1; string id; var set = existing.ToHashSet(StringComparer.Ordinal); do id = $"{factory}:{kind}:{index++:D6}"; while (set.Contains(id)); return id; }
    private static TaskState CreateTask(FactoryState factory, OperationRequest request, string target, TaskType type, double duration, IEnumerable<GridCell> cells) => new() { TaskId = NextId(factory.FactoryId, "task", factory.Tasks.Keys), FactoryId = factory.FactoryId, TargetInstanceId = target, Type = type, Priority = type == TaskType.RepairPipe ? 100 : 50, CreatedSequence = factory.Tasks.Count == 0 ? 1 : factory.Tasks.Values.Max(x => x.CreatedSequence) + 1, State = TaskLifeState.Queued, RequiredWorkSeconds = Math.Max(0.01, duration), CandidateWorkCells = cells.SelectMany(Neighbours).Where(factory.Grid.Cells.ContainsKey).Distinct().ToList(), SourceRequestId = request.RequestId, ManuallyAssignedAgentId = request is ConfirmBlueprintRequest b ? b.ManuallyAssignedAgentId : null, AllowAutomaticReassignment = true };
    private static IEnumerable<GridCell> Neighbours(GridCell c) { yield return new GridCell(c.X, c.Y + 1); yield return new GridCell(c.X + 1, c.Y); yield return new GridCell(c.X, c.Y - 1); yield return new GridCell(c.X - 1, c.Y); }
    private static DeviceOperationLock Lock(DeviceOperationType type, OperationRequest request, WorldState world) => new() { IsLocked = true, OperationType = type, RequestId = request.RequestId, LockedAtSimTime = world.SimTimeSeconds };
    private static OperationResult Success(OperationRequest request, WorldState world, string affected, string? created, decimal money) { var result = Result(request, true, "ok", world.WorldVersion, world.WorldVersion); result.AffectedObjectIds.Add(affected); if (created != null) result.CreatedObjectIds.Add(created); result.MoneyChange = money; return result; }
    private static OperationResult Result(OperationRequest request, bool success, string code, long previous, long resulting) => new() { RequestId = request.RequestId, Success = success, ResultCode = code, PreviousWorldVersion = previous, ResultingWorldVersion = resulting };
    private static OperationResult Complete(OperationRequest request, WorldState world, bool success, string code, long previous, OperationResult? source = null) { if (success) world.WorldVersion++; var result = Result(request, success, code, previous, world.WorldVersion); if (source != null) { result.AffectedObjectIds.AddRange(source.AffectedObjectIds); result.CreatedObjectIds.AddRange(source.CreatedObjectIds); result.MoneyChange = source.MoneyChange; } world.ProcessedRequests[request.RequestId] = new ProcessedRequestRecord { RequestId = request.RequestId, PlayerId = request.PlayerId, Success = success, ResultCode = code, ResultingWorldVersion = world.WorldVersion }; return result; }
}
}
