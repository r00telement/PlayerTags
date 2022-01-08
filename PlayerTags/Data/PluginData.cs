using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PlayerTags.Configuration;
using PlayerTags.GameInterface.ContextMenus;
using PlayerTags.PluginStrings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Data
{
    public class PluginData
    {
        public DefaultPluginData Default;
        public Tag AllTags;
        public Tag AllRoleTags;
        public Dictionary<Role, Tag> RoleTags;
        public Dictionary<DpsRole, Tag> DpsRoleTags;
        public Dictionary<RangedDpsRole, Tag> RangedDpsRoleTags;
        public Dictionary<LandHandRole, Tag> LandHandRoleTags;
        public Dictionary<string, Tag> JobTags;
        public Tag AllCustomTags;
        public List<Tag> CustomTags;
        public List<Identity> Identities;

        private PluginConfiguration m_PluginConfiguration;

        public PluginData(PluginConfiguration pluginConfiguration)
        {
            m_PluginConfiguration = pluginConfiguration;

            Default = new DefaultPluginData();

            // Set the default changes and saved changes
            AllTags = new Tag(new LocalizedPluginString(nameof(AllTags)), Default.AllTags);
            AllTags.SetChanges(pluginConfiguration.AllTagsChanges);

            AllRoleTags = new Tag(new LocalizedPluginString(nameof(AllRoleTags)), Default.AllRoleTags);
            AllRoleTags.SetChanges(pluginConfiguration.AllRoleTagsChanges);

            RoleTags = new Dictionary<Role, Tag>();
            foreach (var role in Enum.GetValues<Role>())
            {
                if (Default.RoleTags.TryGetValue(role, out var defaultTag))
                {
                    RoleTags[role] = new Tag(new LocalizedPluginString(Localizer.GetName(role)), defaultTag);
                    if (pluginConfiguration.RoleTagsChanges.TryGetValue(role, out var savedChanges))
                    {
                        RoleTags[role].SetChanges(savedChanges);
                    }
                }
            }

            DpsRoleTags = new Dictionary<DpsRole, Tag>();
            foreach (var dpsRole in Enum.GetValues<DpsRole>())
            {
                if (Default.DpsRoleTags.TryGetValue(dpsRole, out var defaultTag))
                {
                    DpsRoleTags[dpsRole] = new Tag(new LocalizedPluginString(Localizer.GetName(dpsRole)), defaultTag);
                    if (pluginConfiguration.DpsRoleTagsChanges.TryGetValue(dpsRole, out var savedChanges))
                    {
                        DpsRoleTags[dpsRole].SetChanges(savedChanges);
                    }
                }
            }

            RangedDpsRoleTags = new Dictionary<RangedDpsRole, Tag>();
            foreach (var rangedDpsRole in Enum.GetValues<RangedDpsRole>())
            {
                if (Default.RangedDpsRoleTags.TryGetValue(rangedDpsRole, out var defaultTag))
                {
                    RangedDpsRoleTags[rangedDpsRole] = new Tag(new LocalizedPluginString(Localizer.GetName(rangedDpsRole)), defaultTag);
                    if (pluginConfiguration.RangedDpsRoleTagsChanges.TryGetValue(rangedDpsRole, out var savedChanges))
                    {
                        RangedDpsRoleTags[rangedDpsRole].SetChanges(savedChanges);
                    }
                }
            }

            LandHandRoleTags = new Dictionary<LandHandRole, Tag>();
            foreach (var landHandRole in Enum.GetValues<LandHandRole>())
            {
                if (Default.LandHandRoleTags.TryGetValue(landHandRole, out var defaultChanges))
                {
                    LandHandRoleTags[landHandRole] = new Tag(new LocalizedPluginString(Localizer.GetName(landHandRole)), defaultChanges);
                    if (pluginConfiguration.LandHandRoleTagsChanges.TryGetValue(landHandRole, out var savedChanges))
                    {
                        LandHandRoleTags[landHandRole].SetChanges(savedChanges);
                    }
                }
            }

            JobTags = new Dictionary<string, Tag>();
            foreach ((var jobAbbreviation, var role) in RoleHelper.RolesByJobAbbreviation)
            {
                if (Default.JobTags.TryGetValue(jobAbbreviation, out var defaultChanges))
                {
                    JobTags[jobAbbreviation] = new Tag(new LiteralPluginString(jobAbbreviation), defaultChanges);
                    if (pluginConfiguration.JobTagsChanges.TryGetValue(jobAbbreviation, out var savedChanges))
                    {
                        JobTags[jobAbbreviation].SetChanges(savedChanges);
                    }
                }
            }

            AllCustomTags = new Tag(new LocalizedPluginString(nameof(AllCustomTags)), Default.AllCustomTags);
            AllCustomTags.SetChanges(pluginConfiguration.AllCustomTagsChanges);

            CustomTags = new List<Tag>();
            foreach (var savedChanges in pluginConfiguration.CustomTagsChanges)
            {
                var tag = new Tag(new LocalizedPluginString(nameof(CustomTags)));
                tag.SetChanges(savedChanges);
                CustomTags.Add(tag);
            }

            // Set up the inheritance heirarchy
            AllRoleTags.Parent = AllTags;
            foreach ((var role, var roleTag) in RoleTags)
            {
                roleTag.Parent = AllRoleTags;

                if (role == Role.Dps)
                {
                    foreach ((var dpsRole, var dpsRoleTag) in DpsRoleTags)
                    {
                        dpsRoleTag.Parent = roleTag;

                        if (dpsRole == DpsRole.Ranged)
                        {
                            foreach ((var rangedDpsRole, var rangedDpsRoleTag) in RangedDpsRoleTags)
                            {
                                rangedDpsRoleTag.Parent = dpsRoleTag;
                            }
                        }
                    }
                }
                else if (role == Role.LandHand)
                {
                    foreach ((var landHandRole, var landHandRoleTag) in LandHandRoleTags)
                    {
                        landHandRoleTag.Parent = roleTag;
                    }
                }
            }

            foreach ((var jobAbbreviation, var jobTag) in JobTags)
            {
                if (RoleHelper.RolesByJobAbbreviation.TryGetValue(jobAbbreviation, out var role))
                {
                    if (RoleHelper.DpsRolesByJobAbbreviation.TryGetValue(jobAbbreviation, out var dpsRole))
                    {
                        if (RoleHelper.RangedDpsRolesByJobAbbreviation.TryGetValue(jobAbbreviation, out var rangedDpsRole))
                        {
                            jobTag.Parent = RangedDpsRoleTags[rangedDpsRole];
                        }
                        else
                        {
                            jobTag.Parent = DpsRoleTags[dpsRole];
                        }
                    }
                    else if (RoleHelper.LandHandRolesByJobAbbreviation.TryGetValue(jobAbbreviation, out var landHandRole))
                    {
                        jobTag.Parent = LandHandRoleTags[landHandRole];
                    }
                    else
                    {
                        jobTag.Parent = RoleTags[RoleHelper.RolesByJobAbbreviation[jobAbbreviation]];
                    }
                }
            }

            AllCustomTags.Parent = AllTags;
            foreach (var tag in CustomTags)
            {
                tag.Parent = AllCustomTags;
            }

            Identities = pluginConfiguration.Identities;

            // Migrate old custom tag identity assignments
            bool customTagsMigrated = false;
            foreach (var customTag in CustomTags)
            {
                if (customTag.CustomId.Value == Guid.Empty)
                {
                    customTag.CustomId.Behavior = Inheritables.InheritableBehavior.Enabled;
                    customTag.CustomId.Value = Guid.NewGuid();
                    customTagsMigrated = true;
                }

                foreach (string identityToAddTo in customTag.IdentitiesToAddTo)
                {
                    Identity? identity = Identities.FirstOrDefault(identity => identity.Name.ToLower() == identityToAddTo.ToLower());
                    if (identity == null)
                    {
                        identity = new Identity(identityToAddTo);
                        Identities.Add(identity);
                    }

                    if (identity != null)
                    {
                        identity.CustomTagIds.Add(customTag.CustomId.Value);
                        customTagsMigrated = true;
                    }
                }

                if (customTag.GameObjectNamesToApplyTo.Behavior != Inheritables.InheritableBehavior.Inherit)
                {
                    customTag.GameObjectNamesToApplyTo.Behavior = Inheritables.InheritableBehavior.Inherit;
                    customTag.GameObjectNamesToApplyTo.Value = "";
                    customTagsMigrated = true;
                }
            }

            if (customTagsMigrated)
            {
                pluginConfiguration.Save(this);
            }
        }

        public void AddCustomTagToIdentity(Tag customTag, Identity identity)
        {
            if (!identity.CustomTagIds.Contains(customTag.CustomId.Value))
            {
                identity.CustomTagIds.Add(customTag.CustomId.Value);
            }

            if (!Identities.Contains(identity))
            {
                Identities.Add(identity);
            }
        }

        public void RemoveCustomTagFromIdentity(Tag customTag, Identity identity)
        {
            identity.CustomTagIds.Remove(customTag.CustomId.Value);

            if (!identity.CustomTagIds.Any())
            {
                Identities.Remove(identity);
            }
        }

        public void RemoveCustomTagFromIdentities(Tag customTag)
        {
            foreach (var identity in Identities.ToArray())
            {
                RemoveCustomTagFromIdentity(customTag, identity);
            }
        }

        public Identity GetIdentity(string name, uint? worldId)
        {
            foreach (var identity in Identities)
            {
                if (identity.Name.ToLower().Trim() == name.ToLower().Trim())
                {
                    if (identity.WorldId == null && worldId != null)
                    {
                        identity.WorldId = worldId;
                        m_PluginConfiguration.Save(this);

                        return identity;
                    }
                    else
                    {
                        return identity;
                    }
                }
            }

            return new Identity(name)
            {
                WorldId = worldId
            };
        }

        public Identity? GetIdentity(ContextMenuOpenedArgs contextMenuOpenedArgs)
        {
            if (contextMenuOpenedArgs.GameObjectContext == null
                || contextMenuOpenedArgs.GameObjectContext.Name == null
                || contextMenuOpenedArgs.GameObjectContext.WorldId == 0
                || contextMenuOpenedArgs.GameObjectContext.WorldId == 65535)
            {
                return null;
            }

            return GetIdentity(contextMenuOpenedArgs.GameObjectContext.Name.TextValue, contextMenuOpenedArgs.GameObjectContext.WorldId);
        }

        public Identity GetIdentity(PlayerCharacter playerCharacter)
        {
            return GetIdentity(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.GameData.RowId);
        }

        public Identity GetIdentity(PartyMember partyMember)
        {
            return GetIdentity(partyMember.Name.TextValue, partyMember.World.GameData.RowId);
        }

        public Identity GetIdentity(PlayerPayload playerPayload)
        {
            return GetIdentity(playerPayload.PlayerName, playerPayload.World.RowId);
        }
    }
}
