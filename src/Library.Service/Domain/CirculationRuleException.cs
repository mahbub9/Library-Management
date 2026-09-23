namespace Library.Service.Domain;

public enum CirculationRule
{
    PatronNotFound,
    BookNotFound,
    LoanNotFound,
    AlreadyCheckedOut,
    AlreadyReturned
}

public sealed class CirculationRuleException(CirculationRule rule, string message)
    : Exception(message)
{
    public CirculationRule Rule { get; } = rule;
}
