using Dalamud.Logging;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.GameInterface.ContextMenus;
using PlayerTags.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    public class CustomTagsContextMenuFeature : IDisposable
    {
        private string?[] SupportedAddonNames = new string?[]
        {
            null,
            "_PartyList",
            "ChatLog",
            "ContactList",
            "ContentMemberList",
            "CrossWorldLinkshell",
            "FreeCompany",
            "FriendList",
            "LookingForGroup",
            "LinkShell",
            "PartyMemberList",
            "SocialList",
        };

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        private ContextMenu? m_ContextMenu;

        public CustomTagsContextMenuFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;

            m_ContextMenu = new ContextMenu();
            if (!m_ContextMenu.IsValid)
            {
                m_ContextMenu = null;
            }

            if (m_ContextMenu != null)
            {
                m_ContextMenu.ContextMenuOpened += ContextMenuHooks_ContextMenuOpened;
            }
        }

        public void Dispose()
        {
            if (m_ContextMenu != null)
            {
                m_ContextMenu.ContextMenuOpened -= ContextMenuHooks_ContextMenuOpened;

                m_ContextMenu.Dispose();
                m_ContextMenu = null;
            }
        }

        private void ContextMenuHooks_ContextMenuOpened(ContextMenuOpenedArgs contextMenuOpenedArgs)
        {
            if (contextMenuOpenedArgs.GameObjectContext != null)
            {
                PluginLog.Debug($"ContextMenuHooks_ContextMenuOpened   {contextMenuOpenedArgs.GameObjectContext?.Id}   {contextMenuOpenedArgs.GameObjectContext?.ContentIdLower}   '{contextMenuOpenedArgs.GameObjectContext?.Name}'   {contextMenuOpenedArgs.GameObjectContext?.WorldId}");
            }

            if (contextMenuOpenedArgs.ItemContext != null)
            {
                PluginLog.Debug($"ContextMenuHooks_ContextMenuOpened   {contextMenuOpenedArgs.ItemContext?.Id}   {contextMenuOpenedArgs.ItemContext?.Count}   {contextMenuOpenedArgs.ItemContext?.IsHighQuality}");
            }

            contextMenuOpenedArgs.ContextMenuItems.Add(new CustomContextMenuItem("Root1", (itemSelectedArgs =>
            {
                PluginLog.Debug("Executed Root1");
            })));

            contextMenuOpenedArgs.ContextMenuItems.Add(new OpenSubContextMenuItem("Root2", (subContextMenuOpenedArgs =>
            {
                PluginLog.Debug("Executed Root2");

                List<ContextMenuItem> newContextMenuItems = new List<ContextMenuItem>();
                newContextMenuItems.Add(new OpenSubContextMenuItem("Inner1", (subContextMenuOpenedArgs2 =>
                {
                    PluginLog.Debug("Executed Inner1");

                    List<ContextMenuItem> newContextMenuItems = new List<ContextMenuItem>();
                    newContextMenuItems.Add(new CustomContextMenuItem("Inner3", (itemSelectedArgs =>
                    {
                        PluginLog.Debug("Executed Inner3");
                    })));

                    subContextMenuOpenedArgs2.ContextMenuItems.InsertRange(0, newContextMenuItems);
                })));

                newContextMenuItems.Add(new CustomContextMenuItem("Inner2", (itemSelectedArgs =>
                {
                    PluginLog.Debug("Executed Inner2");
                })));

                subContextMenuOpenedArgs.ContextMenuItems.InsertRange(0, newContextMenuItems);
            })));

            if (!m_PluginConfiguration.IsCustomTagsContextMenuEnabled || !SupportedAddonNames.Contains(contextMenuOpenedArgs.ParentAddonName))
            {
                return;
            }

            Identity? identity = m_PluginData.GetIdentity(contextMenuOpenedArgs);
            if (identity != null)
            {
                var notAddedTags = m_PluginData.CustomTags.Where(customTag => !identity.CustomTagIds.Contains(customTag.CustomId.Value));
                if (notAddedTags.Any())
                {
                    contextMenuOpenedArgs.ContextMenuItems.Add(new OpenSubContextMenuItem(Strings.Loc_Static_ContextMenu_AddTag, (subContextMenuOpenedArgs =>
                    {
                        List<ContextMenuItem> newContextMenuItems = new List<ContextMenuItem>();
                        foreach (var notAddedTag in notAddedTags)
                        {
                            newContextMenuItems.Add(new CustomContextMenuItem(notAddedTag.Text.Value, (args =>
                            {
                                m_PluginData.AddCustomTagToIdentity(notAddedTag, identity);
                                m_PluginConfiguration.Save(m_PluginData);
                            })));
                        }

                        subContextMenuOpenedArgs.ContextMenuItems.InsertRange(0, newContextMenuItems);
                    })));
                }

                var addedTags = m_PluginData.CustomTags.Where(customTag => identity.CustomTagIds.Contains(customTag.CustomId.Value));
                if (addedTags.Any())
                {
                    contextMenuOpenedArgs.ContextMenuItems.Add(new OpenSubContextMenuItem(Strings.Loc_Static_ContextMenu_RemoveTag, (subContextMenuOpenedArgs =>
                    {
                        List<ContextMenuItem> newContextMenuItems = new List<ContextMenuItem>();
                        foreach (var addedTag in addedTags)
                        {
                            newContextMenuItems.Add(new CustomContextMenuItem(addedTag.Text.Value, (args =>
                            {
                                m_PluginData.RemoveCustomTagFromIdentity(addedTag, identity);
                                m_PluginConfiguration.Save(m_PluginData);
                            })));
                        }

                        subContextMenuOpenedArgs.ContextMenuItems.InsertRange(0, newContextMenuItems);
                    })));
                }
            }
        }
    }
}
