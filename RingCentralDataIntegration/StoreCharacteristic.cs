//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RingCentralDataIntegration
{
    using System;
    using System.Collections.Generic;
    
    public partial class StoreCharacteristic
    {
        public int StoreCharacteristicID { get; set; }
        public int LocationID { get; set; }
        public int StoreID { get; set; }
        public Nullable<System.DateTime> StartDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public int StoreCharacteristicTypeID { get; set; }
        public System.DateTime UpdatedDate { get; set; }
    
        public virtual Location Location { get; set; }
        public virtual Store Store { get; set; }
        public virtual StoreCharacteristicType StoreCharacteristicType { get; set; }
    }
}
