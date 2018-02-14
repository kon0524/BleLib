using System;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            BleLib.BleDevice device = new BleLib.BleDevice();
            device.BleEvent += Device_BleEvent;

            // Connect to Ble device.
            bool result = device.Connect("Local Name", 5000);
            if (!result)
            {
                Console.WriteLine("Failed to connection.");
                return;
            }

            // Get service uuid array.
            Guid[] service = device.GetServiceUuids();
            foreach (Guid s in service)
            {
                Console.WriteLine($"Service UUID        : {s}");

                // Get characteristic uuid array.
                Guid[] chara = device.GetCharacteristicUuids(s);
                foreach (Guid c in chara)
                {
                    Console.WriteLine($"Characteristic UUID : {c}");
                }
            }

            // Disconnect from Ble device.
            device.Disconnect();
        }

        private static void Device_BleEvent(object sender, BleLib.BleEventArgs e)
        {
            Console.WriteLine($"BleEvent Received. type:{e.Type}");
        }
    }
}
