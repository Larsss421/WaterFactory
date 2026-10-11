#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;

namespace AgentFactory.Contracts.World
{

public sealed class WorldState
{
    public int SaveVersion { get; set; } = 1;
    public int ConfigVersion { get; set; }
    public long WorldVersion { get; set; }
    public long SimulationStepId { get; set; }
    public double SimTimeSeconds { get; set; }
    public FeatureFlags Features { get; set; } = new();
    public HashSet<WorldPauseReason> PauseReasons { get; set; } = new();
    public EconomyState Economy { get; set; } = new();
    public Dictionary<string, FactoryState> Factories { get; set; } = new();
    public StoryState Story { get; set; } = new();
    public List<MoneyTransaction> Ledger { get; set; } = new();
    public List<GameEvent> EventLog { get; set; } = new();
    public Dictionary<string, ProcessedRequestRecord> ProcessedRequests { get; set; } = new();
}

public sealed class FeatureFlags
{
    public bool TemperatureEnabled { get; set; }
    public bool PhaseChangeEnabled { get; set; }
    public bool DayNightEnabled { get; set; }
    public bool AiAssistantEnabled { get; set; }
}

public sealed class FactoryState
{
    public string FactoryId { get; set; } = string.Empty;
    public string FactoryDefinitionId { get; set; } = string.Empty;
    public bool IsPurchased { get; set; }
    public double PurchasedAtSimTime { get; set; }
    public GridRuntimeState Grid { get; set; } = new();
    public Dictionary<string, LandParcelState> Parcels { get; set; } = new();
    public Dictionary<string, DeviceState> Devices { get; set; } = new();
    public Dictionary<string, PipeState> Pipes { get; set; } = new();
    public Dictionary<string, ConnectionState> Connections { get; set; } = new();
    public Dictionary<string, PipeNetworkState> PipeNetworks { get; set; } = new();
    public Dictionary<string, FluidPacketState> FluidPackets { get; set; } = new();
    public Dictionary<string, AgentState> Agents { get; set; } = new();
    public Dictionary<string, TaskState> Tasks { get; set; } = new();
    public Dictionary<string, OrderState> Orders { get; set; } = new();
    public long TopologyVersion { get; set; }
}

public sealed class GridRuntimeState
{
    public GridSize Size { get; set; }
    public Dictionary<GridCell, GridCellState> Cells { get; set; } = new();
}

public sealed class GridCellState
{
    public GridCell Coordinate { get; set; }
    public string? ParcelId { get; set; }
    public bool IsPurchased { get; set; }
    public bool IsBuildable { get; set; }
    public bool IsWalkable { get; set; }
    public bool IsBoundaryExit { get; set; }
    public string? OccupantInstanceId { get; set; }
}

public sealed class LandParcelState
{
    public string ParcelId { get; set; } = string.Empty;
    public string ParcelDefinitionId { get; set; } = string.Empty;
    public string FactoryId { get; set; } = string.Empty;
    public bool IsPurchased { get; set; }
    public double? PurchasedAtSimTime { get; set; }
    public decimal? ActualPurchasePrice { get; set; }
    public List<GridCell> Cells { get; set; } = new();
}

public abstract class PlaceableState
{
    public string InstanceId { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string FactoryId { get; set; } = string.Empty;
    public GridAnchor Anchor { get; set; }
    public List<GridCell> OccupiedCells { get; set; } = new();
    public PlaceableLifeState LifeState { get; set; }
    public string? CreatedByRequestId { get; set; }
    public long CreatedAtWorldVersion { get; set; }
}

public sealed class DeviceState : PlaceableState
{
    public DeviceWorkState WorkState { get; set; }
    public int Level { get; set; } = 1;
    public float Health { get; set; } = 100;
    public float ProcessingSpeedMultiplier { get; set; } = 1;
    public string? SelectedRecipeId { get; set; }
    public string? PrimaryBlockReason { get; set; }
    public List<PortState> Ports { get; set; } = new();
    public Dictionary<string, FluidBufferState> Buffers { get; set; } = new();
    public ConstructionState? Construction { get; set; }
    public UpgradeRuntimeState? Upgrade { get; set; }
    public RecipeRuntimeState RecipeRuntime { get; set; } = new();
    public DeviceOperationLock OperationLock { get; set; } = new();
}

public sealed class PipeState : PlaceableState
{
    public string PipeTypeId { get; set; } = string.Empty;
    public PipeRuntimeState PipeStateValue { get; set; }
    public float Health { get; set; } = 100;
    public float CurrentPressure { get; set; }
    public double CurrentContainedAmount { get; set; }
    public double CurrentInflowRate { get; set; }
    public double CurrentOutflowRate { get; set; }
    public List<string> PacketIds { get; set; } = new();
    public Dictionary<GridDirection, string> ConnectionsByDirection { get; set; } = new();
    public string? NetworkId { get; set; }
    public PipeDamageState Damage { get; set; } = new();
}

public sealed class PortState
{
    public string PortId { get; set; } = string.Empty;
    public string PortDefinitionId { get; set; } = string.Empty;
    public string OwnerInstanceId { get; set; } = string.Empty;
    public GridCell WorldCell { get; set; }
    public GridDirection WorldFacing { get; set; }
    public PortDirection Direction { get; set; }
    public PortMedium Medium { get; set; }
    public double MaxFlowRate { get; set; }
    public string? ConnectionId { get; set; }
    public string? ConnectedObjectId { get; set; }
    public PortRuntimeStatus Status { get; set; }
}

public sealed class FluidBufferState
{
    public string BufferId { get; set; } = string.Empty;
    public string OwnerInstanceId { get; set; } = string.Empty;
    public BufferRole Role { get; set; }
    public double Capacity { get; set; }
    public bool AllowMixedInput { get; set; }
    public List<FluidStack> Contents { get; set; } = new();
}

public sealed class FluidStack
{
    public string FluidId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public float? Temperature { get; set; }
    public Phase? Phase { get; set; }
}

public sealed class FluidPacketState
{
    public string PacketId { get; set; } = string.Empty;
    public string FactoryId { get; set; } = string.Empty;
    public string FluidId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public FluidLocationType LocationType { get; set; }
    public string HolderInstanceId { get; set; } = string.Empty;
    public string? HolderSlotId { get; set; }
    public string? NetworkId { get; set; }
    public double Progress01 { get; set; }
    public string? PreviousConnectionId { get; set; }
    public string? TargetConnectionId { get; set; }
    public float? Temperature { get; set; }
    public Phase? Phase { get; set; }
    public PacketRuntimeState State { get; set; }
    public long CreatedSequence { get; set; }
}

public sealed class ConnectionState
{
    public string ConnectionId { get; set; } = string.Empty;
    public string FactoryId { get; set; } = string.Empty;
    public ConnectionEndpoint EndpointA { get; set; } = new();
    public ConnectionEndpoint EndpointB { get; set; } = new();
    public ConnectionStatus Status { get; set; }
    public double MaxFlowRate { get; set; }
}

public sealed class ConnectionEndpoint
{
    public string OwnerInstanceId { get; set; } = string.Empty;
    public ConnectionEndpointType Type { get; set; }
    public string? PortId { get; set; }
    public string? ChannelId { get; set; }
    public GridCell Cell { get; set; }
    public GridDirection Facing { get; set; }
}

public sealed class PipeNetworkState
{
    public string NetworkId { get; set; } = string.Empty;
    public string FactoryId { get; set; } = string.Empty;
    public HashSet<string> DevicePortIds { get; set; } = new();
    public HashSet<string> PipeIds { get; set; } = new();
    public HashSet<string> ConnectionIds { get; set; } = new();
    public NetworkRuntimeStatus Status { get; set; }
    public double TotalContainedAmount { get; set; }
    public long TopologyVersion { get; set; }
}
}
