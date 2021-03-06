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
    
    public partial class CustomerTimeline
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CustomerTimeline()
        {
            this.Invoices = new HashSet<Invoice>();
        }
    
        public int CustomerTimelineID { get; set; }
        public Nullable<int> CustomerID { get; set; }
        public Nullable<int> CustomerContactID { get; set; }
        public Nullable<int> CustomerAddressID { get; set; }
        public System.DateTime TimelineDate { get; set; }
        public int TimelineOrder { get; set; }
    
        public virtual CustomerAddress CustomerAddress { get; set; }
        public virtual CustomerContact CustomerContact { get; set; }
        public virtual Customer Customer { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Invoice> Invoices { get; set; }
    }
}
