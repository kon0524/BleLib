using System;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            string localName = args[0];

            BleLib.BleDevice device = new BleLib.BleDevice();
            device.BleEvent += Device_BleEvent;

            // Connect to Ble device.
            bool result = device.Connect(localName, 10000);
            if (!result)
            {
                Console.WriteLine("Failed to connection.");
                return;
            }

            if (args.Length < 2)
            {
                // Get service uuid array.
                Guid[] service = device.GetServiceUuids();
                foreach (Guid s in service)
                {
                    Console.WriteLine("-----------------------------------------");
                    Console.WriteLine($"Service UUID        : {s}");

                    // Get characteristic uuid array.
                    Guid[] chara = device.GetCharacteristicUuids(s);
                    foreach (Guid c in chara)
                    {
                        Console.WriteLine($"Characteristic UUID : {c}");
                        //device.Read(s, c);
                    }
                }
            }
            else
            {
                Guid s = Guid.Parse(args[1]);
                Guid c = Guid.Parse(args[2]);

                Console.WriteLine($"{s} {c}");

                byte[] data = device.Read(s, c);
                if (data != null)
                {
                    Console.WriteLine(BitConverter.ToString(data));
                }
                else
                {
                    Console.WriteLine("null");
                }
            }

            // Disconnect from Ble device.
            device.Disconnect();
        }

        private static void Device_BleEvent(object sender, BleLib.BleEventArgs e)
        {
            Console.WriteLine($"BleEvent Received. type:{e.Type}, localName:{e.LocalName}");
        }
    }
}
