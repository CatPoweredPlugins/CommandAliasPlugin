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

namespace CommandAliasPlugin;

#pragma warning disable CA1812 // ASF uses this class during runtime
[UsedImplicitly]
internal sealed class CommandAliasPlugin : IGitHubPluginUpdates, IBotCommand2, IASF {
	public string Name => nameof(CommandAliasPlugin);
	public string RepositoryName => "Rudokhvist/CommandAliasPlugin";
	public Version Version => typeof(CommandAliasPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	internal static List<Alias>? Aliases;

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
			string command = commandFormat;
			for (int i = 1; i < alias.ParamNumber; i++) {
				command = command.Replace($"%{i}", args[i], StringComparison.OrdinalIgnoreCase);
			}
			command = command.Replace($"%{alias.ParamNumber}", Utilities.GetArgsAsText(message, alias.ParamNumber), StringComparison.OrdinalIgnoreCase);
			if (command.Split(" ")[0].Equals("wait", StringComparison.OrdinalIgnoreCase)) {
				if (int.TryParse(command.Split(" ")?.ElementAt(1), out int waitmsec)) {
					await Task.Delay(waitmsec).ConfigureAwait(false);
				}
			} else {
				result += await bot.Commands.Response(EAccess.Owner, command, steamID).ConfigureAwait(false) + Environment.NewLine;
			}
		}

		return alias.AllResponses ? result : bot.Commands.FormatBotResponse(Strings.Done);

	}

	public Task OnLoaded() {
		ASF.ArchiLogger.LogGenericInfo($"$\"{{Name}} by Rudokhvist");

		return Task.CompletedTask;
	}
}
#pragma warning restore CA1812 // ASF uses this class during runtime
