using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Pilz.Dalamud.Icons;
using PlayerTags.Inheritables;
using PlayerTags.PluginStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayerTags.Data
{
    public class Tag
    {
        public IPluginString Name { get; init; }

        [JsonProperty("Parent")]
        private Tag? m_Parent = null;

        [JsonIgnore]
        public Tag? Parent
        {
            get => m_Parent;
            set
            {
                if (m_Parent != value)
                {
                    if (m_Parent != null)
                    {
                        if (m_Parent.Children.Contains(this))
                        {
                            m_Parent.Children.Remove(this);
                        }
                    }

                    m_Parent = value;
                    if (m_Parent != null)
                    {
                        m_Parent.Children.Add(this);
                        foreach ((var name, IInheritable inheritable) in Inheritables)
                        {
                            inheritable.Parent = m_Parent.Inheritables[name];
                        }
                    }
                }
            }
        }

        public List<Tag> Children { get; } = new List<Tag>();

        [JsonIgnore]
        public IEnumerable<Tag> Descendents
        {
            get
            {
                IEnumerable<Tag> descendents = Children.Prepend(this);

                foreach (var child in Children)
                {
                    descendents = descendents.Union(child.Descendents);
                }

                return descendents.Distinct();
            }
        }

        [JsonIgnore]
        private Dictionary<string, IInheritable>? m_Inheritables = null;
        [JsonIgnore]
        public Dictionary<string, IInheritable> Inheritables
        {
            get
            {
                if (m_Inheritables == null)
                {
                    m_Inheritables = new Dictionary<string, IInheritable>();

                    var inheritableFields = GetType().GetFields().Where(field => typeof(IInheritable).IsAssignableFrom(field.FieldType));
                    foreach (var inheritableField in inheritableFields)
                    {
                        IInheritable? inheritable = inheritableField.GetValue(this) as IInheritable;
                        if (inheritable != null)
                        {
                            Inheritables[inheritableField.Name] = inheritable;
                        }
                    }
                }

                return m_Inheritables!;
            }
        }

        public InheritableValue<bool> IsSelected = new InheritableValue<bool>(false)
        {
            Behavior = InheritableBehavior.Enabled
        };

        public InheritableValue<bool> IsExpanded = new InheritableValue<bool>(false)
        {
            Behavior = InheritableBehavior.Enabled
        };

        // Deprecated
        public InheritableReference<string> GameObjectNamesToApplyTo = new InheritableReference<string>("");

        public InheritableValue<Guid> CustomId = new InheritableValue<Guid>(Guid.Empty);

        [JsonProperty, Obsolete]
        private InheritableValue<bool> IsIconVisibleInChat
        {
            set => IsRoleIconVisibleInChat = value;
        }

        [JsonProperty, Obsolete]
        private InheritableValue<bool> IsIconVisibleInNameplate
        {
            set => IsRoleIconVisibleInNameplates = value;
        }

        [InheritableCategory("IconCategory")]
        public InheritableValue<BitmapFontIcon> Icon = new InheritableValue<BitmapFontIcon>(BitmapFontIcon.Aethernet);
        [InheritableCategory("IconCategory")]
        public InheritableValue<bool> IsRoleIconVisibleInChat = new InheritableValue<bool>(false);
        [InheritableCategory("IconCategory")]
        public InheritableValue<bool> IsRoleIconVisibleInNameplates = new InheritableValue<bool>(false);
        [InheritableCategory("IconCategory")]
        public InheritableValue<bool> IsJobIconVisibleInNameplates = new InheritableValue<bool>(false);
        [InheritableCategory("IconCategory")]
        public InheritableValue<JobIconSetName> JobIconSet = new InheritableValue<JobIconSetName>(JobIconSetName.Framed);

        [InheritableCategory("TextCategory")]
        public InheritableReference<string> Text = new InheritableReference<string>("");
        [InheritableCategory("TextCategory")]
        public InheritableValue<ushort> TextColor = new InheritableValue<ushort>(6);
        [InheritableCategory("TextCategory")]
        public InheritableValue<ushort> TextGlowColor = new InheritableValue<ushort>(6);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextItalic = new InheritableValue<bool>(false);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextVisibleInChat = new InheritableValue<bool>(false);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextVisibleInNameplates = new InheritableValue<bool>(false);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextColorAppliedToChatName = new InheritableValue<bool>(false);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextColorAppliedToNameplateName = new InheritableValue<bool>(false);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextColorAppliedToNameplateTitle = new InheritableValue<bool>(false);
        [InheritableCategory("TextCategory")]
        public InheritableValue<bool> IsTextColorAppliedToNameplateFreeCompany = new InheritableValue<bool>(false);

        //[InheritableCategory("NameplateCategory")]
        //public InheritableValue<NameplateFreeCompanyVisibility> NameplateFreeCompanyVisibility = new InheritableValue<NameplateFreeCompanyVisibility>(Data.NameplateFreeCompanyVisibility.Default);
        //[InheritableCategory("NameplateCategory")]
        //public InheritableValue<NameplateTitleVisibility> NameplateTitleVisibility = new InheritableValue<NameplateTitleVisibility>(Data.NameplateTitleVisibility.Default);
        //[InheritableCategory("NameplateCategory")]
        //public InheritableValue<NameplateTitlePosition> NameplateTitlePosition = new InheritableValue<NameplateTitlePosition>(Data.NameplateTitlePosition.Default);

        [InheritableCategory("PositionCategory")]
        public InheritableValue<TagPosition> TagPositionInChat = new InheritableValue<TagPosition>(TagPosition.Before);
        [InheritableCategory("PositionCategory")]
        public InheritableValue<bool> InsertBehindNumberPrefixInChat = new InheritableValue<bool>(true);
        [InheritableCategory("PositionCategory")]
        public InheritableValue<TagPosition> TagPositionInNameplates = new InheritableValue<TagPosition>(TagPosition.Before);
        [InheritableCategory("PositionCategory")]
        public InheritableValue<NameplateElement> TagTargetInNameplates = new InheritableValue<NameplateElement>(NameplateElement.Name);

        [InheritableCategory("ActivityCategory")]
        public InheritableValue<bool> IsVisibleInPveDuties = new InheritableValue<bool>(false);
        [InheritableCategory("ActivityCategory")]
        public InheritableValue<bool> IsVisibleInPvpDuties = new InheritableValue<bool>(false);
        [InheritableCategory("ActivityCategory")]
        public InheritableValue<bool> IsVisibleInOverworld = new InheritableValue<bool>(false);

        [InheritableCategory("PlayerCategory")]
        public InheritableValue<bool> IsVisibleForSelf = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerCategory")]
        public InheritableValue<bool> IsVisibleForFriendPlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerCategory")]
        public InheritableValue<bool> IsVisibleForPartyPlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerCategory")]
        public InheritableValue<bool> IsVisibleForAlliancePlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerCategory")]
        public InheritableValue<bool> IsVisibleForEnemyPlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerCategory")]
        public InheritableValue<bool> IsVisibleForOtherPlayers = new InheritableValue<bool>(false);

        [InheritableCategory("ChatFeatureCategory")]
        public InheritableReference<List<XivChatType>> TargetChatTypes = new(new List<XivChatType>(Enum.GetValues<XivChatType>()));

        [JsonIgnore]
        public string[] IdentitiesToAddTo
        {
            get
            {
                if (GameObjectNamesToApplyTo == null || GameObjectNamesToApplyTo.InheritedValue == null)
                {
                    return new string[] { };
                }

                return GameObjectNamesToApplyTo.InheritedValue.Split(';', ',').Where(item => !string.IsNullOrEmpty(item)).Select(item => item.Trim()).ToArray();
            }
        }

        private Tag? m_Defaults;
        [JsonIgnore]
        public bool HasDefaults
        {
            get { return m_Defaults != null; }
        }

        public Tag()
        {
            Name = new LiteralPluginString("");
            m_Defaults = null;
        }

        public Tag(IPluginString name)
        {
            Name = name;
            m_Defaults = null;
        }

        public Tag(IPluginString name, Tag defaults)
        {
            Name = name;
            m_Defaults = defaults;
            SetChanges(defaults.GetChanges());
        }

        public Dictionary<string, InheritableData> GetChanges(Dictionary<string, InheritableData>? defaultChanges = null)
        {
            Dictionary<string, InheritableData> changes = new Dictionary<string, InheritableData>();

            foreach ((var name, var inheritable) in Inheritables)
            {
                // If there's a default for this name, only set the value if it's different from the default
                if (defaultChanges != null && defaultChanges.TryGetValue(name, out var defaultInheritableData))
                {
                    var inheritableData = inheritable.GetData();
                    if (inheritableData.Behavior != defaultInheritableData.Behavior ||
                        !EqualsInheritableData(inheritableData, defaultInheritableData))
                    {
                        changes[name] = inheritable.GetData();
                    }
                }
                // If there's no default, then only set the value if it's not inherited
                else if (inheritable.Behavior != InheritableBehavior.Inherit)
                {
                    changes[name] = inheritable.GetData();
                }
            }

            return changes;
        }

        private static bool EqualsInheritableData(InheritableData data1, InheritableData data2)
        {
            if (data1.Value is List<XivChatType>)
                return EqualsInheritableDataListXivChatType<XivChatType>(data1, data2);
            else
                return data1.Value.Equals(data2.Value);
        }

        private static bool EqualsInheritableDataListXivChatType<TEnum>(InheritableData data1, InheritableData data2)
        {
            var list1 = data1.Value as List<TEnum>;
            var list2 = data2.Value as List<TEnum>;

            if (list1 is null || list2 is null || list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!list1[i].Equals(list2[i]))
                    return false;
            }

            return true;
        }

        public void SetChanges(IEnumerable<KeyValuePair<string, InheritableData>> changes)
        {
            foreach ((var name, var inheritableData) in changes)
            {
                Inheritables[name].SetData(inheritableData);
            }
        }

        private Dictionary<string, InheritableData> GetAllAsChanges()
        {
            Dictionary<string, InheritableData> changes = new Dictionary<string, InheritableData>();

            foreach ((var name, var inheritable) in Inheritables)
            {
                changes[name] = inheritable.GetData();
            }

            return changes;
        }

        public void SetDefaults()
        {
            if (m_Defaults != null)
            {
                // Exclude IsSelected and IsExpanded for UX purposes
                SetChanges(m_Defaults.GetAllAsChanges().Where(change => change.Key != nameof(IsSelected) && change.Key != nameof(IsExpanded)));
            }
        }
    }
}
