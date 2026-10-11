#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public sealed class GridPathfinder : IPathfinder
{
    private static readonly GridCell[] Directions =
    {
        new(0, 1), new(1, 0), new(0, -1), new(-1, 0)
    };

    public PathResult FindPath(FactoryState factory, GridCell start, IReadOnlyList<GridCell> goals)
    {
        if (goals.Count == 0) return new PathResult(false, Array.Empty<GridCell>(), null, 0, "path.no_goal");
        var validGoals = goals.Where(x => IsWalkable(factory, x)).Distinct().OrderBy(x => x.X).ThenBy(x => x.Y).ToArray();
        if (validGoals.Length == 0) return new PathResult(false, Array.Empty<GridCell>(), null, 0, "path.no_walkable_goal");

        var open = new List<(GridCell cell, int score)>();
        var cameFrom = new Dictionary<GridCell, GridCell>();
        var cost = new Dictionary<GridCell, int> { [start] = 0 };
        open.Add((start, Heuristic(start, validGoals)));

        while (open.Count > 0)
        {
            open.Sort((a, b) =>
            {
                var byScore = a.score.CompareTo(b.score);
                if (byScore != 0) return byScore;
                var byX = a.cell.X.CompareTo(b.cell.X);
                return byX != 0 ? byX : a.cell.Y.CompareTo(b.cell.Y);
            });
            var current = open[0].cell;
            open.RemoveAt(0);
            if (validGoals.Contains(current))
            {
                var path = Reconstruct(cameFrom, current);
                return new PathResult(true, path, current, cost[current], null);
            }
            foreach (var delta in Directions)
            {
                var next = new GridCell(current.X + delta.X, current.Y + delta.Y);
                if (!IsWalkable(factory, next)) continue;
                var candidate = cost[current] + 1;
                if (cost.TryGetValue(next, out var known) && candidate >= known) continue;
                cost[next] = candidate;
                cameFrom[next] = current;
                open.Add((next, candidate + Heuristic(next, validGoals)));
            }
        }
        return new PathResult(false, Array.Empty<GridCell>(), null, 0, "path.unreachable");
    }

    private static bool IsWalkable(FactoryState factory, GridCell cell) =>
        factory.Grid.Cells.TryGetValue(cell, out var state) && state.IsWalkable && state.IsPurchased;

    private static int Heuristic(GridCell cell, IEnumerable<GridCell> goals) =>
        goals.Min(x => Math.Abs(x.X - cell.X) + Math.Abs(x.Y - cell.Y));

    private static IReadOnlyList<GridCell> Reconstruct(Dictionary<GridCell, GridCell> cameFrom, GridCell current)
    {
        var path = new List<GridCell> { current };
        while (cameFrom.TryGetValue(current, out current)) path.Add(current);
        path.Reverse();
        return path;
    }
}

public sealed class TaskDispatcher : ITaskDispatcher
{
    private readonly IPathfinder _pathfinder;
    public TaskDispatcher(IPathfinder pathfinder) => _pathfinder = pathfinder;

    public void Dispatch(WorldState world, FactoryState factory)
    {
        var agents = factory.Agents.Values.Where(x => x.State == AgentWorkState.Idle && x.CurrentTaskId == null)
            .OrderBy(x => x.AgentId, StringComparer.Ordinal).ToList();
        var tasks = factory.Tasks.Values.Where(x => x.State is TaskLifeState.Created or TaskLifeState.Queued)
            .OrderByDescending(x => x.Priority).ThenBy(x => x.CreatedSequence).ThenBy(x => x.TaskId, StringComparer.Ordinal).ToList();

        foreach (var task in tasks)
        {
            var candidates = agents.Where(x => task.ManuallyAssignedAgentId == null || x.AgentId == task.ManuallyAssignedAgentId)
                .Select(agent => (agent, path: _pathfinder.FindPath(factory, agent.CurrentCell, task.CandidateWorkCells)))
                .Where(x => x.path.Reachable)
                .OrderBy(x => x.path.TotalCost).ThenBy(x => x.agent.AgentId, StringComparer.Ordinal).ToList();
            if (candidates.Count == 0) { task.State = TaskLifeState.Blocked; task.FailureReasonCode = "task.no_reachable_agent"; continue; }
            var selected = candidates[0];
            task.AssignedAgentId = selected.agent.AgentId;
            task.SelectedWorkCell = selected.path.SelectedGoal;
            task.State = TaskLifeState.Claimed;
            selected.agent.CurrentTaskId = task.TaskId;
            selected.agent.CurrentPath = selected.path.Path.Skip(1).ToList();
            selected.agent.State = selected.agent.CurrentPath.Count == 0 ? AgentWorkState.Working : AgentWorkState.Moving;
            agents.Remove(selected.agent);
            if (agents.Count == 0) break;
        }
    }
}
}
