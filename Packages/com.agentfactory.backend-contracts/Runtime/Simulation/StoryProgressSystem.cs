#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public sealed class StoryProgressSystem : IStoryProgressSystem
{
    private readonly HashSet<string> _processedEvents = new(StringComparer.Ordinal);
    public void ProcessEvent(WorldState world, GameEvent gameEvent, GameDefinitions definitions)
    {
        if (!_processedEvents.Add(gameEvent.EventId)) return;
        foreach (var contract in world.Story.Contracts.Values.Where(x => x.Status is StoryProgressStatus.Active or StoryProgressStatus.Available))
        {
            var definition = definitions.Story.Chapters.SelectMany(x => x.Contracts).FirstOrDefault(x => x.ContractDefinitionId == contract.ContractDefinitionId);
            if (definition == null) continue;
            foreach (var objectiveDefinition in definition.Objectives)
            {
                var objective = contract.Objectives[objectiveDefinition.ObjectiveId];
                if (!Matches(objectiveDefinition, gameEvent)) continue;
                if (gameEvent.RelatedObjectId != null && !objective.ContributingObjectIds.Add(gameEvent.RelatedObjectId)) continue;
                objective.CurrentCount++;
                if (gameEvent.Data.TryGetValue("amount", out var amount) && double.TryParse(amount, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)) objective.CurrentAmount += parsed;
                objective.IsCompleted = objective.CurrentCount >= Math.Max(1, objectiveDefinition.RequiredCount) && objective.CurrentAmount + 1e-9 >= objectiveDefinition.RequiredAmount;
            }
            if (!contract.Objectives.Values.All(x => x.IsCompleted)) continue;
            contract.Status = StoryProgressStatus.Completed;
            if (!contract.CompletionRewardsGranted) { foreach (var unlock in definition.UnlockIdsOnCompletion) world.Story.UnlockedIds.Add(unlock); contract.CompletionRewardsGranted = true; }
        }
    }
    private static bool Matches(StoryObjectiveDefinition objective, GameEvent gameEvent) => objective.Type switch
    { StoryObjectiveType.BuildDevice => gameEvent.EventType == "task.Build.completed", StoryObjectiveType.BuildPipe => gameEvent.EventType == "task.Build.completed", StoryObjectiveType.RepairPipe => gameEvent.EventType == "task.RepairPipe.completed", StoryObjectiveType.UpgradeDevice => gameEvent.EventType == "task.Upgrade.completed", StoryObjectiveType.DeliverOrder => gameEvent.EventType == "order.delivered", StoryObjectiveType.ProduceFluid => gameEvent.EventType == "fluid.produced", _ => false };
}
}
