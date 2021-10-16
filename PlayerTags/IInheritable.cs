namespace PlayerTags
{
    public interface IInheritable
    {
        public IInheritable? Parent { get; set; }

        public InheritableBehavior Behavior { get; set; }

        public abstract void SetData(InheritableData inheritableData);

        public abstract InheritableData GetData();
    }
}
