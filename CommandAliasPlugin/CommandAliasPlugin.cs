using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;
using JetBrains.Annotations;
using System.Globalization;
using ArchiSteamFarm.Web.GitHub.Data;
using ArchiSteamFarm.Steam.Data;
using System.Collections.ObjectModel;
using ArchiSteamFarm;
using System.Reflection;
using ArchiSteamFarm.NLog;
using static System.Collections.Specialized.BitVector32;

namespace CommandAliasPlugin;

#pragma warning disable CA1812 // ASF uses this class during runtime
[UsedImplicitly]
internal sealed class CommandAliasPlugin : IGitHubPluginUpdates, IBotCommand2, IASF {
	public string Name => nameof(CommandAliasPlugin);
	public string RepositoryName => "Rudokhvist/CommandAliasPlugin";
	public Version Version => typeof(CommandAliasPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	internal static List<Alias>? Aliases;

	public Task<ReleaseAsset?> GetTargetReleaseAsset(Version asfVersion, string asfVariant, Version newPluginVersion, IReadOnlyCollection<ReleaseAsset> releaseAssets) {
		ArgumentNullException.ThrowIfNull(asfVersion);
		ArgumentException.ThrowIfNullOrEmpty(asfVariant);
		ArgumentNullException.ThrowIfNull(newPluginVersion);

		if ((releaseAssets == null) || (releaseAssets.Count == 0)) {
			throw new ArgumentNullException(nameof(releaseAssets));
		}

		Collection<ReleaseAsset?> matches = [.. releaseAssets.Where(r => r.Name.Equals(Name + ".zip", StringComparison.OrdinalIgnoreCase))];

		if (matches.Count != 1) {
			return Task.FromResult((ReleaseAsset?) null);
		}

		ReleaseAsset? release = matches[0];

		return (Version.Major == newPluginVersion.Major && Version.Minor == newPluginVersion.Minor && Version.Build == newPluginVersion.Build) || asfVersion != Assembly.GetExecutingAssembly().GetName().Version
			? Task.FromResult(release)
			: Task.FromResult((ReleaseAsset?) null);
	}
	public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		if (additionalConfigProperties == null) {
			return Task.CompletedTask;
		}

		foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
			if (configProperty.Key.Equals("Aliases", StringComparison.OrdinalIgnoreCase) && configProperty.Value.ValueKind == JsonValueKind.Array) {
				try {
					Aliases = configProperty.Value.ToJsonObject<List<Alias>>();
					ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, Strings.PluginLoaded, "Aliases configuration"));
				} catch (Exception) {
					ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, "Aliases configuration"));
				}
			}
		}

		return Task.CompletedTask;
	}
	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		if (Aliases == null || Aliases.Count == 0) {
			return null;
		}
		List<Alias> foundAliases = [.. Aliases.Where(alias => !string.IsNullOrEmpty(alias.AliasName) && alias.AliasName.Equals(args[0], StringComparison.OrdinalIgnoreCase) && alias.ParamNumber <= (args.Length - 1)).OrderByDescending(alias => alias.ParamNumber)];

		if (foundAliases.Count == 0) {
			return null;
		}
		Alias alias = foundAliases[0];

		if (args.Length > 1 && alias.ParamNumber == 0) {
			return null;
		}

		if (alias.Commands == null || alias.Commands.Length == 0) {
			return null;
		}

		if (!Enum.TryParse(alias.Access, out EAccess commandAccess)) {
			commandAccess = EAccess.Master;
		}

		if (access < commandAccess) {
			return null;
		}
		string result = "";


		foreach (string commandFormat in alias.Commands) {
			try {
				string command = string.Format(CultureInfo.CurrentCulture, commandFormat, [.. args[1..alias.ParamNumber], Utilities.GetArgsAsText(message, alias.ParamNumber)]);
				switch (command.Split(" ")[0].ToUpper(CultureInfo.CurrentCulture)) { //just in case of future increase of meta-commands
					case "wait":
						if (int.TryParse(command.Split(" ")?.ElementAt(1), out int waitmsec)) {
							await Task.Delay(waitmsec).ConfigureAwait(false);
						}
						break;
					default:
						result += await bot.Commands.Response(EAccess.Owner, command, steamID).ConfigureAwait(false) + Environment.NewLine;
						break;
				}
			} catch (Exception) {
				ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, "Aliase configuration"));
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, "Aliase configuration");
			}
		}

		return alias.AllResponses ? result : Strings.Done;


	}

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"$\"{{Name}} by Rudokhvist");

		return Task.CompletedTask;
	}
}
#pragma warning restore CA1812 // ASF uses this class during runtime
