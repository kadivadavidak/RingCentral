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
    
    public partial class GLAccountDetail
    {
        public int GLAccountDetailID { get; set; }
        public Nullable<int> CostCenterID { get; set; }
        public int GLAccountTypeID { get; set; }
        public System.DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int TBAccountID { get; set; }
    
        public virtual CostCenter CostCenter { get; set; }
        public virtual GLAccountType GLAccountType { get; set; }
        public virtual TBAccount TBAccount { get; set; }
    }
}