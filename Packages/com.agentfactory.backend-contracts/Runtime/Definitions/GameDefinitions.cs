#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;

namespace AgentFactory.Contracts.Definitions
{

public sealed class GameDefinitions
{
    public int ConfigVersion { get; set; }
    public Dictionary<string, FluidDefinition> Fluids { get; set; } = new();
    public Dictionary<string, RecipeDefinition> Recipes { get; set; } = new();
    public Dictionary<string, DeviceDefinition> Devices { get; set; } = new();
    public Dictionary<string, PipeDefinition> Pipes { get; set; } = new();
    public Dictionary<string, UpgradeDefinition> Upgrades { get; set; } = new();
    public Dictionary<string, FactoryDefinition> Factories { get; set; } = new();
    public StoryDefinition Story { get; set; } = new();
}

public sealed class FluidDefinition
{
    public string FluidId { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public decimal PurchasePricePerUnit { get; set; }
    public decimal FixedSalePricePerUnit { get; set; }
    public bool CanPurchase { get; set; }
    public bool CanSellAtFixedPrice { get; set; }
    public float? DefaultTemperature { get; set; }
    public Phase? DefaultPhase { get; set; }
}

public sealed class RecipeDefinition
{
    public string RecipeId { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public List<RecipeItemDefinition> Inputs { get; set; } = new();
    public List<RecipeItemDefinition> Outputs { get; set; } = new();
    public double CycleDurationSeconds { get; set; }
    public decimal EnergyCostPerCycle { get; set; }
    public HashSet<string> CompatibleDeviceDefinitionIds { get; set; } = new();
}

public sealed record RecipeItemDefinition(string FluidId, double Amount);

public sealed class DeviceDefinition
{
    public string DefinitionId { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public decimal BuildCost { get; set; }
    public double BuildDurationSeconds { get; set; }
    public double DemolitionDurationSeconds { get; set; }
    public GridSize Footprint { get; set; }
    public int MaxPerFactory { get; set; }
    public string? RequiredUnlockId { get; set; }
    public bool RequiresBoundaryPlacement { get; set; }
    public List<PortDefinition> Ports { get; set; } = new();
    public List<BufferDefinition> Buffers { get; set; } = new();
    public HashSet<string> SupportedRecipeIds { get; set; } = new();
}

public sealed record PortDefinition(string PortDefinitionId, GridCell LocalCell, GridDirection LocalFacing, PortDirection Direction, PortMedium Medium, double MaxFlowRate);
public sealed record BufferDefinition(string BufferId, BufferRole Role, double Capacity, bool AllowMixedInput);

public sealed class PipeDefinition
{
    public string PipeTypeId { get; set; } = string.Empty;
    public decimal BuildCost { get; set; }
    public double Capacity { get; set; }
    public double MaxFlowRate { get; set; }
    public float SafePressure { get; set; }
    public float CriticalPressure { get; set; }
    public float MaxHealth { get; set; } = 100;
    public float PreventiveRepairThreshold { get; set; }
    public float BaseDamageRate { get; set; }
    public float RuptureDamageMultiplier { get; set; }
    public decimal RepairBaseCost { get; set; }
    public double RepairDurationSeconds { get; set; }
    public double BasePacketSpeed { get; set; }
}

public sealed record UpgradeDefinition(string UpgradeId, string DeviceDefinitionId, int FromLevel, int ToLevel, decimal UpgradeCost, double UpgradeDurationSeconds, float ProcessingSpeedMultiplier);

public sealed class FactoryDefinition
{
    public string FactoryDefinitionId { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public GridSize GridSize { get; set; }
    public List<LandParcelDefinition> Parcels { get; set; } = new();
    public HashSet<GridCell> ValidBoundaryExitCells { get; set; } = new();
    public HashSet<GridCell> FixedObstacleCells { get; set; } = new();
    public List<GridCell> AgentSpawnCells { get; set; } = new();
    public HashSet<string> LocallyAvailableFluidIds { get; set; } = new();
    public int MaxAgentCount { get; set; }
    public string? RequiredUnlockId { get; set; }
}

public sealed class LandParcelDefinition
{
    public string ParcelDefinitionId { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public List<GridCell> Cells { get; set; } = new();
    public string? RequiredUnlockId { get; set; }
    public string? RequiredPreviousParcelId { get; set; }
    public bool InitiallyPurchased { get; set; }
}

public sealed class StoryDefinition
{
    public string StoryDefinitionId { get; set; } = "main";
    public List<StoryChapterDefinition> Chapters { get; set; } = new();
}

public sealed class StoryChapterDefinition
{
    public string ChapterId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public List<StoryContractDefinition> Contracts { get; set; } = new();
    public List<string> UnlockIdsOnCompletion { get; set; } = new();
}

public sealed class StoryContractDefinition
{
    public string ContractDefinitionId { get; set; } = string.Empty;
    public string ChapterId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string? FactoryDefinitionId { get; set; }
    public List<StoryObjectiveDefinition> Objectives { get; set; } = new();
    public List<string> UnlockIdsOnCompletion { get; set; } = new();
    public bool CanRetryAfterExpiration { get; set; }
}

public sealed class StoryObjectiveDefinition
{
    public string ObjectiveId { get; set; } = string.Empty;
    public StoryObjectiveType Type { get; set; }
    public string? TargetDefinitionId { get; set; }
    public string? TargetFluidId { get; set; }
    public string? TargetRecipeId { get; set; }
    public double RequiredAmount { get; set; }
    public int RequiredCount { get; set; }
    public bool MustRemainTrue { get; set; }
}
}
