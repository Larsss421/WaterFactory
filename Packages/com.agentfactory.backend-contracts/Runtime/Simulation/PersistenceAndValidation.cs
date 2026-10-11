#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AgentFactory.Contracts.Simulation
{

public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    JObject Migrate(JObject source);
}

public sealed class WorldSerializer
{
    public const int CurrentSaveVersion = 1;
    private readonly IReadOnlyDictionary<int, ISaveMigration> _migrations;
    private readonly JsonSerializerSettings _settings;

    public WorldSerializer(IEnumerable<ISaveMigration>? migrations = null)
    {
        _migrations = (migrations ?? Array.Empty<ISaveMigration>()).ToDictionary(x => x.FromVersion);
        _settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Culture = CultureInfo.InvariantCulture,
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };
        _settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
    }

    public string Serialize(WorldState world) => JsonConvert.SerializeObject(world, _settings);

    public WorldState Deserialize(string json)
    {
        var token = JObject.Parse(json);
        var version = token.Value<int?>("saveVersion") ?? token.Value<int?>("SaveVersion") ?? 0;
        if (version > CurrentSaveVersion) throw new InvalidOperationException($"save.unsupported_future_version:{version}");
        while (version < CurrentSaveVersion)
        {
            if (!_migrations.TryGetValue(version, out var migration) || migration.ToVersion <= version)
                throw new InvalidOperationException($"save.migration_missing:{version}");
            token = migration.Migrate(token); version = migration.ToVersion; token["saveVersion"] = version;
        }
        var world = token.ToObject<WorldState>(JsonSerializer.Create(_settings)) ?? throw new InvalidOperationException("save.deserialize_null");
        world.SaveVersion = CurrentSaveVersion;
        Normalize(world);
        return world;
    }

    private static void Normalize(WorldState world)
    {
        world.Features ??= new FeatureFlags(); world.PauseReasons ??= new(); world.Economy ??= new EconomyState();
        world.Factories ??= new(); world.Story ??= new StoryState(); world.Ledger ??= new(); world.EventLog ??= new(); world.ProcessedRequests ??= new();
        foreach (var factory in world.Factories.Values)
        { factory.Grid ??= new GridRuntimeState(); factory.Devices ??= new(); factory.Pipes ??= new(); factory.Tasks ??= new(); factory.Agents ??= new(); factory.Orders ??= new(); factory.Connections ??= new(); factory.PipeNetworks ??= new(); factory.FluidPackets ??= new(); factory.Parcels ??= new(); }
    }
}

public sealed class WorldInvariantValidator : IWorldInvariantValidator
{
    private const double Epsilon = 1e-6;
    public ValidationReport Validate(WorldState world, GameDefinitions definitions)
    {
        var report = new ValidationReport();
        var ledgerBalance = world.Economy.InitialBalance + world.Ledger.Sum(x => x.Amount);
        if (ledgerBalance != world.Economy.Balance) report.Errors.Add($"economy.balance_mismatch:{ledgerBalance}:{world.Economy.Balance}");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var factory in world.Factories.Values.OrderBy(x => x.FactoryId, StringComparer.Ordinal))
        {
            Unique(ids, factory.FactoryId, report);
            foreach (var value in factory.Devices.Values) { Unique(ids, value.InstanceId, report); ValidatePlaceable(factory, value, report); ValidateBuffers(value, report); }
            foreach (var value in factory.Pipes.Values) { Unique(ids, value.InstanceId, report); ValidatePlaceable(factory, value, report); if (value.CurrentContainedAmount < -Epsilon || definitions.Pipes.TryGetValue(value.PipeTypeId, out var definition) && value.CurrentContainedAmount > definition.Capacity + Epsilon) report.Errors.Add($"pipe.capacity:{value.InstanceId}"); }
            foreach (var task in factory.Tasks.Values)
            { Unique(ids, task.TaskId, report); if (task.FactoryId != factory.FactoryId) report.Errors.Add($"task.wrong_factory:{task.TaskId}"); if (task.AssignedAgentId != null && (!factory.Agents.TryGetValue(task.AssignedAgentId, out var agent) || agent.CurrentTaskId != task.TaskId)) report.Errors.Add($"task.agent_link:{task.TaskId}"); }
            foreach (var agent in factory.Agents.Values)
            { Unique(ids, agent.AgentId, report); if (agent.FactoryId != factory.FactoryId) report.Errors.Add($"agent.wrong_factory:{agent.AgentId}"); if (agent.CurrentTaskId != null && (!factory.Tasks.TryGetValue(agent.CurrentTaskId, out var task) || task.AssignedAgentId != agent.AgentId)) report.Errors.Add($"agent.task_link:{agent.AgentId}"); }
            foreach (var order in factory.Orders.Values) if (order.DeliveredAmount < -Epsilon || order.DeliveredAmount > order.RequiredAmount + Epsilon) report.Errors.Add($"order.delivery_range:{order.OrderId}");
            foreach (var cell in factory.Grid.Cells.Values.Where(x => x.OccupantInstanceId != null)) if (!factory.Devices.ContainsKey(cell.OccupantInstanceId!) && !factory.Pipes.ContainsKey(cell.OccupantInstanceId!)) report.Errors.Add($"grid.missing_occupant:{cell.Coordinate}");
        }
        return report;
    }
    private static void Unique(HashSet<string> ids, string id, ValidationReport report) { if (!ids.Add(id)) report.Errors.Add($"id.duplicate:{id}"); }
    private static void ValidatePlaceable(FactoryState factory, PlaceableState value, ValidationReport report) { if (value.FactoryId != factory.FactoryId) report.Errors.Add($"placeable.wrong_factory:{value.InstanceId}"); foreach (var cell in value.OccupiedCells) if (!factory.Grid.Cells.TryGetValue(cell, out var state) || state.OccupantInstanceId != value.InstanceId) report.Errors.Add($"placeable.grid_link:{value.InstanceId}:{cell}"); }
    private static void ValidateBuffers(DeviceState device, ValidationReport report) { foreach (var buffer in device.Buffers.Values) { var total = buffer.Contents.Sum(x => x.Amount); if (total < -Epsilon || total > buffer.Capacity + Epsilon) report.Errors.Add($"buffer.capacity:{buffer.BufferId}"); if (buffer.Contents.Any(x => x.Amount < -Epsilon)) report.Errors.Add($"buffer.negative:{buffer.BufferId}"); } }
}
}
