using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace FDModels.Products
{
    [DataContract]
    [Table("dbo.Category")]
    public class FDCategory : IEquatable<FDCategory>
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public string URI { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is FDCategory)
            {
                FDCategory cat = (FDCategory)obj;
                if (cat.ID == this.ID)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        public bool Equals(FDCategory other)
        {
            if (other?.ID == this.ID)
            {
                return true;
            }
            return false;
        }
    }
}
