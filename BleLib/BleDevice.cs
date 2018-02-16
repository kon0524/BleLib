using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Timers;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BleLib
{
    public class BleDevice
    {
        #region Members
        private BluetoothLEAdvertisementWatcher _watcher;
        private BluetoothLEDevice _device;
        private List<GattDeviceService> _services;

        private Timer _timer;

        private string _localName;
        #endregion

        #region Properties
        public delegate void BleEventHandler(object sender, BleEventArgs e);
        public event BleEventHandler BleEvent;
        public bool IsConnected { get; private set; }
        #endregion

        #region Constructor
        public BleDevice()
        {
            _watcher = new BluetoothLEAdvertisementWatcher();
            _watcher.ScanningMode = BluetoothLEScanningMode.Active;
            _watcher.Received += _watcher_Received;

            _timer = new Timer();
            _timer.Elapsed += _timer_Elapsed;

            IsConnected = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// LocalNameを指定して接続する
        /// </summary>
        /// <param name="localName"></param>
        /// <param name="timeout">(ミリ秒)</param>
        /// <returns></returns>
        public bool Connect(string localName, int timeout = 10000)
        {
            _localName = localName;
            _timer.Interval = timeout;

            _watcher.Start();
            _timer.Start();

            // あえて接続を待っている
            var timeoutDatetime = DateTime.Now.AddMilliseconds(timeout);
            while (!IsConnected)
            {
                System.Threading.Thread.Sleep(100);
                if (timeoutDatetime < DateTime.Now) break;
            }

            return IsConnected;
        }

        /// <summary>
        /// 切断
        /// </summary>
        public void Disconnect()
        {
            if (_services != null)
            {
                foreach (var s in _services)
                {
                    s.Session.Dispose();
                    s.Dispose();
                }
                _services = null;
            }
            if (_device != null)
            {
                _device.ConnectionStatusChanged -= _device_ConnectionStatusChanged;
                _device.GattServicesChanged -= _device_GattServicesChanged;
                _device.Dispose();
                _device = null;
            }
            IsConnected = false;
        }

        /// <summary>
        /// サービスのUUIDを取得する
        /// </summary>
        /// <returns></returns>
        public Guid[] GetServiceUuids()
        {
            if (_services == null) return null;

            List<Guid> sUuids = new List<Guid>();
            foreach (var s in _services)
            {
                sUuids.Add(s.Uuid);
            }

            return sUuids.ToArray();
        }

        /// <summary>
        /// キャラクタリスティックのUUIDを取得する(同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public Guid[] GetCharacteristicUuids(Guid serviceUuid)
        {
            if (_services == null) return null;
            var service = _services.Find(s => s.Uuid == serviceUuid);
            var cresult = service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask().Result;
            if (cresult.Status != GattCommunicationStatus.Success) return null;

            List<Guid> cUuids = new List<Guid>();
            foreach (var c in cresult.Characteristics)
            {
                cUuids.Add(c.Uuid);
            }

            return cUuids.ToArray();
        }

        /// <summary>
        /// キャラクタリスティックのUUIDを取得する(非同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public async Task<Guid[]> GetCharacteristicUuidsAsync(Guid serviceUuid)
        {
            if (_services == null) return null;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var result = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success) return null;

            List<Guid> cUuids = new List<Guid>();
            foreach (var c in result.Characteristics)
            {
                cUuids.Add(c.Uuid);
            }

            return cUuids.ToArray();
        }

        /// <summary>
        /// キャラクタリスティックを取得する
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public GattCharacteristic GetCharacteristic(Guid serviceUuid, Guid characteristicUuid)
        {
            if (_services == null) return null;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var cresult = service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached).AsTask().Result;
            if (cresult.Status != GattCommunicationStatus.Success) return null;

            return cresult.Characteristics.First();
        }

        /// <summary>
        /// キャラクタリスティックを取得する(非同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public async Task<GattCharacteristic> GetCharacteristicAsync(Guid serviceUuid, Guid characteristicUuid)
        {
            if (_services == null) return null;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var cresult = await service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached);
            if (cresult.Status != GattCommunicationStatus.Success) return null;

            return cresult.Characteristics.First();
        }

        /// <summary>
        /// キャラクタリスティックの値を読み取る
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public byte[] Read(Guid serviceUuid, Guid characteristicUuid)
        {
            var characteristic = GetCharacteristic(serviceUuid, characteristicUuid);
            if (characteristic == null) return null;

#if false   // for debug
            Console.WriteLine($"AttributeHandle          : {characteristic.AttributeHandle}");
            Console.WriteLine($"CharacteristicProperties : {characteristic.CharacteristicProperties}");
            Console.WriteLine($"PresentationFormats Num  : {characteristic.PresentationFormats.Count}");
            Console.WriteLine($"ProtectionLevel          : {characteristic.ProtectionLevel}");
            Console.WriteLine($"UserDescription          : {characteristic.UserDescription}");

            var gdresult = characteristic.GetDescriptorsAsync().AsTask().Result;
            if (gdresult.Status == GattCommunicationStatus.Success)
            {
                Console.WriteLine($"Descriptors Num          : {gdresult.Descriptors.Count}");
            }
            else
            {
                Console.WriteLine($"GetDescriptorsAsync failed.");
            }
#endif
            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            {
                Debug.WriteLine("Read not supported.");
                return null;
            }
            var rresult = characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask().Result;
            if (rresult.Status != GattCommunicationStatus.Success) return null;

            return rresult.Value.ToArray();
        }

        /// <summary>
        /// キャラクタリスティックの値を読み取る(非同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadAsync(Guid serviceUuid, Guid characteristicUuid)
        {
            var characteristic = await GetCharacteristicAsync(serviceUuid, characteristicUuid);
            if (characteristic == null) return null;

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            {
                Debug.WriteLine("Read not supported.");
                return null;
            }
            var rresult = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (rresult.Status != GattCommunicationStatus.Success) return null;

            return rresult.Value.ToArray();
        }

        /// <summary>
        /// キャラクタリスティックに値を書き込む
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Write(Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            var characteristic = GetCharacteristic(serviceUuid, characteristicUuid);
            if (characteristic == null) return false;

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
            {
                Debug.WriteLine("Write not supported.");
                return false;
            }
            var status = characteristic.WriteValueAsync(data.AsBuffer()).AsTask().Result;

            return status == GattCommunicationStatus.Success;
        }

        /// <summary>
        /// キャラクタリスティックに値を書き込む(非同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        public async Task<bool> WriteAsync(Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            var characteristic = await GetCharacteristicAsync(serviceUuid, characteristicUuid);
            if (characteristic == null) return false;

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
            {
                Debug.WriteLine("Write not supported.");
                return false;
            }
            var status = await characteristic.WriteValueAsync(data.AsBuffer());

            return status == GattCommunicationStatus.Success;
        }
