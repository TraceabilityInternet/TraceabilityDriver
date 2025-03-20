using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FDModels.Products
{
    public class FDProductIngredient
    {
        public string URI { get; set; }
        public string Name { get; set; }
        public int Ranking { get; set; }
        public double Percentage { get; set; }

        public List<FDProductIngredient> Ingredients { get; set; } = new List<FDProductIngredient>();

        public override string ToString()
        {
            return this.Name ?? "";
        }

        public static List<FDProductIngredient> Parse(string ingredientsStr)
        {
            try
            {
                List<FDProductIngredient> ingredients = new List<FDProductIngredient>();
                Stack<FDProductIngredient> parents = new Stack<FDProductIngredient>();
                List<char> chars = new List<char>() { ',', '[', ']', '(', ')', '{', '}' };
                List<char> closingChars = new List<char>() { ']', '}', ')' };
                List<char> openningChars = new List<char>() { '[', '{', '(' };
                string currentName = "";
                for (int i = 0; i < ingredientsStr.Length; i++)
                {
                    char c = ingredientsStr[i];
                    if (chars.Contains(c))
                    {
                        if (!string.IsNullOrWhiteSpace(currentName))
                        {
                            currentName = currentName.Trim();
                            FDProductIngredient ti = new FDProductIngredient()
                            {
                                Name = currentName.ToLower().Replace(".", "").Replace(";", "").Trim()
                            };
                            if (parents.Count > 0)
                            {
                                parents.First().Ingredients.Add(ti);
                            }
                            else
                            {
                                ingredients.Add(ti);
                            }
                            currentName = "";
                        }
                        if (openningChars.Contains(c))
                        {
                            if (parents.Count > 0)
                            {
                                if (parents.First().Ingredients.LastOrDefault() != null)
                                {
                                    parents.Push(parents.First().Ingredients.Last());
                                }
                            }
                            else
                            {
                                parents.Push(ingredients.Last());
                            }
                        }
                        else if (closingChars.Contains(c))
                        {
                            if (parents.Count > 0)
                            {
                                parents.Pop();
                            }
                        }
                    }
                    else
                    {
                        currentName += c;
                    }
                }
                if (!string.IsNullOrWhiteSpace(currentName))
                {
                    currentName = currentName.Trim();
                    FDProductIngredient ti = new FDProductIngredient()
                    {
                        Name = currentName.ToLower().Replace(".", "").Replace(";", "").Trim()
                    };
                    if (parents.Count > 0)
                    {
                        parents.First().Ingredients.Add(ti);
                    }
                    else
                    {
                        ingredients.Add(ti);
                    }
                    currentName = "";
                }
                return ingredients;
            }
            catch (Exception Ex)
            {
                throw new Exception($"Failed to parse ingredients from string.\n{ingredientsStr}\n", Ex);
            }
        }

        public static string ToString(List<FDProductIngredient> ingredients)
        {
            string ingredientStr = "";
            foreach (FDProductIngredient ingredient in ingredients)
            {
                ingredientStr += ingredient.Name;
                if (ingredient.Ingredients.Count > 0)
                {
                    ingredientStr += " (" + ToString(ingredient.Ingredients) + ")";
                }
                ingredientStr += ",";
            }
            return ingredientStr.TrimEnd(',');
        }
    }
}
