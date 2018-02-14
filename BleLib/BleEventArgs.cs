using System;

namespace BleLib
{
    public class BleEventArgs : EventArgs
    {
        public enum BleEventType {
            Connected,
            Disconnected,
            Timeout,
            Error
        }

        public BleEventType Type { get; set; }
    }
}
