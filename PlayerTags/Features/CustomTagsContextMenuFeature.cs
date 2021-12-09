using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Resources;
using System;
using System.Linq;
using XivCommon;
using XivCommon.Functions.ContextMenu;

namespace PlayerTags.Features
{
    public class CustomTagsContextMenuFeature : IDisposable
    {
        private string?[] CustomTagsSupportedAddonNames = new string?[]
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

        private XivCommonBase m_XivCommon;
        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        public CustomTagsContextMenuFeature(XivCommonBase xivCommon, PluginConfiguration pluginConfiguration, PluginData pluginData)
        {
            m_XivCommon = xivCommon;
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;

            m_XivCommon.Functions.ContextMenu.OpenContextMenu += ContextMenu_OpenContextMenu;
        }

        public void Dispose()
        {
            m_XivCommon.Functions.ContextMenu.OpenContextMenu -= ContextMenu_OpenContextMenu;
        }

        private void ContextMenu_OpenContextMenu(ContextMenuOpenArgs args)
        {
            if (!m_PluginConfiguration.IsCustomTagsContextMenuEnabled || !DoesContextMenuSupportCustomTags(args))
            {
                return;
            }

            string gameObjectName = args.Text!.TextValue;

            var notAddedTags = m_PluginData.CustomTags.Where(tag => !tag.IncludesGameObjectNameToApplyTo(gameObjectName));
            if (notAddedTags.Any())
            {
                args.Items.Add(new NormalContextSubMenuItem(Strings.Loc_Static_ContextMenu_AddTag, (itemArgs =>
                {
                    foreach (var notAddedTag in notAddedTags)
                    {
                        itemArgs.Items.Add(new NormalContextMenuItem(notAddedTag.Text.Value, (args =>
                        {
                            notAddedTag.AddGameObjectNameToApplyTo(gameObjectName);
                        })));
                    }
                })));
            }

            var addedTags = m_PluginData.CustomTags.Where(tag => tag.IncludesGameObjectNameToApplyTo(gameObjectName));
            if (addedTags.Any())
            {
                args.Items.Add(new NormalContextSubMenuItem(Strings.Loc_Static_ContextMenu_RemoveTag, (itemArgs =>
                {
                    foreach (var addedTag in addedTags)
                    {
                        itemArgs.Items.Add(new NormalContextMenuItem(addedTag.Text.Value, (args =>
                        {
                            addedTag.RemoveGameObjectNameToApplyTo(gameObjectName);
                        })));
                    }
                })));
            }
        }

        private bool DoesContextMenuSupportCustomTags(BaseContextMenuArgs args)
        {
            if (args.Text == null || args.ObjectWorld == 0 || args.ObjectWorld == 65535)
            {
                return false;
            }

            return CustomTagsSupportedAddonNames.Contains(args.ParentAddonName);
        }
    }
}
