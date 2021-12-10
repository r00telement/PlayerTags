using System;

namespace PlayerTags.Data
{
    public class InheritableCategoryAttribute : Attribute
    {
        public string CategoryId { get; private set; }

        public InheritableCategoryAttribute(string categoryId)
        {
            CategoryId = categoryId;
        }
    }
}
