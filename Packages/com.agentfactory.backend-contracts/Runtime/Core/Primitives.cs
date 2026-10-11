namespace AgentFactory.Contracts.Core;

public readonly record struct GridCell(int X, int Y);
public readonly record struct GridSize(int Width, int Height);
public readonly record struct GridAnchor(GridCell Cell, GridDirection Rotation);

public enum GridDirection { North, East, South, West }
public enum Phase { Solid, Liquid, Gas }
public enum PlaceableLifeState { Blueprint, UnderConstruction, Active, Disabled, Removing, Removed }
public enum PortDirection { Input, Output, Bidirectional }
public enum PortMedium { Fluid }
public enum PortRuntimeStatus { Disconnected, Connected, Blocked, Disabled }
public enum ConnectionStatus { Active, Disabled, Blocked, Broken }
public enum ConnectionEndpointType { DevicePort, PipeSide, PipeChannel }
public enum NetworkRuntimeStatus { Empty, Ready, Flowing, Mixed, Blocked, Ruptured, Invalid }
public enum PipeRuntimeState { UnderConstruction, Active, Damaged, Ruptured, UnderRepair, Blocked, Removing, Removed }
public enum DeviceWorkState { Idle, Running, WaitingForInput, OutputFull, Draining, Faulted, UnderMaintenance, Upgrading, Disabled }
public enum TaskType { Build, RepairDevice, RepairPipe, Upgrade, Remove, AdjustValve, SetTemperature, ClearBlockage }
public enum TaskLifeState { Created, Queued, Claimed, Moving, Executing, WaitingForFunds, Blocked, Completed, Failed, Cancelled }
public enum AgentWorkState { Idle, PreparingTask, Moving, Working, Returning }
public enum BufferRole { Input, Output, Storage, Import, Export }
public enum FluidLocationType { DeviceBuffer, Pipe, Port, Leaked, Consumed, Sold }
public enum PacketRuntimeState { Waiting, Moving, Blocked, Arrived, Removed }
public enum OrderSource { Normal, Story }
public enum OrderStatus { Available, Active, Completed, Expired, Abandoned }
public enum MoneyCategory { BuildCost, UpgradeCost, LandPurchase, FactoryPurchase, AgentHire, RawMaterialPurchase, Electricity, Repair, FixedPriceSale, OrderDelivery, ConstructionCancelRefund, UpgradeCancelRefund, DemolitionRefund }
public enum SpendingPolicy { RejectIfInsufficient, AllowNegativeBalance }
public enum InventoryHandlingMode { RequireEmpty, DrainFirst, Discard, Preserve }
public enum DeviceOperationType { None, Repair, Upgrade, Demolition, RecipeChange, PortDirectionChange }
public enum StoryProgressStatus { Locked, Available, Active, Completed, Expired, Abandoned }
public enum StoryObjectiveType { PurchaseLand, PurchaseFactory, HireAgent, BuildDevice, BuildPipe, CreateValidConnection, CreateProductionLine, SelectRecipe, EnableImport, ProduceFluid, DeliverOrder, SellFluid, RepairDevice, RepairPipe, UpgradeDevice, ReachBalance, MaintainProduction }
public enum WorldPauseReason { PlayerPause, SharedMultiplayerPause, BankruptcyPause, Loading }
