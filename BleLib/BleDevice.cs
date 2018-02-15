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
                    s.Dispose();
                }
                _services = null;
            }
            if (_device != null)
            {
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
            var task = service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask();
            task.Wait();
            var result = task.Result;
            if (result.Status != GattCommunicationStatus.Success) return null;

            List<Guid> cUuids = new List<Guid>();
            foreach (var c in result.Characteristics)
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
        /// キャラクタリスティックの値を読み取る
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public byte[] Read(Guid serviceUuid, Guid characteristicUuid)
        {
            if (_services == null) return null;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var ctask = service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached).AsTask();
            ctask.Wait();
            if (ctask.Result.Status != GattCommunicationStatus.Success) return null;

            var characteristic = ctask.Result.Characteristics.First();
            var rtask = characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask();
            rtask.Wait();
            if (rtask.Result.Status != GattCommunicationStatus.Success) return null;

            return rtask.Result.Value.ToArray();
        }

        /// <summary>
        /// キャラクタリスティックの値を読み取る(非同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadAsync(Guid serviceUuid, Guid characteristicUuid)
        {
            if (_services == null) return null;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var cresult = await service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached);
            if (cresult.Status != GattCommunicationStatus.Success) return null;

            var characteristic = cresult.Characteristics.First();
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
            if (_services == null) return false;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var ctask = service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached).AsTask();
            ctask.Wait();
            if (ctask.Result.Status != GattCommunicationStatus.Success) return false;

            var characteristic = ctask.Result.Characteristics.First();
            var wtask = characteristic.WriteValueAsync(data.AsBuffer()).AsTask();
            wtask.Wait();

            return wtask.Result == GattCommunicationStatus.Success;
        }

        /// <summary>
        /// キャラクタリスティックに値を書き込む(非同期)
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        public async Task<bool> WriteAsync(Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            if (_services == null) return false;

            var service = _services.Find(s => s.Uuid == serviceUuid);
            var cresult = await service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached);
            if (cresult.Status != GattCommunicationStatus.Success) return false;

            var characteristic = cresult.Characteristics.First();
            var status = await characteristic.WriteValueAsync(data.AsBuffer());

            return status == GattCommunicationStatus.Success;
        }
        #endregion

        #region Events
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _watcher.Stop();
            BleEvent(this, new BleEventArgs() { Type = BleEventArgs.BleEventType.Timeout });
        }

        private async void _watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Debug.WriteLine($"LocalName : {args.Advertisement.LocalName}");
            if (!string.IsNullOrEmpty(_localName) && (_localName == args.Advertisement.LocalName))
            {
                _timer.Stop();
                _watcher.Stop();

                _device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (_device == null)
                {
                    BleEvent(this, new BleEventArgs() { Type = BleEventArgs.BleEventType.Error });
                    return;
                }

                var result = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                {
                    Disconnect();
                    BleEvent(this, new BleEventArgs() { Type = BleEventArgs.BleEventType.Error });
                    return;
                }

                _services = new List<GattDeviceService>();
                foreach(var s in result.Services)
                {
                    _services.Add(s);
                }
                IsConnected = true;
                BleEvent(this, new BleEventArgs() { Type = BleEventArgs.BleEventType.Connected });
            }
        }
        #endregion
    }
}
