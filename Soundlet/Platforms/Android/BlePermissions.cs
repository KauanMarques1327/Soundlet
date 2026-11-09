namespace Soundlet.Platforms.Android;

public sealed class BlePermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(31)
            ? [("android.permission.BLUETOOTH_SCAN", true), ("android.permission.BLUETOOTH_CONNECT", true)]
            : OperatingSystem.IsAndroidVersionAtLeast(23)
                ? [("android.permission.ACCESS_FINE_LOCATION", true)]
                : [];
}
