using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Abstracts;

public delegate T CreateRewardFromSave<out T>(SerializableReward save, Player player) where T : CustomReward;
