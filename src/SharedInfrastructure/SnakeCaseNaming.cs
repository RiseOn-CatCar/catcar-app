using Microsoft.EntityFrameworkCore;

namespace CatCar.Persistence;

/// <summary>
/// Applies the shared snake_case naming convention to an EF Core model.
/// </summary>
public static class SnakeCaseNaming
{
    /// <summary>
    /// Converts table, column and foreign-key names to snake_case.
    /// </summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is not null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entityType.GetProperties())
            {
                if (entityType.IsOwned() && entityType.FindOwnership() is { } ownership && ownership.Properties.Contains(property))
                {
                    continue;
                }

                var columnName = property.GetColumnName();
                property.SetColumnName(ToSnakeCase(columnName ?? property.Name));
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                if (foreignKey.IsOwnership)
                {
                    continue;
                }

                foreach (var property in foreignKey.Properties)
                {
                    var columnName = property.GetColumnName();
                    property.SetColumnName(ToSnakeCase(columnName ?? property.Name));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (var i = 1; i < input.Length; i++)
        {
            var character = input[i];
            if (char.IsUpper(character))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(character));
            }
            else
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }
}
