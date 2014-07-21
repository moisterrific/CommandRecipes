﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace CommandRecipes
{
    public class ILog
    {
        public ILog()
        {

        }

        private static string path = Path.Combine(CmdRec.configDir, "CraftLog.txt");
        public FileStream Stream { get; set; }
		public StreamReader Reader { get; protected set; }
        public StreamWriter Writer { get; protected set; }
        public Dictionary<string, List<Recipe>> CompletedRecipes { get; protected set; }

        #region Initialize
        public void Initialize()
        {
            Stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			Reader = new StreamReader(Stream);
            Writer = new StreamWriter(Stream);
            Task.Factory.StartNew(() => Load());
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
			Writer.Dispose();
        }
        #endregion

        #region Load
        /// <summary>
        /// Reloads the CompletedRecipes property
        /// </summary>
        public void Load()
        {
            try
            {
                CompletedRecipes = LoadRecipes();
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
            }
        }
        #endregion

        #region LogRecipe
        /// <summary>
        /// Logs a crafted recipe to the log file.
        /// </summary>
        public void Recipe(Recipe recipe, string player)
        {
			try
			{
				var ingredients = String.Join(",", Utils.ListIngredients(recipe.ingredients));
				var products = String.Join(",", Utils.ListIngredients(recipe.products));
				var str = String.Format("Player ({0}) crafted recipe ({1}), using ({2}) and obtaining ({3}).",
					player,
					recipe.name,
					ingredients,
					products);
				Writer.WriteLine(str);
				CompletedRecipes.AddToList(new KeyValuePair<string, Recipe>(player, recipe));

			}
			catch (Exception ex)
			{
				Log.ConsoleError(ex.ToString());
			}
        }
        #endregion

        #region LoadRecipes
        /// <summary>
        /// Returns the list of crafted recipes directly from the log.
        /// </summary>
        public Dictionary<string, List<Recipe>> LoadRecipes()
        {
            var dic = new Dictionary<string, List<Recipe>>();
            KeyValuePair<string,Recipe> pair;
            while (!Reader.EndOfStream)
            {
                pair = ParseLine(Reader.ReadLine());
                if (!dic.ContainsKey(pair.Key))
                {
                    dic.Add(pair.Key, new List<Recipe>());
                }
                dic[pair.Key].Add(pair.Value);
            }
			Reader.DiscardBufferedData();
            return dic;

        }
        #endregion

        #region GetRecipes
        /// <summary>
        /// Returns a list of Recipes crafted by player name
        /// </summary>
        public List<Recipe> GetRecipes(string player)
        {
            return CompletedRecipes[player];
        }
        #endregion

        #region Helper Methods
        #region ParseToString
        List<string> FormatItems(List<RecItem> items)
        {
            var list = new List<string>();
			string prefix;
            foreach (var item in items)
            {
				prefix = item.prefix > 0 ? String.Format("[{0}] ",
					Utils.GetPrefixById(item.prefix)) : "";
                list.Add(String.Format("{0} {1}{2}",
                    item.stack,
                    prefix,
                    item.name));
            }
            return list;
        }
        #endregion
        #region ParseItems
        List<RecItem> ParseItems(string items)
        {
            var list = new List<RecItem>();
            string name = String.Empty;
            string stack = String.Empty;
            string prefix = String.Empty;
            ReadMode pos = ReadMode.Stack;
            foreach (char ch in items)
            {
                switch (ch)
                {
                    case ',':
                        list.Add(new RecItem(
                            name.Trim(),
                            Int32.Parse(stack),
                            TShock.Utils.GetPrefixByName(prefix.Trim()).First()));
                        break;
                    case '[':
                        pos = ReadMode.Prefix;
                        break;
                    case ']':
                        pos = ReadMode.Name;
                        break;
                    default:
						if (Char.IsWhiteSpace(ch) && pos != ReadMode.Name)
						{
							pos = ReadMode.Name;
							break;
						}
                        switch (pos)
                        {
                            case ReadMode.Stack:
                                stack += ch;
                                break;
                            case ReadMode.Prefix:
                                prefix += ch;
                                break;
                            case ReadMode.Name:
                                name += ch;
                                break;
                        }
                        break;
                }
            }
            // Additional one for the last item
            list.Add(new RecItem(
                name.Trim(),
                Int32.Parse(stack.Trim()),
                TShock.Utils.GetPrefixByName(prefix.Trim()).First()));
            return list;
        }
        #endregion
        #region ParseLine
        KeyValuePair<string, Recipe> ParseLine(string line)
        {
            bool reading = false;
            int pos = 0;
            var reader = new[]
                {
                    String.Empty,
                    String.Empty,
                    String.Empty,
                    String.Empty
                };
            foreach (char ch in line)
            {
                switch (ch)
                {
                    case '(':
                        reading = true;
                        break;
                    case ')':
                        reading = false;
                        pos++;
                        break;
                    default:
                        if (reading)
                        {
                            reader[pos] += ch;
                        }
                        break;
                }
            }

            return new KeyValuePair<string, Recipe>(
                reader[0],
                new Recipe(reader[1], ParseItems(reader[2]), ParseItems(reader[3])));
        }
        #endregion
        #endregion

        enum ReadMode
        {
            Stack,
            Prefix,
            Name
        }
	}
}