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
using System.Collections.ObjectModel;
using System.Reflection;
using ArchiSteamFarm.Web.GitHub;

namespace CommandAliasPlugin;

#pragma warning disable CA1812 // ASF uses this class during runtime
[UsedImplicitly]
internal sealed class CommandAliasPlugin : IGitHubPluginUpdates, IBotCommand2, IASF {
	public string Name => nameof(CommandAliasPlugin);
	public string RepositoryName => "CatPoweredPlugins/CommandAliasPlugin";
	public Version Version => typeof(CommandAliasPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	internal static List<Alias>? Aliases;

	public async Task<Uri?> GetTargetReleaseURL(Version asfVersion, string asfVariant, bool asfUpdate, bool stable, bool forced) {
		ArgumentNullException.ThrowIfNull(asfVersion);
		ArgumentException.ThrowIfNullOrEmpty(asfVariant);

		if (string.IsNullOrEmpty(RepositoryName)) {
			ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, (nameof(RepositoryName))));

			return null;
		}

		ReleaseResponse? releaseResponse = await GitHubService.GetLatestRelease(RepositoryName, stable).ConfigureAwait(false);

		if (releaseResponse == null) {
			return null;
		}

		Version newVersion = new(releaseResponse.Tag);

		if (!(Version.Major == newVersion.Major && Version.Minor == newVersion.Minor && Version.Build == newVersion.Build) && !(asfUpdate || forced)) {
			ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, "New {0} plugin version {1} is only compatible with latest ASF version", Name, newVersion));
			return null;
		}


		if (Version >= newVersion & !forced) {
			ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateNotFound, Name, Version, newVersion));

			return null;
		}

		if (releaseResponse.Assets.Count == 0) {
			ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateNoAssetFound, Name, Version, newVersion));

			return null;
		}

		ReleaseAsset? asset = await ((IGitHubPluginUpdates) this).GetTargetReleaseAsset(asfVersion, asfVariant, newVersion, releaseResponse.Assets).ConfigureAwait(false);

		if ((asset == null) || !releaseResponse.Assets.Contains(asset)) {
			ASF.ArchiLogger.LogGenericWarning(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateNoAssetFound, Name, Version, newVersion));

			return null;
		}

		ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, Strings.PluginUpdateFound, Name, Version, newVersion));

		return asset.DownloadURL;
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
				string command = alias.ParamNumber > 0 ? string.Format(CultureInfo.CurrentCulture, commandFormat, [.. args[1..alias.ParamNumber], Utilities.GetArgsAsText(message, alias.ParamNumber)]) : commandFormat;
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
				ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, "Alias configuration"));
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, "Alias configuration");
			}
		}

		return alias.AllResponses ? result : Strings.Done;


	}

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"{Name} by Rudokhvist. This plugin is cat-powered!");

		return Task.CompletedTask;
	}
}
#pragma warning restore CA1812 // ASF uses this class during runtime
