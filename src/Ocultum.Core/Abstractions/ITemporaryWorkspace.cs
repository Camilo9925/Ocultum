namespace Ocultum.Core.Abstractions;

/// <summary>Isolated, short-lived directories (one per operation) identified by random ids.</summary>
public interface ITemporaryWorkspace
{
    IWorkspaceDirectory Create();

    /// <summary>Returns null when the id is malformed, unknown or expired.</summary>
    IWorkspaceDirectory? Find(string id);

    void Remove(string id);

    int PurgeExpired();
}
