#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Operations;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public sealed class EconomySystem : IEconomySystem
{
    public MoneyOperationResult Apply(WorldState world, MoneyOperation operation)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (world.Ledger.Any(x => x.TransactionId == operation.OperationId))
        {
            var previous = world.Ledger.First(x => x.TransactionId == operation.OperationId);
            return new MoneyOperationResult(true, "economy.already_applied", previous.TransactionId,
                operation.Amount, 0m, world.Economy.Balance, world.Economy.Balance, 0m);
        }

        var before = world.Economy.Balance;
        var after = before + operation.Amount;
        if (after < 0m && operation.SpendingPolicy == SpendingPolicy.RejectIfInsufficient)
            return new MoneyOperationResult(false, "economy.insufficient_funds", null, operation.Amount,
                0m, before, before, -after);

        var sequence = Math.Max(1, world.Economy.NextTransactionSequence++);
        var transaction = new MoneyTransaction
        {
            TransactionId = operation.OperationId,
            Sequence = sequence,
            SimTime = world.SimTimeSeconds,
            Category = operation.Category,
            Amount = operation.Amount,
            BalanceBefore = before,
            BalanceAfter = after,
            FactoryId = operation.FactoryId,
            RelatedObjectId = operation.RelatedObjectId,
            RelatedOrderId = operation.RelatedOrderId,
            RelatedTaskId = operation.RelatedTaskId,
            RelatedRequestId = operation.RelatedRequestId
        };
        world.Economy.Balance = after;
        if (operation.Amount >= 0m) world.Economy.LifetimeIncome += operation.Amount;
        else world.Economy.LifetimeExpense += -operation.Amount;
        world.Ledger.Add(transaction);
        return new MoneyOperationResult(true, "ok", transaction.TransactionId, operation.Amount,
            operation.Amount, before, after, 0m);
    }

    public void EvaluateBankruptcy(WorldState world, string reasonCode = "economy.negative_balance")
    {
        if (world.Economy.Balance >= 0m) return;
        world.Economy.Bankruptcy.IsBankruptcyPaused = true;
        world.Economy.Bankruptcy.TriggeredAtSimTime ??= world.SimTimeSeconds;
        world.Economy.Bankruptcy.BalanceWhenTriggered = world.Economy.Balance;
        world.Economy.Bankruptcy.TriggerReasonCode = reasonCode;
        world.PauseReasons.Add(WorldPauseReason.BankruptcyPause);
    }
}
}
