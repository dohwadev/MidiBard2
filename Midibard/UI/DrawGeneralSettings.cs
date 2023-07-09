﻿// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard2.Resources;
using MidiBard.Util;
using static ImGuiNET.ImGui;
using static MidiBard2.Resources.Language;

namespace MidiBard;

public partial class PluginUI
{
	private readonly string[] _toolTips = {
		"Off: Does not take over game's guitar tone control.",
		"Standard: Standard midi channel and ProgramChange handling, each channel will keep it's program state separately.",
		"Simple: Simple ProgramChange handling, ProgramChange event on any channel will change all channels' program state. (This is BardMusicPlayer's default behavior.)",
		"Override by track: Assign guitar tone manually for each track and ignore ProgramChange events.",
	};

	private bool _resetPlotWindowPosition = false;
	private bool showSettingsPanel;

	private unsafe void DrawSettingsWindow()
	{
		//var itemWidth = ImGuiHelpers.GlobalScale * 100;
		//SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);

		ImGuiGroupPanel.BeginGroupPanel(setting_group_label_general_settings);
		{
			Checkbox(setting_label_auto_open_MidiBard, ref MidiBard.config.AutoOpenPlayerWhenPerforming);
			ImGuiUtil.ToolTip(setting_label_auto_open_MidiBard);

			//Checkbox(Low_latency_mode, ref MidiBard.config.LowLatencyMode);
			//ImGuiUtil.ToolTip(low_latency_mode_tooltip);

            //ImGui.Checkbox(checkbox_auto_restart_listening, ref MidiBard.config.autoRestoreListening);
			//ImGuiUtil.ToolTip(checkbox_auto_restart_listening_tooltip);

			//ImGui.SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);
			//ImGui.Checkbox("Auto listening new device".Localize(), ref MidiBard.config.autoStartNewListening);
			//ImGuiUtil.ToolTip("Auto start listening new midi input device when idle.".Localize());

			ColorEdit4(setting_label_theme_color, ref MidiBard.config.themeColor,
				ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
			//ImGuiUtil.ColorPickerButton(1000, label_theme_color, ref MidiBard.config.themeColor,
			//	ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
			//if (ImGui.ColorEdit4("Theme color".Localize(), ref MidiBard.config.themeColor,
			//	ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))

			if (IsItemClicked(ImGuiMouseButton.Right)) {
				var @in = 0xFFFFA8A8;
				MidiBard.config.themeColor = ColorConvertU32ToFloat4(@in);
			}

			if (Combo(setting_label_select_ui_language, ref MidiBard.config.uiLang, uilangStrings,
				    uilangStrings.Length)) {
				MidiBard.ConfigureLanguage(MidiBard.GetCultureCodeString((MidiBard.CultureCode)MidiBard.config.uiLang));
			}
		}
		ImGuiGroupPanel.EndGroupPanel();


		ImGuiGroupPanel.BeginGroupPanel(setting_group_label_ensemble_settings);

		Checkbox(setting_label_sync_clients, ref MidiBard.config.SyncClients);
		ImGuiUtil.ToolTip(setting_tooltip_sync_clients);

		SameLine(ImGuiUtil.GetWindowContentRegionWidth() - GetFrameHeightWithSpacing() - ImGuiUtil.GetIconButtonSize((FontAwesomeIcon)0xF362).X);
		if (ImGuiUtil.IconButton((FontAwesomeIcon)0xF362, "syncbtn", icon_button_tooltip_sync_settings)) {
			IPCHandles.SyncAllSettings();
			IPCHandles.SyncPlaylist();
		}

		Checkbox(setting_label_monitor_ensemble, ref MidiBard.config.MonitorOnEnsemble);
		ImGuiUtil.ToolTip(setting_tooltip_monitor_ensemble);

		ImGui.Checkbox(ensemble_config_Draw_ensemble_progress_indicator_on_visualizer,
			ref MidiBard.config.UseEnsembleIndicator);

		Spacing();
		TextUnformatted(ensemble_config_Ensemble_indicator_delay);
		Spacing();
		ImGui.DragFloat("##" + ensemble_config_Ensemble_indicator_delay, ref MidiBard.config.EnsembleIndicatorDelay,
			0.01f, -10, 0, $"{MidiBard.config.EnsembleIndicatorDelay:F3}s");

		ImGuiGroupPanel.EndGroupPanel();

		ImGuiGroupPanel.BeginGroupPanel(setting_group_label_performance_settings);

		Checkbox(setting_label_auto_switch_instrument_bmp, ref MidiBard.config.bmpTrackNames);
		ImGuiUtil.ToolTip(setting_tooltip_auto_switch_transpose_instrument_bmp_trackname);

		ImGui.Checkbox(setting_label_auto_switch_instrument_by_file_name,
			ref MidiBard.config.autoSwitchInstrumentBySongName);
		ImGuiUtil.ToolTip(setting_tooltip_label_auto_switch_instrument_by_file_name);

		Checkbox(setting_label_auto_transpose_by_file_name, ref MidiBard.config.autoTransposeBySongName);
		ImGuiUtil.ToolTip(setting_tooltip_auto_transpose_by_file_name);

		if (ImGui.Checkbox("Play Lyrics", ref MidiBard.config.playLyrics))
		{
			IPCHandles.SyncAllSettings();
		}
		ImGuiUtil.ToolTip("Choose this if you want to post lyrics.");

		bool pmdWasOn = MidiBard.config.playOnMultipleDevices;
		if (ImGui.Checkbox("Play on Multiple Devices", ref MidiBard.config.playOnMultipleDevices))
		{
			if (pmdWasOn || MidiBard.config.playOnMultipleDevices)
			{
				PartyChatCommand.SendPMD(MidiBard.config.playOnMultipleDevices);
			}
		}
		ImGuiUtil.ToolTip("Choose this if your bards are spread between different devices.");

		if (MidiBard.config.playOnMultipleDevices)
		{
			if (ImGui.Checkbox("Using File Sharing Services", ref MidiBard.config.usingFileSharingServices))
			{
				IPCHandles.SyncAllSettings();
			}
			ImGuiUtil.ToolTip("Using File Sharing Services like Google Drive to sync songs and performer settings.");
		}

		ImGui.Text($"Default Performer Folder:");
		SameLine();
		ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin()).X - 50);
		ImGui.SetNextItemWidth(50);
		if (ImGui.Button("Change"))
		{
			RunSetDefaultPerformerFolderImGui();
		}

		ImGui.Text(Path.ChangeExtension(MidiBard.config.defaultPerformerFolder, null).EllipsisString(40));

		ImGuiGroupPanel.EndGroupPanel();
		Spacing();
	}

	private void RunSetDefaultPerformerFolderImGui()
	{
		fileDialogManager.OpenFolderDialog("Set Default Performer Folder", (b, filePath) =>
		{
			PluginLog.Debug($"dialog result: {b}\n{string.Join("\n", filePath)}");
			if (b)
			{
				MidiFileConfigManager.SetDefaultPerformerFolder(filePath);
				MidiBard.SaveConfig();
				IPCHandles.SyncAllSettings();
				IPCHandles.UpdateDefaultPerformer();
			}
		}, MidiBard.config.defaultPerformerFolder);
	}
}