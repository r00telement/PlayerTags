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
        public Tag AllTags { get; private set; }

        public Tag AllRoleTags { get; private set; }
        public Dictionary<Role, Tag> RoleTags { get; private set; }
        public Dictionary<DpsRole, Tag> DpsRoleTags { get; private set; }
        public Dictionary<RangedDpsRole, Tag> RangedDpsRoleTags { get; private set; }
        public Dictionary<LandHandRole, Tag> LandHandRoleTags { get; private set; }
        public Dictionary<string, Tag> JobTags { get; private set; }

        public Tag AllCustomTags { get; private set; }

        public DefaultPluginData(DefaultPluginDataTemplate template)
        {
            SetupTemplate(template);
        }

        private void SetupTemplate(DefaultPluginDataTemplate template)
        {
            Clear();

            switch(template)
            {
                case DefaultPluginDataTemplate.None:
                    SetupTemplateNone();
                    break;
                case DefaultPluginDataTemplate.Basic:
                    SetupTemplateBasic();
                    break;
                case DefaultPluginDataTemplate.Simple:
                    SetupTemplateSimple();
                    break;
                case DefaultPluginDataTemplate.Full:
                    SetupTemplateFull();
                    break;
            }

            SetupJobTags();
        }

        private void Clear()
        {

            RoleTags = new Dictionary<Role, Tag>();
            DpsRoleTags = new Dictionary<DpsRole, Tag>();
            RangedDpsRoleTags = new Dictionary<RangedDpsRole, Tag>();
            LandHandRoleTags = new Dictionary<LandHandRole, Tag>();
        }

        private void SetupTemplateNone()
        {
            AllTags = new Tag()
            {
                IsSelected = true,
                IsExpanded = true,
            };

            AllRoleTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
            };

            RoleTags[Role.LandHand] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            RoleTags[Role.Tank] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            RoleTags[Role.Healer] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false,
            };

            RoleTags[Role.Dps] = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
            };

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

            AllCustomTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
            };
        }

        private void SetupTemplateBasic()
        {
            AllTags = new Tag()
            {
                IsSelected = true,
                IsExpanded = true,

                TagPositionInChat = TagPosition.Before,
                InsertBehindNumberPrefixInChat = true,
                TagPositionInNameplates = TagPosition.Replace,
                TagTargetInNameplates = NameplateElement.Title,
                
                TargetChatTypes = new List<XivChatType>(Enum.GetValues<XivChatType>()),
                TargetChatTypesIncludeUndefined = true,
            };

            AllRoleTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
            };

            RoleTags[Role.LandHand] = new Tag()
            {
                IsSelected = false,
                IsExpanded = false
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

            AllCustomTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            };
        }

        private void SetupTemplateSimple()
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

                IsVisibleForSelf = true,
                IsVisibleForFriendPlayers = true,
                IsVisibleForPartyPlayers = true,
                IsVisibleForAlliancePlayers = true,
                IsVisibleForEnemyPlayers = true,
                IsVisibleForOtherPlayers = true,

                TargetChatTypes = new List<XivChatType>(Enum.GetValues<XivChatType>()),
                TargetChatTypesIncludeUndefined = true,
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

            AllCustomTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            };
        }

        private void SetupTemplateFull()
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

                IsVisibleForSelf = true,
                IsVisibleForFriendPlayers = true,
                IsVisibleForPartyPlayers = true,
                IsVisibleForAlliancePlayers = true,
                IsVisibleForEnemyPlayers = true,
                IsVisibleForOtherPlayers = true,

                TargetChatTypes = new List<XivChatType>(Enum.GetValues<XivChatType>()),
                TargetChatTypesIncludeUndefined = true,
            };

            AllRoleTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                IsRoleIconVisibleInChat = true,
                IsTextVisibleInChat = true,
                IsRoleIconVisibleInNameplates = true,
                IsTextVisibleInNameplates = true,
                IsTextColorAppliedToNameplateName = true,
                IsTextColorAppliedToChatName = true,
                IsJobIconVisibleInNameplates = true,
            };

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

            AllCustomTags = new Tag()
            {
                IsSelected = false,
                IsExpanded = true,
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            };
        }

        private void SetupJobTags()
        {
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
        }
    }
}