#endregion

        #region Events
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _watcher.Stop();
            BleEvent(this, new BleEventArgs(BleEventArgs.BleEventType.Timeout));
        }

        private async void _watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Debug.WriteLine($"LocalName : {args.Advertisement.LocalName}");
            if (!string.IsNullOrEmpty(args.Advertisement.LocalName))
            {
                BleEvent(this, new BleEventArgs(args.Advertisement.LocalName));
            }

            if (!string.IsNullOrEmpty(_localName) && (_localName == args.Advertisement.LocalName))
            {
                _timer.Stop();
                _watcher.Stop();

                _device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (_device == null)
                {
                    BleEvent(this, new BleEventArgs(BleEventArgs.BleEventType.Error));
                    return;
                }

                var result = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                {
                    Disconnect();
                    BleEvent(this, new BleEventArgs(BleEventArgs.BleEventType.Error));
                    return;
                }

                _services = new List<GattDeviceService>();
                foreach(var s in result.Services)
                {
                    _services.Add(s);
                }

                _device.GattServicesChanged += _device_GattServicesChanged;
                _device.ConnectionStatusChanged += _device_ConnectionStatusChanged;

                IsConnected = true;
                BleEvent(this, new BleEventArgs(BleEventArgs.BleEventType.Connected));
            }
        }

        private void _device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
        }

        private void _device_GattServicesChanged(BluetoothLEDevice sender, object args)
        {
        }
        #endregion
    }
}
