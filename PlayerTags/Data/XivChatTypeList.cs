using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Data
{
    public class EnumList<TEnum> : List<TEnum> where TEnum : Enum
    {
        public EnumList() : base()
        {
        }

        public EnumList(IEnumerable<TEnum> collection) : base(collection)
        {
        }

        //// this is first one '=='
        //public static bool operator ==(EnumList<TEnum> obj1, EnumList<TEnum> obj2)
        //{
            
        //}

        //// this is second one '!='
        //public static bool operator !=(EnumList<TEnum> obj1, EnumList<TEnum> obj2)
        //{
        //    return !(obj1 == obj2);
        //}

        public override bool Equals(object? obj)
        {
            var obj1 = this;
            var obj2 = obj as EnumList<TEnum>;

            if (obj1 is not null && obj2 is not null)
            {
                if (obj1.Count != obj2.Count)
                    return false;

                for (int i = 0; i < obj1.Count; i++)
                {
                    if (!obj1[i]?.Equals(obj2[i]) ?? true)
                        return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
