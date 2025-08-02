using System;

namespace LevelExtender
{
    public class LEEvents
    {
        public event EventHandler<EXPEventArgs> OnXPChanged;
        public void RaiseEvent(EXPEventArgs args)
        {
            OnXPChanged?.Invoke(this, args);
        }
    }

    public class EXPEventArgs : EventArgs
    {
        public int Key { get; set; }
    }
}