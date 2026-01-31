namespace HMSCore.Areas.Admin.Models
{

    public class SessionModel
    {
        public string Name { get; set; } = string.Empty;   // Morning/Afternoon/Evening
        public TimeSpan? Start { get; set; }               // Start time of session
        public TimeSpan? End { get; set; }                 // End time of session
    }

}
