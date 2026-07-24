using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Config.UI;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Config;

public class SimpleModConfig : ModConfig
{
	private sealed class DividerTracker
	{
		private readonly List<(Control Divider, Control Upper, Control Lower)> _pairs = new List<(Control, Control, Control)>();

		private Control? _pendingDivider;

		private Control? _pendingUpperRow;

		public void CompletePending(Control lowerRow)
		{
			if (_pendingDivider != null)
			{
				_pairs.Add((_pendingDivider, _pendingUpperRow, lowerRow));
				_pendingDivider = null;
			}
		}

		public void AddPending(Control divider, Control upperRow)
		{
			_pendingDivider = divider;
			_pendingUpperRow = upperRow;
		}

		public void UpdateAll()
		{
			foreach (var (val, val2, val3) in _pairs)
			{
				((CanvasItem)val).Visible = ((CanvasItem)val2).Visible && ((CanvasItem)val3).Visible;
			}
		}
	}

	private sealed class SectionTracker
	{
		private readonly Dictionary<Control, List<Control>> _headerRows = new Dictionary<Control, List<Control>>();

		public Control? CurrentHeader { get; private set; }

		public string? CurrentSectionName { get; private set; }

		public void MaybeStartNew(string? sectionName, Func<string, bool, bool, NConfigCollapsibleSection> createSection, Control targetContainer, ref Control currentContainer)
		{
			if (sectionName != null && !(sectionName == CurrentSectionName))
			{
				NConfigCollapsibleSection nConfigCollapsibleSection = createSection(sectionName, arg2: false, arg3: false);
				((Node)targetContainer).AddChild((Node)(object)nConfigCollapsibleSection, false, (InternalMode)0);
				currentContainer = (Control)(object)nConfigCollapsibleSection.ContentContainer;
				CurrentSectionName = sectionName;
				CurrentHeader = (Control?)(object)nConfigCollapsibleSection;
				_headerRows[CurrentHeader] = new List<Control>();
			}
		}

		public void RegisterRow(Control row)
		{
			if (CurrentHeader != null)
			{
				_headerRows[CurrentHeader].Add(row);
			}
		}

		public void UpdateHeaderVisibility(Control header)
		{
			((CanvasItem)header).Visible = _headerRows[header].Any((Control r) => ((CanvasItem)r).Visible);
		}

		public void UpdateAllHeaderVisibility()
		{
			foreach (var (header, _) in _headerRows)
			{
				UpdateHeaderVisibility(header);
			}
		}
	}

	protected readonly List<EventHandler> _configChangedHandlers = new List<EventHandler>();

	protected readonly List<Action> _configReloadedHandlers = new List<Action>();

	private static readonly NodePath selfNodePath = new NodePath(".");

	public override void SetupConfigUI(Control optionContainer)
	{
		BaseLibMain.Logger.Info("Setting up SimpleModConfig " + GetType().FullName, 1);
		GenerateOptionsForAllProperties(optionContainer);
		AddRestoreDefaultsButton(optionContainer);
		SetupFocusNeighbors(optionContainer);
	}

	protected void AddRestoreDefaultsButton(Control optionContainer)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		NConfigButton nConfigButton = CreateRawButtonControl(ModConfig.GetBaseLibLabelText("RestoreDefaultsButton"), async delegate
		{
			try
			{
				await ConfirmRestoreDefaults();
			}
			catch (Exception ex)
			{
				ModConfigLogger.Error("Unable to show restore confirmation dialog: " + ex.Message);
			}
		});
		((Node)nConfigButton).Name = StringName.op_Implicit("ResetDefaultsButton");
		((Control)nConfigButton).CustomMinimumSize = new Vector2(360f, ((Control)nConfigButton).CustomMinimumSize.Y);
		nConfigButton.SetColor(Color.FromHtml("#b03f3f".AsSpan()));
		CenterContainer val = new CenterContainer();
		((Node)val).Name = StringName.op_Implicit("ResetDefaultsButtonContainer");
		((Control)val).CustomMinimumSize = new Vector2(0f, 128f);
		((Node)val).AddChild((Node)(object)nConfigButton, false, (InternalMode)0);
		((Node)optionContainer).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	public async Task ConfirmRestoreDefaults()
	{
		NGenericPopup val = NGenericPopup.Create();
		if (val != null && NModalContainer.Instance != null)
		{
			NModalContainer.Instance.Add((Node)(object)val, true);
			if (await val.WaitForConfirmation(new LocString("settings_ui", "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.body"), new LocString("settings_ui", "BASELIB-RESTORE_MODCONFIG_CONFIRMATION.header"), new LocString("main_menu_ui", "GENERIC_POPUP.cancel"), new LocString("main_menu_ui", "GENERIC_POPUP.confirm")))
			{
				RestoreDefaultsNoConfirm();
			}
		}
	}

