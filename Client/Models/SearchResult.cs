﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class SearchResult
    {
        public List<SearchNode> SearchNodes { get; set; }
        public string Host { get; set; }

        public string Word { get; set; }

        public string Device { get; set; } = "pc";

        public string Rank
        {
            get
            {
                if (SearchNodes != null)
                {
                    var model = SearchNodes.FirstOrDefault(m => m.Url.Host.Contains(Host.Replace("www.", "")));

                    if (model != null)
                    {
                        return model.Rank;
                    }
                }
                return $"{(SearchNodes != null ? SearchNodes.Count : 100)}+";
            }
        }

        public override string ToString()
        {
            if (SearchNodes != null)
            {
                var model = SearchNodes.FirstOrDefault(m => m.Url.Host.Contains(Host.Replace("www.", "")));

                if (model != null)
                {
                    return $"{Host}\t{Word}\t{Device}\t{model.Rank}";
                }
            }
            var rank = "0";
            if (SearchNodes == null)
            {
                rank = "0";
            }
            else if (SearchNodes.Any())
            {
                rank = SearchNodes.Last().Rank;
            }
            else
            {
                rank = SearchNodes.Count.ToString();
            }

            return $"{Host}\t{Word}\t{Device}\t{(SearchNodes != null ? rank : "100")}+";
            //return base.ToString();
        }

    }
}
