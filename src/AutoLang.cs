using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using PlayerSettings;
using System.Globalization;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;

namespace CS2_AutoLang
{
    public class AutoLang : BasePlugin
	{
		private ISettingsApi? _PlayerSettingsAPI;
		private readonly PluginCapability<ISettingsApi?> _PlayerSettingsAPICapability = new("settings:nfcore");
		public override string ModuleName => "Auto Lang";
		public override string ModuleDescription => "Remembers the language selected by the player";
		public override string ModuleAuthor => "DarkerZ [RUS]";
		public override string ModuleVersion => "1.DZ.0";
		public override void OnAllPluginsLoaded(bool hotReload)
		{
			_PlayerSettingsAPI = _PlayerSettingsAPICapability.Get();
			if (_PlayerSettingsAPI == null)
				PrintToConsole("PlayerSettings core not found...");
		}
		public override void Load(bool hotReload)
		{
			RegisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
			RegisterEventHandler<EventPlayerDisconnect>(OnEventPlayerDisconnect);
		}
		public override void Unload(bool hotReload)
		{
			DeregisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
			DeregisterEventHandler<EventPlayerDisconnect>(OnEventPlayerDisconnect);
		}
		private HookResult OnEventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			GetLanguage(@event.Userid);
			return HookResult.Continue;
		}
		private HookResult OnEventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			SetLanguage(@event.Userid);
			return HookResult.Continue;
		}
		private void GetLanguage(CCSPlayerController? player)
		{
			if (_PlayerSettingsAPI != null && player != null && player.IsValid && !player.IsBot)
			{
				string? countrycode = GetPlayerCountryCode(player);
				string language = _PlayerSettingsAPI.GetPlayerSettingsValue(player, "lang", countrycode != null ? countrycode : CultureInfo.GetCultureInfo(CoreConfig.ServerLanguage).Name);
				Server.NextFrame(() =>
				{
					if (language != null)
					{
						if (language != null) player.ExecuteClientCommandFromServer($"css_lang {language}");
					}
				});
			}
		}
		private void SetLanguage(CCSPlayerController? player)
		{
			if (_PlayerSettingsAPI != null && player != null && player.IsValid && (player.Connected == PlayerConnectedState.PlayerConnected) && !player.IsBot)
			{
				string language = player.GetLanguage().Name;
				if (language != null) _PlayerSettingsAPI.SetPlayerSettingsValue(player, "lang", language);
			}
		}
		public string? GetPlayerCountryCode(CCSPlayerController player)
		{
			if (!File.Exists(Path.Combine(ModuleDirectory, "GeoLite2-Country.mmdb")))
			{
				PrintToConsole("GeoLite2-Country.mmdb not found in the plugin directory. You can download it from https://github.com/P3TERX/GeoLite.mmdb/releases");
				return null;
			}
			string? playerIp = player.IpAddress;

			if (playerIp == null)
				return null;

			string[] parts = playerIp.Split(':');
			string realIP = parts.Length == 2 ? parts[0] : playerIp;

			using DatabaseReader reader = new DatabaseReader(Path.Combine(ModuleDirectory, "GeoLite2-Country.mmdb"));
			{
				try
				{
					MaxMind.GeoIP2.Responses.CountryResponse response = reader.Country(realIP);
					return response.Country.IsoCode ?? null;
				}
				catch (AddressNotFoundException)
				{
					PrintToConsole($"The address {realIP} is not in the database.");
					return null;
				}
				catch (GeoIP2Exception ex)
				{
					PrintToConsole($"Error: {ex.Message}");
					return null;
				}
			}
		}
		static void PrintToConsole(string sValue)
		{
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("[");
			Console.ForegroundColor = (ConsoleColor)6;
			Console.Write("AutoLang");
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("] ");
			Console.ForegroundColor = (ConsoleColor)3;
			Console.WriteLine(sValue);
			Console.ResetColor();
		}
	}
}
