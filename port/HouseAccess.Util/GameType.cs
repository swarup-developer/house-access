using System;
using System.Collections.Generic;
using System.Reflection;

namespace HouseAccess.Util;

/// <summary>
/// Resolves game (interop) types by name at runtime instead of via typeof(...).
///
/// Older GOG interop placed game types in Assembly-CSharp.dll. Current Steam
/// builds split them between EekCharacterEngine, EekUI, EekEvents and HouseParty.
/// A JITted typeof() for a type that no longer exists throws TypeLoadException,
/// and when that happens while a method's argument list is evaluated - as in
/// Patches.Apply - it kills everything the method would have done afterwards,
/// including every per-frame driver host. Looking types up by name turns each
/// miss into a per-patch skip. Strongly typed bridge references still require
/// rebuilding against the installed game's assemblies; this lookup cannot fix them.
/// </summary>
public static class GameType
{
	private static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();

	// Interop namespace spellings to try around a bare name. Older decompiler-era
	// interop dumps and newer generator builds do not always agree on the prefix.
	private static readonly string[] Namespaces = new string[9]
	{
		"Il2Cpp",
		"Il2CppEekUI",
		"Il2CppEekCharacterEngine",
		"Il2CppEekCharacterEngine.Support",
		"Il2CppEekCharacterEngine.Canvas",
		"Il2CppEekCharacterEngine.Interaction",
		"Il2CppEekCharacterEngine.Dialogues",
		"Il2CppHouseParty",
		"Il2CppHouseParty.Interface"
	};

	/// <summary>
	/// Resolve a game type by its bare name (e.g. "DialogueUI") or an already
	/// qualified name. Returns null when the type is not present in this game
	/// build; the result is cached, including the miss.
	/// </summary>
	public static Type Of(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		if (Cache.TryGetValue(name, out Type cached))
		{
			return cached;
		}
		Type type = Find(name);
		Cache[name] = type;
		if (type == null)
		{
			Log.Info("GameType: '" + name + "' is not present in this game build; related hooks are skipped.");
		}
		return type;
	}

	private static Type Find(string name)
	{
		try
		{
			// An already-qualified name (e.g. "EekUI.DialogueUI"): try the exact
			// spelling against every loaded assembly FIRST, in every known interop
			// namespace spelling, and only then fall back to a bare-name match. The
			// exact pass runs before the bare pass so an ambiguous short name (like
				// GameManager) can never grab a type from the wrong namespace.
			if (name.IndexOf('.') >= 0)
			{
				Type exact = Scan(AppDomain.CurrentDomain.GetAssemblies(), (string full) => full == name);
				if (exact != null)
				{
					return exact;
				}
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			string[] namespaces = Namespaces;
			foreach (string ns in namespaces)
			{
				Type qualified = Scan(assemblies, (string full) => full == ns + "." + name);
				if (qualified != null)
				{
					return qualified;
				}
			}
			// Last resort: the bare name in any namespace, prefix or not.
			return Scan(assemblies, delegate(string full)
			{
				if (full == name)
				{
					return true;
				}
				return full.EndsWith("." + name, StringComparison.Ordinal);
			});
		}
		catch
		{
		}
		return null;
	}

	private static Type Scan(Assembly[] assemblies, Func<string, bool> match)
	{
		foreach (Assembly assembly in assemblies)
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch
			{
				continue;
			}
			foreach (Type type in types)
			{
				string fullName = type.FullName;
				if (fullName != null && match(fullName))
				{
					return type;
				}
			}
		}
		return null;
	}
}
