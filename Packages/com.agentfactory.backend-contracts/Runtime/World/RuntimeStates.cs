using AgentFactory.Contracts.Core;

namespace AgentFactory.Contracts.World;

public sealed class ConstructionState
{
    public decimal BuildCost { get; set; }
    public decimal PaidAmount { get; set; }
    public double RequiredWorkSeconds { get; set; }
    public double CompletedWorkSeconds { get; set; }
    public float Progress01 { get; set; }
    public bool HasStarted { get; set; }
}

public sealed class UpgradeRuntimeState
{
    public required string UpgradeId { get; set; }
    public int OriginalLevel { get; set; }
    public int TargetLevel { get; set; }
    public decimal PaidAmount { get; set; }
    public double RequiredWorkSeconds { get; set; }
    public double CompletedWorkSeconds { get; set; }
    public float Progress01 { get; set; }
    public bool HasStarted { get; set; }
    public string? TaskId { get; set; }
}

public sealed class RecipeRuntimeState
{
    public double CycleProgressSeconds { get; set; }
    public bool InputsReserved { get; set; }
    public List<FluidStack> ReservedInputs { get; set; } = new();
    public string? BlockReason { get; set; }
    public string? PendingRecipeId { get; set; }
}

public sealed class DeviceOperationLock
{
    public bool IsLocked { get; set; }
    public DeviceOperationType OperationType { get; set; }
    public string? TaskId { get; set; }
    public string? RequestId { get; set; }
    public double LockedAtSimTime { get; set; }
}

public sealed class PipeDamageState
{
    public bool OverSafePressure { get; set; }
    public bool OverCriticalPressure { get; set; }
    public double ContinuousOverpressureTime { get; set; }
    public float LastDamageAmount { get; set; }
    public string? LastDamageReasonCode { get; set; }
    public double? DamagedAtSimTime { get; set; }
    public double? RupturedAtSimTime { get; set; }
    public string? ActiveRepairTaskId { get; set; }
}

public sealed class TaskState
{
    public required string TaskId { get; set; }
    public TaskType Type { get; set; }
    public required string FactoryId { get; set; }
    public required string TargetInstanceId { get; set; }
    public int Priority { get; set; }
    public long CreatedSequence { get; set; }
    public string? ManuallyAssignedAgentId { get; set; }
    public bool AllowAutomaticReassignment { get; set; }
    public TaskLifeState State { get; set; }
    public string? AssignedAgentId { get; set; }
    public float Progress01 { get; set; }
    public double CompletedWorkSeconds { get; set; }
    public double RequiredWorkSeconds { get; set; }
    public List<GridCell> CandidateWorkCells { get; set; } = new();
    public GridCell? SelectedWorkCell { get; set; }
    public decimal PaidAmount { get; set; }
    public bool PaymentCompleted { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? SourceRequestId { get; set; }
    public string? FailureReasonCode { get; set; }
    public string? BlockedCellKey { get; set; }
    public int RetryCount { get; set; }
}

public sealed class AgentState
{
    public required string AgentId { get; set; }
    public required string FactoryId { get; set; }
    public AgentWorkState State { get; set; }
    public GridCell CurrentCell { get; set; }
    public string? CurrentTaskId { get; set; }
    public List<GridCell> CurrentPath { get; set; } = new();
}

public sealed class OrderState
{
    public required string OrderId { get; set; }
    public string? OrderDefinitionId { get; set; }
    public required string FactoryId { get; set; }
    public OrderSource Source { get; set; }
    public OrderStatus Status { get; set; }
    public required string TargetFluidId { get; set; }
    public double RequiredAmount { get; set; }
    public double DeliveredAmount { get; set; }
    public decimal PricePerUnit { get; set; }
    public double AcceptedAtSimTime { get; set; }
    public double DeadlineSimTime { get; set; }
    public long AcceptedSequence { get; set; }
}

public sealed class EconomyState
{
    public decimal Balance { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal LifetimeIncome { get; set; }
    public decimal LifetimeExpense { get; set; }
    public BankruptcyState Bankruptcy { get; set; } = new();
    public long NextTransactionSequence { get; set; }
}

public sealed class BankruptcyState
{
    public bool IsBankruptcyPaused { get; set; }
    public double? TriggeredAtSimTime { get; set; }
    public decimal BalanceWhenTriggered { get; set; }
    public string? TriggerReasonCode { get; set; }
    public string? RelatedObjectId { get; set; }
}

public sealed class MoneyTransaction
{
    public required string TransactionId { get; set; }
    public long Sequence { get; set; }
    public double SimTime { get; set; }
    public MoneyCategory Category { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? FactoryId { get; set; }
    public string? RelatedObjectId { get; set; }
    public string? RelatedOrderId { get; set; }
    public string? RelatedTaskId { get; set; }
    public string? RelatedRequestId { get; set; }
    public string? NoteKey { get; set; }
}

public sealed class StoryState
{
    public string StoryDefinitionId { get; set; } = "main";
    public string? CurrentChapterId { get; set; }
    public Dictionary<string, StoryChapterState> Chapters { get; set; } = new();
    public Dictionary<string, StoryContractState> Contracts { get; set; } = new();
    public HashSet<string> UnlockedIds { get; set; } = new();
    public bool MainStoryCompleted { get; set; }
    public bool EndlessModeUnlocked { get; set; }
}

public sealed class StoryChapterState
{
    public required string ChapterId { get; set; }
    public StoryProgressStatus Status { get; set; }
    public double? StartedAtSimTime { get; set; }
    public double? CompletedAtSimTime { get; set; }
}

public sealed class StoryContractState
{
    public required string ContractId { get; set; }
    public required string ContractDefinitionId { get; set; }
    public required string ChapterId { get; set; }
    public string? FactoryId { get; set; }
    public StoryProgressStatus Status { get; set; }
    public Dictionary<string, StoryObjectiveState> Objectives { get; set; } = new();
    public string? LinkedOrderId { get; set; }
    public int AttemptNumber { get; set; }
    public bool CompletionRewardsGranted { get; set; }
}

public sealed class StoryObjectiveState
{
    public required string ObjectiveId { get; set; }
    public bool IsCompleted { get; set; }
    public double CurrentAmount { get; set; }
    public int CurrentCount { get; set; }
    public HashSet<string> ContributingObjectIds { get; set; } = new();
}

public sealed class GameEvent
{
    public required string EventId { get; set; }
    public long Sequence { get; set; }
    public required string EventType { get; set; }
    public double SimTime { get; set; }
    public string? FactoryId { get; set; }
    public string? RelatedObjectId { get; set; }
    public string? RelatedTaskId { get; set; }
    public string? RelatedOrderId { get; set; }
    public string? RelatedRequestId { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}

public sealed class ProcessedRequestRecord
{
    public required string RequestId { get; set; }
    public required string PlayerId { get; set; }
    public bool Success { get; set; }
    public required string ResultCode { get; set; }
    public long ResultingWorldVersion { get; set; }
}
