using System;

namespace BleLib
{
    public class BleEventArgs : EventArgs
    {
        public enum BleEventType {
            Connected,
            Disconnected,
            Timeout,
            Error,
            LocalName
        }

        public BleEventType Type { get; private set; }

        /// <summary>
        /// LocalName (When Type is LocalName)
        /// </summary>
        public string LocalName { get; private set; }

        public BleEventArgs(BleEventType type)
        {
            Type = type;
        }

        public BleEventArgs(string localName)
        {
            Type = BleEventType.LocalName;
            LocalName = localName;
        }
    }
}
