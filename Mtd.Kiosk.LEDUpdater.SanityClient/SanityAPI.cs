using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanity.Linq;

namespace Mtd.Kiosk.LEDUpdater.SanityAPI;

public class SanityClient
{

	private readonly ILogger<SanityClient> _logger;
	private readonly SanityOptions _sanityOptions;

	#region Constructors

	public SanityClient(ILogger<SanityClient> logger, IOptions<SanityClientConfig> config)
	{
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));
		ArgumentNullException.ThrowIfNull(config?.Value, nameof(config));

        _sanityOptions = new SanityOptions
        {
            ProjectId = config.Value.ProjectId,
            Dataset = config.Value.Dataset,
            Token = config.Value.Token,
            UseCdn = config.Value.UseCdn,
            ApiVersion = config.Value.ApiVersion
        };

        _logger = logger;
	}

	#endregion Constructors

	#region Helpers

	public SanityDataContext GetSanityDataContext()
	{
		return new SanityDataContext(_sanityOptions);
	}

	#endregion Helpers


}