	protected NConfigOptionRow CreateToggleOption(PropertyInfo property, bool addHoverTip = false)
	{
		return CreateStandardOption(base.CreateRawTickboxControl, property, addHoverTip);
	}

	protected NConfigOptionRow CreateSliderOption(PropertyInfo property, bool addHoverTip = false)
	{
		return CreateStandardOption(base.CreateRawSliderControl, property, addHoverTip);
	}

	protected NConfigOptionRow CreateDropdownOption(PropertyInfo property, bool addHoverTip = false)
	{
		return CreateStandardOption(base.CreateRawDropdownControl, property, addHoverTip);
	}

	protected NConfigOptionRow CreateLineEditOption(PropertyInfo property, bool addHoverTip = false)
	{
		return CreateStandardOption(base.CreateRawLineEditControl, property, addHoverTip);
	}

	protected NConfigOptionRow CreateColorPickerOption(PropertyInfo property, bool addHoverTip = false)
	{
		return CreateStandardOption(base.CreateRawColorPickerControl, property, addHoverTip);
	}

	protected NConfigOptionRow CreateButton(string rowLabelKey, string buttonLabelKey, Action onPressed, bool addHoverTip = false)
	{
		NConfigButton nConfigButton = CreateRawButtonControl(GetLabelText(buttonLabelKey), onPressed);
		MegaRichTextLabel label = ModConfig.CreateRawLabelControl(GetLabelText(rowLabelKey), 28);
		NConfigOptionRow nConfigOptionRow = new NConfigOptionRow(base.ModPrefix, rowLabelKey, (Control)(object)label, (Control)(object)nConfigButton);
		((Control)(object)nConfigButton).ClearFocusNeighbors();
		if (addHoverTip)
		{
			nConfigOptionRow.AddHoverTip();
		}
		return nConfigOptionRow;
	}

