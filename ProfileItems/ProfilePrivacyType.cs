namespace FlexRouter.ProfileItems
{
    public enum ProfileItemPrivacyType
    {
        Private,
        Public,
    }

    public abstract class ProfileItemPrivacy
    {
        private ProfileItemPrivacyType _profileItemPrivacyType = ApplicationSettings.DisablePersonalProfile ? ProfileItemPrivacyType.Public : ProfileItemPrivacyType.Private;
        public void SetPrivacyType(ProfileItemPrivacyType profileItemPrivacyType)
        {
            _profileItemPrivacyType = profileItemPrivacyType;
        }

        public ProfileItemPrivacyType GetPrivacyType()
        {
            return _profileItemPrivacyType;
        }
    }
}
