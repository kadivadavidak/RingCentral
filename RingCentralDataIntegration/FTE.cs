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
    
    public partial class FTE
    {
        public int FTEID { get; set; }
        public int StoreID { get; set; }
        public System.DateTime Date { get; set; }
        public double NumberFTE { get; set; }
    
        public virtual Store Store { get; set; }
    }
}
