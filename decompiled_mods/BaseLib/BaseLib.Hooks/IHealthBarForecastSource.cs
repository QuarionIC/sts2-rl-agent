using System.Collections.Generic;

namespace BaseLib.Hooks;

public interface IHealthBarForecastSource
{
	IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context);
}
