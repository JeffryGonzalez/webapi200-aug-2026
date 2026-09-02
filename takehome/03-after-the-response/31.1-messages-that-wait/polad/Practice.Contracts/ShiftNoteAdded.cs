namespace Practice.Contracts;

/// <summary>
/// Chatter on the crew board. Nobody is harmed if one of these is lost.
/// </summary>
public record ShiftNoteAdded(string Crew, string Note);
