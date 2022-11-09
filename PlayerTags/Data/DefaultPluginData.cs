using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Data
{
    public class DefaultPluginData
    {
        public Tag AllTags { get; }

        public Tag AllRoleTags { get; }
        public Dictionary<Role, Tag> RoleTags { get; }
        public Dictionary<DpsRole, Tag> DpsRoleTags { get; }
        public Dictionary<RangedDpsRole, Tag> RangedDpsRoleTags { get; }
        public Dictionary<LandHandRole, Tag> LandHandRoleTags { get; }
        public Dictionary<string, Tag> JobTags { get; }

        public Tag AllCustomTags { get; }

        public DefaultPluginData()
        {
            AllTags = new Tag()
            {
                IsSelected = true,
                IsExpanded = true,
                TagPositionInChat = TagPosition.Before,
                InsertBehindNumberPrefixInChat = true,
                TagPositionInNameplates = TagPosition.Replace,
                TagTargetInNameplates = NameplateElement.Title,
                IsTextItalic = true,

                IsVisibleInOverworld = true,
                IsVisibleInPveDuties = true,
                IsVisibleInPvpDuties = true,

                //NameplateFreeCompanyVisibility = NameplateFreeCompanyVisibility.Never,
                //NameplateTitleVisibility = NameplateTitleVisibility.Always,
                //NameplateTitlePosition = NameplateTitlePosition.AlwaysAboveName,

                IsVisibleForSelf = true,
                IsVisibleForFriendPlayers = true,
                IsVisibleForPartyPlayers = true,
                IsVisibleForAlliancePlayers = true,
                IsVisibleForEnemyPlayers = true,
                IsVisibleForOtherPlayers = true,

                TargetChatTypes = new List<XivChatType>(Enum.GetValues<XivChatType>()),
            };

            AllRoleTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                IsRoleIconVisibleInChat = true,
                IsTextVisibleInChat = true,
                IsRoleIconVisibleInNameplates = true,
                IsTextVisibleInNameplates = true,
                IsTextColorAppliedToChatName = true,
            };

            RoleTags = new Dictionary<Role, Tag>();
            RoleTags[Role.LandHand] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.Crafter,
                TextColor = 3,
            };

            RoleTags[Role.Tank] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.Tank,
                TextColor = 546,
            };

            RoleTags[Role.Healer] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
                Icon = BitmapFontIcon.Healer,
                TextColor = 43,
            };

            RoleTags[Role.Dps] = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                Icon = BitmapFontIcon.DPS,
                TextColor = 508,
            };

            DpsRoleTags = new Dictionary<DpsRole, Tag>();
            DpsRoleTags[DpsRole.Melee] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            DpsRoleTags[DpsRole.Ranged] = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
            };

            RangedDpsRoleTags = new Dictionary<RangedDpsRole, Tag>();
            RangedDpsRoleTags[RangedDpsRole.Magical] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            RangedDpsRoleTags[RangedDpsRole.Physical] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            LandHandRoleTags = new Dictionary<LandHandRole, Tag>();
            LandHandRoleTags[LandHandRole.Land] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            LandHandRoleTags[LandHandRole.Hand] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            JobTags = new Dictionary<string, Tag>();

            var classJobs = PluginServices.DataManager.GetExcelSheet<ClassJob>();
            if (classJobs != null)
            {
                foreach ((var role, var roleTagChanges) in RoleTags)
                {
                    foreach (var classJob in classJobs.Where(classJob => RoleHelper.RolesByRoleId[classJob.Role] == role && !string.IsNullOrEmpty(classJob.Abbreviation.RawString)))
                    {
                        if (!JobTags.ContainsKey(classJob.Abbreviation.RawString))
                        {
                            JobTags[classJob.Abbreviation.RawString] = new Tag()
                            {
                                IsSelected = false,
                                IsExpanded = false,
                                Text = classJob.Abbreviation.RawString,
                            };
                        }
                    }
                }
            }

            AllCustomTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            };
        }
    }
}
