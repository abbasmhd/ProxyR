namespace ProxyR.Abstractions.Commands
{
    public enum DbObjectType
    {
        InlineTableValuedFunction = 1,
        ServiceQueue              = 2,
        ForeignKeyConstraint      = 3,
        UserTable                 = 4,
        DefaultConstraint         = 5,
        PrimaryKeyConstraint      = 6,
        SystemTable               = 7,
        InternalTable             = 8,
        TableValuedFunction       = 9,
        View                      = 10,
        NotSuported               = 9999999,
    }
}