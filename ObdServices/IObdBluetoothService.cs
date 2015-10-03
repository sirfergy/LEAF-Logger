namespace ObdServices
{
    using System.Threading.Tasks;
    using Models;
    using Windows.Networking.Proximity;

    public class ObdErrorCodes
    {
        // Error code constants
        public const uint ERR_BLUETOOTH_OFF = 0x8007048F;      // The Bluetooth radio is off
        public const uint ERR_MISSING_CAPS = 0x80070005;       // A capability is missing from your WMAppManifest.xml
        public const uint ERR_NOT_ADVERTISING = 0x8000000E;    // You are currently not advertising your presence using PeerFinder.Start()
    }

    public enum ObdGroups
    {
        Charge = 0,
        Temperature = 1
    }

    public class PeerInformationWrapper
    {
        private PeerInformation peerInformation;
        private string displayName;

        public PeerInformation PeerInformation
        {
            get
            {
                return this.peerInformation;
            }

            set
            {
                this.peerInformation = value;
                if (value == null)
                {
                    this.DisplayName = null;
                }
                else
                {
                    this.DisplayName = value.DisplayName;
                }
            }
        }

        public string DisplayName
        {
            get
            {
                return this.displayName;
            }

            set
            {
                this.displayName = value;
            }
        }
    }

    public interface IObdBluetoothService
    {
        Task<ObdModel> GetObdData(PeerInformationWrapper selectedDevice);
        Task<PeerInformationWrapper> GetObdDevice(string bluetoothDeviceName);
    }
}
