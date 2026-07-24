using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Automaton.AutomatonCode;
using Automaton.AutomatonCode.Vfx;
using Awakened.AwakenedCode;
using Awakened.AwakenedCode.Vfx;
using Champ.ChampCode;
using Champ.ChampCode.Vfx;
using Downfall.DownfallCode;
using Downfall.DownfallCode.Nodes;
using Downfall.DownfallCode.Utils.UI;
using Downfall.DownfallCode.Vfx;
using Downfall.DownfallCode.Voting;
using Godot;
using Guardian.GuardianCode;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Vfx;
using Hermit.HermitCode;
using Hermit.HermitCode.Vfx;
using Hexaghost.HexaghostCode;
using Hexaghost.HexaghostCode.Localization;
using Hexaghost.HexaghostCode.Vfx;
using SlimeBoss.SlimeBossCode;
using SlimeBoss.SlimeBossCode.Vfx;
using Snecko.SneckoCode;
using Snecko.SneckoCode.Vfx;

[assembly: IgnoresAccessChecksTo("sts2")]
[assembly: IgnoresAccessChecksTo("GodotSharp")]
[assembly: IgnoresAccessChecksTo("0Harmony")]
[assembly: IgnoresAccessChecksTo("BaseLib")]
[assembly: AssemblyCompany("Downfall")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.1.4.0")]
[assembly: AssemblyInformationalVersion("0.1.4+a30ce5928034d8e4087dc470e5bf218bbe1c7cd3")]
[assembly: AssemblyProduct("Downfall")]
[assembly: AssemblyTitle("Downfall")]
[assembly: AssemblyHasScripts(new Type[]
{
	typeof(DownfallMainFile),
	typeof(NCustomCardHolder),
	typeof(NCustomCombatCardPile),
	typeof(NCustomTopBarButton),
	typeof(NCustomTopBarDisplayElement),
	typeof(NBlurWaveParticle),
	typeof(NHemokinesisEffect),
	typeof(NHemokinesisParticle),
	typeof(NShockWaveVfx),
	typeof(NSpineMerchantCharacter),
	typeof(NStatusBar),
	typeof(NStatusPart),
	typeof(ScreenFlashEffect),
	typeof(NArtVotingCardContainer),
	typeof(NArtVotingContainer),
	typeof(NArtVotingRow),
	typeof(NArtVotingScreen),
	typeof(NVoteCard),
	typeof(VotingApi),
	typeof(AutomatonMainFile),
	typeof(NAutomatonCreatureVisuals),
	typeof(NAutomatonMerchantCharacter),
	typeof(NAutomatonSlot),
	typeof(NSequenceDisplay),
	typeof(NSlotRevealDisplay),
	typeof(NStashDisplay),
	typeof(AwakenedMainFile),
	typeof(NAwakenedCreatureVisuals),
	typeof(NAwakenedMerchantCharacter),
	typeof(NSpellbookDisplay),
	typeof(ChampMainFile),
	typeof(NChampCreatureVisuals),
	typeof(NChampMerchantCharacter),
	typeof(NChampStanceDisplay),
	typeof(StanceIconControl),
	typeof(CardGemDisplay),
	typeof(GuardianMainFile),
	typeof(NGemShootEffect),
	typeof(NGemUpgradeSelectScreen),
	typeof(NGuardianCreatureVisuals),
	typeof(NGuardianDisplay),
	typeof(NGuardianMerchantCharacter),
	typeof(NStasisSlot),
	typeof(HexaghostMainFile),
	typeof(RichTextAfterlife),
	typeof(NFire),
	typeof(NFireballEffect),
	typeof(NGhostflames),
	typeof(NHexaghostCreatureVisuals),
	typeof(NHexaghostMerchantCharacter),
	typeof(NHexaghostVisuals),
	typeof(HermitMainFile),
	typeof(NHermitCreatureVisuals),
	typeof(NHermitMerchantCharacter),
	typeof(SlimeBossMainFile),
	typeof(NSlimeBossCreatureVisuals),
	typeof(NSlimeBossMerchantCharacter),
	typeof(NSlimeCreatureVisuals),
	typeof(SneckoMainFile),
	typeof(NSneckoCharacterSelect),
	typeof(NSneckoCreatureVisuals),
	typeof(NSneckoMerchantCharacter)
})]
[assembly: AssemblyVersion("0.1.4.0")]
[module: RefSafetyRules(11)]
