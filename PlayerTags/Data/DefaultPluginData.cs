using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;
using PlayerTags.Inheritables;
using PlayerTags.PluginStrings;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Data
{
    public class DefaultPluginData
    {
        public Dictionary<byte, Role> RolesById { get; }
        public Dictionary<string, Role> RolesByJobAbbreviation { get; }

        public Dictionary<string, InheritableData> AllTagsChanges { get; }

        public Dictionary<string, InheritableData> AllRoleTagsChanges { get; }
        public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges { get; }
        public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges { get; }

        public Dictionary<string, InheritableData> AllCustomTagsChanges { get; }

        public DefaultPluginData()
        {
            RolesById = new Dictionary<byte, Role>()
            {
                { 0, Role.LandHand },
                { 1, Role.Tank },
                { 2, Role.DPS },
                { 3, Role.DPS },
                { 4, Role.Healer },
            };

            RolesByJobAbbreviation = new Dictionary<string, Role>();

            AllTagsChanges = new Tag(new LiteralPluginString(""))
            {
                IsSelected = true,
                IsExpanded = true,
                TagPositionInChat = TagPosition.Before,
                TagPositionInNameplates = TagPosition.Replace,
                TagTargetInNameplates = NameplateElement.Title,
                IsTextItalic = true,

                IsVisibleInOverworld = true,
                IsVisibleInPveDuties = true,
                IsVisibleInPvpDuties = true,

                IsVisibleForSelf = true,
                IsVisibleForFriendPlayers = true,
                IsVisibleForPartyPlayers = true,
                IsVisibleForAlliancePlayers = true,
                IsVisibleForEnemyPlayers = true,
                IsVisibleForOtherPlayers = true,
            }.GetChanges();

            AllRoleTagsChanges = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = true,
                IsIconVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            }.GetChanges();

            RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();
            RoleTagsChanges[Role.LandHand] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.Crafter,
                TextColor = 3,
            }.GetChanges();

            RoleTagsChanges[Role.Tank] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.Tank,
                TextColor = 546,
            }.GetChanges();

            RoleTagsChanges[Role.Healer] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.Healer,
                TextColor = 43,
            }.GetChanges();

            RoleTagsChanges[Role.DPS] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.DPS,
                TextColor = 508,
            }.GetChanges();

            JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();
            foreach ((var role, var roleTagChanges) in RoleTagsChanges)
            {
                var classJobs = PluginServices.DataManager.GetExcelSheet<ClassJob>();
                if (classJobs == null)
                {
                    break;
                }

                foreach (var classJob in classJobs.Where(classJob => RolesById[classJob.Role] == role && !string.IsNullOrEmpty(classJob.Abbreviation.RawString)))
                {
                    RolesByJobAbbreviation[classJob.Abbreviation.RawString] = role;

                    if (!JobTagsChanges.ContainsKey(classJob.Abbreviation.RawString))
                    {
                        JobTagsChanges[classJob.Abbreviation.RawString] = new Tag(new LiteralPluginString(""))
                        {
                            IsSelected = false,
                            IsExpanded = false,
                            Text = classJob.Abbreviation.RawString,
                        }.GetChanges();
                    }
                }
            }

            AllCustomTagsChanges = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = true,
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            }.GetChanges();
        }
    }
}
