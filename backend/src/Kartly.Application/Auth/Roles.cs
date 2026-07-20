namespace Kartly.Application.Auth;

/// <summary>Application roles carried in the JWT and used for authorization.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static readonly IReadOnlyList<string> All = [Admin, Customer];
}