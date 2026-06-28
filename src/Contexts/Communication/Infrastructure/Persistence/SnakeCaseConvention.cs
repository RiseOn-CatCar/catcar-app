using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CatCar.Contexts.Communication.Infrastructure.Persistence;

/// <summary>
/// Configures EF Core to use snake_case naming convention for tables and columns.
/// AC-013: Schema-per-BC naming convention
/// </summary>
public class SnakeCaseConvention : IModelCustomizer
{
    /// <summary>
    /// Customizes the model to use snake_case naming convention.
    /// AC-013
    /// </summary>
    public void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table name to snake_case
            var tableName = entityType.GetTableName();
            if (tableName != null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            // Convert column names to snake_case
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Convert foreign key column names to snake_case
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreach (var property in foreignKey.Properties)
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
