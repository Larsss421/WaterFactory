#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace AgentFactory.Contracts.Core
{

public readonly struct GridCell : IEquatable<GridCell>
{
    public GridCell(int x, int y) { X = x; Y = y; }
    public int X { get; }
    public int Y { get; }
    public bool Equals(GridCell other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is GridCell other && Equals(other);
    public override int GetHashCode() => unchecked((X * 397) ^ Y);
    public override string ToString() => $"({X},{Y})";
    public static bool operator ==(GridCell left, GridCell right) => left.Equals(right);
    public static bool operator !=(GridCell left, GridCell right) => !left.Equals(right);
}

public readonly struct GridSize : IEquatable<GridSize>
{
    public GridSize(int width, int height) { Width = width; Height = height; }
    public int Width { get; }
    public int Height { get; }
    public bool Equals(GridSize other) => Width == other.Width && Height == other.Height;
    public override bool Equals(object obj) => obj is GridSize other && Equals(other);
    public override int GetHashCode() => unchecked((Width * 397) ^ Height);
}

public readonly struct GridAnchor : IEquatable<GridAnchor>
{
    public GridAnchor(GridCell cell, GridDirection rotation) { Cell = cell; Rotation = rotation; }
    public GridCell Cell { get; }
    public GridDirection Rotation { get; }
    public bool Equals(GridAnchor other) => Cell.Equals(other.Cell) && Rotation == other.Rotation;
    public override bool Equals(object obj) => obj is GridAnchor other && Equals(other);
    public override int GetHashCode() => unchecked((Cell.GetHashCode() * 397) ^ (int)Rotation);
}

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
}
