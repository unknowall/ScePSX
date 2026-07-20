using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ScePSX
{
    public delegate void EventUpdateCallback(int cycles);

    public class PsxEvent
    {
        private readonly PsxEventManager _Manager;
        private readonly string _Name;
        private readonly EventUpdateCallback _OnUpdate;
        private int _CyclesUntilEvent;
        private int _PendingCycles;
        private bool _Active;

        internal PsxEvent(PsxEventManager manager, string name, EventUpdateCallback onUpdate)
        {
            _Manager = manager;
            _Name = name;
            _OnUpdate = onUpdate;
        }

        public string Name => _Name;
        public bool IsActive => _Active;

        public void Reset()
        {
            _CyclesUntilEvent = 0;
            _PendingCycles = 0;
            _Active = false;
        }

        public void UpdateEarly()
        {
            if (!_Active)
                return;

            int Pending = _PendingCycles + _Manager.PendingCycles;
            while (Pending > 0 && _Active)
            {
                int UpdateCycles = Math.Min(Pending, _CyclesUntilEvent);
                Pending -= UpdateCycles;
                _Manager.UpdateEventInternal(this, UpdateCycles);
            }
            _Manager.ScheduleNextEventInternal(this);
        }

        public void Schedule(int CyclesFromNow)
        {
            if (!_Active)
            {
                _PendingCycles = -_Manager.PendingCycles;
                _Active = true;
            }
            else if (_CyclesUntilEvent == CyclesFromNow)
            {
                return;
            }

            _CyclesUntilEvent = CyclesFromNow;
            _Manager.ScheduleNextEventInternal(this);
        }

        public void Cancel()
        {
            if (_Active)
            {
                _PendingCycles = 0;
                _CyclesUntilEvent = 0;
                _Active = false;
                _Manager.ScheduleNextEventInternal(this);
            }
        }

        public void Pause()
        {
            if (!_Active)
                return;

            _PendingCycles += _Manager.PendingCycles;
            _Active = false;
            _Manager.ScheduleNextEventInternal(this);
        }

        public void Resume()
        {
            if (_Active || _CyclesUntilEvent == 0)
                return;

            _PendingCycles -= _Manager.PendingCycles;
            _Active = true;
            _Manager.ScheduleNextEventInternal(this);
        }

        public int GetPendingCycles()
        {
            if (!_Active)
                Console.WriteLine("[EventManager] Event is not active");

            return _PendingCycles + _Manager.PendingCycles;
        }

        public int GetRemainingCycles() => _CyclesUntilEvent - GetPendingCycles();

        public float GetProgress()
        {
            if (_CyclesUntilEvent <= 0)
                return 1f;
            int Pending = GetPendingCycles();
            return Math.Clamp((float)Pending / _CyclesUntilEvent, 0f, 1f);
        }

        internal void AddPendingCycles(int Cycles)
        {
            if (_Active)
                _PendingCycles += Cycles;
        }

        internal void UpdateInternal(int Cycles)
        {
            if (!_Active || Cycles <= 0 || Cycles > _CyclesUntilEvent)
                return;

            _CyclesUntilEvent -= Cycles;
            _PendingCycles -= Cycles;

            _OnUpdate?.Invoke(Cycles);

            if (_CyclesUntilEvent == 0)
            {
                _PendingCycles = 0;
                _Active = false;
            }
        }

        internal int GetLocalRemainingCycles() => _CyclesUntilEvent - _PendingCycles;
    }

    public class PsxEventManager
    {
        private const int InfiniteCycles = int.MaxValue;

        private readonly List<PsxEvent> _Events = new();
        private PsxEvent _NextEvent;
        private int _CyclesUntilNextEvent;
        private int _PendingCycles;
        private int _CyclesUntilGteComplete;
        private int _CyclesThisFrame;
        private bool _Updating;

        public int PendingCycles => _PendingCycles;

        public void Reset()
        {
            Console.WriteLine("[EventManager] Reset");
            _PendingCycles = 0;
            _CyclesUntilNextEvent = 0;
            _CyclesUntilGteComplete = 0;
            _CyclesThisFrame = 0;
            _NextEvent = null;

            foreach (var Ev in _Events)
                Ev.Reset();
        }

        public PsxEvent CreateEvent(string Name, EventUpdateCallback OnUpdate)
        {
            var Ev = new PsxEvent(this, Name, OnUpdate);
            _Events.Add(Ev);
            return Ev;
        }

        public PsxEvent FindEvent(string Name)
        {
            return _Events.FirstOrDefault(E => E.Name == Name);
        }

        public void AddCyclesAndUpdateEvents(int Cycles)
        {
            AddCycles(Cycles);
            if (ReadyForNextEvent())
                UpdateNextEvent();
        }

        public void AddCycles(int Cycles)
        {
            _PendingCycles += Cycles;
        }

        public void UpdateNextEvent()
        {
            if (!ReadyForNextEvent() || _Updating)
                return;

            do
            {
                if (_NextEvent == null)
                    return;

                if (_PendingCycles > 0)
                {
                    foreach (var Ev in _Events)
                        Ev.AddPendingCycles(_PendingCycles);

                    _CyclesUntilGteComplete = Math.Max(_CyclesUntilGteComplete - _PendingCycles, 0);
                    _CyclesThisFrame += _PendingCycles;
                    _PendingCycles = 0;
                }

                var Ev1 = _NextEvent;
                _NextEvent = null;

                if (!Ev1.IsActive)
                    continue;

                if (Ev1.GetLocalRemainingCycles() > 0)
                    continue;

                UpdateEventInternal(Ev1, Ev1.GetLocalRemainingCycles() + _PendingCycles); // _PendingCycles is 0 here
                ScheduleNextEventInternal(null);
            }
            while (ReadyForNextEvent());
        }

        public void EndFrame()
        {
            _CyclesThisFrame += _PendingCycles;

            Console.WriteLine($"[EventManager] EndFrame -- CPU cycles this frame: {_CyclesThisFrame}");

            _CyclesThisFrame = -_PendingCycles;
        }

        private bool ReadyForNextEvent() => _PendingCycles >= _CyclesUntilNextEvent;

        internal void UpdateEventInternal(PsxEvent Ev, int Cycles)
        {
            if (_Updating)
                return;

            _Updating = true;
            Ev.UpdateInternal(Cycles);
            _Updating = false;
        }

        internal void ScheduleNextEventInternal(PsxEvent ChangedEvent)
        {
            if (_Updating)
                return;

            if (_NextEvent != null && ChangedEvent != null)
            {
                int NewCycles = ChangedEvent.IsActive ? ChangedEvent.GetLocalRemainingCycles() : InfiniteCycles;
                if (_NextEvent == ChangedEvent)
                {
                    if (NewCycles < _CyclesUntilNextEvent)
                    {
                        _CyclesUntilNextEvent = NewCycles;
                        return;
                    }
                }
                else
                {
                    if (NewCycles >= _CyclesUntilNextEvent)
                        return;
                }
            }

            var (Index, MinCycles) = FindNextEvent();
            if (Index == -1 || MinCycles == InfiniteCycles)
                return;

            _NextEvent = _Events[Index];
            if (!_NextEvent.IsActive || _NextEvent.GetLocalRemainingCycles() <= 0)
                return;

            _CyclesUntilNextEvent = MinCycles;
        }

        private (int Index, int Cycles) FindNextEvent()
        {
            int MinIndex = -1;
            int MinCycles = InfiniteCycles;

            for (int i = 0; i < _Events.Count; i++)
            {
                var Ev = _Events[i];
                if (!Ev.IsActive)
                    continue;

                int Rem = Ev.GetLocalRemainingCycles();
                if (Rem < MinCycles)
                {
                    MinCycles = Rem;
                    MinIndex = i;
                }
            }

            return (MinIndex, MinCycles);
        }

        public void RemoveEvent(PsxEvent psxevent)
        {
            Console.WriteLine($"[EventManager] RemoveEvent -- [{psxevent.Name}]");

            _Events.Remove(psxevent);
        }

        public void AddGteCycles(int Cycles) => _CyclesUntilGteComplete = _PendingCycles + Cycles;
        public void StallUntilGteComplete() => _PendingCycles = Math.Max(_PendingCycles, _CyclesUntilGteComplete);
    }
}
