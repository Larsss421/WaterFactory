using AgentFactory.Contracts.Core;

namespace AgentFactory.Contracts.Definitions;

public sealed class GameDefinitions
{
    public int ConfigVersion { get; init; }
    public Dictionary<string, FluidDefinition> Fluids { get; init; } = new();
    public Dictionary<string, RecipeDefinition> Recipes { get; init; } = new();
    public Dictionary<string, DeviceDefinition> Devices { get; init; } = new();
    public Dictionary<string, PipeDefinition> Pipes { get; init; } = new();
    public Dictionary<string, UpgradeDefinition> Upgrades { get; init; } = new();
    public Dictionary<string, FactoryDefinition> Factories { get; init; } = new();
    public StoryDefinition Story { get; init; } = new();
}

public sealed class FluidDefinition
{
    public required string FluidId { get; init; }
    public required string DisplayNameKey { get; init; }
    public decimal PurchasePricePerUnit { get; init; }
    public decimal FixedSalePricePerUnit { get; init; }
    public bool CanPurchase { get; init; }
    public bool CanSellAtFixedPrice { get; init; }
    public float? DefaultTemperature { get; init; }
    public Phase? DefaultPhase { get; init; }
}

public sealed class RecipeDefinition
{
    public required string RecipeId { get; init; }
    public required string DisplayNameKey { get; init; }
    public List<RecipeItemDefinition> Inputs { get; init; } = new();
    public List<RecipeItemDefinition> Outputs { get; init; } = new();
    public double CycleDurationSeconds { get; init; }
    public decimal EnergyCostPerCycle { get; init; }
    public HashSet<string> CompatibleDeviceDefinitionIds { get; init; } = new();
}

public sealed record RecipeItemDefinition(string FluidId, double Amount);

public sealed class DeviceDefinition
{
    public required string DefinitionId { get; init; }
    public required string DisplayNameKey { get; init; }
    public decimal BuildCost { get; init; }
    public double BuildDurationSeconds { get; init; }
    public double DemolitionDurationSeconds { get; init; }
    public GridSize Footprint { get; init; }
    public int MaxPerFactory { get; init; }
    public string? RequiredUnlockId { get; init; }
    public bool RequiresBoundaryPlacement { get; init; }
    public List<PortDefinition> Ports { get; init; } = new();
    public List<BufferDefinition> Buffers { get; init; } = new();
    public HashSet<string> SupportedRecipeIds { get; init; } = new();
}

public sealed record PortDefinition(string PortDefinitionId, GridCell LocalCell, GridDirection LocalFacing, PortDirection Direction, PortMedium Medium, double MaxFlowRate);
public sealed record BufferDefinition(string BufferId, BufferRole Role, double Capacity, bool AllowMixedInput);

public sealed class PipeDefinition
{
    public required string PipeTypeId { get; init; }
    public decimal BuildCost { get; init; }
    public double Capacity { get; init; }
    public double MaxFlowRate { get; init; }
    public float SafePressure { get; init; }
    public float CriticalPressure { get; init; }
    public float MaxHealth { get; init; } = 100;
    public float PreventiveRepairThreshold { get; init; }
    public float BaseDamageRate { get; init; }
    public float RuptureDamageMultiplier { get; init; }
    public decimal RepairBaseCost { get; init; }
    public double RepairDurationSeconds { get; init; }
    public double BasePacketSpeed { get; init; }
}

public sealed record UpgradeDefinition(string UpgradeId, string DeviceDefinitionId, int FromLevel, int ToLevel, decimal UpgradeCost, double UpgradeDurationSeconds, float ProcessingSpeedMultiplier);

public sealed class FactoryDefinition
{
    public required string FactoryDefinitionId { get; init; }
    public required string DisplayNameKey { get; init; }
    public decimal PurchasePrice { get; init; }
    public GridSize GridSize { get; init; }
    public List<LandParcelDefinition> Parcels { get; init; } = new();
    public HashSet<GridCell> ValidBoundaryExitCells { get; init; } = new();
    public HashSet<GridCell> FixedObstacleCells { get; init; } = new();
    public List<GridCell> AgentSpawnCells { get; init; } = new();
    public HashSet<string> LocallyAvailableFluidIds { get; init; } = new();
    public int MaxAgentCount { get; init; }
    public string? RequiredUnlockId { get; init; }
}

public sealed class LandParcelDefinition
{
    public required string ParcelDefinitionId { get; init; }
    public decimal PurchasePrice { get; init; }
    public List<GridCell> Cells { get; init; } = new();
    public string? RequiredUnlockId { get; init; }
    public string? RequiredPreviousParcelId { get; init; }
    public bool InitiallyPurchased { get; init; }
}

public sealed class StoryDefinition
{
    public string StoryDefinitionId { get; init; } = "main";
    public List<StoryChapterDefinition> Chapters { get; init; } = new();
}

public sealed class StoryChapterDefinition
{
    public required string ChapterId { get; init; }
    public int Sequence { get; init; }
    public List<StoryContractDefinition> Contracts { get; init; } = new();
    public List<string> UnlockIdsOnCompletion { get; init; } = new();
}

public sealed class StoryContractDefinition
{
    public required string ContractDefinitionId { get; init; }
    public required string ChapterId { get; init; }
    public int Sequence { get; init; }
    public string? FactoryDefinitionId { get; init; }
    public List<StoryObjectiveDefinition> Objectives { get; init; } = new();
    public List<string> UnlockIdsOnCompletion { get; init; } = new();
    public bool CanRetryAfterExpiration { get; init; }
}

public sealed class StoryObjectiveDefinition
{
    public required string ObjectiveId { get; init; }
    public StoryObjectiveType Type { get; init; }
    public string? TargetDefinitionId { get; init; }
    public string? TargetFluidId { get; init; }
    public string? TargetRecipeId { get; init; }
    public double RequiredAmount { get; init; }
    public int RequiredCount { get; init; }
    public bool MustRemainTrue { get; init; }
}
