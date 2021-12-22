using PlayerTags.Configuration;
using PlayerTags.PluginStrings;
using System;
using System.Collections.Generic;

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

        public PluginData(PluginConfiguration pluginConfiguration)
        {
            Default = new DefaultPluginData();

            // Set the default changes and saved changes
            AllTags = new Tag(new LocalizedPluginString(nameof(AllTags)));
            AllTags.SetChanges(Default.AllTagsChanges);
            AllTags.SetChanges(pluginConfiguration.AllTagsChanges);

            AllRoleTags = new Tag(new LocalizedPluginString(nameof(AllRoleTags)));
            AllRoleTags.SetChanges(Default.AllRoleTagsChanges);
            AllRoleTags.SetChanges(pluginConfiguration.AllRoleTagsChanges);

            RoleTags = new Dictionary<Role, Tag>();
            foreach (var role in Enum.GetValues<Role>())
            {
                RoleTags[role] = new Tag(new LocalizedPluginString(Localizer.GetName(role)));

                if (Default.RoleTagsChanges.TryGetValue(role, out var defaultChanges))
                {
                    RoleTags[role].SetChanges(defaultChanges);
                }

                if (pluginConfiguration.RoleTagsChanges.TryGetValue(role, out var savedChanges))
                {
                    RoleTags[role].SetChanges(savedChanges);
                }
            }

            DpsRoleTags = new Dictionary<DpsRole, Tag>();
            foreach (var dpsRole in Enum.GetValues<DpsRole>())
            {
                DpsRoleTags[dpsRole] = new Tag(new LocalizedPluginString(Localizer.GetName(dpsRole)));

                if (Default.DpsRoleTagsChanges.TryGetValue(dpsRole, out var defaultChanges))
                {
                    DpsRoleTags[dpsRole].SetChanges(defaultChanges);
                }

                if (pluginConfiguration.DpsRoleTagsChanges.TryGetValue(dpsRole, out var savedChanges))
                {
                    DpsRoleTags[dpsRole].SetChanges(savedChanges);
                }
            }

            RangedDpsRoleTags = new Dictionary<RangedDpsRole, Tag>();
            foreach (var rangedDpsRole in Enum.GetValues<RangedDpsRole>())
            {
                RangedDpsRoleTags[rangedDpsRole] = new Tag(new LocalizedPluginString(Localizer.GetName(rangedDpsRole)));

                if (Default.RangedDpsRoleTagsChanges.TryGetValue(rangedDpsRole, out var defaultChanges))
                {
                    RangedDpsRoleTags[rangedDpsRole].SetChanges(defaultChanges);
                }

                if (pluginConfiguration.RangedDpsRoleTagsChanges.TryGetValue(rangedDpsRole, out var savedChanges))
                {
                    RangedDpsRoleTags[rangedDpsRole].SetChanges(savedChanges);
                }
            }

            LandHandRoleTags = new Dictionary<LandHandRole, Tag>();
            foreach (var landHandRole in Enum.GetValues<LandHandRole>())
            {
                LandHandRoleTags[landHandRole] = new Tag(new LocalizedPluginString(Localizer.GetName(landHandRole)));

                if (Default.LandHandRoleTagsChanges.TryGetValue(landHandRole, out var defaultChanges))
                {
                    LandHandRoleTags[landHandRole].SetChanges(defaultChanges);
                }

                if (pluginConfiguration.LandHandRoleTagsChanges.TryGetValue(landHandRole, out var savedChanges))
                {
                    LandHandRoleTags[landHandRole].SetChanges(savedChanges);
                }
            }

            JobTags = new Dictionary<string, Tag>();
            foreach ((var jobAbbreviation, var role) in RoleHelper.RolesByJobAbbreviation)
            {
                JobTags[jobAbbreviation] = new Tag(new LiteralPluginString(jobAbbreviation));

                if (Default.JobTagsChanges.TryGetValue(jobAbbreviation, out var defaultChanges))
                {
                    JobTags[jobAbbreviation].SetChanges(defaultChanges);
                }

                if (pluginConfiguration.JobTagsChanges.TryGetValue(jobAbbreviation, out var savedChanges))
                {
                    JobTags[jobAbbreviation].SetChanges(savedChanges);
                }
            }

            AllCustomTags = new Tag(new LocalizedPluginString(nameof(AllCustomTags)));
            CustomTags = new List<Tag>();

            AllCustomTags.SetChanges(Default.AllCustomTagsChanges);
            AllCustomTags.SetChanges(pluginConfiguration.AllCustomTagsChanges);

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
        }
    }
}
