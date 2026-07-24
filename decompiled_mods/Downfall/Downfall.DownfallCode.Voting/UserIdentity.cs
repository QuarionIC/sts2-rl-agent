using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;

namespace Downfall.DownfallCode.Voting;

public static class UserIdentity
{
	private static string? _id;

	private static TaskCompletionSource<string?>? _ticketTcs;

	private static Callback<GetTicketForWebApiResponse_t>? _cb;

	private static bool IsAvailable => SteamInitializer.Initialized;

	public static string? Id
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			if (_id != null)
			{
				return _id;
			}
			if (!IsAvailable)
			{
				return null;
			}
			_id = SteamUser.GetSteamID().m_SteamID.ToString();
			return _id;
		}
	}

	public static Task<string?> GetWebTicket()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (!IsAvailable)
		{
			return Task.FromResult<string>(null);
		}
		_ticketTcs = new TaskCompletionSource<string>();
		_cb = Callback<GetTicketForWebApiResponse_t>.Create((DispatchDelegate<GetTicketForWebApiResponse_t>)OnTicket);
		SteamUser.GetAuthTicketForWebApi("votingservice");
		return _ticketTcs.Task;
	}

	private static void OnTicket(GetTicketForWebApiResponse_t r)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		string result = BitConverter.ToString(r.m_rgubTicket, 0, r.m_cubTicket).Replace("-", "");
		_ticketTcs?.TrySetResult(result);
		_cb?.Dispose();
		_cb = null;
	}
}
