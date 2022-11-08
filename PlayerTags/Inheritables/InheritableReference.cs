using Newtonsoft.Json;

namespace PlayerTags.Inheritables
{
    public class InheritableReference<T> : IInheritable
        where T : class
    {
        public IInheritable? Parent { get; set; }

        public InheritableBehavior Behavior { get; set; }

        [JsonProperty]
        public T Value;

        [JsonIgnore]
        public T? InheritedValue
        {
            get
            {
                IInheritable? current = this;
                while (current != null)
                {
                    if (current.Behavior == InheritableBehavior.Enabled && current is InheritableReference<T> currentOfSameType)
                    {
                        return currentOfSameType.Value;
                    }
                    else if (current.Behavior == InheritableBehavior.Disabled)
                    {
                        return default;
                    }

                    current = current.Parent;
                }

                return default;
            }
        }

        public static implicit operator InheritableReference<T>(T value) => new InheritableReference<T>(value)
        {
            Behavior = InheritableBehavior.Enabled
        };

        public InheritableReference(T value)
        {
            Value = value;
        }

        public void SetData(InheritableData inheritableData)
        {
            Behavior = inheritableData.Behavior;
            Value = (T)inheritableData.Value;
        }

        public InheritableData GetData()
        {
            return new InheritableData
            {
                Behavior = Behavior,
                Value = Value
            };
        }
    }
}
