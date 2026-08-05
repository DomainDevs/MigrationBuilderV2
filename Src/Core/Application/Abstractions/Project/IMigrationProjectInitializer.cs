namespace Infrastructure.ProjectSystem;

public interface IMigrationProjectInitializer
{
    public void CreateProject(string name);
    public void DeleteProject(string name);
    public void CreateArtefac(string ProjectName, string TableName);
    public void DeleteArtefac(string ProjectName, string TableName);
}