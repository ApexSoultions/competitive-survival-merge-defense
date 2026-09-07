public interface IGlobalActiveCastHandler
{
    string AbilityId { get; }

    bool TryExecute(ActiveAbilityDefinition definition, GlobalActiveCastContext context);
}
