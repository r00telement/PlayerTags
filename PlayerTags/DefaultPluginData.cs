using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags
{
    public class DefaultPluginData
    {
        public Dictionary<byte, Role> RolesById { get; } = new Dictionary<byte, Role>()
        {
            { 0, Role.LandHand },
            { 1, Role.Tank },
            { 2, Role.DPS },
            { 3, Role.DPS },
            { 4, Role.Healer },
        };

        public Dictionary<string, Role> RolesByJobAbbreviation { get; } = new Dictionary<string, Role>();

        public Dictionary<string, InheritableData> AllTagsChanges = new Dictionary<string, InheritableData>();
        public Dictionary<string, InheritableData> AllRoleTagsChanges = new Dictionary<string, InheritableData>();
        public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();
        public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();
        public Dictionary<string, InheritableData> AllCustomTagsChanges = new Dictionary<string, InheritableData>();

        public void Initialize(DataManager dataManager)
        {
            AllTagsChanges = new Tag(new LiteralPluginString(""))
            {
                TagPositionInChat = TagPosition.Before,
                TagPositionInNameplates = TagPosition.Replace,
                TagTargetInNameplates = NameplateElement.Title,
                IsTextItalic = true,
            }.GetChanges();

            AllRoleTagsChanges = new Tag(new LiteralPluginString(""))
            {
                IsIconVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            }.GetChanges();

            RoleTagsChanges[Role.LandHand] = new Tag(new LiteralPluginString(""))
            {
                Icon = BitmapFontIcon.Crafter,
                TextColor = 3,
            }.GetChanges();

            RoleTagsChanges[Role.Tank] = new Tag(new LiteralPluginString(""))
            {
                Icon = BitmapFontIcon.Tank,
                TextColor = 546,
            }.GetChanges();

            RoleTagsChanges[Role.Healer] = new Tag(new LiteralPluginString(""))
            {
                Icon = BitmapFontIcon.Healer,
                TextColor = 43,
            }.GetChanges();

            RoleTagsChanges[Role.DPS] = new Tag(new LiteralPluginString(""))
            {
                Icon = BitmapFontIcon.DPS,
                TextColor = 508,
            }.GetChanges();

            foreach ((var role, var roleTagChanges) in RoleTagsChanges)
            {
                var classJobs = dataManager.GetExcelSheet<ClassJob>();
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
                            Text = classJob.Abbreviation.RawString,
                        }.GetChanges();
                    }
                }
            }

            AllCustomTagsChanges = new Tag(new LiteralPluginString(""))
            {
                IsTextVisibleInChat = true,
                IsTextVisibleInNameplates = true,
            }.GetChanges();
        }
    }
}
