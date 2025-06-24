using System;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MP2
{
    public class MapperRoutineSkill : INotifyPropertyChanged
    {
        private Stopwatch delaySw = Stopwatch.StartNew();
        private string _slotName;
        private string _slotIndex;
        private bool _enabled;
        private bool _castOnMe;
        private string _sktype;
        private double _delay;
        public MapperRoutineSkill(string slotName, string slotIndex, bool enabled, bool castOnMe, string sktype, double delay)
        {
            Enabled = enabled;
            CastOnMe = castOnMe;
            SlotName = slotName;
            SlotIndex = slotIndex;
            SkType = sktype;
            Delay = delay;
        }
        public string SlotName
        {
            get { return _slotName; }
            set
            {
                _slotName = value;
                NotifyPropertyChanged(nameof(SlotName));
            }
        }
        public string SlotIndex
        {
            get { return _slotIndex; }
            set
            {
                _slotIndex = value;
                NotifyPropertyChanged(nameof(SlotIndex));
            }
        }
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                NotifyPropertyChanged(nameof(Enabled));
            }
        }
        public bool CastOnMe
        {
            get { return _castOnMe; }
            set
            {
                _castOnMe = value;
                NotifyPropertyChanged(nameof(CastOnMe));
            }
        }
        public string SkType
        {
            get { return _sktype; }
            set
            {
                Mp2Routine.Log.Info("SkType Change: " + _sktype + " to:" + value);
                _sktype = value;
                NotifyPropertyChanged(nameof(SkType));
            }
        }
        public double Delay
        {
            get { return _delay; }
            set
            {
                _delay = Math.Round(value, 1, MidpointRounding.AwayFromZero);
                NotifyPropertyChanged(nameof(Delay));
            }
        }

        public bool IsReadyToCast
        {
            get
            {
                return  delaySw.ElapsedMilliseconds > Delay * 1000;
            }
        }
        public void ResetDelay()
        {
            delaySw.Restart();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
