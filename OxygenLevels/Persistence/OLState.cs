namespace OxygenLevels.Persistence
{
    internal class OLState
    {
        public int Version = 2;

        public float CriticalAcclimatizationHours;
        public float InsufficientAcclimatizationHours;

        public bool CriticalAcclimatized;
        public bool InsufficientAcclimatized;

        public float UnpreparedInsufficientO2Hours;
        public float AMSInsufficientO2Hours;

        public bool HasLastKnownOutdoorAltitude;
        public float LastKnownOutdoorAltitude;

        public bool HasDysenteryRestForCureOverride;
        public float DysenteryRestForCureHours;

        public bool HasFoodPoisoningRestForCureOverride;
        public float FoodPoisoningRestForCureHours;
    }
}