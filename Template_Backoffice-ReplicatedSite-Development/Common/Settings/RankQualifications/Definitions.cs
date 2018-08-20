using System.Collections.Generic;

namespace ExigoService
{
    public static partial class Exigo
    {
        private static readonly IEnumerable<IRankRequirementDefinition> RankQualificationDefinitions = new List<IRankRequirementDefinition>
        {
            Boolean("Customer Type", 
                Expression: @"^MUST BE A VALID CUSTOMER TYPE",
                Description: "You must be a Distributor."                
            ),

            Boolean("Distributor",
                Expression: @"^MUST BE A VALID DISTRIBUTOR TYPE",
                Description: "You must be a Distributor."
            ),

            Boolean("Customer Status",  
                Expression: @"^CUSTOMER STATUS IS ACTIVE",
                Description: "Your account must be in good standing."                
            ),

            Boolean("Active", 
                Expression: @"^ACTIVE$", 
                Description: "You must be considered Active."                
            ),

            Boolean("Qualified", 
                Expression: @"^MUST BE QUALIFIED$",
                Description: "You must be qualified to receive commissions."
            ),

            Boolean("Enroller Tree", 
                Expression: @"^DISTRIBUTOR MUST BE IN ENROLLER TREE$",
                Description: "You must have a current position in the enroller tree."
            ),

            Decimal("GBV",
                Expression: @"IS GBV > \d+$",
                Description: "You need at least {{RequiredValueAsDecimal:N0}} GBV."
            ),

            Decimal("Lesser Leg Volume", 
                Expression: @"^\d+ LESSER LEG VOLUME$",
                Description: "You need at least {{RequiredValueAsDecimal:N0}} volume in your lesser leg."
            ),

            Decimal("C500 Legs in Enroller Tree", 
                Expression: @"^\d+ C500 LEGS ENROLLER TREE$",
                Description: "You must personally enroll at least {{RequiredValueAsDecimal:N0}} C500 distributor(s)."
            ),

            Decimal("PV", 
                Expression: @"^\d+ PV$",
                Description: "You need at least {{RequiredValueAsDecimal:N0}} PV."
            ),

            Decimal("PV Last Period", 
                Expression: @"^\d+ PV 1 PERIOD",
                Description: "You needed at least {{RequiredValueAsDecimal:N0}} PV last period."
            ),

            Decimal("PV 2 Periods Ago", 
                Expression: @"^\d+ PV 2 PERIOD",
                Description: "You needed at least {{RequiredValueAsDecimal:N0}} PV two periods ago."
            ),

            Decimal("PV 3 Periods Ago", 
                Expression: @"^\d+ PV 3 PERIOD",
                Description: "You needed at least {{RequiredValueAsDecimal:N0}} PV three periods ago."
            ),

            Decimal("Capped Enrollment GPV at 50% per leg", 
                Expression: @"^\d+ CAPPED ENROLLMENT GROUP PV AT 50% PER LEG$",
                Description: "You need at least {{RequiredValueAsDecimal:N0}} capped enrollment GPV at 50% per leg this period."
            ),

            Decimal("Capped Enrollment GPV at 50% per leg last period", 
                Expression: @"^\d+ CAPPED ENROLLMENT GROUP PV AT 50% PER LEG 1 PERIOD",
                Description: "You needed at least {{RequiredValueAsDecimal:N0}} capped enrollment GPV at 50% per leg last period."
            ),

            Decimal("Capped Enrollment GPV at 50% per leg two periods ago", 
                Expression: @"^\d+ CAPPED ENROLLMENT GROUP PV AT 50% PER LEG 2 PERIOD",
                Description: "You needed at least {{RequiredValueAsDecimal:N0}} capped enrollment GPV at 50% per leg two periods ago."
            ),

            Decimal("Capped Enrollment GPV at 50% per leg three periods ago", 
                Expression: @"^\d+ CAPPED ENROLLMENT GROUP PV AT 50% PER LEG 3 PERIOD",
                Description: "You needed at least {{RequiredValueAsDecimal:N0}} capped enrollment GPV at 50% per leg three periods ago."
            )
        };
    }
}