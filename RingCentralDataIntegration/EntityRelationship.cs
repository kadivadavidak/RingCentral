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
    
    public partial class EntityRelationship
    {
        public int EntityRelationshipID { get; set; }
        public int ParentEntityID { get; set; }
        public int ChildEntityID { get; set; }
        public int RelationshipDefinitionID { get; set; }
        public System.DateTime CreateDate { get; set; }
        public System.DateTime UpdateDate { get; set; }
    
        public virtual Entity Entity { get; set; }
        public virtual Entity Entity1 { get; set; }
        public virtual RelationshipDefinition RelationshipDefinition { get; set; }
    }
}
