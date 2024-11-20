using System;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading.Tasks;

namespace NetMonX
{
    public partial class MainPage : ContentPage
    {
        // Коллекция для хранения найденных устройств
        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        public MainPage()
        {
            InitializeComponent();
            DevicesList.ItemsSource = Devices; // Привязка коллекции к интерфейсу
        }

        // Обработчик кнопки "Сканировать сеть"
        private async void OnScanNetworkClicked(object sender, EventArgs e)
        {
            LoadingIndicator.IsVisible = true; // Показать индикатор выполнения
            LoadingIndicator.IsRunning = true;
            Devices.Clear(); // Очистить предыдущие результаты

            string localIP = GetLocalIPAddress();
            if (localIP == null)
            {
                await DisplayAlert("Ошибка", "Не удалось определить локальный IP-адрес.", "ОК");
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                return;
            }

            string subnet = localIP.Substring(0, localIP.LastIndexOf('.')); // Определяем подсеть

            // Сканирование сети (первые 254 адреса)
            for (int i = 1; i <= 254; i++)
            {
                string ip = $"{subnet}.{i}";
                await ScanDevice(ip);
            }

            LoadingIndicator.IsRunning = false; // Скрыть индикатор выполнения
            LoadingIndicator.IsVisible = false;
        }

        // Сканирование устройства
        private async Task ScanDevice(string ipAddress)
        {
            using var ping = new Ping();
            try
            {
                var reply = await ping.SendPingAsync(ipAddress, 500);
                if (reply.Status == IPStatus.Success)
                {
                    Devices.Add(new DeviceInfo
                    {
                        IPAddress = ipAddress,
                        MACAddress = GetMacAddress(ipAddress),
                        HostName = GetHostName(ipAddress),
                        ResponseTime = reply.RoundtripTime
                    });
                }
            }
            catch
            {
                // Игнорируем недоступные устройства
            }
        }

        // Получение MAC-адреса (заглушка, т.к. требуется доступ к ARP)
        private string GetMacAddress(string ipAddress)
        {
            return "N/A"; // Реализация ARP-запросов для получения MAC-адреса зависит от платформы
        }

        // Получение имени хоста
        private string GetHostName(string ipAddress)
        {
            try
            {
                return Dns.GetHostEntry(ipAddress).HostName;
            }
            catch
            {
                return "Unknown";
            }
        }

        // Получение локального IP-адреса
        private string GetLocalIPAddress()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var properties = networkInterface.GetIPProperties();
                    foreach (var ip in properties.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return null;
        }
    }

    // Класс для представления информации об устройстве
    public class DeviceInfo
    {
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public string HostName { get; set; }
        public long ResponseTime { get; set; }
    }
}
