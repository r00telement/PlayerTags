﻿using Dalamud.Data;
using System;
using System.Collections.Generic;

namespace PlayerTags
{
    public class PluginData
    {
        public DefaultPluginData Default;
        public Tag AllTags;
        public Tag AllRoleTags;
        public Dictionary<Role, Tag> RoleTags;
        public Dictionary<string, Tag> JobTags;
        public Tag AllCustomTags;
        public List<Tag> CustomTags;

        public PluginData()
        {
            Default = new DefaultPluginData();
            AllTags = new Tag(new LocalizedPluginString(nameof(AllTags)));
            AllRoleTags = new Tag(new LocalizedPluginString(nameof(AllRoleTags)));
            RoleTags = new Dictionary<Role, Tag>();
            JobTags = new Dictionary<string, Tag>();
            AllCustomTags = new Tag(new LocalizedPluginString(nameof(AllCustomTags)));
            CustomTags = new List<Tag>();
        }

        public void Initialize(DataManager dataManager, PluginConfiguration pluginConfiguration)
        {
            Default.Initialize(dataManager);

            // Set the default changes and saved changes
            AllTags.SetChanges(Default.AllTagsChanges);
            AllTags.SetChanges(pluginConfiguration.AllTagsChanges);

            AllRoleTags.SetChanges(Default.AllRoleTagsChanges);
            AllRoleTags.SetChanges(pluginConfiguration.AllRoleTagsChanges);

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

            JobTags = new Dictionary<string, Tag>();
            foreach ((var jobAbbreviation, var role) in Default.RolesByJobAbbreviation)
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
            }

            foreach ((var jobAbbreviation, var jobTag) in JobTags)
            {
                jobTag.Parent = RoleTags[Default.RolesByJobAbbreviation[jobAbbreviation]];
            }

            AllCustomTags.Parent = AllTags;
            foreach (var tag in CustomTags)
            {
                tag.Parent = AllCustomTags;
            }
        }
    }
}