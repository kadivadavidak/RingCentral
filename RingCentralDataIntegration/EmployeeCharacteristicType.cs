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
    
    public partial class EmployeeCharacteristicType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EmployeeCharacteristicType()
        {
            this.EmployeeCharacteristics = new HashSet<EmployeeCharacteristic>();
        }
    
        public int EmployeeCharacteristicTypeID { get; set; }
        public string EmployeeCharacteristicType1 { get; set; }
        public string EmployeeCharacteristicClass { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EmployeeCharacteristic> EmployeeCharacteristics { get; set; }
    }
}
