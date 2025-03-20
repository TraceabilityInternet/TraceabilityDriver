using DSUtil.Interfaces;
using FDModels;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    [DataContract]
    public class FDCategoryProfile
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public long UserID { get; set; }

        [DataMember]
        public FDCategory Category { get; set; }

        [DataMember]
        public double Price { get; set; }

        [DataMember]
        public string  Currency { get; set; }

        [DataMember]
        public List<FDCategoryProfileClaim> Claims { get; set; } = new List<FDCategoryProfileClaim>();

        public FDCategoryProfile()
        {

        }

        public FDCategoryProfile(IDSRecordSet rec, FDCategory category, List<FDClaim> claims)
        {
            this.ID = rec.Field("ID").Int64Value;
            this.Price = rec.Field("Price").dblValue;
            this.Currency = rec.Field("Currency").StrValue;
            this.Category = category;
            foreach (FDClaim claim in claims)
            {
                if (rec.Fields.Exists(f => f.Name == claim.Column))
                {
                    this.Claims.Add(new FDCategoryProfileClaim()
                    {
                        Claim = claim,
                        IsTicked = rec.Field(claim.Column).boolValue
                    });
                }
            }
        }
    }

    [DataContract]
    public class FDCategoryProfileClaim : BaseModel
    {
        private bool _isTicked;

        [DataMember]
        public FDClaim Claim { get; set; }

        [DataMember]
        public bool IsTicked
        {
            get
            {
                return _isTicked;
            }
            set
            {
                if (value != _isTicked)
                {
                    _isTicked = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
