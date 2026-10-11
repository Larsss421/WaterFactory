#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;

namespace AgentFactory.Contracts.Operations
{

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
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string? MessageKey { get; set; }
    public long PreviousWorldVersion { get; set; }
    public long ResultingWorldVersion { get; set; }
    public List<string> AffectedObjectIds { get; set; } = new();
    public List<string> CreatedObjectIds { get; set; } = new();
    public decimal MoneyChange { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
}

public sealed record MoneyOperation(string OperationId, MoneyCategory Category, decimal Amount, SpendingPolicy SpendingPolicy, string? FactoryId = null, string? RelatedObjectId = null, string? RelatedOrderId = null, string? RelatedTaskId = null, string? RelatedRequestId = null);
}
