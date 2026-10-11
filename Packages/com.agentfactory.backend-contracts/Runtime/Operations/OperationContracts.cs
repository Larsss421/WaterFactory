using AgentFactory.Contracts.Core;

namespace AgentFactory.Contracts.Operations;

public abstract record OperationRequest(string RequestId, string PlayerId, long ExpectedWorldVersion);
public sealed record ConfirmBlueprintRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string DefinitionId, GridAnchor Anchor, string? ManuallyAssignedAgentId = null) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record CancelConstructionRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string TargetInstanceId) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record ConfirmDemolitionRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string TargetInstanceId, InventoryHandlingMode InventoryHandlingMode) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record ConfirmUpgradeRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string TargetInstanceId, string UpgradeId, InventoryHandlingMode InventoryHandlingMode) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record ChangeRecipeRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string TargetInstanceId, string NewRecipeId, InventoryHandlingMode InventoryHandlingMode) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record PurchaseLandParcelRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string ParcelId) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record PurchaseFactoryRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryDefinitionId) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record HireAgentRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);
public sealed record AcceptOrderRequest(string RequestId, string PlayerId, long ExpectedWorldVersion, string FactoryId, string OrderId) : OperationRequest(RequestId, PlayerId, ExpectedWorldVersion);

public sealed class OperationResult
{
    public required string RequestId { get; init; }
    public bool Success { get; init; }
    public required string ResultCode { get; init; }
    public string? MessageKey { get; init; }
    public long PreviousWorldVersion { get; init; }
    public long ResultingWorldVersion { get; init; }
    public List<string> AffectedObjectIds { get; init; } = new();
    public List<string> CreatedObjectIds { get; init; } = new();
    public decimal MoneyChange { get; init; }
    public Dictionary<string, string> Details { get; init; } = new();
}

public sealed record MoneyOperation(string OperationId, MoneyCategory Category, decimal Amount, SpendingPolicy SpendingPolicy, string? FactoryId = null, string? RelatedObjectId = null, string? RelatedOrderId = null, string? RelatedTaskId = null, string? RelatedRequestId = null);
