namespace backend.Constants;

public static class Permissions
{
    public const string ViewDashboard = "ViewDashboard";
    public const string ViewCamera = "ViewCamera";
    public const string ControlRover = "ControlRover";
    public const string ViewEvents = "ViewEvents";
    public const string UpdateEvents = "UpdateEvents";
    public const string EmergencyStop = "EmergencyStop";
    public const string ChangeOperatingMode = "ChangeOperatingMode";
    public const string AccessAdmin = "AccessAdmin";

    public static readonly string[] All =
    {
        ViewDashboard,
        ViewCamera,
        ControlRover,
        ViewEvents,
        UpdateEvents,
        EmergencyStop,
        ChangeOperatingMode,
        AccessAdmin
    };
}
