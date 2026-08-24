using HalloGames.Architecture.Events;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Gameplay.Ship;
using HalloGames.SpaceRTS.Gameplay.Ship.Control;
using HalloGames.SpaceRTS.Gameplay.Targets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Input
{
    public readonly struct SelectionChangedEvent : IEvent
    {
        public readonly IReadOnlyList<ShipEntity> SelectedShips;

        public SelectionChangedEvent(IReadOnlyList<ShipEntity> selectedShips)
        {
            SelectedShips = selectedShips;
        }
    }

    public class ShipSelection
    {
        private class SelectionEntry
        {
            public ShipEntity Entity { get; }
            public IControllable Controllable { get; }
            public Action DeathHandler { get; set; }

            public SelectionEntry(ShipEntity entity, IControllable controllable)
            {
                Entity = entity;
                Controllable = controllable;
            }
        }

        private readonly List<SelectionEntry> _entries = new List<SelectionEntry>();

        public void SetSingle(ShipEntity entity, IControllable controllable)
        {
            ClearEntries();
            AddEntry(entity, controllable);
            PublishSelectionChanged();
        }

        public void SetGroup(List<(ShipEntity entity, IControllable controllable)> ships)
        {
            ClearEntries();
            foreach (var (entity, controllable) in ships)
                AddEntry(entity, controllable);

            PublishSelectionChanged();
        }

        public void Clear()
        {
            if (_entries.Count == 0)
                return;

            ClearEntries();
            PublishSelectionChanged();
        }

        public void TargetAll(ITargetable target, SideData side)
        {
            foreach (var entry in _entries)
            {
                if (entry.Controllable.IsEnableToControl(side))
                    entry.Controllable.Target(target);
            }
        }

        public void TargetPositionAll(Vector3 targetPos, SideData side)
        {
            foreach (var entry in _entries)
            {
                if (entry.Controllable.IsEnableToControl(side))
                    entry.Controllable.TargetPosition(targetPos);
            }
        }

        private void AddEntry(ShipEntity entity, IControllable controllable)
        {
            var entry = new SelectionEntry(entity, controllable);
            entry.DeathHandler = () => RemoveDeadEntry(entry);

            controllable.Select();
            if (entity.DeathController != null)
                entity.DeathController.OnDeath += entry.DeathHandler;

            _entries.Add(entry);
        }

        private void RemoveDeadEntry(SelectionEntry entry)
        {
            if (!_entries.Remove(entry))
                return;

            entry.Controllable.DeSelect();
            PublishSelectionChanged();
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries)
            {
                entry.Controllable.DeSelect();
                if (entry.Entity != null && entry.Entity.DeathController != null)
                    entry.Entity.DeathController.OnDeath -= entry.DeathHandler;
            }

            _entries.Clear();
        }

        private void PublishSelectionChanged()
        {
            var selectedShips = new List<ShipEntity>(_entries.Count);
            foreach (var entry in _entries)
                selectedShips.Add(entry.Entity);

            EventsManager.Publish(new SelectionChangedEvent(selectedShips));
        }
    }
}