	protected MarginContainer CreateSectionHeader(string labelName, bool alignToTop, bool centered)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		MegaRichTextLabel val = CreateRawHeaderLabel(labelName, alignToTop, centered);
		MarginContainer val2 = new MarginContainer
		{
			Name = StringName.op_Implicit("Container_" + labelName.Replace(" ", ""))
		};
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), (!alignToTop) ? 16 : 0);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 16);
		((Node)val2).AddChild((Node)(object)val, false, (InternalMode)0);
		return val2;
	}

	protected MarginContainer CreateSectionHeader(string labelName, bool alignToTop = false)
	{
		return CreateSectionHeader(labelName, alignToTop, centered: true);
	}

	protected NConfigCollapsibleSection CreateCollapsibleSection(string labelName, bool alignToTop = false, bool collapsedByDefault = false)
	{
		MegaRichTextLabel label = CreateRawHeaderLabel(labelName, alignToTop, centered: false);
		return NConfigCollapsibleSection.Create(labelName, (RichTextLabel)(object)label, alignToTop, collapsedByDefault);
	}

	protected MegaRichTextLabel CreateRawHeaderLabel(string labelName, bool alignToTop, bool centered)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		MegaRichTextLabel val = ModConfig.CreateRawLabelControl("[b]" + GetLabelText(labelName) + "[/b]", 40);
		((Node)val).Name = StringName.op_Implicit("SectionLabel_" + labelName.Replace(" ", ""));
		((Control)val).CustomMinimumSize = new Vector2(0f, 64f);
		((Control)val).SizeFlagsHorizontal = (SizeFlags)(centered ? 4 : 0);
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((RichTextLabel)val).FitContent = true;
		((RichTextLabel)val).AutowrapMode = (AutowrapMode)0;
		if (alignToTop)
		{
			((RichTextLabel)val).VerticalAlignment = (VerticalAlignment)0;
		}
		return val;
	}

	protected NConfigOptionRow CreateStandardOption(Func<PropertyInfo, Control> controlCreator, PropertyInfo property, bool addHoverTip = false)
	{
		Control val = controlCreator(property);
		MegaRichTextLabel label = ModConfig.CreateRawLabelControl(GetLabelText(property.Name), 28);
		NConfigOptionRow nConfigOptionRow = new NConfigOptionRow(base.ModPrefix, property.Name, (Control)(object)label, val);
		val.ClearFocusNeighbors();
		if (addHoverTip)
		{
			nConfigOptionRow.AddHoverTip();
		}
		return nConfigOptionRow;
	}

	protected NConfigOptionRow GenerateOptionFromProperty(PropertyInfo property)
	{
		Type propertyType = property.PropertyType;
		bool flag = property.GetCustomAttribute<ConfigColorPickerAttribute>() != null;
		NConfigOptionRow nConfigOptionRow;
		if (propertyType == typeof(bool))
		{
			nConfigOptionRow = CreateToggleOption(property);
		}
		else if (propertyType == typeof(Color) || (propertyType == typeof(string) && flag))
		{
			nConfigOptionRow = CreateColorPickerOption(property);
		}
		else if (propertyType == typeof(string))
		{
			nConfigOptionRow = CreateLineEditOption(property);
		}
		else if (NConfigSlider.SupportedTypes.Contains(propertyType))
		{
			nConfigOptionRow = CreateSliderOption(property);
		}
		else
		{
			if (!propertyType.IsEnum)
			{
				throw new NotSupportedException("Type " + propertyType.FullName + " is not supported by SimpleModConfig.");
			}
			nConfigOptionRow = CreateDropdownOption(property);
		}
		AddHoverTipToOptionRowIfEnabled(nConfigOptionRow, property);
		return nConfigOptionRow;
	}

	protected NConfigOptionRow GenerateButtonRowFromMethod(MethodInfo method)
	{
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		ConfigButtonAttribute configButtonAttribute = method.GetCustomAttribute<ConfigButtonAttribute>() ?? throw new ArgumentException("GenerateOptionFromMethod called on " + method.Name + " but it lacks a [ConfigButton] attribute.");
		ParameterInfo[] parameters = method.GetParameters();
		foreach (ParameterInfo param in parameters)
		{
			ResolveButtonArgument(param, null);
		}
		NConfigOptionRow optionRow = null;
		Action onPressed = delegate
		{
			try
			{
				object[] parameters2 = (from param2 in method.GetParameters()
					select ResolveButtonArgument(param2, optionRow)).ToArray();
				method.Invoke(method.IsStatic ? null : this, parameters2);
			}
			catch (Exception value)
			{
				BaseLibMain.Logger.Error($"Error executing [ConfigButton] method {method.Name}:\n{value}", 1);
			}
			ConfigReloaded();
			ModConfig.ShowAndClearPendingErrors();
		};
		optionRow = CreateButton(method.Name, configButtonAttribute.ButtonLabelKey, onPressed);
		if (optionRow.SettingControl is NConfigButton nConfigButton)
		{
			nConfigButton.SetColor(Color.FromHtml(configButtonAttribute.Color.AsSpan()));
		}
		AddHoverTipToOptionRowIfEnabled(optionRow, method);
		return optionRow;
	}

	protected object ResolveButtonArgument(ParameterInfo param, NConfigOptionRow? optionRow)
	{
		Type parameterType = param.ParameterType;
		if (typeof(ModConfig).IsAssignableFrom(parameterType))
		{
			return this;
		}
		if (parameterType == typeof(NConfigOptionRow))
		{
			return optionRow;
		}
		if (parameterType == typeof(NConfigButton))
		{
			return optionRow?.SettingControl;
		}
		throw new ArgumentException($"Unsupported parameter type '{parameterType.Name}' for method {param.Member.Name}.");
	}

	protected void AddHoverTipToOptionRowIfEnabled(NConfigOptionRow row, MemberInfo member)
	{
		ConfigHoverTipAttribute? customAttribute = member.GetCustomAttribute<ConfigHoverTipAttribute>();
		bool flag = GetType().GetCustomAttribute<ConfigHoverTipsByDefaultAttribute>() != null;
		if (customAttribute?.Enabled ?? flag)
		{
			row.AddHoverTip();
		}
	}

	protected void GenerateOptionsForAllProperties(Control targetContainer)
	{
		SectionTracker sections = new SectionTracker();
		DividerTracker dividers = new DividerTracker();
		Control currentContainer = targetContainer;
		List<MemberInfo> filteredMembers = GetFilteredMembers(GetType());
		for (int i = 0; i < filteredMembers.Count; i++)
		{
			MemberInfo memberInfo = filteredMembers[i];
			MemberInfo memberInfo2 = ((i < filteredMembers.Count - 1) ? filteredMembers[i + 1] : null);
			string sectionName = memberInfo.GetCustomAttribute<ConfigSectionAttribute>()?.Name;
			sections.MaybeStartNew(sectionName, CreateCollapsibleSection, targetContainer, ref currentContainer);
			NConfigOptionRow nConfigOptionRow2;
			try
			{
				NConfigOptionRow nConfigOptionRow;
				if (!(memberInfo is PropertyInfo property))
				{
					if (!(memberInfo is MethodInfo method))
					{
						throw new UnreachableException("Invalid type that should have been filtered out");
					}
					nConfigOptionRow = GenerateButtonRowFromMethod(method);
				}
				else
				{
					nConfigOptionRow = GenerateOptionFromProperty(property);
				}
				nConfigOptionRow2 = nConfigOptionRow;
			}
			catch (NotSupportedException ex)
			{
				BaseLibMain.Logger.Error("Not creating UI for unsupported property '" + memberInfo.Name + "': " + ex.Message, 1);
				continue;
			}
			((Node)nConfigOptionRow2).UniqueNameInOwner = true;
			((Node)currentContainer).AddChild((Node)(object)nConfigOptionRow2, false, (InternalMode)0);
			((Node)nConfigOptionRow2).Owner = (Node)(object)targetContainer;
			dividers.CompletePending((Control)(object)nConfigOptionRow2);
			sections.RegisterRow((Control)(object)nConfigOptionRow2);
			Action rowVisibilityUpdater = BuildVisibilityUpdater(memberInfo, (Control)(object)nConfigOptionRow2);
			Action triggerVisibilityUpdate = null;
			if (rowVisibilityUpdater != null)
			{
				Control headerForThisRow = sections.CurrentHeader;
				triggerVisibilityUpdate = delegate
				{
					rowVisibilityUpdater();
					dividers.UpdateAll();
					if (headerForThisRow != null)
					{
						sections.UpdateHeaderVisibility(headerForThisRow);
					}
				};
				EventHandler eventHandler = delegate
				{
					triggerVisibilityUpdate();
				};
				base.ConfigChanged += eventHandler;
				base.OnConfigReloaded += triggerVisibilityUpdate;
				_configChangedHandlers.Add(eventHandler);
				_configReloadedHandlers.Add(triggerVisibilityUpdate);
			}
			string text = memberInfo2?.GetCustomAttribute<ConfigSectionAttribute>()?.Name;
			bool flag = text == null || text == sections.CurrentSectionName;
			if (memberInfo2 != null && flag)
			{
				ColorRect val = ModConfig.CreateDividerControl();
				((Node)currentContainer).AddChild((Node)(object)val, false, (InternalMode)0);
				dividers.AddPending((Control)(object)val, (Control)(object)nConfigOptionRow2);
			}
			triggerVisibilityUpdate?.Invoke();
		}
		sections.UpdateAllHeaderVisibility();
		dividers.UpdateAll();
	}

	public static void SetupFocusNeighbors(Control optionContainer)
	{
		List<Control> list = FindAllFocusables((Node?)(object)optionContainer).ToList();
		if (list.Count == 0)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Control obj = list[i];
			Control obj2;
			if (i != 0)
			{
				obj2 = list[i - 1];
			}
			else
			{
				obj2 = list[list.Count - 1];
			}
			Control val = obj2;
			Control val2 = ((i == list.Count - 1) ? list[0] : list[i + 1]);
			obj.FocusNeighborTop = ((Node)val).GetPath();
			obj.FocusNeighborBottom = ((Node)val2).GetPath();
			obj.FocusNeighborLeft = selfNodePath;
			obj.FocusNeighborRight = selfNodePath;
		}
	}

	private List<MemberInfo> GetFilteredMembers(Type type)
	{
		return type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(IsVisibleMember).OrderBy(GetSourceOrder)
			.ToList();
		static int GetSourceOrder(MemberInfo member)
		{
			if (!(member is MethodInfo { MetadataToken: var metadataToken }))
			{
				if (member is PropertyInfo propertyInfo)
				{
					return propertyInfo.GetMethod?.MetadataToken ?? propertyInfo.SetMethod?.MetadataToken ?? 0;
				}
				return 0;
			}
			return metadataToken;
		}
		bool IsVisibleMember(MemberInfo member)
		{
			if (member is PropertyInfo propertyInfo)
			{
				return ConfigProperties.Contains(propertyInfo) && propertyInfo.GetCustomAttribute<ConfigHideInUI>() == null;
			}
			if (member is MethodInfo element)
			{
				return element.GetCustomAttribute<ConfigButtonAttribute>() != null;
			}
			return false;
		}
	}

	private Action? BuildVisibilityUpdater(MemberInfo member, Control newRow)
	{
		ConfigVisibleIfAttribute customAttribute = member.GetCustomAttribute<ConfigVisibleIfAttribute>();
		if (customAttribute == null)
		{
			return null;
		}
		Func<bool> condition = BuildVisibilityCondition(customAttribute, member);
		if (condition == null)
		{
			return null;
		}
		return delegate
		{
			((CanvasItem)newRow).Visible = condition();
		};
	}

	private Func<bool>? BuildVisibilityCondition(ConfigVisibleIfAttribute visibleIf, MemberInfo annotatedMember)
	{
		PropertyInfo property = GetType().GetProperty(visibleIf.TargetName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null)
		{
			if (visibleIf.Args.Length != 0 || property.PropertyType == typeof(bool))
			{
				return BuildPropertyCondition(property, visibleIf, annotatedMember);
			}
			BaseLibMain.Logger.Error($"[ConfigVisibleIf] on '{annotatedMember.Name}': property '{visibleIf.TargetName}' is not a bool; at least one value to compare against is required.", 1);
			return null;
		}
		MethodInfo method = GetType().GetMethod(visibleIf.TargetName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (method != null && method.ReturnType == typeof(bool))
		{
			return BuildMethodCondition(method, visibleIf, annotatedMember);
		}
		BaseLibMain.Logger.Error($"[ConfigVisibleIf] on '{annotatedMember.Name}': no valid property or boolean method named '{visibleIf.TargetName}' found on {GetType().Name}.", 1);
		return null;
	}

	private static Func<bool>? BuildPropertyCondition(PropertyInfo prop, ConfigVisibleIfAttribute visibleIf, MemberInfo annotatedMember)
	{
		object?[] convertedArgs = Array.Empty<object>();
		if (visibleIf.Args.Length != 0)
		{
			Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
			try
			{
				convertedArgs = visibleIf.Args.Select((object arg) => (arg != null) ? ((!propType.IsEnum) ? Convert.ChangeType(arg, propType, CultureInfo.InvariantCulture) : Enum.ToObject(propType, arg)) : null).ToArray();
			}
			catch (Exception ex)
			{
				BaseLibMain.Logger.Error($"[ConfigVisibleIf] on '{annotatedMember.Name}': could not convert arguments to '{propType.Name}': {ex.Message}", 1);
				return null;
			}
		}
		return delegate
		{
			object value = prop.GetValue(null);
			bool flag = ((value == null) ? convertedArgs.Any((object a) => a == null) : ((convertedArgs.Length != 0) ? convertedArgs.Any(value.Equals) : (value is bool && (bool)value)));
			return (!visibleIf.Invert) ? flag : (!flag);
		};
	}

	private Func<bool> BuildMethodCondition(MethodInfo method, ConfigVisibleIfAttribute visibleIf, MemberInfo annotatedMember)
	{
		Queue<object?> argsQueue = new Queue<object>(visibleIf.Args);
		object[] preResolvedArgs = (from param in method.GetParameters()
			select ResolveVisibilityMethodArgument(param, annotatedMember, argsQueue)).ToArray();
		return delegate
		{
			try
			{
				bool flag = (bool)method.Invoke(method.IsStatic ? null : this, preResolvedArgs);
				return visibleIf.Invert ? (!flag) : flag;
			}
			catch (Exception value)
			{
				BaseLibMain.Logger.Error($"[ConfigVisibleIf] error invoking '{method.Name}':\n{value}", 1);
				return true;
			}
		};
	}

	protected object? ResolveVisibilityMethodArgument(ParameterInfo param, MemberInfo memberInfo, Queue<object?> argsQueue)
	{
		Type type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
		if (typeof(ModConfig).IsAssignableFrom(type))
		{
			return this;
		}
		if (type == typeof(MemberInfo))
		{
			return memberInfo;
		}
		if (type == typeof(PropertyInfo))
		{
			if (memberInfo is PropertyInfo result)
			{
				return result;
			}
			throw new ArgumentException($"Visibility method '{param.Member.Name}' asks for a PropertyInfo, but was applied to a Button ('{memberInfo.Name}'). Change the parameter to " + "MemberInfo to support both.");
		}
		if (type == typeof(MethodInfo))
		{
			if (memberInfo is MethodInfo result2)
			{
				return result2;
			}
			throw new ArgumentException($"Visibility method '{param.Member.Name}' asks for a MethodInfo, but was applied to a Property ('{memberInfo.Name}'). Change the parameter to " + "MemberInfo to support both.");
		}
		if (!argsQueue.TryDequeue(out object result3))
		{
			throw new ArgumentException($"Method '{param.Member.Name}' requires more arguments than provided in the [ConfigVisibleIf] attribute, and parameter '{param.Name}' is not an auto-injectable type.");
		}
		try
		{
			return (result3 != null) ? Convert.ChangeType(result3, type, CultureInfo.InvariantCulture) : null;
		}
		catch (Exception innerException)
		{
			throw new ArgumentException($"Cannot convert [ConfigVisibleIf] argument '{result3}' to expected type '{type.Name}' for method {param.Member.Name}.", innerException);
		}
	}

	public void ClearUIEventHandlers()
	{
		foreach (EventHandler configChangedHandler in _configChangedHandlers)
		{
			base.ConfigChanged -= configChangedHandler;
		}
		foreach (Action configReloadedHandler in _configReloadedHandlers)
		{
			base.OnConfigReloaded -= configReloadedHandler;
		}
		_configChangedHandlers.Clear();
		_configReloadedHandlers.Clear();
	}

	private static IEnumerable<Control> FindAllFocusables(Node? node)
	{
		if (node == null)
		{
			yield break;
		}
		Control val = (Control)(object)((node is Control) ? node : null);
		if (val != null && (long)val.FocusMode == 2)
		{
			yield return val;
			yield break;
		}
		foreach (Node child in node.GetChildren(false))
		{
			foreach (Control item in FindAllFocusables(child))
			{
				yield return item;
			}
		}
	}
}
