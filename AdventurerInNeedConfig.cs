﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Game.Chat;

namespace AdventurerInNeed {
    public class RouletteConfig {
        public bool Enabled;
        public bool Tank;
        public bool Healer;
        public bool DPS;
    }

    public class AdventurerInNeedConfig : IPluginConfiguration {
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        [NonSerialized]
        private AdventurerInNeed plugin;

        [NonSerialized] private bool showWebhookWindow;

        public Dictionary<uint, RouletteConfig> Roulettes { get; set; } = new Dictionary<uint, RouletteConfig>();

#if DEBUG
        public bool AlwaysShowAlert { get; set; }
#endif

        public int Version { get; set; }
        public bool InGameAlert { get; set; }
        public bool WebhookAlert { get; set; }
        public List<string> Webhooks { get; set; } = new List<string>();
        public XivChatType ChatType { get; set; } = XivChatType.SystemMessage;


        public void Init(AdventurerInNeed plugin, DalamudPluginInterface pluginInterface) {
            this.plugin = plugin;
            this.pluginInterface = pluginInterface;
        }

        public void Save() {
            pluginInterface.SavePluginConfig(this);
        }

        public bool DrawConfigUI() {
            var drawConfig = true;

            var scale = ImGui.GetIO().FontGlobalScale;

            var modified = false;

            ImGui.SetNextWindowSize(new Vector2(360 * scale, 350), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(360 * scale, 350), new Vector2(560 * scale, 650));
            ImGui.Begin($"{plugin.Name} Config", ref drawConfig, ImGuiWindowFlags.NoCollapse);

#if DEBUG
            var alwaysShowAlert = AlwaysShowAlert;
            if (ImGui.Checkbox("Debug: Always Alert", ref alwaysShowAlert)) {
                AlwaysShowAlert = alwaysShowAlert;
                Save();
            }

            if (ImGui.Button("Debug: Print All Alerts")) {
                foreach (var r in plugin.RouletteList) {
                    if (r.ContentRouletteRoleBonus.Row > 0) {
                        try {
                            plugin.ShowAlert(r, Roulettes[r.RowId], PreferredRole.Tank);
                            plugin.ShowAlert(r, Roulettes[r.RowId], PreferredRole.Healer);
                            plugin.ShowAlert(r, Roulettes[r.RowId], PreferredRole.DPS);
                        } catch (Exception ex) {
                            PluginLog.LogError(ex.ToString());
                        }
                    }
                }
            }
#endif

            var inGameAlerts = InGameAlert;
            if (ImGui.Checkbox("Send alerts in game chat.", ref inGameAlerts)) {
                InGameAlert = inGameAlerts;
                Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);

            var selectedDetails = ChatType.GetDetails();

            if (ImGui.BeginCombo("###chatType", ChatType == XivChatType.None ? "Any" : (selectedDetails == null ? ChatType.ToString() : selectedDetails.FancyName))) {

                foreach (var chatType in ((XivChatType[]) Enum.GetValues(typeof(XivChatType)))) {

                    var details = chatType.GetDetails();

                    if (ImGui.Selectable(chatType == XivChatType.None ? "Any" : (details == null ? chatType.ToString() : details.FancyName), chatType == ChatType)) {
                        ChatType = chatType;
                        Save();
                    }

                    if (chatType == ChatType) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            var webhookAlerts = WebhookAlert;
            if (ImGui.Checkbox("Send alerts to webhook.", ref webhookAlerts)) {
                WebhookAlert = webhookAlerts;
                Save();
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Setup Webhooks")) {
                showWebhookWindow = true;
            }

            ImGui.Separator();
            ImGui.Columns(6, "###cols", false);
            ImGui.SetColumnWidth(0, 40f * scale);
            ImGui.SetColumnWidth(1, ImGui.GetWindowWidth() - 240f * scale);
            ImGui.SetColumnWidth(2, 40f * scale);
            ImGui.SetColumnWidth(3, 40f * scale);
            ImGui.SetColumnWidth(4, 40f * scale);
            ImGui.SetColumnWidth(5, 80f * scale);

            ImGui.NextColumn();
            ImGui.Text("Roulette");
            ImGui.NextColumn();
            ImGui.Text("T");
            ImGui.NextColumn();
            ImGui.Text("H");
            ImGui.NextColumn();
            ImGui.Text("D");
            ImGui.NextColumn();
            ImGui.Text("Current");
            ImGui.NextColumn();

            ImGui.Separator();

            if (plugin.RouletteList != null) {
                foreach (var r in plugin.RouletteList.Where(r => r != null && r.ContentRouletteRoleBonus != null && r.ContentRouletteRoleBonus.Row > 0)) {
                    var rCfg = Roulettes.ContainsKey(r.RowId) ? Roulettes[r.RowId] : new RouletteConfig();
                    modified = ImGui.Checkbox($"###rouletteEnabled{r.RowId}", ref rCfg.Enabled) || modified;
                    ImGui.NextColumn();

                    ImGui.Text(r.Name);
                    ImGui.NextColumn();
                    modified = ImGui.Checkbox($"###rouletteTankEnabled{r.RowId}", ref rCfg.Tank) || modified;
                    ImGui.NextColumn();
                    modified = ImGui.Checkbox($"###rouletteHealerEnabled{r.RowId}", ref rCfg.Healer) || modified;
                    ImGui.NextColumn();
                    modified = ImGui.Checkbox($"###rouletteDPSEnabled{r.RowId}", ref rCfg.DPS) || modified;
                    ImGui.NextColumn();

                    if (plugin.LastPreferredRoleList != null) {
                        var currentRole = plugin.LastPreferredRoleList.Get(r.ContentRouletteRoleBonus.Row);
                        ImGui.Text(currentRole.ToString());
                    }

                    ImGui.NextColumn();

                    Roulettes[r.RowId] = rCfg;
                }
            }
            
            ImGui.Columns(1);

            ImGui.End();

            if (modified) {
                Save();
            }


            if (showWebhookWindow) {
                ImGui.Begin($"{plugin.Name}: Webhooks", ref showWebhookWindow);

                ImGui.TextWrapped($"Add webhook urls here to have {plugin.Name} send alerts somewhere else, like discord.");

                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Discord Webhook Guide");
                if (ImGui.IsItemHovered()) {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemClicked(0)) {
                    Process.Start("https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks");
                }

                for (var i = 0; i < Webhooks.Count; i++) {
                    var url = Webhooks[i];
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText($"###webhook{i}", ref url, 1024)) {
                        Webhooks[i] = url;
                        Save();
                    }
                }

                if (ImGui.SmallButton("Add Webhook")) {
                    Webhooks.Add("");
                }

                ImGui.End();
            }


            return drawConfig;
        }
    }
}