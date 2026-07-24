using System.Collections.Generic;

namespace BaseLib.Abstracts;

public record CharacterLoc(string Title, string TitleObject, string Description, string PronounObject, string PronounSubject, string PronounPossessive, string PossessiveAdjective, string AromaPrinciple, string EndTurnPingAlive, string EndTurnPingDead, string EventDeathPrevention, string GoldMonologue, string CardsModifierTitle, string CardsModifierDescription, params (string, string)[] ExtraLoc)
{
	public static implicit operator List<(string, string)>(CharacterLoc loc)
	{
		List<(string, string)> list = new List<(string, string)>();
		list.Add(("title", loc.Title));
		list.Add(("titleObject", loc.TitleObject));
		list.Add(("description", loc.Description));
		list.Add(("pronounObject", loc.PronounObject));
		list.Add(("pronounSubject", loc.PronounSubject));
		list.Add(("pronounPossessive", loc.PronounPossessive));
		list.Add(("possessiveAdjective", loc.PossessiveAdjective));
		list.Add(("aromaPrinciple", loc.AromaPrinciple));
		list.Add(("banter.alive.endTurnPing", loc.EndTurnPingAlive));
		list.Add(("banter.dead.endTurnPing", loc.EndTurnPingDead));
		list.Add(("eventDeathPrevention", loc.EventDeathPrevention));
		list.Add(("goldMonologue", loc.GoldMonologue));
		list.Add(("cardsModifierTitle", loc.CardsModifierTitle));
		list.Add(("cardsModifierDescription", loc.CardsModifierDescription));
		list.AddRange(loc.ExtraLoc);
		return list;
	}
}
