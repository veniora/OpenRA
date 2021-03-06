#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Widgets;
using System.Net;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MainMenuButtonsLogic
	{
		Widget rootMenu;

		[ObjectCreator.UseCtor]
		public MainMenuButtonsLogic(Widget widget)
		{
			rootMenu = widget;
           
            Game.Settings.Campaign.SinglePlayer = false;
            Game.Settings.Save();
            
			Game.modData.WidgetLoader.LoadWidget( new WidgetArgs(), Ui.Root, "PERF_BG" );
            widget.Get<ButtonWidget>("MAINMENU_BUTTON_SINGLEPLAYER").OnClick = () => NewCampaign();
            widget.Get<ButtonWidget>("MAINMENU_BUTTON_CONTINUE").OnClick = () => ContinueCampaign();
            widget.Get<ButtonWidget>("MAINMENU_BUTTON_JOIN").OnClick = () => OpenGamePanel("JOINSERVER_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_CREATE").OnClick = () => OpenGamePanel("CREATESERVER_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_DIRECTCONNECT").OnClick = () => OpenGamePanel("DIRECTCONNECT_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_SETTINGS").OnClick = () => Ui.OpenWindow("SETTINGS_MENU");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_MUSIC").OnClick = () => Ui.OpenWindow("MUSIC_MENU");

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_MODS").OnClick = () =>
				Ui.OpenWindow("MODS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => {} },
					{ "onSwitch", RemoveShellmapUI }
				});

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_REPLAY_VIEWER").OnClick = () =>
				Ui.OpenWindow("REPLAYBROWSER_BG", new WidgetArgs()
				{
					{ "onExit", () => {} },
					{ "onStart", RemoveShellmapUI }
				});
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_QUIT").OnClick = () => Game.Exit();
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}

		void OpenGamePanel(string id)
		{
			Ui.OpenWindow(id, new WidgetArgs()
			{
				{ "onExit", () => {} },
				{ "openLobby", () => OpenLobbyPanel() }
			});
		}

		void OpenLobbyPanel()
		{
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
			{
				{ "onExit", () => { Game.Disconnect(); } },
				{ "onStart", RemoveShellmapUI },
				{ "addBots", false }
			});
		}


        void NewCampaign()
        {
            // Game settings for the first map chosen are saved and set
            Game.Settings.Campaign.Name = "Allies";         
            Game.Settings.Campaign.NextMission = 1;

            Game.Settings.Save();

            // Upon single player selection on main menu, the first Allies 01 map code is accessed
            OpenSinglePlayerPanel(OpenRA.FileFormats.Thirdparty.GammaCruxYamlHelper.getMap(1));
        }

        void ContinueCampaign()
        {
            OpenSinglePlayerPanel(OpenRA.FileFormats.Thirdparty.GammaCruxYamlHelper.getNextMap());
        }

        void OpenSinglePlayerPanel(string mapId)
        {
            // Save new settings
            Game.Settings.Server.Name = "Single Player";

            // Begin with Allies 01
            Game.Settings.Server.Map = mapId;

            //Auto-set settings
            Game.Settings.Server.ListenPort = 1234;
            Game.Settings.Server.ExternalPort = 1234;
            Game.Settings.Server.AdvertiseOnline = false;
            Game.Settings.Server.AllowUPnP = false;

            Game.Settings.Campaign.SinglePlayer = true;

            Game.Settings.Save();

            // Take a copy so that subsequent changes don't affect the server
            var settings = new ServerSettings(Game.Settings.Server);

            // Create the server
            Game.CreateServer(settings);

            ConnectionLogic.Connect(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort,
                () => Game.OpenWindow("SINGLEPLAYER_BG", new WidgetArgs()
                {
                    // NOTE: These aren't used because single player logic no longer requires them.
                    { "onExit", () => { Game.Disconnect(); } },
                    { "onStart", RemoveShellmapUI }
                  
                }),
                () => { });

        }

	}
}
