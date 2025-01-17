using FluentMigrator;

namespace OpenSourceRecipes.Migrations;

[Migration(2024010103)]
public class CreateRecipeTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE TABLE \"Recipe\"" +
                    "(" +
                    "\"RecipeId\" SERIAL PRIMARY KEY," +
                    "\"RecipeTitle\" VARCHAR(255) NOT NULL," +
                    "\"TagLine\" VARCHAR(255) NOT NULL," +
                    "\"Difficulty\" INT NOT NULL," +
                    "\"TimeToPrepare\" INT NOT NULL," +
                    "\"RecipeMethod\" TEXT NOT NULL," +
                    "\"PostedOn\" DATE NOT NULL DEFAULT CURRENT_TIMESTAMP," +
                    "\"Cuisine\" TEXT NOT NULL," +
                    "\"RecipeImg\" VARCHAR(255) NOT NULL," +

                    "\"ForkedFromId\" INT DEFAULT NULL," +
                    "\"OriginalRecipeId\" INT DEFAULT NULL," +

                    "\"UserId\" INT REFERENCES \"User\" (\"UserId\") ON DELETE SET NULL," +
                    "\"CuisineId\" INT NOT NULL REFERENCES \"Cuisine\" (\"CuisineId\") ON DELETE CASCADE" +
                    ");");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE \"Recipe\"");
    }
}
