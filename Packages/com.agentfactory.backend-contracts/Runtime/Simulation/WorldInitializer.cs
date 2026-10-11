#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AgentFactory.Contracts.Core;
using AgentFactory.Contracts.Definitions;
using AgentFactory.Contracts.World;

namespace AgentFactory.Contracts.Simulation
{

public sealed class WorldInitializer
{
    public WorldState Create(GameDefinitions definitions, decimal initialBalance = 5000m)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        var world = new WorldState
        {
            SaveVersion = 1,
            ConfigVersion = definitions.ConfigVersion,
            Economy = new EconomyState
            {
                Balance = initialBalance,
                InitialBalance = initialBalance,
                NextTransactionSequence = 1
            },
            Story = new StoryState { StoryDefinitionId = definitions.Story.StoryDefinitionId }
        };

        var first = definitions.Factories.Values.OrderBy(x => x.FactoryDefinitionId, StringComparer.Ordinal).FirstOrDefault();
        if (first != null)
            world.Factories.Add(first.FactoryDefinitionId, CreateFactory(first, true));

        InitializeStory(world, definitions.Story);
        return world;
    }

    public FactoryState CreateFactory(FactoryDefinition definition, bool purchased)
    {
        var factory = new FactoryState
        {
            FactoryId = definition.FactoryDefinitionId,
            FactoryDefinitionId = definition.FactoryDefinitionId,
            IsPurchased = purchased,
            Grid = new GridRuntimeState { Size = definition.GridSize }
        };

        for (var y = 0; y < definition.GridSize.Height; y++)
        for (var x = 0; x < definition.GridSize.Width; x++)
        {
            var coordinate = new GridCell(x, y);
            factory.Grid.Cells[coordinate] = new GridCellState
            {
                Coordinate = coordinate,
                IsBuildable = !definition.FixedObstacleCells.Contains(coordinate),
                IsWalkable = !definition.FixedObstacleCells.Contains(coordinate),
                IsBoundaryExit = definition.ValidBoundaryExitCells.Contains(coordinate)
            };
        }

        foreach (var parcelDefinition in definition.Parcels.OrderBy(x => x.ParcelDefinitionId, StringComparer.Ordinal))
        {
            var parcel = new LandParcelState
            {
                ParcelId = parcelDefinition.ParcelDefinitionId,
                ParcelDefinitionId = parcelDefinition.ParcelDefinitionId,
                FactoryId = factory.FactoryId,
                IsPurchased = parcelDefinition.InitiallyPurchased,
                ActualPurchasePrice = parcelDefinition.InitiallyPurchased ? 0m : null,
                Cells = parcelDefinition.Cells.ToList()
            };
            factory.Parcels.Add(parcel.ParcelId, parcel);
            foreach (var cell in parcel.Cells.Where(factory.Grid.Cells.ContainsKey))
            {
                factory.Grid.Cells[cell].ParcelId = parcel.ParcelId;
                factory.Grid.Cells[cell].IsPurchased = parcel.IsPurchased;
            }
        }

        for (var i = 0; i < definition.AgentSpawnCells.Count; i++)
        {
            var id = $"{factory.FactoryId}:agent:{i + 1:D2}";
            factory.Agents[id] = new AgentState
            {
                AgentId = id,
                FactoryId = factory.FactoryId,
                CurrentCell = definition.AgentSpawnCells[i],
                State = AgentWorkState.Idle
            };
        }
        return factory;
    }

    private static void InitializeStory(WorldState world, StoryDefinition definition)
    {
        foreach (var chapterDefinition in definition.Chapters.OrderBy(x => x.Sequence).ThenBy(x => x.ChapterId, StringComparer.Ordinal))
        {
            var chapter = new StoryChapterState
            {
                ChapterId = chapterDefinition.ChapterId,
                Status = chapterDefinition.Sequence == definition.Chapters.Min(x => x.Sequence)
                    ? StoryProgressStatus.Available
                    : StoryProgressStatus.Locked
            };
            world.Story.Chapters[chapter.ChapterId] = chapter;
            foreach (var contractDefinition in chapterDefinition.Contracts.OrderBy(x => x.Sequence))
            {
                var contract = new StoryContractState
                {
                    ContractId = contractDefinition.ContractDefinitionId,
                    ContractDefinitionId = contractDefinition.ContractDefinitionId,
                    ChapterId = chapter.ChapterId,
                    Status = chapter.Status == StoryProgressStatus.Available
                        ? StoryProgressStatus.Available
                        : StoryProgressStatus.Locked
                };
                foreach (var objectiveDefinition in contractDefinition.Objectives)
                    contract.Objectives[objectiveDefinition.ObjectiveId] = new StoryObjectiveState { ObjectiveId = objectiveDefinition.ObjectiveId };
                world.Story.Contracts[contract.ContractId] = contract;
            }
        }
        world.Story.CurrentChapterId = definition.Chapters.OrderBy(x => x.Sequence).Select(x => x.ChapterId).FirstOrDefault();
    }
}
}
