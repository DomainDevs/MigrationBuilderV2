namespace DataToolkit.Library.Fluent;

public interface IFluentQuery
{
    (string Sql, object Parameters) Build();

    IFluentQuery Select(params string[] columns);

    IFluentQuery From(params string[] tables);

    IFluentQuery InnerJoin(string table, string on);

    IFluentQuery LeftJoin(string table, string on);

    IFluentQuery RightJoin(string table, string on);

    IFluentQuery FullJoin(string table, string on);

    IFluentQuery Where(
        string sql,
        object? parameters = null);

    IFluentQuery And(
        string sql,
        object? parameters = null);

    IFluentQuery Or(
        string sql,
        object? parameters = null);

    IFluentQuery WhereIf(
        bool condition,
        string sql,
        object? parameters = null);

    IFluentQuery GroupBy(params string[] columns);

    IFluentQuery OrderBy(params string[] columns);

    IFluentQuery Skip(int rows);

    IFluentQuery Take(int rows);

    string ToSql();
}