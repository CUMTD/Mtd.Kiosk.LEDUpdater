// TODO: do not use this fil
using Microsoft.Extensions.Logging;
using ServiceReference1;
using System.ServiceModel;
using System.Xml;

namespace Mtd.Kiosk.LEDUpdater.IPDisplays.API
{
    public class IpDisplaysApi
    {

        #region Properties

        private Uri Uri { get; }
        private TimeSpan Timeout { get; set; }
        private ILogger Logger { get; }

        #endregion Properties

        #region Constructors

        private IpDisplaysApi(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Timeout = TimeSpan.FromMilliseconds(16000);
        }

        public IpDisplaysApi(string ip, ILogger logger = null) : this(logger)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }
            Uri = new Uri($"http://{ip}/soap1.wsdl");
        }

        #endregion Constructors

        #region Helpers

        public SignSvrSoapPortClient GetClient()
        {
            var binding = new BasicHttpBinding
            {
                MaxBufferSize = int.MaxValue,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = true,
                CloseTimeout = Timeout,
                OpenTimeout = Timeout,
                ReceiveTimeout = Timeout,
                SendTimeout = Timeout
            };
            var endpointAddress = new EndpointAddress(Uri);
            return new SignSvrSoapPortClient(binding, endpointAddress);
        }

        #endregion Helpers

        #region Api Methods

        public async Task<bool> SetLayout(string layoutName, bool enabled)
        {
            using var client = GetClient();
            try
            {
                _ = await client.SetLayoutStateAsync(layoutName, enabled ? 1 : 0);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{name} Failed to Execute", nameof(SetLayout));
            }
            return false;
        }

        public async Task<bool> UpdateDataItem(string name, string value)
        {
            using var client = GetClient();
            try
            {
                _ = await client.UpdateDataItemValueByNameAsync(name, value);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{name} Failed to Execute", nameof(UpdateDataItem));
            }
            return false;
        }

        #endregion Api Methods

        #region Config

        public void SetTimeout(int miliseconds) =>
            Timeout = TimeSpan.FromMilliseconds(miliseconds);

        #endregion Config

    }
}
