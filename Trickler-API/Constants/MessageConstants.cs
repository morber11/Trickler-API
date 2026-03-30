namespace Trickler_API.Constants
{
    public static class MessageConstants
    {
        public static class Common
        {
            public const string InternalServerError = "Internal server error";
        }

        public static class Account
        {
            public const string EmailAndPasswordRequired = "Email and password are required";
            public const string RegistrationFailed = "Registration failed";
            public const string UserRegisteredSuccessfully = "User registered successfully";
            public const string CannotChangePasswordForOidcUsers = "Cannot change password for OIDC authenticated users";
            public const string OldAndNewPasswordRequired = "Old password and new password are required";
            public const string FailedToChangePassword = "Failed to change password";
            public const string PasswordChangedSuccessfully = "Password changed successfully";
            public const string UserNotFound = "User not found";
        }

        public static class Auth
        {
            public const string InvalidCredentials = "Invalid credentials";
            public const string LoginSuccessful = "Login successful";
            public const string OidcLoginSuccessful = "OIDC login successful";
            public const string UserNotAuthenticated = "User not authenticated";
            public const string InvalidReturnUrl = "Invalid return URL";
            public const string LoggedOutSuccessfully = "Logged out successfully";
        }

        public static class Trickle
        {
            public const string TrickleNotFound = "Trickle with ID {0} not found";
            public const string TextIsRequired = "Text is required";
        }

        public static class Answers
        {
            public const string AttemptLimitReached = "Attempt limit reached for today";
        }

        public static class Validation
        {
            public const string TitleRequired = "Title is required";
            public const string TitleMaxLength = "Title cannot exceed 1000 characters";

            public const string TextRequired = "Text is required";
            public const string TextMaxLength = "Text cannot exceed 1000 characters";

            public const string AnswersCannotContainEmptyValues = "Answers cannot contain empty values";

            public const string AvailabilityTypeRequired = "Type is required";
            public const string InvalidAvailabilityType = "Invalid availability type";
            public const string InvalidAvailabilityTypeFormat = "Invalid availability type: {0}";

            public const string FromDateBeforeUntil = "From date must be before or equal to Until date";
            public const string DatesRequired = "Dates are required for SpecificDates availability type";
            public const string DaysOfWeekRequired = "Days of week are required for Weekly availability type";
        }
    }
}