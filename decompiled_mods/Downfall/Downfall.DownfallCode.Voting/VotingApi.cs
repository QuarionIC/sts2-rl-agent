using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Voting;

[ScriptPath("res://DownfallCode/Voting/VotingApi.cs")]
public class VotingApi : Node
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	private const string BaseUrl = "https://njndpcayvomsutxgrezp.supabase.co/rest/v1";

	private const string Key = "sb_publishable_YkFWtYobqAQ9CZY7VzSHNg_dEXVlP1N";

	public static VotingApi Instance { get; private set; }

	private static string[] Headers => new string[3] { "apikey: sb_publishable_YkFWtYobqAQ9CZY7VzSHNg_dEXVlP1N", "Authorization: Bearer sb_publishable_YkFWtYobqAQ9CZY7VzSHNg_dEXVlP1N", "Content-Type: application/json" };

	public override void _Ready()
	{
		Instance = this;
	}

	public async Task<List<ArtEntry>?> GetSubmissions(string categoryId)
	{
		string id = UserIdentity.Id;
		if (id == null)
		{
			GD.PrintErr("CastVote skipped: no SteamID (Steam not running)");
			return null;
		}
		Dictionary val = new Dictionary();
		val.Add(Variant.op_Implicit("p_category"), Variant.op_Implicit(long.Parse(categoryId)));
		val.Add(Variant.op_Implicit("p_user"), Variant.op_Implicit(id));
		string body = Json.Stringify(Variant.op_Implicit(val), "", true, false);
		var (num, text) = await Send("https://njndpcayvomsutxgrezp.supabase.co/rest/v1/rpc/submissions_for_user", (Method)2, body);
		if (num == 200)
		{
			return Parse(text);
		}
		GD.PrintErr($"GetSubmissions {num}: {text}");
		return null;
	}

	public async Task CastVote(long submissionId, int value)
	{
		string id = UserIdentity.Id;
		if (id == null)
		{
			GD.PrintErr("CastVote skipped: no SteamID (Steam not running)");
			return;
		}
		Dictionary val = new Dictionary();
		val.Add(Variant.op_Implicit("p_submission"), Variant.op_Implicit(submissionId));
		val.Add(Variant.op_Implicit("p_user"), Variant.op_Implicit(id));
		val.Add(Variant.op_Implicit("p_value"), Variant.op_Implicit(value));
		string body = Json.Stringify(Variant.op_Implicit(val), "", true, false);
		var (num, value2) = await Send("https://njndpcayvomsutxgrezp.supabase.co/rest/v1/rpc/cast_vote", (Method)2, body);
		if ((num < 200 || num > 299) ? true : false)
		{
			GD.PrintErr($"CastVote {num}: {value2}");
		}
	}

	public async Task ToggleFlag(long submissionId, string reason, bool on)
	{
		string id = UserIdentity.Id;
		if (id == null)
		{
			GD.PrintErr("ToggleFlag skipped: no SteamID (Steam not running)");
			return;
		}
		Dictionary val = new Dictionary();
		val.Add(Variant.op_Implicit("p_submission"), Variant.op_Implicit(submissionId));
		val.Add(Variant.op_Implicit("p_user"), Variant.op_Implicit(id));
		val.Add(Variant.op_Implicit("p_reason"), Variant.op_Implicit(reason));
		val.Add(Variant.op_Implicit("p_on"), Variant.op_Implicit(on));
		string body = Json.Stringify(Variant.op_Implicit(val), "", true, false);
		var (num, value) = await Send("https://njndpcayvomsutxgrezp.supabase.co/rest/v1/rpc/toggle_flag", (Method)2, body);
		if ((num < 200 || num > 299) ? true : false)
		{
			GD.PrintErr($"ToggleFlag {num}: {value}");
		}
	}

	private async Task<(long code, string body)> Send(string url, Method method, string body = "")
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		DownfallMainFile.Logger.Info($"[VotingApi] -> {method} {url} {body}", 1);
		HttpRequest http = new HttpRequest();
		((Node)this).AddChild((Node)(object)http, false, (InternalMode)0);
		Error val = http.Request(url, Headers, method, body);
		if ((int)val != 0)
		{
			((Node)http).QueueFree();
			DownfallMainFile.Logger.Info($"[VotingApi] <- request failed to send: {val}", 1);
			return (code: 0L, body: "request failed");
		}
		Variant[] array = await ((GodotObject)this).ToSignal((GodotObject)(object)http, SignalName.RequestCompleted);
		((Node)http).QueueFree();
		long num = ((Variant)(ref array[1])).AsInt64();
		string text = Encoding.UTF8.GetString(((Variant)(ref array[3])).AsByteArray());
		DownfallMainFile.Logger.Info($"[VotingApi] <- {num} {url} :: {text}", 1);
		return (code: num, body: text);
	}

	public async Task ClearVote(long submissionId)
	{
		string id = UserIdentity.Id;
		if (id == null)
		{
			GD.PrintErr("ClearVote skipped: no SteamID");
			return;
		}
		Dictionary val = new Dictionary();
		val.Add(Variant.op_Implicit("p_submission"), Variant.op_Implicit(submissionId));
		val.Add(Variant.op_Implicit("p_user"), Variant.op_Implicit(id));
		string body = Json.Stringify(Variant.op_Implicit(val), "", true, false);
		var (num, value) = await Send("https://njndpcayvomsutxgrezp.supabase.co/rest/v1/rpc/clear_vote", (Method)2, body);
		if ((num < 200 || num > 299) ? true : false)
		{
			GD.PrintErr($"ClearVote {num}: {value}");
		}
	}

	public async Task<List<ArtData>?> GetCategories()
	{
		var (num, text) = await Send("https://njndpcayvomsutxgrezp.supabase.co/rest/v1/categories?order=id", (Method)0);
		if (num != 200)
		{
			GD.PrintErr($"GetCategories {num}: {text}");
			return null;
		}
		Variant val = Json.ParseString(text);
		if ((long)((Variant)(ref val)).VariantType != 28)
		{
			return null;
		}
		return ((IEnumerable<Variant>)((Variant)(ref val)).AsGodotArray()).Select((Variant item) => ((Variant)(ref item)).AsGodotDictionary()).Select(delegate(Dictionary d)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Expected O, but got Unknown
			ArtData artData = new ArtData();
			Variant val2 = d[Variant.op_Implicit("id")];
			artData.Id = ((Variant)(ref val2)).AsInt64().ToString();
			val2 = d[Variant.op_Implicit("category")];
			string text2 = ((Variant)(ref val2)).AsString();
			val2 = d[Variant.op_Implicit("entry")];
			artData.ModelId = new ModelId(text2, ((Variant)(ref val2)).AsString());
			return artData;
		}).ToList();
	}

	private static List<ArtEntry>? Parse(string json)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I8
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		Variant val = Json.ParseString(json);
		if ((long)((Variant)(ref val)).VariantType != 28)
		{
			return null;
		}
		List<ArtEntry> list = new List<ArtEntry>();
		foreach (Variant item in ((Variant)(ref val)).AsGodotArray())
		{
			Variant current = item;
			Dictionary val2 = ((Variant)(ref current)).AsGodotDictionary();
			HashSet<string> hashSet = new HashSet<string>();
			Variant val3;
			if (val2.ContainsKey(Variant.op_Implicit("my_flags")))
			{
				val3 = val2[Variant.op_Implicit("my_flags")];
				foreach (Variant item2 in ((Variant)(ref val3)).AsGodotArray())
				{
					Variant current2 = item2;
					hashSet.Add(((Variant)(ref current2)).AsString());
				}
			}
			ArtEntry artEntry = new ArtEntry();
			val3 = val2[Variant.op_Implicit("id")];
			artEntry.Id = ((Variant)(ref val3)).AsInt64();
			val3 = val2[Variant.op_Implicit("image_url")];
			artEntry.ImagePath = ((Variant)(ref val3)).AsString();
			val3 = val2[Variant.op_Implicit("author")];
			artEntry.Author = ((Variant)(ref val3)).AsString();
			val3 = val2[Variant.op_Implicit("name")];
			artEntry.Name = ((Variant)(ref val3)).AsString();
			val3 = val2[Variant.op_Implicit("upvotes")];
			artEntry.Upvotes = ((Variant)(ref val3)).AsInt32();
			val3 = val2[Variant.op_Implicit("downvotes")];
			artEntry.Downvotes = ((Variant)(ref val3)).AsInt32();
			val3 = val2[Variant.op_Implicit("my_vote")];
			artEntry.MyVote = ((Variant)(ref val3)).AsInt32();
			artEntry.MyFlags = hashSet;
			list.Add(artEntry);
		}
		return list;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((Node)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((Node)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
	}
}
