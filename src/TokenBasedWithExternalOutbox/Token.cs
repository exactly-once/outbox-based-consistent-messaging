using System;

public class Token
{
    public string Id { get; set; }
    public Guid? ClaimedBy { get; set; }
    public object VersionInfo { get; set; }
}