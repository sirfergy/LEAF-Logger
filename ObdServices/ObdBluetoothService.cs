namespace ObdServices
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Windows.Networking.Proximity;
    using Windows.Networking.Sockets;
    using Windows.Storage.Streams;

    public class MockObdBluetoothService : IObdBluetoothService
    {
        private Random random = new Random();
        private int soc;
        private double capacity;

        public MockObdBluetoothService()
        {
            soc = random.Next(80, 95);
            capacity = ((double)random.Next(90, 95) + random.NextDouble()) / 100;
        }

        public async Task<ObdModel> GetObdData(PeerInformationWrapper selectedDevice)
        {
            Thread.Sleep(1000);
            soc = random.Next(soc - 5, soc);

            return new ObdModel()
            {
                SoC = ((double)soc + random.NextDouble()) / 100,
                Capacity = capacity,
                RawCapacity = 66,
                Temperature1 = random.Next(32, 100),
                Temperature2 = random.Next(32, 100),
                Temperature3 = random.Next(32, 100),
                Temperature4 = random.Next(32, 100),
                When = DateTime.Now.ToUniversalTime()
            };
        }

        public async Task<PeerInformationWrapper> GetObdDevice(string bluetoothDeviceName)
        {
            Thread.Sleep(1000);
            return new PeerInformationWrapper()
            {
                DisplayName = "OBDII"
            };
        }
    }

    public class ObdBluetoothService : IObdBluetoothService
    {
        private int totalCapacity = 66;

        public async Task<PeerInformationWrapper> GetObdDevice(string bluetoothDeviceName)
        {
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var pairedDevices = await PeerFinder.FindAllPeersAsync();
            if (pairedDevices.Count > 0)
            {
                PeerInformation selectedDevice = pairedDevices.Where(x => x.DisplayName == bluetoothDeviceName).FirstOrDefault();
                if (selectedDevice != null)
                {
                    return new PeerInformationWrapper()
                    {
                        PeerInformation = selectedDevice
                    };
                }
            }

            return null;
        }

        public async Task<ObdModel> GetObdData(PeerInformationWrapper selectedDevice)
        {
            string errorMessage = null;
            string[] result = null;
            try
            {
                ObdModel model = null;
                result = await this.InitializeAndReadObdStream(selectedDevice.PeerInformation);

                // Charge
                string[] rows = result[(int)ObdGroups.Charge].Split('\r');
                if (rows != null &&
                    rows.Length == 8)
                {
                    string socString = rows[4].Replace(" ", "").Trim().Substring(14);
                    string capString = rows[5].Replace(" ", "").Trim().Substring(8, 6);

                    int socValue = int.Parse(socString, NumberStyles.AllowHexSpecifier);
                    int capValue = int.Parse(capString, NumberStyles.AllowHexSpecifier);

                    model = new ObdModel()
                    {
                        SoC = (double)socValue / 1000000,
                        Capacity = (double)capValue / (totalCapacity * 10000),
                        RawCapacity = (double)capValue / 10000,
                        When = DateTime.Now.ToUniversalTime()
                    };
                }
                else
                {
                    // retry
                    if (result[(int)ObdGroups.Charge].StartsWith("NO"))
                    {
                        errorMessage = "No charge data available";
                    }
                    else
                    {
                        errorMessage = "Communication interrupted";
                    }
                }

                if (errorMessage == null)
                {
                    rows = result[(int)ObdGroups.Temperature].Split('\r');
                    if (rows != null &&
                        rows.Length == 5)
                    {
                        int temp1 = int.Parse(rows[0].Replace(" ", "").Trim().Substring(16, 2), NumberStyles.AllowHexSpecifier);
                        int temp2 = int.Parse(rows[1].Replace(" ", "").Trim().Substring(8, 2), NumberStyles.AllowHexSpecifier);
                        int temp3 = int.Parse(rows[1].Replace(" ", "").Trim().Substring(14, 2), NumberStyles.AllowHexSpecifier);
                        int temp4 = int.Parse(rows[2].Replace(" ", "").Trim().Substring(6, 2), NumberStyles.AllowHexSpecifier);

                        model.Temperature1 = temp1 * 1.8 + 32.0;
                        model.Temperature2 = temp2 * 1.8 + 32.0;
                        model.Temperature3 = temp3 * 1.8 + 32.0;
                        model.Temperature4 = temp4 * 1.8 + 32.0;
                    }
                    else
                    {
                        // retry
                        if (result[(int)ObdGroups.Temperature].StartsWith("NO"))
                        {
                            errorMessage = "No temperature data available";
                        }
                        else
                        {
                            errorMessage = "Communication interrupted";
                        }
                    }
                }

                if (model != null)
                {
                    return model;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            throw new Exception(errorMessage);
        }

        private async Task<string[]> InitializeAndReadObdStream(PeerInformation selectedDevice)
        {
            string[] result = new string[] { string.Empty, string.Empty };

            using (StreamSocket socket = new StreamSocket())
            {
                await socket.ConnectAsync(selectedDevice.HostName, "1");

                using (var dataWriter = new DataWriter(socket.OutputStream))
                using (var dataReader = new DataReader(socket.InputStream))
                {
                    dataReader.InputStreamOptions = InputStreamOptions.None;

                    // full reset
                    //dataWriter.WriteString("AT Z");
                    //dataWriter.WriteString(Environment.NewLine);
                    //await dataWriter.StoreAsync();
                    //string reset = await this.ReadString(dataReader);

                    // default reset
                    //dataWriter.WriteString("AT D");
                    //dataWriter.WriteString(Environment.NewLine);
                    //await dataWriter.StoreAsync();
                    //string reset = await this.ReadString(dataReader);

                    // reset CRA settings
                    dataWriter.WriteString("AT AR");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader, "AT AR");

                    // 9600 baud
                    //dataWriter.WriteString("AT IB 96");
                    //dataWriter.WriteString(Environment.NewLine);
                    //await dataWriter.StoreAsync();
                    //await this.ReadAndValidateCommand(dataReader);

                    // echo off
                    dataWriter.WriteString("AT E0");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader, "AT E0");

                    // read voltage
                    dataWriter.WriteString("AT RV");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    string voltage = await this.ReadString(dataReader);

                    // set protocol to 6
                    dataWriter.WriteString("AT SP 6");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // printing of spaces off
                    dataWriter.WriteString("AT S0");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

#if FALSE
                    // line feed off 
                    dataWriter.WriteString("AT L0");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // automatically receive
                    dataWriter.WriteString("AT AR");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // CAN Receive Address (CAR CAN)
                    dataWriter.WriteString("AT CRA 1CB");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // CAN Receive Address (EV CAN)
                    dataWriter.WriteString("AT CRA 5BC");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);
#endif

                    // headers on
                    dataWriter.WriteString("AT H1");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // don't send wakeup messages
                    dataWriter.WriteString("AT SW 00");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // display data length
                    dataWriter.WriteString("AT D1");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Set header
                    dataWriter.WriteString("AT SH 79B");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set header
                    dataWriter.WriteString("AT FC SH 79B");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set data
                    dataWriter.WriteString("AT FC SD 30 00 18");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set mode (user defined responses)
                    dataWriter.WriteString("AT FC SM 1");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Group 1 (21 01) SOC = 5th line last three bytes, CAP = 6th (last) line second, third and fourth bytes.
                    dataWriter.WriteString("21 01");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    result[(int)ObdGroups.Charge] = await this.ReadString(dataReader);

                    // Group 4 (21 04) 
                    dataWriter.WriteString("21 04");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    result[(int)ObdGroups.Temperature] = await this.ReadString(dataReader);

#if PRESSURE
                    // Set header
                    dataWriter.WriteString("AT SH 745");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set header
                    dataWriter.WriteString("AT FC SH 745");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Set receive header
                    dataWriter.WriteString("AT CRA 746");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set receive header
                    dataWriter.WriteString("AT FC RA 746");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);
#endif

#if L2COUNT
                    // Set header
                    dataWriter.WriteString("AT SH 797");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set header
                    dataWriter.WriteString("AT FC SH 797");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // set protocol to 6
                    //dataWriter.WriteString("AT SP 7");
                    //dataWriter.WriteString(Environment.NewLine);
                    //await dataWriter.StoreAsync();
                    //await this.ReadAndValidateCommand(dataReader); 

                    // Set receive header
                    //dataWriter.WriteString("AT CRA 79A");
                    //dataWriter.WriteString(Environment.NewLine);
                    //await dataWriter.StoreAsync();
                    //await this.ReadAndValidateCommand(dataReader);

                    // Flow control set receive header
                    //dataWriter.WriteString("AT FC RA 79A");
                    //dataWriter.WriteString(Environment.NewLine);
                    //await dataWriter.StoreAsync();
                    //await this.ReadAndValidateCommand(dataReader);

                    dataWriter.WriteString("03 22");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    string l2Count = await this.ReadString(dataReader);

#endif

#if DEBUG
                    // Set header
                    dataWriter.WriteString("AT SH 797");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Flow control set header
                    dataWriter.WriteString("AT FC SH 797");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    await this.ReadAndValidateCommand(dataReader);

                    // Group 1 (21 01) SOC = 5th line last three bytes, CAP = 6th (last) line second, third and fourth bytes.
                    dataWriter.WriteString("03 22 11 11");
                    dataWriter.WriteString(Environment.NewLine);
                    await dataWriter.StoreAsync();
                    string ambient = await this.ReadString(dataReader);
#endif
                }
            }

            return result;
        }

        private async Task<string> ReadAndValidateCommand(DataReader dataReader, string command = null)
        {
            return await this.ReadAndValidateString(dataReader, (result) =>
                {
                    if ((command == null && !result.StartsWith("OK")) ||
                        (command != null && !result.Replace(command, string.Empty).TrimStart().StartsWith("OK")))
                    {
                        throw new Exception("Unexpected result while initializing the OBD reader");
                    }
                });
        }

        private async Task<string> ReadAndValidateString(DataReader dataReader, Action<string> validate)
        {
            string result = await this.ReadString(dataReader);

            validate(result);

            return result;
        }

        private async Task<string> ReadString(DataReader dataReader)
        {
            StringBuilder resultString = new StringBuilder();
            string value = null;
            uint result;
            const uint buffer = 1;

            do
            {
                try
                {
                    //Thread.Sleep(10);
                    result = await dataReader.LoadAsync(buffer);
                    if (result > 0)
                    {
                        value = dataReader.ReadString(result);
                        resultString.Append(value);
                    }

                    if (value == ">")
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    result = 0;
                }
            }
            while (result == buffer);

            return resultString.ToString();
        }
    }
}
