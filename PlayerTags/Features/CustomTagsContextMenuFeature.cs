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
    /// <summary>
    /// A feature that adds options for the management of custom tags to context menus.
    /// </summary>
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

        //private ContextMenu? m_ContextMenu;

        public CustomTagsContextMenuFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;
            
            //m_ContextMenu = new ContextMenu();
            //if (!m_ContextMenu.IsValid)
            //{
            //    m_ContextMenu = null;
            //}

            //if (m_ContextMenu != null)
            //{
            //    m_ContextMenu.ContextMenuOpened += ContextMenuHooks_ContextMenuOpened;
            //}
        }

        public void Dispose()
        {
            //if (m_ContextMenu != null)
            //{
            //    m_ContextMenu.ContextMenuOpened -= ContextMenuHooks_ContextMenuOpened;

            //    m_ContextMenu.Dispose();
            //    m_ContextMenu = null;
            //}
        }

        //private void ContextMenuHooks_ContextMenuOpened(ContextMenuOpenedArgs contextMenuOpenedArgs)
        //{
        //    if (!m_PluginConfiguration.IsCustomTagsContextMenuEnabled
        //        || !SupportedAddonNames.Contains(contextMenuOpenedArgs.ParentAddonName))
        //    {
        //        return;
        //    }

        //    Identity? identity = m_PluginData.GetIdentity(contextMenuOpenedArgs);
        //    if (identity != null)
        //    {
        //        var notAddedTags = m_PluginData.CustomTags.Where(customTag => !identity.CustomTagIds.Contains(customTag.CustomId.Value));
        //        if (notAddedTags.Any())
        //        {
        //            contextMenuOpenedArgs.Items.Add(new OpenSubContextMenuItem(Strings.Loc_Static_ContextMenu_AddTag, (subContextMenuOpenedArgs =>
        //            {
        //                List<ContextMenuItem> newContextMenuItems = new List<ContextMenuItem>();
        //                foreach (var notAddedTag in notAddedTags)
        //                {
        //                    newContextMenuItems.Add(new CustomContextMenuItem(notAddedTag.Text.Value, (args =>
        //                    {
        //                        m_PluginData.AddCustomTagToIdentity(notAddedTag, identity);
        //                        m_PluginConfiguration.Save(m_PluginData);
        //                    })));
        //                }
        //                subContextMenuOpenedArgs.Items.InsertRange(0, newContextMenuItems);
        //            })));
        //        }

        //        var addedTags = m_PluginData.CustomTags.Where(customTag => identity.CustomTagIds.Contains(customTag.CustomId.Value));
        //        if (addedTags.Any())
        //        {
        //            contextMenuOpenedArgs.Items.Add(new OpenSubContextMenuItem(Strings.Loc_Static_ContextMenu_RemoveTag, (subContextMenuOpenedArgs =>
        //            {
        //                List<ContextMenuItem> newContextMenuItems = new List<ContextMenuItem>();
        //                foreach (var addedTag in addedTags)
        //                {
        //                    newContextMenuItems.Add(new CustomContextMenuItem(addedTag.Text.Value, (args =>
        //                    {
        //                        m_PluginData.RemoveCustomTagFromIdentity(addedTag, identity);
        //                        m_PluginConfiguration.Save(m_PluginData);
        //                    })));
        //                }
        //                subContextMenuOpenedArgs.Items.InsertRange(0, newContextMenuItems);
        //            })));
        //        }
        //    }
        //}
    }
}
