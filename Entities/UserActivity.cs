namespace PracticeLLD.Entities
{
    public class UserActivity
    {
        public string id { get; set; }

        public string UserId { get; set; }

        public DateTimeOffset ActivityDate { get; set; }

        public string ActivityType { get; set; }

        public string Details { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public static class ActivityTypes
    {
        public const string Login = "Login";
    }
}
