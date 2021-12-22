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
        public Dictionary<string, InheritableData> AllTagsChanges { get; }

        public Dictionary<string, InheritableData> AllRoleTagsChanges { get; }
        public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges { get; }
        public Dictionary<DpsRole, Dictionary<string, InheritableData>> DpsRoleTagsChanges { get; }
        public Dictionary<RangedDpsRole, Dictionary<string, InheritableData>> RangedDpsRoleTagsChanges { get; }
        public Dictionary<LandHandRole, Dictionary<string, InheritableData>> LandHandRoleTagsChanges { get; }
        public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges { get; }

        public Dictionary<string, InheritableData> AllCustomTagsChanges { get; }

        public DefaultPluginData()
        {
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
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
                IsTextColorAppliedToChatName = true
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

            RoleTagsChanges[Role.Dps] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = true,
                Icon = BitmapFontIcon.DPS,
                TextColor = 508,
            }.GetChanges();

            DpsRoleTagsChanges = new Dictionary<DpsRole, Dictionary<string, InheritableData>>();
            DpsRoleTagsChanges[DpsRole.Melee] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
            }.GetChanges();

            DpsRoleTagsChanges[DpsRole.Ranged] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
            }.GetChanges();

            RangedDpsRoleTagsChanges = new Dictionary<RangedDpsRole, Dictionary<string, InheritableData>>();
            RangedDpsRoleTagsChanges[RangedDpsRole.Magical] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
            }.GetChanges();

            RangedDpsRoleTagsChanges[RangedDpsRole.Physical] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
            }.GetChanges();

            LandHandRoleTagsChanges = new Dictionary<LandHandRole, Dictionary<string, InheritableData>>();
            LandHandRoleTagsChanges[LandHandRole.Land] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
            }.GetChanges();

            LandHandRoleTagsChanges[LandHandRole.Hand] = new Tag(new LiteralPluginString(""))
            {
                IsSelected = false,
                IsExpanded = false,
            }.GetChanges();

            JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();

            var classJobs = PluginServices.DataManager.GetExcelSheet<ClassJob>();
            if (classJobs != null)
            {
                foreach ((var role, var roleTagChanges) in RoleTagsChanges)
                {
                    foreach (var classJob in classJobs.Where(classJob => RoleHelper.RolesByRoleId[classJob.Role] == role && !string.IsNullOrEmpty(classJob.Abbreviation.RawString)))
                    {
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
