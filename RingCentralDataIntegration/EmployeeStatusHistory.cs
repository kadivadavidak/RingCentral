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
    
    public partial class EmployeeStatusHistory
    {
        public int EmployeeStatusHistoryID { get; set; }
        public int EmployeeID { get; set; }
        public int EmployeeStatusID { get; set; }
        public int StatusOrder { get; set; }
        public System.DateTime StatusDate { get; set; }
    
        public virtual Employee Employee { get; set; }
        public virtual EmployeeStatu EmployeeStatu { get; set; }
    }
}