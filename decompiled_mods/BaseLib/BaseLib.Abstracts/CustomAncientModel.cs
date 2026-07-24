using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Abstracts;

public abstract class CustomAncientModel : AncientEventModel, ICustomModel, ILocalizationProvider
{
	private readonly bool _logDialogueLoad;

	private OptionPools? _optionPools;

	public virtual List<(string, string)>? Localization => null;

	protected abstract OptionPools MakeOptionPools { get; }

	public OptionPools OptionPools
	{
		get
		{
			if (_optionPools == null)
			{
				_optionPools = MakeOptionPools;
			}
			return _optionPools;
		}
	}

	public override IEnumerable<EventOption> AllPossibleOptions => OptionPools.AllOptions.SelectMany((AncientOption option) => option.AllVariants.Select((RelicModel relic) => ((AncientEventModel)this).RelicOption(relic, "INITIAL", (string)null)));

	public virtual string? CustomScenePath => null;

	public virtual string? CustomMapIconPath => null;

	public virtual string? CustomMapIconOutlinePath => null;

	public virtual string? CustomRunHistoryIconPath => null;

	public virtual string? CustomRunHistoryIconOutlinePath => null;

	private string FirstVisit => ((AbstractModel)this).Id.Entry + ".talk.firstvisitEver.0-0.ancient";

	public CustomAncientModel(bool autoAdd = true, bool logDialogueLoad = false)
	{
		if (autoAdd)
		{
			CustomContentDictionary.AddAncient(this);
		}
		_logDialogueLoad = logDialogueLoad;
	}

	public virtual bool IsValidForAct(ActModel act)
	{
		return true;
	}

	public virtual bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
	{
		return false;
	}

	protected override void AfterCloned()
	{
		((EventModel)this).AfterCloned();
		_optionPools = null;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (from option in OptionPools.Roll(((EventModel)this).Rng, (AncientEventModel)(object)this)
			select ((AncientEventModel)this).RelicOption(option.ModelForOption, "INITIAL", (string)null)).ToList();
	}

	public static WeightedList<AncientOption> MakePool(params RelicModel[] options)
	{
		WeightedList<AncientOption> weightedList = new WeightedList<AncientOption>();
		foreach (AncientOption item in options.Select((RelicModel model) => (AncientOption)model))
		{
			weightedList.Add(item);
		}
		return weightedList;
	}

	public static WeightedList<AncientOption> MakePool(params AncientOption[] options)
	{
		WeightedList<AncientOption> weightedList = new WeightedList<AncientOption>();
		foreach (AncientOption item in options)
		{
			weightedList.Add(item);
		}
		return weightedList;
	}

	public static AncientOption AncientOption<T>(int weight = 1, Func<T, RelicModel>? relicPrep = null, Func<T, IEnumerable<RelicModel>>? makeAllVariants = null) where T : RelicModel
	{
		return new AncientOption<T>(weight)
		{
			ModelPrep = relicPrep,
			Variants = makeAllVariants
		};
	}

	public override IEnumerable<string> GetAssetPaths(IRunState runState)
	{
		string customScenePath = CustomScenePath;
		if (customScenePath == null)
		{
			return ((EventModel)this).GetAssetPaths(runState);
		}
		return new _003C_003Ez__ReadOnlySingleElementList<string>(customScenePath);
	}

	protected override AncientDialogueSet DefineDialogues()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		StringBuilder stringBuilder = (_logDialogueLoad ? new StringBuilder("Prepping dialogue for ancient '" + ((AbstractModel)this).Id.Entry + "'") : null);
		string[] array = new string[1];
		string value = (array[0] = AncientDialogueUtil.SfxPath(FirstVisit));
		AncientDialogue firstVisitEverDialogue = new AncientDialogue(array);
		if (stringBuilder != null)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
			handler.AppendLiteral("First visit with sfx '");
			handler.AppendFormatted(value);
			handler.AppendLiteral("'");
			stringBuilder2.AppendLine(ref handler);
		}
		Dictionary<string, IReadOnlyList<AncientDialogue>> dictionary = new Dictionary<string, IReadOnlyList<AncientDialogue>>();
		foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
		{
			string baseKey = AncientDialogueUtil.BaseLocKey(((AbstractModel)this).Id.Entry, ((AbstractModel)allCharacter).Id.Entry);
			dictionary[((AbstractModel)allCharacter).Id.Entry] = AncientDialogueUtil.GetDialoguesForKey("ancients", baseKey, stringBuilder);
		}
		AncientDialogueSet val = new AncientDialogueSet();
		val.set_FirstVisitEverDialogue(firstVisitEverDialogue);
		val.set_CharacterDialogues(dictionary);
		val.set_AgnosticDialogues((IReadOnlyList<AncientDialogue>)AncientDialogueUtil.GetDialoguesForKey("ancients", AncientDialogueUtil.BaseLocKey(((AbstractModel)this).Id.Entry, "ANY"), stringBuilder));
		if (stringBuilder != null)
		{
			BaseLibMain.Logger.Info(stringBuilder.ToString(), 1);
		}
		return val;
	}
}
