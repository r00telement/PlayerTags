using Dalamud.Game.Gui.ContextMenus;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using PlayerTags.Configuration;
using PlayerTags.Data;
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
        private ContextMenu? m_ContextMenu;

        public CustomTagsContextMenuFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;

            m_ContextMenu = new ContextMenu();
            
            if (m_PluginConfiguration.IsCustomTagsContextMenuEnabled)
            {
                m_ContextMenu.ContextMenuOpened += ContextMenuHooks_ContextMenuOpened;
                PluginServices.GameGui.Enable();
            }
        }

        public void Dispose()
        {
            if (m_ContextMenu != null)
            {
                m_ContextMenu.ContextMenuOpened -= ContextMenuHooks_ContextMenuOpened;
                ((IDisposable)m_ContextMenu).Dispose();
                m_ContextMenu = null;
            }
        }

        private void ContextMenuHooks_ContextMenuOpened(ContextMenuOpenedArgs contextMenuOpenedArgs)
        {
            if (!m_PluginConfiguration.IsCustomTagsContextMenuEnabled
                || !SupportedAddonNames.Contains(contextMenuOpenedArgs.ParentAddonName))
            {
                return;
            }

            Identity? identity = m_PluginData.GetIdentity(contextMenuOpenedArgs);
            if (identity != null)
            {
                var notAddedTags = m_PluginData.CustomTags.Where(customTag => !identity.CustomTagIds.Contains(customTag.CustomId.Value));
                if (notAddedTags.Any())
                {
                    contextMenuOpenedArgs.AddCustomSubMenu(Strings.Loc_Static_ContextMenu_AddTag, subContextMenuOpenedArgs =>
                    {
                        foreach (var notAddedTag in notAddedTags)
                        {
                            subContextMenuOpenedArgs.AddCustomItem(notAddedTag.Text.Value, args =>
                            {
                                m_PluginData.AddCustomTagToIdentity(notAddedTag, identity);
                                m_PluginConfiguration.Save(m_PluginData);
                            });
                        }
                    });
                }

                var addedTags = m_PluginData.CustomTags.Where(customTag => identity.CustomTagIds.Contains(customTag.CustomId.Value));
                if (addedTags.Any())
                {
                    contextMenuOpenedArgs.AddCustomSubMenu(Strings.Loc_Static_ContextMenu_RemoveTag, subContextMenuOpenedArgs =>
                    {
                        foreach (var addedTag in addedTags)
                        {
                            subContextMenuOpenedArgs.AddCustomItem(addedTag.Text.Value, args =>
                            {
                                m_PluginData.RemoveCustomTagFromIdentity(addedTag, identity);
                                m_PluginConfiguration.Save(m_PluginData);
                            });
                        }
                    });
                }
            }
        }
    }
}
