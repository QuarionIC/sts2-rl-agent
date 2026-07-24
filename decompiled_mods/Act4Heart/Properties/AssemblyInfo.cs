using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Act4Heart;
using Act4Heart.Keys;
using Godot;

[assembly: AssemblyHasScripts(new Type[]
{
	typeof(NCreatureVisualsDolso),
	typeof(NSuperElitePoint),
	typeof(NFlameAnimationEffect)
})]
[assembly: IgnoresAccessChecksTo("0Harmony")]
[assembly: IgnoresAccessChecksTo("sts2")]
[assembly: AssemblyVersion("0.0.0.0")]
[module: RefSafetyRules(11)]
