using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Localization;

public interface IExtraDescriptionSource
{
	IEnumerable<string> GetLines(CardModel card);
}
